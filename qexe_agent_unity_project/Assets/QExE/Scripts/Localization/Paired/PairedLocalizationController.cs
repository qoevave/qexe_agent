using System;
using System.Globalization;
using extOSC;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace QExE.Localization
{
    public enum PairedLocalizationState
    {
        /// <summary>Waiting for the participant to press Start.</summary>
        WaitingForStart,
        /// <summary>Start was sent to Max, waiting for the first pair.</summary>
        Starting,
        /// <summary>Within a pair, nothing playing: the participant can press A/B and (once heard) answer.</summary>
        PairIdle,
        /// <summary>A or B was pressed, waiting for Max to answer.</summary>
        Requesting,
        /// <summary>Waiting for the participant to face the front before playback.</summary>
        Aligning,
        /// <summary>Aligned was sent, waiting for the onset.</summary>
        AlignedWaiting,
        /// <summary>A or B is playing.</summary>
        Playing,
        /// <summary>An answer was sent, waiting for Max to accept it.</summary>
        Submitted,
        /// <summary>The pair is complete, waiting for the next one.</summary>
        BetweenPairs,
        /// <summary>All pairs of this block are done.</summary>
        Complete
    }

    /// <summary>
    /// Unity side of paired (A/B) localization. Max (method_PairedLocalization.js) owns the order and
    /// the pair state; this component shows the A/B buttons, requests playbacks, runs the alignment
    /// check, collects the answers (dial or pointer, confirmed in the dial centre) and shows the locked
    /// A answer as a grey reference while B is answered.
    ///
    /// Reuses the pointer, dial, head ring, alignment indicator and listening position of the single
    /// localization method; all OSC traffic uses <see cref="QExEPairedLocalizationProtocol"/>.
    /// </summary>
    public class PairedLocalizationController : MonoBehaviour
    {
        [Tooltip("OSC receiver of the scene. Found automatically on the 'OSCManager' object if empty.")]
        public OSCReceiver receiver;
        [Tooltip("OSC transmitter of the scene. Found automatically on the 'OSCManager' object if empty.")]
        public OSCTransmitter transmitter;

        public LocalizationPointer pointer;
        public LocalizationDial dial;
        public LocalizationHeadRing headRing;
        public LocalizationAlignment alignment;
        public LocalizationABButtons buttons;

        [Header("Look")]
        [Tooltip("Colour of the marked candidate direction in the scene.")]
        public Color candidateColor = new Color(1f, 0.75f, 0.2f, 1f);
        public float candidateDotSize = 0.06f;
        [Tooltip("Show the head ring outside of blocks too (e.g. while waiting for Start).")]
        public bool showRingOutsideTrials = true;

        [Header("While listening")]
        [Tooltip("Hide the dial, buttons, pointer and markers from a button press until the sound has finished.")]
        public bool hideUIWhileListening = true;
        [Tooltip("Keep the head ring visible while the rest of the UI is hidden.")]
        public bool keepRingWhileListening = false;
        [Tooltip("Keep the front alignment target visible while the sound plays.")]
        public bool showAlignmentWhileListening = true;

        [Header("A/B switching while answering B")]
        [Tooltip("Keep the A/B buttons (and the pointer ray) visible while a comparison playback runs, so the participant can switch by pointing. The dial stays hidden.")]
        public bool showButtonsWhileComparing = true;
        [Tooltip("Toggles between A and B during a comparison playback. Default: B/Y (secondary button) on either controller, Tab on the keyboard. " +
                 "(A/X opens the menu.)")]
        public InputActionProperty switchAction = new InputActionProperty(CreateDefaultSwitchAction());

        [Header("Events")]
        public UnityEvent<int, int> onPairStarted = new UnityEvent<int, int>();
        public UnityEvent<int> onBlockComplete = new UnityEvent<int>();

        public PairedLocalizationState State { get; private set; } = PairedLocalizationState.WaitingForStart;
        public int CurrentTrial { get; private set; } = -1;
        public int PairCount { get; private set; }
        public string CurrentItemId { get; private set; } = "";
        public bool IsTraining { get; private set; }

        // Pair state from Max.
        private bool _aEnabled, _bEnabled, _answerOpen;
        private string _audibleSide = "";          // during a playback
        private string _answerSide = "A";
        private double _answerOpenSince = -1;      // when the current side could first be answered

        // Answers.
        private PointingSample? _candidate;
        private string _candidateSource = "";
        private double _candidateTime = -1;
        private PointingSample? _submitted;
        private PointingSample? _lockedA;
        private Transform _candidateDot;

        // Head during a playback.
        private float _onsetYaw, _onsetPitch, _maxAbsYaw, _maxAbsPitch;
        private bool _trackingDeviation;

        private OSCBind _modeBind, _pairBind, _stateBind, _readyBind, _playingBind, _playedBind, _answeredBind, _completeBind, _abortBind, _selectedBind;

        private static InputAction CreateDefaultSwitchAction()
        {
            var action = new InputAction("QExE Paired Localization Switch A/B", InputActionType.Button);
            action.AddBinding("<XRController>{RightHand}/secondaryButton");
            action.AddBinding("<XRController>{LeftHand}/secondaryButton");
            action.AddBinding("<Keyboard>/tab"); // desktop testing
            return action;
        }

        /// <summary>A comparison playback is running: A is answered, A and B can be switched.</summary>
        public bool IsComparing => State == PairedLocalizationState.Playing && _answerSide == "B";

        // ------------------------------------------------------------------------------------------
        // Status texts
        // ------------------------------------------------------------------------------------------

        /// <summary>Short instruction shown above the dial.</summary>
        public string ShortStatus
        {
            get
            {
                switch (State)
                {
                    case PairedLocalizationState.WaitingForStart: return "Open the menu and press Start";
                    case PairedLocalizationState.Starting: return "Get ready";
                    case PairedLocalizationState.Requesting:
                    case PairedLocalizationState.Aligning: return "Face the front target";
                    case PairedLocalizationState.AlignedWaiting: return "Listen";
                    case PairedLocalizationState.Playing:
                        return _answerSide == "B" ? $"Listen ({_audibleSide}): switch between A and B at will" : "Listen";
                    case PairedLocalizationState.PairIdle:
                        if (!_answerOpen)
                            return _answerSide == "A" ? "Press A to listen" : "Press B to listen (A to compare)";
                        return _candidate.HasValue
                            ? $"Confirm {_answerSide} in the centre"
                            : $"Mark the direction of {_answerSide}";
                    case PairedLocalizationState.Submitted: return "Recorded";
                    case PairedLocalizationState.BetweenPairs: return "Next pair";
                    case PairedLocalizationState.Complete: return "Done: press Next in the menu";
                    default: return "";
                }
            }
        }

        /// <summary>Longer description for the menu.</summary>
        public string StatusText
        {
            get
            {
                string pairText = PairCount > 0 && CurrentTrial >= 0 ? $"{(IsTraining ? "Training" : "Pair")} {CurrentTrial + 1} / {PairCount}: " : "";
                switch (State)
                {
                    case PairedLocalizationState.WaitingForStart: return "Press Start to begin.";
                    case PairedLocalizationState.Starting: return "Starting... close the menu and get ready.";
                    case PairedLocalizationState.Complete: return "Block complete. Open the menu and press Next.";
                    default: return pairText + ShortStatus + ".";
                }
            }
        }

        // ------------------------------------------------------------------------------------------
        // Lifecycle
        // ------------------------------------------------------------------------------------------

        private void OnEnable()
        {
            ResolveReferences();

            if (receiver != null)
            {
                _modeBind = receiver.Bind(QExEPairedLocalizationProtocol.Mode, OnMode);
                _pairBind = receiver.Bind(QExEPairedLocalizationProtocol.Pair, OnPair);
                _stateBind = receiver.Bind(QExEPairedLocalizationProtocol.State, OnState);
                _readyBind = receiver.Bind(QExEPairedLocalizationProtocol.Ready, OnReady);
                _playingBind = receiver.Bind(QExEPairedLocalizationProtocol.Playing, OnPlaying);
                _playedBind = receiver.Bind(QExEPairedLocalizationProtocol.Played, OnPlayed);
                _answeredBind = receiver.Bind(QExEPairedLocalizationProtocol.Answered, OnAnswered);
                _completeBind = receiver.Bind(QExEPairedLocalizationProtocol.Complete, OnComplete);
                _abortBind = receiver.Bind(QExEPairedLocalizationProtocol.Abort, OnAbort);
                _selectedBind = receiver.Bind(QExEPairedLocalizationProtocol.Selected, OnSelected);
            }
            else
            {
                Debug.LogError("QExE Paired Localization: no OSCReceiver found. Is the 'OSCManager' prefab in the scene?", this);
            }

            if (pointer != null)
            {
                if (pointer.dial == null) pointer.dial = dial;
                pointer.Confirmed += OnPointerConfirmed;
                pointer.DialPressed += OnDialPressed;
                if (buttons != null) pointer.RegisterPressable(buttons);
                pointer.SetArmed(false);
            }
            else
            {
                Debug.LogError("QExE Paired Localization: no LocalizationPointer assigned.", this);
            }

            switchAction.action?.Enable();
            if (buttons != null) buttons.Pressed += OnButtonPressed;
            else Debug.LogError("QExE Paired Localization: no LocalizationABButtons assigned. Run QExE > Paired Localization > 2. Set Up Open Scene.", this);

            ClearPairVisuals();
            UpdateVisibility();
        }

        private void OnDisable()
        {
            if (receiver != null)
            {
                foreach (var bind in new[] { _modeBind, _pairBind, _stateBind, _readyBind, _playingBind, _playedBind, _answeredBind, _completeBind, _abortBind, _selectedBind })
                    if (bind != null) receiver.Unbind(bind);
            }
            if (pointer != null)
            {
                pointer.Confirmed -= OnPointerConfirmed;
                pointer.DialPressed -= OnDialPressed;
                if (buttons != null) pointer.UnregisterPressable(buttons);
            }
            if (buttons != null) buttons.Pressed -= OnButtonPressed;
            // Only disable an action owned by this component; a referenced asset action may be shared.
            if (switchAction.reference == null) switchAction.action?.Disable();
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
            if (buttons == null) buttons = GetComponent<LocalizationABButtons>();
        }

        private void Update()
        {
            if (State == PairedLocalizationState.Aligning && (alignment == null || alignment.IsAligned))
            {
                GetHeadYawPitch(out float yaw, out float pitch);
                var message = new OSCMessage(QExEPairedLocalizationProtocol.Aligned);
                message.AddValue(OSCValue.Int(CurrentTrial));
                message.AddValue(OSCValue.Float(yaw));
                message.AddValue(OSCValue.Float(pitch));
                Send(message);
                SetState(PairedLocalizationState.AlignedWaiting);
            }

            if (IsComparing && switchAction.action != null && switchAction.action.WasPressedThisFrame())
                OnButtonPressed(_audibleSide == "A" ? "B" : "A");

            if (_trackingDeviation)
            {
                GetHeadYawPitch(out float yaw, out float pitch);
                _maxAbsYaw = Mathf.Max(_maxAbsYaw, Mathf.Abs(yaw));
                _maxAbsPitch = Mathf.Max(_maxAbsPitch, Mathf.Abs(pitch));
            }

            if (dial != null)
            {
                dial.SetCounter(PairCount > 0 && CurrentTrial >= 0
                    ? $"{(IsTraining ? "Training" : "Pair")} {CurrentTrial + 1} / {PairCount}"
                    : (IsTraining ? "Training" : ""));
                dial.SetStatus(ShortStatus);
            }
        }

        // ------------------------------------------------------------------------------------------
        // Participant actions
        // ------------------------------------------------------------------------------------------

        /// <summary>Called by the method UI when the participant presses Start.</summary>
        public void RequestStart()
        {
            if (State != PairedLocalizationState.WaitingForStart) return;
            var space = LocalizationSpace.Find(this);
            if (space != null && space.calibrateOnStart) space.CalibrateToHead();
            SetState(PairedLocalizationState.Starting);
            Send(new OSCMessage(QExEPairedLocalizationProtocol.Start));
        }

        private void OnButtonPressed(string side)
        {
            if (IsComparing)
            {
                // Switch the audible side; Max crossfades and confirms with "selected".
                if (side == _audibleSide) return;
                var switchMessage = new OSCMessage(QExEPairedLocalizationProtocol.Play);
                switchMessage.AddValue(OSCValue.Int(CurrentTrial));
                switchMessage.AddValue(OSCValue.String(side));
                Send(switchMessage);
                return;
            }
            if (State != PairedLocalizationState.PairIdle) return;
            if ((side == "A" && !_aEnabled) || (side == "B" && !_bEnabled)) return;

            var message = new OSCMessage(QExEPairedLocalizationProtocol.Play);
            message.AddValue(OSCValue.Int(CurrentTrial));
            message.AddValue(OSCValue.String(side));
            Send(message);
            SetState(PairedLocalizationState.Requesting);
        }

        private void OnPointerConfirmed(PointingSample sample)
        {
            if (State != PairedLocalizationState.PairIdle || !_answerOpen) return;
            SetCandidate(sample, "pointer");
        }

        private void OnDialPressed(float azimuth, bool inCentre)
        {
            if (State != PairedLocalizationState.PairIdle || !_answerOpen) return;

            if (inCentre)
            {
                if (_candidate.HasValue) Submit();
                return;
            }
            if (pointer != null)
                SetCandidate(pointer.SampleFromDirection(pointer.DirectionFromAzEl(azimuth, 0f)), "dial");
        }

        private void SetCandidate(PointingSample sample, string source)
        {
            _candidate = sample;
            _candidateSource = source;
            _candidateTime = Time.realtimeSinceStartupAsDouble;

            if (dial != null) dial.SetPreliminary(sample.Azimuth);
            if (headRing != null) headRing.SetMarker(sample.Azimuth);
            if (_candidateDot == null)
                _candidateDot = LocalizationVisuals.CreateDot(transform, "CandidateMarker", candidateDotSize, candidateColor);
            _candidateDot.position = sample.WorldPoint;
            _candidateDot.gameObject.SetActive(true);
            UpdateSubmitHint();
        }

        private void ClearCandidate()
        {
            _candidate = null;
            _candidateSource = "";
            _candidateTime = -1;
            if (_candidateDot != null) _candidateDot.gameObject.SetActive(false);
            if (dial != null) dial.SetPreliminary(null);
            if (headRing != null) headRing.SetMarker(null);
            UpdateSubmitHint();
        }

        private void Submit()
        {
            if (!_candidate.HasValue) return;
            var sample = _candidate.Value;
            double now = Time.realtimeSinceStartupAsDouble;
            float rtMs = _answerOpenSince >= 0 ? (float)((now - _answerOpenSince) * 1000.0) : -1f;
            float markMs = _answerOpenSince >= 0 && _candidateTime >= 0 ? (float)((_candidateTime - _answerOpenSince) * 1000.0) : -1f;

            var message = new OSCMessage(QExEPairedLocalizationProtocol.Response);
            message.AddValue(OSCValue.Int(CurrentTrial));
            message.AddValue(OSCValue.String(_answerSide));
            message.AddValue(OSCValue.Float(sample.Azimuth));
            message.AddValue(OSCValue.Float(sample.Elevation));
            message.AddValue(OSCValue.Float(sample.AzimuthHead));
            message.AddValue(OSCValue.Float(sample.ElevationHead));
            message.AddValue(OSCValue.Float(sample.HeadYaw));
            message.AddValue(OSCValue.Float(sample.HeadPitch));
            message.AddValue(OSCValue.Float(rtMs));
            message.AddValue(OSCValue.String(DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture)));
            message.AddValue(OSCValue.String(_candidateSource));
            message.AddValue(OSCValue.Float(markMs));
            Send(message);

            _submitted = sample;
            SetState(PairedLocalizationState.Submitted);
            Debug.Log($"<color=cyan><b>QExE:pair:</b></color> pair {CurrentTrial} {_answerSide} ({_candidateSource}): az {sample.Azimuth:F1}, el {sample.Elevation:F1}, rt {rtMs:F0} ms");
        }

        // ------------------------------------------------------------------------------------------
        // Messages from Max
        // ------------------------------------------------------------------------------------------

        private void OnMode(OSCMessage message)
        {
            IsTraining = message.Values.Count > 0 && message.Values[0].Type == OSCValueType.String &&
                         message.Values[0].StringValue == "training";
            if (TryGetInt(message, 1, out int count)) PairCount = count;
            Debug.Log($"<color=cyan><b>QExE:pair:</b></color> block mode: {(IsTraining ? "training" : "test")}, {PairCount} pairs");
        }

        private void OnPair(OSCMessage message)
        {
            if (!TryGetInt(message, 0, out int trial)) return;
            CurrentTrial = trial;
            if (TryGetInt(message, 1, out int count)) PairCount = count;
            CurrentItemId = TryGetString(message, 2);

            _aEnabled = true;
            _bEnabled = false;
            _answerOpen = false;
            _answerSide = "A";
            _answerOpenSince = -1;
            _lockedA = null;
            _submitted = null;
            _trackingDeviation = false;
            ClearPairVisuals();
            SetState(PairedLocalizationState.PairIdle);
            onPairStarted.Invoke(trial, PairCount);
        }

        private void OnState(OSCMessage message)
        {
            if (!TryGetInt(message, 0, out int trial) || trial != CurrentTrial) return;
            TryGetInt(message, 1, out int aEnabled);
            TryGetInt(message, 2, out int bEnabled);
            TryGetInt(message, 3, out int answerOpen);
            string side = TryGetString(message, 4);
            if (side != "A" && side != "B") side = _answerSide;

            if (side != _answerSide)
            {
                // Moved on to answering B: the candidate belonged to A.
                _answerSide = side;
                _answerOpenSince = -1;
                ClearCandidate();
            }
            _aEnabled = aEnabled == 1;
            _bEnabled = bEnabled == 1;
            _answerOpen = answerOpen == 1;
            if (_answerOpen && _answerOpenSince < 0) _answerOpenSince = Time.realtimeSinceStartupAsDouble;

            // A state message means nothing is playing: back to the pair (also after a rejected request or answer).
            if (State == PairedLocalizationState.Requesting || State == PairedLocalizationState.Submitted ||
                State == PairedLocalizationState.PairIdle || State == PairedLocalizationState.Starting)
                SetState(PairedLocalizationState.PairIdle);
            else
                UpdateVisibility();
        }

        private void OnReady(OSCMessage message)
        {
            if (!TryGetInt(message, 0, out int trial) || trial != CurrentTrial) return;
            float tolerance = TryGetFloat(message, 2, out float tol) ? tol : -1f;
            float hold = TryGetInt(message, 3, out int holdMs) ? holdMs : -1f;
            if (alignment != null) alignment.Configure(tolerance, hold);
            SetState(PairedLocalizationState.Aligning);
        }

        private void OnPlaying(OSCMessage message)
        {
            if (!TryGetInt(message, 0, out int trial) || trial != CurrentTrial) return;
            _audibleSide = TryGetString(message, 1);
            if (buttons != null) buttons.SetAudibleSide(_audibleSide);
            GetHeadYawPitch(out _onsetYaw, out _onsetPitch);
            _maxAbsYaw = Mathf.Abs(_onsetYaw);
            _maxAbsPitch = Mathf.Abs(_onsetPitch);
            _trackingDeviation = true;
            SetState(PairedLocalizationState.Playing);
        }

        private void OnPlayed(OSCMessage message)
        {
            if (!TryGetInt(message, 0, out int trial) || trial != CurrentTrial) return;
            TryGetInt(message, 2, out int playIndex);
            _trackingDeviation = false;
            _audibleSide = "";
            if (buttons != null) buttons.SetAudibleSide(null);

            var head = new OSCMessage(QExEPairedLocalizationProtocol.PlayHead);
            head.AddValue(OSCValue.Int(CurrentTrial));
            head.AddValue(OSCValue.Int(playIndex));
            head.AddValue(OSCValue.Float(_onsetYaw));
            head.AddValue(OSCValue.Float(_onsetPitch));
            head.AddValue(OSCValue.Float(_maxAbsYaw));
            head.AddValue(OSCValue.Float(_maxAbsPitch));
            Send(head);

            // The state message that follows reopens the pair.
            SetState(PairedLocalizationState.PairIdle);
        }

        private void OnAnswered(OSCMessage message)
        {
            if (!TryGetInt(message, 0, out int trial) || trial != CurrentTrial) return;
            string side = TryGetString(message, 1);

            if (side == "A")
            {
                _lockedA = _submitted;
                ClearCandidate();
                if (_lockedA.HasValue)
                {
                    if (dial != null) dial.SetReference(_lockedA.Value.Azimuth);
                    if (headRing != null) headRing.SetReferenceMarker(_lockedA.Value.Azimuth);
                }
                // The state message that follows unlocks B.
            }
            else if (side == "B")
            {
                ClearPairVisuals();
                SetState(PairedLocalizationState.BetweenPairs);
            }
        }

        private void OnComplete(OSCMessage message)
        {
            TryGetInt(message, 0, out int count);
            _trackingDeviation = false;
            ClearPairVisuals();
            SetState(PairedLocalizationState.Complete);
            onBlockComplete.Invoke(count);
        }

        /// <summary>Comparison: Max switched the audible side (after a button press or the switch control).</summary>
        private void OnSelected(OSCMessage message)
        {
            if (!TryGetInt(message, 0, out int trial) || trial != CurrentTrial) return;
            string side = TryGetString(message, 1);
            if (side != "A" && side != "B") return;
            _audibleSide = side;
            if (buttons != null) buttons.SetAudibleSide(side);
        }

        /// <summary>Max reset the block (operator Abort, or Start was rejected): wait for Start again.</summary>
        private void OnAbort(OSCMessage message)
        {
            _trackingDeviation = false;
            _audibleSide = "";
            if (buttons != null) buttons.SetAudibleSide(null);
            CurrentTrial = -1;
            _aEnabled = _bEnabled = _answerOpen = false;
            _answerSide = "A";
            _answerOpenSince = -1;
            _lockedA = null;
            _submitted = null;
            ClearPairVisuals();
            SetState(PairedLocalizationState.WaitingForStart);
            Debug.Log("<color=cyan><b>QExE:pair:</b></color> block reset by Max, waiting for Start.");
        }

        // ------------------------------------------------------------------------------------------
        // Visuals
        // ------------------------------------------------------------------------------------------

        private void SetState(PairedLocalizationState state)
        {
            State = state;
            if (pointer != null) pointer.SetArmed(State == PairedLocalizationState.PairIdle && _answerOpen);
            UpdateSubmitHint();
            UpdateVisibility();
        }

        private void ClearPairVisuals()
        {
            ClearCandidate();
            if (dial != null) dial.SetReference(null);
            if (headRing != null) headRing.SetReferenceMarker(null);
        }

        private void UpdateSubmitHint()
        {
            if (dial != null) dial.SetSubmitReady(State == PairedLocalizationState.PairIdle && _answerOpen && _candidate.HasValue);
        }

        private bool UIHiddenForListening
        {
            get
            {
                if (!hideUIWhileListening) return false;
                switch (State)
                {
                    case PairedLocalizationState.Starting:
                    case PairedLocalizationState.Requesting:
                    case PairedLocalizationState.Aligning:
                    case PairedLocalizationState.AlignedWaiting:
                    case PairedLocalizationState.Playing:
                    case PairedLocalizationState.BetweenPairs:
                        return true;
                    default:
                        return false;
                }
            }
        }

        private void UpdateVisibility()
        {
            bool inBlock = State != PairedLocalizationState.WaitingForStart && State != PairedLocalizationState.Complete;
            bool hidden = UIHiddenForListening;
            bool facingFront = State == PairedLocalizationState.Requesting || State == PairedLocalizationState.Aligning ||
                               State == PairedLocalizationState.AlignedWaiting ||
                               (State == PairedLocalizationState.Playing && showAlignmentWhileListening);
            bool pairUI = inBlock && !hidden && State != PairedLocalizationState.Starting;
            bool comparingUI = IsComparing && showButtonsWhileComparing;

            if (dial != null) dial.SetVisible(pairUI);
            if (buttons != null)
            {
                buttons.SetVisible((pairUI || comparingUI) && CurrentTrial >= 0);
                buttons.SetButton("A", !_aEnabled ? LocalizationABButtons.ButtonState.Disabled
                                      : _answerSide == "A" ? LocalizationABButtons.ButtonState.Answering
                                      : LocalizationABButtons.ButtonState.Answered);
                buttons.SetButton("B", !_bEnabled ? LocalizationABButtons.ButtonState.Disabled
                                      : LocalizationABButtons.ButtonState.Answering);
            }
            if (headRing != null) headRing.SetVisible(inBlock ? (!hidden || keepRingWhileListening) : showRingOutsideTrials);
            if (alignment != null) alignment.SetVisible(facingFront);
            if (_candidateDot != null) _candidateDot.gameObject.SetActive(!hidden && _candidate.HasValue);
            if (pointer != null) pointer.SetVisualsVisible((!hidden || comparingUI) && State != PairedLocalizationState.Complete);
        }

        // ------------------------------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------------------------------

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

        private void Send(OSCMessage message)
        {
            if (transmitter == null)
            {
                Debug.LogError("QExE Paired Localization: no OSCTransmitter, cannot send " + message.Address, this);
                return;
            }
            transmitter.Send(message);
        }

        private static string TryGetString(OSCMessage message, int index)
        {
            if (message.Values.Count <= index) return "";
            var v = message.Values[index];
            return v.Type == OSCValueType.String ? v.StringValue : "";
        }

        private static bool TryGetFloat(OSCMessage message, int index, out float value)
        {
            value = 0f;
            if (message.Values.Count <= index) return false;
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
            if (message.Values.Count <= index) return false;
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
