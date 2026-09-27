using System;
using System.Globalization;
using extOSC;
using UnityEngine;
using UnityEngine.Events;

namespace QExE.Localization
{
    public enum LocalizationState
    {
        /// <summary>Waiting for the participant to press Start.</summary>
        WaitingForStart,
        /// <summary>Start was sent to Max, waiting for the first trial.</summary>
        Starting,
        /// <summary>Max is waiting for the participant to face the front (alignment check).</summary>
        Aligning,
        /// <summary>Aligned was sent to Max, waiting for the stimulus to start.</summary>
        AlignedWaiting,
        /// <summary>A stimulus is playing, responses are not accepted yet.</summary>
        Listening,
        /// <summary>The response window is open; the pointer is armed.</summary>
        Responding,
        /// <summary>Response sent, waiting for the next trial.</summary>
        Submitted,
        /// <summary>All trials of this block are done.</summary>
        Complete
    }

    /// <summary>
    /// Runs the Unity side of a localization block. Max decides the trial order and plays the
    /// stimuli; this component only reacts to Max's trial messages, arms the pointer when the
    /// response window opens, and sends the pointing response back.
    ///
    /// All OSC traffic for the localization method goes through this class
    /// (see <see cref="QExELocalizationProtocol"/>).
    /// </summary>
    public class LocalizationTrialController : MonoBehaviour
    {
        [Tooltip("OSC receiver of the scene. Found automatically on the 'OSCManager' object if empty.")]
        public OSCReceiver receiver;

        [Tooltip("OSC transmitter of the scene. Found automatically on the 'OSCManager' object if empty.")]
        public OSCTransmitter transmitter;

        public LocalizationPointer pointer;

        [Tooltip("Optional top-down dial for setting a preliminary azimuth (also while the sound plays).")]
        public LocalizationDial dial;

        [Tooltip("Optional horizontal ring around the head (spatial reference + preliminary marker).")]
        public LocalizationHeadRing headRing;

        [Tooltip("Optional head orientation indicator / alignment check. Without it, alignment is reported immediately.")]
        public LocalizationAlignment alignment;

        [Tooltip("The trigger only marks a candidate direction (pointer or dial); the answer is submitted by pressing the dial centre. Off = the pointer trigger submits directly.")]
        public bool requireCentreConfirm = true;

        [Tooltip("Only used when requireCentreConfirm is off: allow submitting the dial azimuth by pressing the dial centre.")]
        public bool allowDialSubmit = true;

        [Tooltip("Training feedback: colour of the correct direction in the scene.")]
        public Color targetColor = new Color(0.25f, 0.85f, 1f, 1f);

        [Tooltip("Colour of the marked candidate direction in the scene.")]
        public Color candidateColor = new Color(1f, 0.75f, 0.2f, 1f);
        public float candidateDotSize = 0.06f;

        [Tooltip("Show the head ring outside of trials too (e.g. while waiting for Start).")]
        public bool showRingOutsideTrials = true;

        [Tooltip("Show the pointer only while a response is expected (otherwise also while listening).")]
        public bool hidePointerWhileListening = false;

        [Header("Events")]
        public UnityEvent<int, int> onTrialStarted = new UnityEvent<int, int>();
        public UnityEvent<int> onResponseWindowOpened = new UnityEvent<int>();
        public UnityEvent<int> onResponseSubmitted = new UnityEvent<int>();
        public UnityEvent<int> onBlockComplete = new UnityEvent<int>();

        public LocalizationState State { get; private set; } = LocalizationState.WaitingForStart;
        public int CurrentTrial { get; private set; } = -1;
        public int TrialCount { get; private set; }
        public string CurrentStimulusId { get; private set; } = "";

        /// <summary>True during a training block (feedback after each response).</summary>
        public bool IsTraining { get; private set; }

        /// <summary>Training: azimuth error of the last response, degrees (null when no feedback is shown).</summary>
        public float? FeedbackError { get; private set; }

        private PointingSample? _lastSubmitted;
        private Transform _targetDot;

        /// <summary>Azimuth set on the dial during the current trial (reference frame), if any.</summary>
        public float? PreliminaryAzimuth { get; private set; }
        private float _preliminaryTimeMs = -1f;

