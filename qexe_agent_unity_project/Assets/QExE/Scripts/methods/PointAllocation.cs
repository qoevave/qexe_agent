using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class PointAllocation : MonoBehaviour
{
    public float allocatedPoints = 0;

    public GameObject allocatedPointsText;

    public float totalPoints = 21;

    public GameObject totalPointsText;

    public Slider[] attributeSliders;

    private bool initialized = false;

    private float value;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void UpdateAllocatedPoints(Slider changedSlider)
	{
        float tempSum = 0;

        for (int i = 0; i < attributeSliders.Length; i++)
        {
            tempSum += attributeSliders[i].value;
		}


        if (tempSum > totalPoints)
        {
            float excess = tempSum - totalPoints;
            changedSlider.value = changedSlider.value - excess;
            tempSum = 21;
        }


        allocatedPoints = tempSum;
        allocatedPointsText.GetComponent<TextMeshProUGUI>().text = allocatedPoints.ToString();
        
/*
        if(allocatedPoints >= totalPoints)
		{

		}*/

	}
}
