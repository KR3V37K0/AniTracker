using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class DoublSlider : MonoBehaviour
{
    [SerializeField] int min, max;
    [SerializeField] TMP_Text txt;
    [SerializeField] Slider left, right;
    [SerializeField] UI_Search search;

    public void Awake()
    {
        max= DateTime.Now.Year+3;
        min = 1979;

        left.minValue = min;
        right.minValue = min;

        left.maxValue = max;
        right.maxValue = max;

        left.value = min;
        right.value = max;
    }
    public void Update_S(bool l)
    {
        if(l)right.value = Mathf.Clamp(right.value,left.value,max);
        else left.value = Mathf.Clamp(left.value,min,right.value);

        txt.text = ("с "+left.value+" до "+right.value);
        if(left.value==min) txt.text = ("с более ранних до " + right.value);

        search.slider_year(left.value, right.value);
    }
}