        // Candidate answer (requireCentreConfirm): last direction marked with the pointer or the dial.
        private PointingSample? _candidate;
        private string _candidateSource = "";
        private float _candidateTimeMs = -1f;
        private Transform _candidateDot;

        // Head orientation at stimulus onset and the largest deviation while the stimulus plays.
        private float _onsetYaw, _onsetPitch, _maxAbsYaw, _maxAbsPitch;
        private bool _trackingDeviation;

        /// <summary>Short text for the participant / operator describing the current state.</summary>
        public string StatusText
        {
            get
            {
                switch (State)
                {
                    case LocalizationState.WaitingForStart: return "Press Start to begin.";
                    case LocalizationState.Starting: return "Starting... close the menu and get ready.";
                    case LocalizationState.Aligning: return $"Trial {CurrentTrial + 1} / {TrialCount}: face the front target and hold still.";
                    case LocalizationState.AlignedWaiting: return $"Trial {CurrentTrial + 1} / {TrialCount}: listen.";
                    case LocalizationState.Listening: return $"Trial {CurrentTrial + 1} / {TrialCount}: listen. You can mark the direction on the dial.";
                    case LocalizationState.Responding:
                        if (requireCentreConfirm)
                            return _candidate.HasValue
                                ? $"Trial {CurrentTrial + 1} / {TrialCount}: direction marked. Press the dial centre to confirm, or mark again."
                                : $"Trial {CurrentTrial + 1} / {TrialCount}: point to the sound and press the trigger to mark it, then press the dial centre to confirm.";
                        return PreliminaryAzimuth.HasValue && allowDialSubmit
                            ? $"Trial {CurrentTrial + 1} / {TrialCount}: point to the sound and press the trigger, or press the dial centre to keep the dial answer."
                            : $"Trial {CurrentTrial + 1} / {TrialCount}: point to the sound and press the trigger.";
                    case LocalizationState.Submitted: return $"Trial {CurrentTrial + 1} / {TrialCount}: answer recorded.";
                    case LocalizationState.Complete: return "Block complete. Open the menu and press Next.";
                    default: return "";
                }
            }
        }

        private OSCBind _readyBind;
        private OSCBind _modeBind;
        private OSCBind _feedbackBind;
        private OSCBind _trialBind;
        private OSCBind _respondBind;
        private OSCBind _completeBind;
        private double _trialStartTime;

        private void OnEnable()
        {
            ResolveReferences();

            if (receiver != null)
            {
                _readyBind = receiver.Bind(QExELocalizationProtocol.Ready, OnReady);
                _modeBind = receiver.Bind(QExELocalizationProtocol.Mode, OnMode);
                _feedbackBind = receiver.Bind(QExELocalizationProtocol.Feedback, OnFeedback);
                _trialBind = receiver.Bind(QExELocalizationProtocol.Trial, OnTrial);
                _respondBind = receiver.Bind(QExELocalizationProtocol.Respond, OnRespond);
                _completeBind = receiver.Bind(QExELocalizationProtocol.Complete, OnComplete);
            }
            else
            {
                Debug.LogError("QExE Localization: no OSCReceiver found. Is the 'OSCManager' prefab in the scene?", this);
            }

            if (pointer != null)
            {
                if (pointer.dial == null) pointer.dial = dial;
                pointer.Confirmed += OnPointerConfirmed;
                pointer.DialPressed += OnDialPressed;
                pointer.SetArmed(false);
                pointer.SetVisualsVisible(!hidePointerWhileListening);
            }
            else
            {
                Debug.LogError("QExE Localization: no LocalizationPointer assigned.", this);
            }

            ClearPreliminary();
            UpdateAidVisibility();
        }

        private void OnDisable()
        {
            if (receiver != null)
            {
                if (_readyBind != null) receiver.Unbind(_readyBind);
                if (_modeBind != null) receiver.Unbind(_modeBind);
                if (_feedbackBind != null) receiver.Unbind(_feedbackBind);
                if (_trialBind != null) receiver.Unbind(_trialBind);
                if (_respondBind != null) receiver.Unbind(_respondBind);
                if (_completeBind != null) receiver.Unbind(_completeBind);
            }
            if (pointer != null)
            {
                pointer.Confirmed -= OnPointerConfirmed;
                pointer.DialPressed -= OnDialPressed;
            }
        }

