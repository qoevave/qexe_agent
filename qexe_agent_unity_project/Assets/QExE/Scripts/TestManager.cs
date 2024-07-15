using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using extOSC;

public class TestManager : MonoBehaviour
{
     
	#region Public Variables 

    public GameObject OSCManager;

    public bool SceneOSCManagerFound;

    public GameObject TestInterface;

    // Reference to /btn/Next button on the UI (Questionnaire/Test) that triggers the scene change
    public Button NextButton;

    public bool InterfaceNextButtonFound; 

	// Reference to /btn/StartPlayback button on the UI (Questionnaire/Test) that starts scene playback
	public Button StartPlaybackButton;

    public bool InterfacePlayButtonFound;

    public string ThisSceneID;

    public string ThisSceneVideoPath;

    public int ThisSceneVideoID;

    public enum SceneType
	{
        configScene,
        evaluationScene,
        questionnaireScene
	}
    public SceneType ThisSceneType;

    // ID of type <string> passed from the host
    public string NextSceneID;

    // Path to video of type <string> passed from the host
    public string NextVideoPath;

    // ID of type <int> passed from the host
    public int NextVideoID;

    public bool SimulatorNext = false;

    public string ChosenQuestionnaire;
    public string QuestionnaireIntegration;
    public GameObject[] QuestionnaireInterfaces;

    public string ChosenMethodology;

    public int NumberOfConditions;

    public GameObject[] MethodologyInterfaces;


    #endregion

    #region Private Variables
    private OSCTransmitter Transmitter;

    private OSCReceiver Receiver;

    private bool helloHost = false;

    private GameObject _Methodology;

    private GameObject _Questionnaire;

    private static GameObject thisTestManagerGameObject;

    private QEXEPlaybackSync sceneAnimations;

    Scene ActiveScene;
	#endregion

	#region Unity Functions

    /// <summary>
    /// Save this instance of this game object into a new dontdestroyonload thread to keep this TestManager class alive. 
    /// This instance of this game object will now persist into other scenes. 
    /// </summary>
	private void Awake()
    {
        if(thisTestManagerGameObject == null)
		{
            thisTestManagerGameObject = this.gameObject;
            DontDestroyOnLoad(this.gameObject);
        } else if (this != thisTestManagerGameObject)
		{
            Destroy(this.gameObject);
		}
    }

    /// <summary>
    /// Called once per physics call. 
    /// Checks if this is the first physics call since program is running, tells the server the client is running. 
    /// </summary>
    void FixedUpdate()
    {
        if (!helloHost)
		{
            SendMessage("/control/", "TestManager client_is_active 1");
            Debug.Log("<color=cyan><b>QExE:udp:out: </b></color> -->> is_json_loaded?");
            helloHost = true;
        }
    }

    /// <summary>
    /// Call once the application has closed, or the editor stops running. 
    /// </summary>
    void OnApplicationQuit()
    {
        SendMessage("/control/", "TestManager client_is_active 0");
        Debug.Log("<color=bright_white><b>QExE: </b></color>Application ending after " + Time.time + " seconds");
    }

