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

    public Color outOfMonthTextColor;

    private DateTime date;
    private Action<DateTime, GameObject> Click_action;
    public void Setup(DateTime d, bool inMonth, bool isToday, bool isSelected, Action<DateTime, GameObject> onClick)
    {
        date = d;
        dayText.text = d.Day.ToString();
        dayText.color = inMonth ? Color.black : outOfMonthTextColor;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => onClick(date, button.gameObject));
        Click_action=onClick;
    }
    public void Click()
    {
        MobileDebug.Log("miniSC selector  on position " + button.gameObject.transform.position);
        Click_action?.Invoke(date, button.gameObject);
    }
}

