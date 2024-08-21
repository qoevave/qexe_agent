using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using extOSC;
using UnityEngine.SceneManagement;
using TMPro;

public class ABComparison : MonoBehaviour
{
    public Button PlayBtn;

    public Button NextPairBtn;

    public Button NextBtn;

    public Button BtnA;

    public Button BtnB;

    public TestManager _testmanager;

    public OSCPacket _OSCPacket;

    public Slider PCSlider;

    public int numberOfconditions;

    public int Multipler = 1;

    public int currentPair = 0;
  
    public GameObject currentPairsLabel;
    public GameObject maxPairsLabel;
    public int maxPairs = 10;

    private bool initialized = false;
    private bool startingColors = false;

    private Scene currentScene;

 



    Color activeColor = new Color(0.2039216f, 0.4784314f, 0.3568628f, 1.0f);
    Color inactiveColor = new Color(0.4156863f, 0.4156863f, 0.4156863f, 1.0f);


    // Start is called before the first frame update
    void Start()
    {
        activeColor = gameObject.GetComponent<UIColorManager>().primaryButtonColor;
        inactiveColor = gameObject.GetComponent<UIColorManager>().disabledButtonColor;
    }

    private void OnEnable()
    {
        if (!initialized)
            intialization();
    }

    // Update is called once per frame
    void Update()
    {
        if (!startingColors)
            SetInterface();
    }


    private void intialization()
    {
        _testmanager = GameObject.Find("TestManager").GetComponent<TestManager>();
        _OSCPacket = this.GetComponent<OSCPacket>();
        numberOfconditions = _testmanager.NumberOfConditions;
        Multipler = _testmanager.RepetitionMultiplier;
        initialized = true;
        currentScene = SceneManager.GetActiveScene();
        maxPairs = (((numberOfconditions * (numberOfconditions -1)))/2)* Multipler;

        // Add event listeners to the play and next buttons. 
        NextPairBtn.onClick.AddListener(() => OnNextPairButton());
        PlayBtn.onClick.AddListener(() => OnStartPlayback());

        // Add event listeners to the conditions buttons. 
        BtnA.onClick.AddListener(() => SelectA());
        BtnB.onClick.AddListener(() => SelectB());

        // Set initial states of the buttons. 
        NextBtn.GetComponent<Button>().interactable = false;
        NextPairBtn.GetComponent<Button>().interactable = false;
        PlayBtn.gameObject.SetActive(true);

        maxPairsLabel.GetComponent<TextMeshProUGUI>().text = $"/ {maxPairs}";
        currentPairsLabel.GetComponent<TextMeshProUGUI>().text = (currentPair+1).ToString();

        Debug.Log("UI Initialized.");
    }

    private void SetInterface()
	{
        // Set the reference as the starting condition. 
        var colors = BtnA.GetComponent<Button>().colors;
        colors.normalColor = activeColor;
        BtnA.GetComponent<Button>().colors = colors;
        var colorB = BtnB.GetComponent<Button>().colors;
        colorB.normalColor = inactiveColor;
        BtnB.GetComponent<Button>().colors = colorB;

        startingColors = true;
    }

    private void OnStartPlayback()
	{  
        if(currentScene.name == "_config")
        {
            NextBtn.GetComponent<Button>().interactable = true;
            PlayBtn.gameObject.SetActive(false);
        }

        if (currentScene.name != "_config")
        {
            NextPairBtn.GetComponent<Button>().interactable = true;
            PlayBtn.gameObject.SetActive(false);
            if (currentPair == maxPairs - 1)
            {
                NextBtn.GetComponent<Button>().interactable = true;
                NextPairBtn.GetComponent<Button>().interactable = false;
            }
        }
    }

    private void OnNextPairButton()
    {
        if (currentScene.name != "_config")
        {
            Debug.Log((currentPair + 1) + " " + maxPairs);
            if (currentPair < maxPairs)
			{
                currentPair++;
                currentPairsLabel.GetComponent<TextMeshProUGUI>().text = (currentPair+1).ToString();
                _OSCPacket.SendMessage("/btn/nextPair", "incrementPair");
                if (currentPair == maxPairs-1)
                {
                    NextBtn.GetComponent<Button>().interactable = true;
                    NextPairBtn.GetComponent<Button>().interactable = false;
                }
                PCSlider.value = 60;
				BtnA.onClick.Invoke();
         
            }
        }
    }

    private void SelectA()
	{
        _OSCPacket.SendButton(BtnA.name);

        var colorA = BtnA.GetComponent<Button>().colors;
        colorA.normalColor = activeColor;
        BtnA.GetComponent<Button>().colors = colorA;

        var colorB = BtnB.GetComponent<Button>().colors;
        colorB.normalColor = inactiveColor;
        BtnB.GetComponent<Button>().colors = colorB;
    }

    private void SelectB()
    {
        _OSCPacket.SendButton(BtnB.name);

        var colorA = BtnA.GetComponent<Button>().colors;
        colorA.normalColor = inactiveColor;
        BtnA.GetComponent<Button>().colors = colorA;

        var colorB = BtnB.GetComponent<Button>().colors;
        colorB.normalColor = activeColor;
        BtnB.GetComponent<Button>().colors = colorB;
    }
}
