using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("COLORS")]
    [Space]
    [Header("Choose a style or pick custom colors")]
    public bool QExE_Colors = true;
    [Space]
    public bool customColors = false;
    [Space]
    [Header("Choose Custom Colors and pick the colors of your choice")]
    public Color primaryMenuColor = new Color(0.1803922f, 0.1803922f, 0.1803922f, 1);
    public Color secondaryMenuColor = new Color(0.2313726f, 0.2313726f, 0.2313726f, 1);
    [Space]
    public Color primaryButtonColor = new Color(0.2039216f, 0.4784314f, 0.3568628f, 1);
    public Color secondaryButtonColor = new Color(0.3361072f, 0.7803922f, 0.5803922f, 1);
    public Color highlightedButtonColor = new Color(0.3361072f, 0.7803922f, 0.5803922f, 1);
    public Color pressedButtonColor = new Color(0.1215686f, 0.2784314f, 0.2078431f, 1);
    public Color disabledButtonColor = new Color(0.3584906f, 0.3584906f, 0.3584906f, 1);
    [Space]
    public Color TextColor = new Color(0.9490196f, 0.9490196f, 0.9490196f, 1);


    private void Start()
    {
        if (QExE_Colors)
        {
            SetQExETheme();
        }
        if (customColors || customColors && QExE_Colors)
        {
            QExE_Colors = false;
            ChangeColors();
        }
        if (!QExE_Colors && !customColors)
        {
            QExE_Colors=true;
            SetQExETheme();
        }
    }

    void SetQExETheme()
    {
        primaryMenuColor = new Color(0.1803922f, 0.1803922f, 0.1803922f, 1);
        secondaryMenuColor = new Color(0.2313726f, 0.2313726f, 0.2313726f, 1);
        primaryButtonColor = new Color(0.2039216f, 0.4784314f, 0.3568628f, 1);
        secondaryButtonColor = new Color(0.3361072f, 0.7803922f, 0.5803922f, 1);
        highlightedButtonColor = new Color(0.3361072f, 0.7803922f, 0.5803922f, 1);
        pressedButtonColor = new Color(0.1215686f, 0.2784314f, 0.2078431f, 1);
        disabledButtonColor = new Color(0.3584906f, 0.3584906f, 0.3584906f, 1);
        TextColor = new Color(0.9490196f, 0.9490196f, 0.9490196f, 1);
    }

    public void ChangeColors()
    {
        var colormanagers = gameObject.GetComponentsInChildren<UIColorManager>();
        for (int i = 0; i < colormanagers.Length; i++)
        {
            colormanagers[i].primaryMenuColor = primaryMenuColor;
            colormanagers[i].secondaryMenuColor = secondaryMenuColor;
            colormanagers[i].primaryButtonColor = primaryButtonColor;
            colormanagers[i].secondaryButtonColor = secondaryButtonColor;
            colormanagers[i].highlightedButtonColor = highlightedButtonColor;
            colormanagers[i].pressedButtonColor = pressedButtonColor;
            colormanagers[i].disabledButtonColor = disabledButtonColor;
            colormanagers[i].textColor = TextColor;
        }
    }
}
