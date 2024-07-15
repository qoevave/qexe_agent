using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.Linq;

public class UIColorManager : MonoBehaviour
{
    public Color primaryMenuColor;
    public Color secondaryMenuColor;
    [Space]
    public Color primaryButtonColor;
    public Color secondaryButtonColor;
    public Color highlightedButtonColor;
    public Color pressedButtonColor;
    public Color disabledButtonColor;
    [Space]
    public Color textColor;
    [Space]
    [Space]
    public Transform[] primaryMenu;
    public Transform[] secondaryMenu;
    [Space]
    public Transform[] primaryButton;
    public Transform[] secondaryButton;
    [Space]
    public Transform[] textUI;
    public Material textMat;

    private void Start()
    {
        //Set Colors chosen 
        gameObject.GetComponentInParent<UIManager>().ChangeColors();

        //Find all Children with the Tag "UI_PrimaryMenu" and add them to the array
        primaryMenu = gameObject.GetComponentsInChildren<Transform>();
        primaryMenu = primaryMenu.Where(child => child.tag == "UI_PrimaryMenu").ToArray();

        if (primaryMenu != null)
        {
            for (var a = 0; a < primaryMenu.Length; a++)
            {
                //check which kind of component the gameobject has and change its color
                if (primaryMenu[a].GetComponent<Image>() != null)
                {
                    primaryMenu[a].GetComponent<Image>().color = primaryMenuColor;
                }
                if (primaryMenu[a].GetComponent<Button>() != null)
                {
                    var colors = primaryMenu[a].GetComponent<Button>().colors;
                    colors.normalColor = primaryMenuColor;
                    colors.highlightedColor = highlightedButtonColor;
                    colors.pressedColor = pressedButtonColor;   
                    colors.disabledColor = disabledButtonColor;
                    primaryMenu[a].GetComponent<Button>().colors = colors;
                }
                if (primaryMenu[a].GetComponent<Slider>() != null)
                {
                    var colors = primaryMenu[a].GetComponent<Slider>().colors;
                    colors.normalColor = primaryMenuColor;
                    colors.highlightedColor = highlightedButtonColor;
                    colors.pressedColor = pressedButtonColor;
                    colors.disabledColor = disabledButtonColor;
                    primaryMenu[a].GetComponent<Slider>().colors = colors;
                }
                else
                {
                    Debug.Log("No Color Data");
                }
            }
        }

        //Find all Children with the Tag "UI_SecondaryMenu" and add them to the array
        secondaryMenu = gameObject.GetComponentsInChildren<Transform>();
        secondaryMenu = secondaryMenu.Where(child => child.tag == "UI_SecondaryMenu").ToArray();

        if (secondaryMenu != null)
        {
            for (var b = 0; b < secondaryMenu.Length; b++)
            {
                //check which kind of component the gameobject has and change its color
                if (secondaryMenu[b].GetComponent<Image>() != null)
                {
                    secondaryMenu[b].GetComponent<Image>().color = secondaryMenuColor;
                }
                if (secondaryMenu[b].GetComponent<Button>() != null)
                {
                    var colors = secondaryMenu[b].GetComponent<Button>().colors;
                    colors.normalColor = secondaryMenuColor;
                    colors.highlightedColor = highlightedButtonColor;
                    colors.pressedColor = pressedButtonColor;
                    colors.disabledColor = disabledButtonColor;
                    secondaryMenu[b].GetComponent<Button>().colors = colors;
                }
                if (secondaryMenu[b].GetComponent<Slider>() != null)
                {
                    var colors = secondaryMenu[b].GetComponent<Slider>().colors;
                    colors.normalColor = secondaryMenuColor;
                    colors.highlightedColor = highlightedButtonColor;
                    colors.pressedColor = pressedButtonColor;
                    colors.disabledColor = disabledButtonColor;
                    secondaryMenu[b].GetComponent<Slider>().colors = colors;
                }
                else
                {
                    Debug.Log("No Color Data");
                }
            }
        }

        //Find all Children with the Tag "UI_PrimaryButton" and add them to the array
        primaryButton = gameObject.GetComponentsInChildren<Transform>();
        primaryButton = primaryButton.Where(child => child.tag == "UI_PrimaryButton").ToArray();

        if (primaryButton != null)
        {
            for (var c = 0; c < primaryButton.Length; c++)
            {
                //check which kind of component the gameobject has and change its color
                if (primaryButton[c].GetComponent<Image>() != null)
                {
                    primaryButton[c].GetComponent<Image>().color = primaryButtonColor;
                }
                if (primaryButton[c].GetComponent<Button>() != null)
                {
                    var colors = primaryButton[c].GetComponent<Button>().colors;
                    colors.normalColor = primaryButtonColor;
                    colors.highlightedColor = highlightedButtonColor;
                    colors.pressedColor = pressedButtonColor;
                    colors.disabledColor = disabledButtonColor;
                    primaryButton[c].GetComponent<Button>().colors = colors;

                }
                if (primaryButton[c].GetComponent<Slider>() != null)
                {
                    var colors = primaryButton[c].GetComponent<Slider>().colors;
                    colors.normalColor = primaryButtonColor;
                    colors.highlightedColor = highlightedButtonColor;
                    colors.pressedColor = pressedButtonColor;
                    colors.disabledColor = disabledButtonColor;
                    primaryButton[c].GetComponent<Slider>().colors = colors;
                }
                else
                {
                    Debug.Log("No Color Data");
                }
            }
        }

        //Find all Children with the Tag "UI_SecondaryButton" and add them to the array
        secondaryButton = gameObject.GetComponentsInChildren<Transform>();
        secondaryButton = secondaryButton.Where(child => child.tag == "UI_SecondaryButton").ToArray();
        if (secondaryButton != null)
        {
            for (var d = 0; d < secondaryButton.Length; d++)
            {
                //check which kind of component the gameobject has and change its color
                if (secondaryButton[d].GetComponent<Image>() != null)
                {
                    secondaryButton[d].GetComponent<Image>().color = secondaryButtonColor;
                }
                if (secondaryButton[d].GetComponent<Button>() != null)
                {
                    var colors = secondaryButton[d].GetComponent<Button>().colors;
                    colors.normalColor = secondaryButtonColor;
                    colors.highlightedColor = highlightedButtonColor;
                    colors.pressedColor = pressedButtonColor;
                    colors.disabledColor = disabledButtonColor;
                    secondaryButton[d].GetComponent<Button>().colors = colors;
                }
                if (secondaryButton[d].GetComponent<Slider>() != null)
                {
                    var colors = secondaryButton[d].GetComponent<Slider>().colors;
                    colors.normalColor = secondaryButtonColor;
                    colors.highlightedColor = highlightedButtonColor;
                    colors.pressedColor = pressedButtonColor;
                    colors.disabledColor = disabledButtonColor;
                    secondaryButton[d].GetComponent<Slider>().colors = colors;
                }
                else
                {
                    Debug.Log("No Color Data");
                }
            }
        }

        //Find all Children with the Tag "UI_TextColor" and add them to the array
        textUI = gameObject.GetComponentsInChildren<Transform>();
        textUI = textUI.Where(child => child.tag == "UI_TextColor").ToArray();
        textMat.SetColor("_FaceColor", textColor);

        if (textUI != null)
        {
            for (var e = 0; e < textUI.Length; e++)
            {
                //check which kind of component the gameobject has and change its color
                if (textUI[e].GetComponent<Image>() != null)
                {
                    textUI[e].GetComponent<Image>().color = textColor;
                }
                if (textUI[e].GetComponent<Button>() != null)
                {
                    var colors = textUI[e].GetComponent<Button>().colors;
                    colors.normalColor = textColor;
                    colors.highlightedColor = highlightedButtonColor;
                    colors.pressedColor = pressedButtonColor;
                    colors.disabledColor = disabledButtonColor;
                    textUI[e].GetComponent<Button>().colors = colors;
                }
                if (textUI[e].GetComponent<Slider>() != null)
                {
                    var colors = textUI[e].GetComponent<Slider>().colors;
                    colors.normalColor = textColor;
                    colors.highlightedColor = highlightedButtonColor;
                    colors.pressedColor = pressedButtonColor;
                    colors.disabledColor = disabledButtonColor;
                    textUI[e].GetComponent<Slider>().colors = colors;
                }
                else
                {
                    Debug.Log("No Color Data");
                }
            }
        }
    }
}
