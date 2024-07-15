using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using extOSC;

public class Elimination : MonoBehaviour
{
	public GameObject[] ConditionSets;

	public Button[] ConditionButtons;

	public Button[] EliminationButtons;

	private Button btnNext;

	public int numberOfconditions;

	public GameObject playButton;

	public GameObject NextButton;

	private TestManager _testmanager;

	private OSCPacket _OSCPacket;

	private bool initialized = false;

	private bool startingColors = false;

	private int currentbutton = 0;

	private int eliminationCount = 0;

	Color activeColor = new Color(0.2039216f, 0.4784314f, 0.3568628f, 1.0f);
	Color inactiveColor = new Color(0.4156863f, 0.4156863f, 0.4156863f, 1.0f);

	private void Start()
	{
		activeColor = gameObject.GetComponent<UIColorManager>().primaryButtonColor;
		inactiveColor = gameObject.GetComponent<UIColorManager>().disabledButtonColor;

	}

	private void OnEnable()
	{
		if (!initialized)
			intialization();
	}

	private void Update()
	{
		if (!startingColors)
			UpdateSelectColour(0);
	}


	public void SetInterface(int NumberOfConditions)
	{
		float max_w = 64f;
		float division_w = max_w / NumberOfConditions;
		float left_w = max_w / -2;

		for (int i =0; i<ConditionSets.Length; i++)
		{
			ConditionSets[i].SetActive(i < NumberOfConditions ? true : false);

			if (ConditionSets[i].activeSelf)
			{
				ConditionSets[i].transform.localPosition = new Vector3(left_w + (max_w / (NumberOfConditions * 2)) + (i * division_w), 0, 0);
				int index = i;
				ConditionButtons[i].onClick.AddListener(() => selectCondition(index));
				EliminationButtons[i].onClick.AddListener(() => eliminateCondition(index));
			}	
		}
	}

	private void intialization()
	{
		_testmanager = GameObject.Find("TestManager").GetComponent<TestManager>();
		_OSCPacket = this.GetComponent<OSCPacket>();
		numberOfconditions = _testmanager.NumberOfConditions;
		SetInterface(numberOfconditions);
		eliminationCount = 0;

		playButton.GetComponent<Button>().onClick.AddListener(() => OnStartPlayback());

		initialized = true;
	}


	private void OnStartPlayback()
	{
		NextButton.GetComponent<Button>().interactable = true;
		playButton.gameObject.SetActive(false);
	}


	private void selectCondition(int index)
	{
		currentbutton = index;
		_OSCPacket.SendButton(ConditionButtons[index].name);

		UpdateSelectColour(index);
	}

	private void eliminateCondition(int index)
	{
		if (!playButton.activeInHierarchy)
		{
			_OSCPacket.SendButton(EliminationButtons[index].name);
			ConditionSets[index].SetActive(false);
			eliminationCount += 1;
			if(index == currentbutton)
			{
				for(int i = 0; i <= numberOfconditions; i++)
				{
					if (ConditionSets[i].activeSelf)
					{
						currentbutton = i;
						selectCondition(i);
						break;
					}
				}
			}
			StartCoroutine(checkRemainingCondition());
		}
	}


	private void UpdateSelectColour(int index)
	{
		for (int i = 0; i < numberOfconditions; i++)
		{
			if (i == index)
			{
				var colors = ConditionButtons[i].GetComponent<Button>().colors;
				colors.normalColor = activeColor;
				ConditionButtons[i].GetComponent<Button>().colors = colors;
				//ConditionButtons[i].GetComponent<Image>().color = activeColor;
			}
			else
			{
				var colors = ConditionButtons[i].GetComponent<Button>().colors;
				colors.normalColor = inactiveColor;
				ConditionButtons[i].GetComponent<Button>().colors = colors;
				//ConditionButtons[i].GetComponent<Image>().color = inactiveColor;
			}
		}
		startingColors = true;
	}



	IEnumerator checkRemainingCondition()
	{
		yield return new WaitForSeconds(0.5f);

		if (eliminationCount == numberOfconditions - 1)
		{
			for (int i = 0; i < numberOfconditions; i++)
			{
				if (ConditionSets[i].activeSelf)
				{
					_OSCPacket.SendButton(EliminationButtons[i].name);
					ConditionSets[i].SetActive(false);
				}
					
			}
		}
	}
}