        private void ResolveReferences()
        {
            if (receiver == null || transmitter == null)
            {
                var manager = GameObject.Find("OSCManager");
                if (manager != null)
                {
                    if (receiver == null) receiver = manager.GetComponent<OSCReceiver>();
                    if (transmitter == null) transmitter = manager.GetComponent<OSCTransmitter>();
                }
            }
            if (receiver == null) receiver = FindFirstObjectByType<OSCReceiver>();
            if (transmitter == null) transmitter = FindFirstObjectByType<OSCTransmitter>();
            if (pointer == null) pointer = GetComponent<LocalizationPointer>();
            if (dial == null) dial = GetComponent<LocalizationDial>();
            if (headRing == null) headRing = GetComponent<LocalizationHeadRing>();
            if (alignment == null) alignment = GetComponent<LocalizationAlignment>();
        }

        private void Update()
        {
            if (State == LocalizationState.Aligning && (alignment == null || alignment.IsAligned))
            {
                GetHeadYawPitch(out float yaw, out float pitch);
                var message = new OSCMessage(QExELocalizationProtocol.Aligned);
                message.AddValue(OSCValue.Int(CurrentTrial));
                message.AddValue(OSCValue.Float(yaw));
                message.AddValue(OSCValue.Float(pitch));
                Send(message);
                State = LocalizationState.AlignedWaiting;
                UpdateAidVisibility();
            }

            if (_trackingDeviation)
            {
                GetHeadYawPitch(out float yaw, out float pitch);
                _maxAbsYaw = Mathf.Max(_maxAbsYaw, Mathf.Abs(yaw));
                _maxAbsPitch = Mathf.Max(_maxAbsPitch, Mathf.Abs(pitch));
            }

            if (dial != null)
            {
                dial.SetCounter(TrialCount > 0 && CurrentTrial >= 0
                    ? $"{(IsTraining ? "Training" : "Trial")} {CurrentTrial + 1} / {TrialCount}"
                    : (IsTraining ? "Training" : ""));
                dial.SetStatus(ShortStatus);
            }
        }

        /// <summary>Short instruction shown above the dial.</summary>
        public string ShortStatus
        {
            get
            {
                switch (State)
                {
                    case LocalizationState.WaitingForStart: return "Open the menu and press Start";
                    case LocalizationState.Starting: return "Get ready";
                    case LocalizationState.Aligning: return "Face the front target";
                    case LocalizationState.AlignedWaiting:
                    case LocalizationState.Listening: return "Listen";
                    case LocalizationState.Responding:
                        return requireCentreConfirm && _candidate.HasValue ? "Confirm in the centre" : "Mark the direction";
                    case LocalizationState.Submitted:
                        return FeedbackError.HasValue ? $"Correct direction in blue (off by {FeedbackError.Value:F0}°)" : "Recorded";
                    case LocalizationState.Complete: return "Done: press Next in the menu";
                    default: return "";
                }
            }
        }

        private void GetHeadYawPitch(out float yaw, out float pitch)
        {
            if (alignment != null)
            {
                yaw = alignment.Yaw;
                pitch = alignment.Pitch;
                return;
            }
            yaw = pitch = 0f;
            if (pointer != null && pointer.head != null)
                LocalizationVisuals.YawPitch(pointer.head, pointer.Frame, out yaw, out pitch);
        }

        // ------------------------------------------------------------------------------------------
        // Participant actions
        // ------------------------------------------------------------------------------------------

        /// <summary>Called by the method UI when the participant presses Start.</summary>
        public void RequestStart()
        {
            if (State != LocalizationState.WaitingForStart)
                return;

            var space = LocalizationSpace.Find(this);
            if (space != null && space.calibrateOnStart)
                space.CalibrateToHead();

            State = LocalizationState.Starting;
            UpdateAidVisibility();
            Send(new OSCMessage(QExELocalizationProtocol.Start));
        }

