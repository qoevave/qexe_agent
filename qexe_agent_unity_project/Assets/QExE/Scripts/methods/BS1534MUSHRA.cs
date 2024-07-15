using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using extOSC;

public class BS1534MUSHRA : MonoBehaviour
{
	public GameObject ConditionSets;

	public Button ReferenceButton;

	public Button[] ConditionButtons;

	public GameObject[] ConditionSliders;

	public GameObject NextButton;

	public GameObject PlayButton;

	public int numberOfconditions;

	private TestManager _testmanager;

	private OSCPacket _OSCPacket;

	private bool initialized = false;

	private int currentbutton = 0;

	private bool oneSliderAt100 = true;

	private bool playButtonPressed = true;

	private bool startingColors = false;

	Color activeColor = new Color(0.2039216f, 0.4784314f, 0.3568628f, 1.0f);
	Color inactiveColor = new Color(0.4156863f, 0.4156863f, 0.4156863f, 1.0f);

	private void Awake()
	{
		activeColor = gameObject.GetComponent<UIColorManager>().primaryButtonColor;
		inactiveColor = gameObject.GetComponent<UIColorManager>().disabledButtonColor;

	}

	private void OnEnable()
	{
		if (!initialized)
			intialization();
	}


	public void SetInterface(int NumberOfConditions)
	{
		float max_w = 64f;
		float division_w = max_w / NumberOfConditions;
		float left_w = max_w / -2;

		ReferenceButton.onClick.AddListener(() => selectCondition(-1));

		for (int i =0; i<ConditionSliders.Length; i++)
		{
			ConditionSliders[i].SetActive(i < NumberOfConditions ? true : false);
			ConditionButtons[i].gameObject.SetActive(i < NumberOfConditions ? true : false);

			if (ConditionSliders[i].activeSelf)
			{
				ConditionSliders[i].transform.localPosition = new Vector3(left_w + (max_w / (NumberOfConditions * 2)) + (i * division_w), 0, 0);
				ConditionButtons[i].gameObject.transform.localPosition = new Vector3(left_w + (max_w / (NumberOfConditions * 2)) + (i * division_w), -19.429f, 0);
				int index = i;
				ConditionButtons[i].onClick.AddListener(() => selectCondition(index));
			}	
		}
	}

	private void intialization()
	{
		_testmanager = GameObject.Find("TestManager").GetComponent<TestManager>();
		_OSCPacket = this.GetComponent<OSCPacket>();
		numberOfconditions = _testmanager.NumberOfConditions;
		SetInterface(numberOfconditions);
		initialized = true;

		// Set the reference as the starting condition.
		var colors = ReferenceButton.GetComponent<Button>().colors;
		colors.normalColor = activeColor;
		ReferenceButton.GetComponent<Button>().colors = colors;

		PlayButton.GetComponent<Button>().onClick.AddListener(() => OnStartPlayback());
		NextButton.GetComponent<Button>().interactable = false;

		playButtonPressed = false;
		oneSliderAt100 = true;

	}

	private void selectCondition(int index)
	{
		if(index==-1)
		{
			_OSCPacket.SendButton(ReferenceButton.name);
			currentbutton = -1;
			var colors = ReferenceButton.GetComponent<Button>().colors;
			colors.normalColor = activeColor;
			ReferenceButton.GetComponent<Button>().colors = colors;
		}
		else
		{
			currentbutton = index;
			_OSCPacket.SendButton(ConditionButtons[index].name);
			var colors = ReferenceButton.GetComponent<Button>().colors;
			colors.normalColor = inactiveColor;
			ReferenceButton.GetComponent<Button>().colors = colors;
		}

		UpdateSelectColour(currentbutton);
	}

	private void UpdateSelectColour(int index)
	{
		for(int i = 0; i < numberOfconditions; i++) 
		{
			if (i == index)
			{
				var colors = ConditionButtons[i].GetComponent<Button>().colors;
				colors.normalColor = activeColor;
				ConditionButtons[i].GetComponent<Button>().colors = colors;
				ConditionSliders[i].GetComponent<Slider>().interactable = true;
			}
			else
			{
				var colors = ConditionButtons[i].GetComponent<Button>().colors;
				colors.normalColor = inactiveColor;
				ConditionButtons[i].GetComponent<Button>().colors = colors;
				ConditionSliders[i].GetComponent<Slider>().interactable = false;
			}
		}
	}

	private void OnStartPlayback()
	{
		playButtonPressed = true;
		PlayButton.gameObject.SetActive(false);
	}


	private void Update()
	{
		for(int i=0; i <numberOfconditions; i++)
		{
			if(ConditionSliders[i].GetComponent<Slider>().value > 95)
			{
				oneSliderAt100 = true;
				break;
			}
			else
			{
				oneSliderAt100 = false;
			}
		}

		if(oneSliderAt100 == true && playButtonPressed == true)
		{
			NextButton.GetComponent<Button>().interactable = true;
		}
		else
		{
			NextButton.GetComponent<Button>().interactable = false;
		}

		if (!startingColors)
			SetColors();
	}

	private void SetColors()
	{
		var colors = ReferenceButton.GetComponent<Button>().colors;
		colors.normalColor = activeColor;
		ReferenceButton.GetComponent<Button>().colors = colors;

		for(int i = 0; i < numberOfconditions; i++)
		{
			colors = ConditionButtons[i].GetComponent<Button>().colors;
			colors.normalColor = inactiveColor;
			ConditionButtons[i].GetComponent<Button>().colors = colors;
		}
		startingColors = true;
	}
}
