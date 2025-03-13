using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SliderVisual : MonoBehaviour
{
    [SerializeField]TMP_Text txt;
    [SerializeField] Slider slider;

    public void view()
    {
        txt.text = slider.value+"";

    }
}