        private void OnPointerConfirmed(PointingSample sample)
        {
            if (State != LocalizationState.Responding)
                return;

            if (requireCentreConfirm)
                SetCandidate(sample, "pointer");
            else
                Submit(sample, "pointer");
        }

        /// <summary>Trigger pressed while the ray is on the dial.</summary>
        private void OnDialPressed(float azimuth, bool inCentre)
        {
            bool inTrial = State == LocalizationState.Listening || State == LocalizationState.Responding;
            if (!inTrial)
                return;

            if (inCentre)
            {
                if (State != LocalizationState.Responding)
                    return;

                if (requireCentreConfirm)
                {
                    // Centre = confirm whatever was marked last (pointer or dial).
                    if (_candidate.HasValue)
                        Submit(_candidate.Value, _candidateSource, _candidateTimeMs);
                }
                else if (allowDialSubmit && PreliminaryAzimuth.HasValue && pointer != null)
                {
                    var sample = pointer.SampleFromDirection(pointer.DirectionFromAzEl(PreliminaryAzimuth.Value, 0f));
                    Submit(sample, "dial", ElapsedMs());
                }
                return;
            }

            PreliminaryAzimuth = azimuth;
            _preliminaryTimeMs = ElapsedMs();
            if (requireCentreConfirm && pointer != null)
            {
                SetCandidate(pointer.SampleFromDirection(pointer.DirectionFromAzEl(azimuth, 0f)), "dial");
            }
            else
            {
                if (dial != null) dial.SetPreliminary(azimuth);
                if (headRing != null) headRing.SetMarker(azimuth);
                UpdateDialSubmitHint();
            }
        }

        private void SetCandidate(PointingSample sample, string source)
        {
            _candidate = sample;
            _candidateSource = source;
            _candidateTimeMs = ElapsedMs();

            if (dial != null) dial.SetPreliminary(sample.Azimuth);
            if (headRing != null) headRing.SetMarker(sample.Azimuth);

            if (_candidateDot == null)
                _candidateDot = LocalizationVisuals.CreateDot(transform, "CandidateMarker", candidateDotSize, candidateColor);
            _candidateDot.position = sample.WorldPoint;
            _candidateDot.gameObject.SetActive(true);

            UpdateDialSubmitHint();
        }

        private float ElapsedMs()
        {
            return (float)((Time.realtimeSinceStartupAsDouble - _trialStartTime) * 1000.0);
        }

        private void Submit(PointingSample sample, string source)
        {
            Submit(sample, source, ElapsedMs());
        }

        private void Submit(PointingSample sample, string source, float markTimeMs)
        {
            if (State != LocalizationState.Responding)
                return;

            float rtMs = ElapsedMs();

            var message = new OSCMessage(QExELocalizationProtocol.Response);
            message.AddValue(OSCValue.Int(CurrentTrial));
            message.AddValue(OSCValue.Float(sample.Azimuth));
            message.AddValue(OSCValue.Float(sample.Elevation));
            message.AddValue(OSCValue.Float(sample.AzimuthHead));
            message.AddValue(OSCValue.Float(sample.ElevationHead));
            message.AddValue(OSCValue.Float(sample.HeadYaw));
            message.AddValue(OSCValue.Float(sample.HeadPitch));
            message.AddValue(OSCValue.Float(rtMs));
            message.AddValue(OSCValue.String(DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)));
            message.AddValue(OSCValue.String(source));
            message.AddValue(OSCValue.Int(PreliminaryAzimuth.HasValue ? 1 : 0));
            message.AddValue(OSCValue.Float(PreliminaryAzimuth ?? 0f));
            message.AddValue(OSCValue.Float(_preliminaryTimeMs));
            message.AddValue(OSCValue.Float(markTimeMs));
            message.AddValue(OSCValue.Float(_onsetYaw));
            message.AddValue(OSCValue.Float(_onsetPitch));
            message.AddValue(OSCValue.Float(_maxAbsYaw));
            message.AddValue(OSCValue.Float(_maxAbsPitch));
            Send(message);

