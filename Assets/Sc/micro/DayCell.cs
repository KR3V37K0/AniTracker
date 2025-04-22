using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DayCell : MonoBehaviour
{
    public TMP_Text dayText;
    public Image background;
    public Button button;

   //public Color todayColor;
   // public Color selectedColor;
    //public Color normalColor;
    public Color outOfMonthTextColor;

    private DateTime date;

    public void Setup(DateTime d, bool inMonth, bool isToday, bool isSelected, Action<DateTime, GameObject> onClick)
    {
        date = d;
        dayText.text = d.Day.ToString();
        dayText.color = inMonth ? Color.black : outOfMonthTextColor;

       /* if (isToday)
            background.color = todayColor;
        else if (isSelected)
            background.color = selectedColor;
        else
            background.color = normalColor;
      */
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick(date, button.gameObject));
    }
}

