using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SliderButtons : MonoBehaviour
{
    public Slider slider;


    public void ButtonUp()
    {
        slider.value = slider.value + 10;
    }

    public void ButtonDown()
    {
        slider.value = slider.value - 10;
    }
}
