using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using extOSC;

public class ACR : MonoBehaviour
{
    public Button playButton;

    public Button nextButton;

    private bool initialized = false;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    private void OnEnable()
    {
        if (!initialized)
            intialization();
    }

    public void intialization()
	{
        playButton.GetComponent<Button>().onClick.AddListener(() => OnStartPlayback());
        nextButton.GetComponent<Button>().interactable = false;
        initialized = true;
    }

    public void OnStartPlayback()
	{
        nextButton.GetComponent<Button>().interactable = true;
        playButton.gameObject.SetActive(false);
    }
}