            _lastSubmitted = sample;
            State = LocalizationState.Submitted;
            if (pointer != null)
            {
                pointer.SetArmed(false);
                if (hidePointerWhileListening) pointer.SetVisualsVisible(false);
            }
            UpdateDialSubmitHint();
            onResponseSubmitted.Invoke(CurrentTrial);

            Debug.Log($"<color=cyan><b>QExE:loc:</b></color> trial {CurrentTrial} ({source}): az {sample.Azimuth:F1}, el {sample.Elevation:F1}, rt {rtMs:F0} ms" +
                      (PreliminaryAzimuth.HasValue ? $", dial {PreliminaryAzimuth.Value:F1}" : ""));
        }

        private void ClearPreliminary()
        {
            ClearFeedback();
            PreliminaryAzimuth = null;
            _preliminaryTimeMs = -1f;
            _candidate = null;
            _candidateSource = "";
            _candidateTimeMs = -1f;
            if (_candidateDot != null) _candidateDot.gameObject.SetActive(false);
            if (dial != null) dial.SetPreliminary(null);
            if (headRing != null) headRing.SetMarker(null);
            UpdateDialSubmitHint();
        }

        private void UpdateDialSubmitHint()
        {
            if (dial == null) return;
            bool ready = State == LocalizationState.Responding &&
                         (requireCentreConfirm ? _candidate.HasValue : allowDialSubmit && PreliminaryAzimuth.HasValue);
            dial.SetSubmitReady(ready);
        }

        /// <summary>Dial only during trials; ring during trials (and optionally before/after).</summary>
        private void UpdateAidVisibility()
        {
            bool inBlock = State == LocalizationState.Starting || State == LocalizationState.Aligning ||
                           State == LocalizationState.AlignedWaiting || State == LocalizationState.Listening ||
                           State == LocalizationState.Responding || State == LocalizationState.Submitted;
            bool facingFront = State == LocalizationState.Starting || State == LocalizationState.Aligning ||
                               State == LocalizationState.AlignedWaiting || State == LocalizationState.Listening;
            if (dial != null) dial.SetVisible(inBlock);
            if (headRing != null) headRing.SetVisible(inBlock || showRingOutsideTrials);
            if (alignment != null) alignment.SetVisible(facingFront);
        }

        // ------------------------------------------------------------------------------------------
        // Messages from Max
        // ------------------------------------------------------------------------------------------

        private void OnMode(OSCMessage message)
        {
            IsTraining = message.Values.Count > 0 && message.Values[0].Type == OSCValueType.String &&
                         message.Values[0].StringValue == "training";
            if (TryGetInt(message, 1, out int count)) TrialCount = count;
            Debug.Log($"<color=cyan><b>QExE:loc:</b></color> block mode: {(IsTraining ? "training" : "test")}, {TrialCount} trials");
        }

        /// <summary>Training: show the correct direction next to the participant's answer.</summary>
        private void OnFeedback(OSCMessage message)
        {
            if (!TryGetInt(message, 0, out int trial) || trial != CurrentTrial) return;
            if (!TryGetFloat(message, 1, out float targetAz)) return;
            TryGetFloat(message, 2, out float targetEl);

            if (dial != null) dial.SetTarget(targetAz);
            if (headRing != null) headRing.SetTargetMarker(targetAz);

            if (pointer != null && pointer.head != null)
            {
                if (_targetDot == null)
                    _targetDot = LocalizationVisuals.CreateDot(transform, "TargetMarker", candidateDotSize, targetColor);
                _targetDot.position = pointer.SphereCentre + pointer.DirectionFromAzEl(targetAz, targetEl) * pointer.sphereRadius;
                _targetDot.gameObject.SetActive(true);
            }

            FeedbackError = _lastSubmitted.HasValue ? Mathf.Abs(Mathf.DeltaAngle(_lastSubmitted.Value.Azimuth, targetAz)) : (float?)null;
        }

        private void ClearFeedback()
        {
            FeedbackError = null;
            if (dial != null) dial.SetTarget(null);
            if (headRing != null) headRing.SetTargetMarker(null);
            if (_targetDot != null) _targetDot.gameObject.SetActive(false);
        }

