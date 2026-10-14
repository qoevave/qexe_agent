using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QExE.Localization
{
    /// <summary>
    /// Method interface for paired (A/B) localization (the menu the participant opens with the
    /// controller). Start begins the block, Next becomes available once the block is complete.
    ///
    /// TestManager finds the 'btn/StartPlayback' and 'btn/Next' buttons itself and handles scene
    /// changes. Created by QExE > Paired Localization > 1. Create Method Interface Prefab
    /// (Resources/QExEMethods/PairedLocalization.prefab, matching methodType "PairedLocalization").
    /// </summary>
    public class PairedLocalizationMethodUI : MonoBehaviour
    {
        public Button startButton;
        public Button nextButton;
        public TMP_Text titleText;
        public TMP_Text statusText;

        [TextArea]
        public string title = "Sound localization (A/B)";

        private PairedLocalizationController _controller;
        private bool _initialized;

        private void OnEnable()
        {
            if (!_initialized) Initialize();
        }

        private void Initialize()
        {
            _initialized = true;
            _controller = FindFirstObjectByType<PairedLocalizationController>();

            if (titleText != null) titleText.text = title;
            if (startButton != null) startButton.onClick.AddListener(OnStart);
            if (nextButton != null) nextButton.interactable = false;
        }

        private void OnStart()
        {
            if (startButton != null) startButton.gameObject.SetActive(false);

            if (_controller != null)
                _controller.RequestStart();
            else if (nextButton != null)
                nextButton.interactable = true; // e.g. the _config scene: behave like the other methods
        }

        private void Update()
        {
            if (_controller == null)
            {
                if (statusText != null)
                    statusText.text = startButton != null && startButton.gameObject.activeSelf
                        ? "Press Start, then Next to begin the test."
                        : "Press Next to begin the test.";
                return;
            }

            if (statusText != null) statusText.text = _controller.StatusText;

            // After an operator Abort the block waits for Start again.
            if (startButton != null && _controller.State == PairedLocalizationState.WaitingForStart && !startButton.gameObject.activeSelf)
                startButton.gameObject.SetActive(true);
            if (nextButton != null) nextButton.interactable = _controller.State == PairedLocalizationState.Complete;
        }
    }
}
