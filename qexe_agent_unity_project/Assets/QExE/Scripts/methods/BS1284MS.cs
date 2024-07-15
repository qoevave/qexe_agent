using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using extOSC;

public class BS1284MS : MonoBehaviour
{
	public GameObject ConditionSets;

	public Button[] ConditionButtons;

	public GameObject[] ConditionSliders;

	public GameObject NextButton;

	public GameObject PlayButton;

	public int numberOfconditions;

	private TestManager _testmanager;

	private OSCPacket _OSCPacket;

	private bool initialized = false;

	private int currentbutton = 0;

	Color activeColor;
	Color inactiveColor; 

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

		PlayButton.GetComponent<Button>().onClick.AddListener(() => OnStartPlayback());
		NextButton.GetComponent<Button>().interactable = false;

		// Set the condition 1 as the starting condition. 
		UpdateSelectColour(0);
	}

	private void selectCondition(int index)
	{
		currentbutton = index;
		_OSCPacket.SendButton(ConditionButtons[index].name);

		UpdateSelectColour(currentbutton);
	}


	private void UpdateSelectColour(int index)
	{
		for (int i = 0; i < numberOfconditions; i++)
		{
			if (i == index)
			{
				ConditionButtons[i].GetComponent<Image>().color = activeColor;
				ConditionSliders[i].GetComponent<Slider>().interactable = true;
			}
			else
			{
				ConditionButtons[i].GetComponent<Image>().color = inactiveColor;
				ConditionSliders[i].GetComponent<Slider>().interactable = false;
			}
		}
	}

	private void OnStartPlayback()
	{
		NextButton.GetComponent<Button>().interactable = true;
		PlayButton.gameObject.SetActive(false);
	}
}
