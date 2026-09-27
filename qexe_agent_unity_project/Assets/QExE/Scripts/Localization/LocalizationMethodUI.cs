using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QExE.Localization
{
    /// <summary>
    /// Method interface for the localization test (the menu the participant opens with the
    /// controller). Start begins the block, Next becomes available once the block is complete.
    ///
    /// TestManager finds the 'btn/StartPlayback' and 'btn/Next' buttons itself and handles scene
    /// changes; this component only adds the localization-specific behaviour.
    /// Created by the menu item QExE > Localization > Create Method Interface Prefab.
    /// </summary>
    public class LocalizationMethodUI : MonoBehaviour
    {
        public Button startButton;
        public Button nextButton;
        public TMP_Text titleText;
        public TMP_Text statusText;

        [TextArea]
        public string title = "Sound localization";

        private LocalizationTrialController _controller;
        private bool _initialized;

        private void OnEnable()
        {
            if (!_initialized)
                Initialize();
        }

        private void Initialize()
        {
            _initialized = true;
            _controller = FindFirstObjectByType<LocalizationTrialController>();

            if (titleText != null) titleText.text = title;
            if (startButton != null) startButton.onClick.AddListener(OnStart);
            if (nextButton != null) nextButton.interactable = false;
        }

        private void OnStart()
        {
            if (startButton != null) startButton.gameObject.SetActive(false);

            if (_controller != null)
            {
                _controller.RequestStart();
            }
            else
            {
                // No localization controller in this scene (e.g. the _config scene): behave like the
                // other methods and let the participant continue.
                if (nextButton != null) nextButton.interactable = true;
            }
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

            if (statusText != null)
                statusText.text = _controller.StatusText;

            if (nextButton != null)
                nextButton.interactable = _controller.State == LocalizationState.Complete;
        }
    }
}