        private void OnReady(OSCMessage message)
        {
            if (!TryGetInt(message, 0, out int trial) || !TryGetInt(message, 1, out int count))
            {
                Debug.LogWarning("QExE Localization: malformed ready message " + message);
                return;
            }

            CurrentTrial = trial;
            TrialCount = count;
            float tolerance = TryGetFloat(message, 2, out float tol) ? tol : -1f;
            float hold = TryGetInt(message, 3, out int holdMs) ? holdMs : -1f;
            CurrentStimulusId = message.Values.Count > 4 && message.Values[4].Type == OSCValueType.String ? message.Values[4].StringValue : "";

            ClearPreliminary();
            _trackingDeviation = false;
            if (alignment != null) alignment.Configure(tolerance, hold);
            if (pointer != null) pointer.SetArmed(false);

            State = LocalizationState.Aligning;
            UpdateAidVisibility();
        }

        private void OnTrial(OSCMessage message)
        {
            if (!TryGetInt(message, 0, out int trial) || !TryGetInt(message, 1, out int count))
            {
                Debug.LogWarning("QExE Localization: malformed trial message " + message);
                return;
            }

            CurrentTrial = trial;
            TrialCount = count;
            CurrentStimulusId = message.Values.Count > 2 && message.Values[2].Type == OSCValueType.String
                ? message.Values[2].StringValue
                : "";
            _trialStartTime = Time.realtimeSinceStartupAsDouble;

            GetHeadYawPitch(out _onsetYaw, out _onsetPitch);
            _maxAbsYaw = Mathf.Abs(_onsetYaw);
            _maxAbsPitch = Mathf.Abs(_onsetPitch);
            _trackingDeviation = true;

            State = LocalizationState.Listening;
            ClearPreliminary();
            UpdateAidVisibility();
            if (pointer != null)
            {
                pointer.SetArmed(false);
                pointer.SetVisualsVisible(!hidePointerWhileListening);
            }
            onTrialStarted.Invoke(trial, count);
        }

        private void OnRespond(OSCMessage message)
        {
            if (!TryGetInt(message, 0, out int trial) || trial != CurrentTrial)
                return;

            _trackingDeviation = false;
            State = LocalizationState.Responding;
            UpdateDialSubmitHint();
            UpdateAidVisibility();
            if (pointer != null)
            {
                pointer.SetVisualsVisible(true);
                pointer.SetArmed(true);
            }
            onResponseWindowOpened.Invoke(trial);
        }

        private void OnComplete(OSCMessage message)
        {
            TryGetInt(message, 0, out int count);
            _trackingDeviation = false;
            State = LocalizationState.Complete;
            ClearPreliminary();
            UpdateAidVisibility();
            if (pointer != null)
            {
                pointer.SetArmed(false);
                pointer.SetVisualsVisible(false);
            }
            onBlockComplete.Invoke(count);
        }

        // ------------------------------------------------------------------------------------------

        private void Send(OSCMessage message)
        {
            if (transmitter == null)
            {
                Debug.LogError("QExE Localization: no OSCTransmitter, cannot send " + message.Address, this);
                return;
            }
            transmitter.Send(message);
        }

        private static bool TryGetFloat(OSCMessage message, int index, out float value)
        {
            value = 0f;
            if (message.Values.Count <= index)
                return false;

            var v = message.Values[index];
            switch (v.Type)
            {
                case OSCValueType.Float: value = v.FloatValue; return true;
                case OSCValueType.Double: value = (float)v.DoubleValue; return true;
                case OSCValueType.Int: value = v.IntValue; return true;
                case OSCValueType.Long: value = v.LongValue; return true;
                default: return false;
            }
        }

        private static bool TryGetInt(OSCMessage message, int index, out int value)
        {
            value = 0;
            if (message.Values.Count <= index)
                return false;

            var v = message.Values[index];
            switch (v.Type)
            {
                case OSCValueType.Int: value = v.IntValue; return true;
                case OSCValueType.Long: value = (int)v.LongValue; return true;
                case OSCValueType.Float: value = Mathf.RoundToInt(v.FloatValue); return true;
                case OSCValueType.Double: value = (int)Math.Round(v.DoubleValue); return true;
                default: return false;
            }
        }
    }
}