    /// <summary>
    /// Subscribe methods for when the scene has loaded.
    /// </summary>
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    /// <summary>
    /// Unsubscribe methods for when this scene is unloaded.
    /// </summary>
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }
    #endregion


    /// <summary>
    /// Function to find the required game objects inside the scene. 
    /// 1. OSCManager = needed to communicate with the server. 
    /// 2. QEXEPlaybackSync = component to pause any animations at the start of scene playback until the subject presses play. 
    /// 3. QExETestInterface = game object that the test interface will be loaded onto. 
    /// </summary>
    private void FindSceneObjects()
	{
        OSCManager = GameObject.Find("OSCManager");
        SceneOSCManagerFound = true;
        Transmitter = OSCManager.GetComponent<OSCTransmitter>();
        Receiver = OSCManager.GetComponent<OSCReceiver>();
        TestInterface = GameObject.Find("QExETestInterface");

        try{
            sceneAnimations = GameObject.Find("QExEAnimationAudioSync").GetComponent<QEXEPlaybackSync>();
		}
		catch{
            Debug.Log("<color=bright_white><b>QExE: </b></color>No Animations inside scene");
		}

        // Bind all OSC receriving functions. 
        Receiver.Bind("/client/configuration/", ReceiveConfiguration);
        Receiver.Bind("/client/item/", ReceiveItem);
        Receiver.Bind("/client/simulator", ReceiveSimulator);
    }

    /// <summary>
    /// Called when the 'Next' button callback event is triggered. 
    /// 1. Tell the server to stop playing audio. (We send the message regardless if audio is actually playing or not.) 
    /// 2. Tell the server to load the next item in the test. For the server, this means incrementing through the test items found in the testmanager. 
    /// Loading the next item will depending on the current Unity scene type (configScene, evaluationScene, or questionnaireScene), and the integration type of the questionnaire. 
    /// </summary>
    public void OnNextButton()
	{
        // Stop the audio playback.
        SendMessage("/audioplayback/", "stop");
        Debug.Log("<color=bright_white><b>QExE: </b></color>Host to stop audio playback");
        SimulatorNext = false;

        // If the current scene == evaluationScene, and the questionnaire is set to 'Lace', we DO NOT call NextScene() to the server.
        // The NextScene will be the questionnaire scene. 
        if(ThisSceneType == SceneType.evaluationScene && QuestionnaireIntegration == "Lace")
		{
            Debug.Log("<color=bright_white><b>QExE: </b></color>'Next' button pressed. Load the next scene ... ");
            SceneManager.LoadScene(NextSceneID, LoadSceneMode.Single);
        }

        // If the current scene == evaluationScene, and the questionnaire is set to 'End' we check ...
        if (ThisSceneType == SceneType.evaluationScene && QuestionnaireIntegration == "End")
        {
            // ... if the next scene should be the config scene (i.e., the test has finished), force the next scene to actually be the questionnaire scene. 
            if (NextSceneID == "_config")
			{
                NextSceneID = "_questionnaire";
            // ... if not, simply load the next intended evaluationScene. 
            } else
			{
                SendMessage("/control/", "TestManager set_next_item");
                Debug.Log("<color=bright_white><b>QExE: </b></color>Setting next item in server");
            }
            Debug.Log("<color=bright_white><b>QExE: </b></color> 'Next' button pressed. Load the next scene ... ");
            SceneManager.LoadScene(NextSceneID, LoadSceneMode.Single);
        }

        // In the remaining combination of cases, we tell the server to load the next item. 
        if (ThisSceneType == SceneType.configScene && QuestionnaireIntegration == "Lace")
            NextItem();

        if (ThisSceneType == SceneType.configScene && QuestionnaireIntegration == "End")
            NextItem();

        if (ThisSceneType == SceneType.questionnaireScene && QuestionnaireIntegration == "Lace")
            NextItem();

        if (ThisSceneType == SceneType.questionnaireScene && QuestionnaireIntegration == "End")
            NextItem();

        if (QuestionnaireIntegration == "None")
            NextItem();
    }

    /// <summary>
    /// Function to tell the server to load the next item. 
    /// </summary>
    private void NextItem()
	{
        SendMessage("/control/", "TestManager set_next_item");
        Debug.Log("<color=bright_white><b>QExE: </b></color>Setting next item in server");

        Debug.Log("<color=bright_white><b>QExE: </b></color>'Next' button pressed. Load the next scene ... ");
        SceneManager.LoadScene(NextSceneID, LoadSceneMode.Single);
    }


    /// <summary>
    /// Audio and animation control. 
    /// 1. Tells the server to start playing audio. 
    /// 2. Unity will start playing all animations that are saved into the sceneAnimations array. 
    /// Scene animations are manually added to an array on the QExEPlaybackSync.cs class on a scene per scene basis. 
    /// </summary>
    public void OnStartPlayback()
	{
        SendMessage("/audioplayback/", "play");
        Debug.Log("<color=bright_white><b>QExE: </b></color>Host to start audio playback.");
		if (sceneAnimations)
		{
            sceneAnimations.StartAnimations();
		}
    }

    /// <summary>
    /// Called as soon as a new scene is loaded, and before it begins to play.
    /// 1. Find the required and (if any) optional game objects for this scene. 
    /// 2. Transfer the variables from the 'next scene' information, to the 'current scene' information. The 'next scene' information can then be overwritten. 
    /// 3. Request the next scene information depending on the current scene and configuration of the questionnaire. 
    /// </summary>
    /// <param name="scene"></param>
    /// <param name="mode"></param>
    public void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
        Debug.Log("<color=bright_white><b>QExE: </b></color>Scene loaded: " + scene.name);

        FindSceneObjects();

        // Transfer scene variables once the scene has loaded. 
        ThisSceneID = scene.name;
        ThisSceneVideoPath = NextVideoPath;
        ThisSceneVideoID = NextVideoID;
        Transmitter.VideoFilePath = ThisSceneVideoPath;
        Transmitter.CurrentScene = scene.name;

        // If the scene is the _config scene. 
        if (scene.name == "_config")
        {
            ThisSceneType = SceneType.configScene;
            StartCoroutine(AfterSceneStarted());
        }

        // If the scene is the questionnaire scene. 
        if (scene.name == "_questionnaire")
		{
            ThisSceneType = SceneType.questionnaireScene;
            SetQuestionnaireInterface(ChosenQuestionnaire);

            // Requests the next scene information during the questionnaire scene. 
            if(QuestionnaireIntegration == "Lace")
			{
                Debug.Log("Questionnaire integration == Lace. Requesting next scene now...");
                StartCoroutine(AfterSceneStarted());
            }

            if(QuestionnaireIntegration == "End")
			{
                Debug.Log("Questionnaire integration == End. Requesting next scene now...");
                NextSceneID="_config";
            }

            if (QuestionnaireIntegration == "None")
			{
                StartCoroutine(AfterSceneStarted());
            }
		}

        // If the scene is a test item (evaluation scene).
        if (scene.name != "_config" && scene.name != "_questionnaire")
		{
            SetMethodologyInterface(ChosenMethodology);
            ThisSceneType = SceneType.evaluationScene;

            // Does not request next scene information during evaluation content if there is a questionnaire afterwards. 
            if(QuestionnaireIntegration == "Lace")
			{
                Debug.Log("Questionnaire integration == Lace. Will request next item during questionnaire scene after this evaluation content.");
                NextSceneID = "_questionnaire";
			}

            if (QuestionnaireIntegration == "End" || QuestionnaireIntegration == "None")
            {
                StartCoroutine(AfterSceneStarted());
            }
        }
    }

    /// <summary>
    /// 1. Flags the start of the scene in the behavior file. 
    /// 2. Requests the next test item from the server. 
    /// </summary>
    /// <returns></returns>
    IEnumerator AfterSceneStarted()
	{
        yield return new WaitForSeconds(1);
        SendMessage("/behaviour/tag/sceneStart", ThisSceneID);
        //RequestItem();
        SendMessage("/control/", "TestManager  get_next_item");
        Debug.Log("<color=cyan><b>QExE:udp:out: -->> </b></color>requesting_next_item");
    }

    /// <summary>
    /// Clean variables on scene unload ready for the next scene.  
    /// </summary>
    /// <param name="current"></param>
    public void OnSceneUnloaded(Scene current)
	{
        NextButton.onClick.RemoveListener(() => OnNextButton());
        Debug.Log("<color=bright_white><b>QExE: </b></color>Scene unloaded: " + current.name);

        SceneOSCManagerFound = false;
        InterfaceNextButtonFound = false;
        InterfacePlayButtonFound = false;
	}

    /// <summary>
    /// Loads in the correct methodology interface to conduct the test. 
    /// 1. Loop through all method interfaces of the testmanager.cs class set via the unity inspector drop down list. 
    /// 2. Instantiate the method interface at the position of the TestInterface game object. 
    /// 3. Find reference to the required interface components ('Next' and 'Start' button are manditory for every interface).
    /// 4. Set listeners for buttons, and turn off the interface. 
    /// </summary>
    /// <param name="method"></param>
    private void SetMethodologyInterface(string method)
	{
        // Instantiate correct test interface prefab
        foreach (GameObject Interface in MethodologyInterfaces)
		{
            if (Interface.name == method)
			{
              Debug.Log("<color=bright_white><b>QExE: </b></color> Method found. " + Interface.name + ": The method interface provided by the server(JSON file) is listed in the Method Interfaces array. Importing into scene.");
              _Methodology = Instantiate(Interface, TestInterface.transform.position, Quaternion.Euler(TestInterface.transform.rotation.eulerAngles.x, TestInterface.transform.rotation.eulerAngles.y, TestInterface.transform.rotation.eulerAngles.z), TestInterface.transform) as GameObject;
            }
		}

        // Get reference to the Next and Startplayback buttons
        NextButton = GameObject.Find("btn/Next").GetComponent<Button>();
        StartPlaybackButton = GameObject.Find("btn/StartPlayback").GetComponent<Button>();
        InterfaceNextButtonFound = true;
        InterfacePlayButtonFound = true;

        // Set the next button interatable false, so users do not accidently click it to start with. 
        NextButton.interactable = false;

        // Add event listeners
        NextButton.onClick.AddListener(() => OnNextButton());
        StartPlaybackButton.onClick.AddListener(() => OnStartPlayback());

        TestInterface.SetActive(false);
    }

    /// <summary>
    /// Loads in the correct questionnaire interface to conduct the test. 
    /// 1. Loop through all questionnaire interfaces of the testmanager.cs class set via the unity inspector drop down list. 
    /// 2. Instantiate the questionnaire interfaces at the position of the TestInterface game object. 
    /// 3. Find reference to the required interface components ('Next' button is manditory) and set listener.
    /// </summary>
    /// <param name="questionnaire"></param>
    private void SetQuestionnaireInterface(string questionnaire)
    {
        // Instantiate correct test interface prefab
        GameObject QuestionnaireInterface = GameObject.Find("QExEQuestionnaireInterface");
        foreach (GameObject Interface in QuestionnaireInterfaces)
        {
            if (Interface.name == questionnaire)
            {
                Debug.Log("<color=bright_white><b>QExE: </b></color>Questionnaire found. " + Interface.name + ": The questionnaire interface provided by the server (JSON file) is listed in the Questionnaire Interfaces array. Importing into scene.");
                _Questionnaire = Instantiate(Interface, QuestionnaireInterface.transform.position, Quaternion.Euler(QuestionnaireInterface.transform.rotation.eulerAngles.x, QuestionnaireInterface.transform.rotation.eulerAngles.y, QuestionnaireInterface.transform.rotation.eulerAngles.z), QuestionnaireInterface.transform) as GameObject;
            }
        }

        // Get reference to the Next and Startplayback buttons
        NextButton = GameObject.Find("btn/Next").GetComponent<Button>();
        InterfaceNextButtonFound = true;

        // Add event listeners
        NextButton.onClick.AddListener(() => OnNextButton());
    }


    // *****************************
    // OSC Messaging
    // *****************************

    public void ReceiveConfiguration(OSCMessage msg)
	{
        var configCall = msg.FindValues(OSCValueType.String)[0].StringValue;
        switch (configCall)
		{
            case "json_loaded":
                var value = msg.FindValues(OSCValueType.Int)[0].IntValue;
                if (value == 0)
                    Debug.LogWarning("<color=red><b>QExE:udp:in: </b<</color> <<-- is_json_loaded?: No JSON file has been loaded into the server. Please load a JSON file in QExE host.");

                if (value == 1)
                    Debug.Log("<color=cyan><b>QExE:udp:in: </b></color> <<-- is_json_loaded?: A JSON file has been loaded. Continue config ...");
                    SendMessage("/control/", "TestManager get_paradigm_information");
                    Debug.Log("<color=cyan><b>QExE:udp:out: </b></color> -->> requesting_paradigm_information");
                break;

            case "set_paradigm_information":
                ChosenMethodology = msg.FindValues(OSCValueType.String)[1].StringValue;
                NumberOfConditions = msg.FindValues(OSCValueType.Int)[0].IntValue;
                ChosenQuestionnaire = msg.FindValues(OSCValueType.String)[2].StringValue;
                QuestionnaireIntegration = msg.FindValues(OSCValueType.String)[3].StringValue;
                Debug.Log("<color=cyan><b>QExE:udp:in: </b></color> <<-- receiving_paradigm_information:" + ChosenMethodology + NumberOfConditions + ChosenQuestionnaire + QuestionnaireIntegration);
                SetMethodologyInterface(ChosenMethodology);
                break;

            case "set_results_directory":
                break;
        }
    }

    public void ReceiveItem(OSCMessage msg)
	{
        var itemCall = msg.FindValues(OSCValueType.String)[0].StringValue;
		switch(itemCall){
            case "set_scene_item":
                NextSceneID = msg.FindValues(OSCValueType.String)[1].StringValue;
                Debug.Log("<color=cyan><b>QExE:udp:in: </b></color> <<-- receiving_scene_item_ID: " + NextSceneID);
                break;
                 
            case "set_video_item":
                NextVideoPath = msg.FindValues(OSCValueType.String)[1].StringValue;
                NextVideoID = msg.FindValues(OSCValueType.Int)[0].IntValue;
                Debug.Log("<color=cyan><b>QExE:udp:in: </b></color> <<-- receiving_video_item: " + NextVideoPath + NextVideoID);
                break;
		}
    }


    public void ReceiveSimulator(OSCMessage msg)
	{
        var simulatorCall = msg.FindValues(OSCValueType.String)[0].StringValue;
        switch (simulatorCall)
		{
            case "Play":
                Debug.Log("Simulator 'Play' received");
                OnStartPlayback();
                SimulatorNext = true;
                break;
            case "Next":
                Debug.Log("Simulator 'Next' received");
                OnNextButton();
                break;
		}
	}

    public void SendMessage(string address, string msg)
    {
        Send(address, OSCValue.String(msg));
    }

    private void Send(string address, OSCValue value)
    {
        var message = new OSCMessage(address, value);
        Transmitter.Send(message);
    }

}
