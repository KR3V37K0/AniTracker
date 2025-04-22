using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Threading;
using System.Threading.Tasks;
using System;
using UnityEngine.UI;
using System.Globalization;
using PrimeTween;


public class CalendarSC : MonoBehaviour
{
    [Header("MAIN")]
    ManagerSC manager;
    [SerializeField] TMP_Text txt_thisMonth, txt_detailed;
    CultureInfo russian = CultureInfo.GetCultureInfo("ru-RU");
    private void Awake()
    {
        manager = GetComponent<ManagerSC>();
        manager.calendar = manager.GetComponent<CalendarSC>();

        fill_Page();
        StartCoroutine( ShowCalendar(today.Year, today.Month));

        selectedDate = DateTime.Today;
    }

    async Task fill_Page()
    {
        update_Details();
        txt_thisMonth.text = selectedDate.ToString("MMMM yyyy", russian);
    }
    async Task update_Details()
    {
        txt_detailed.text = FormatDateWithRelativeDay(selectedDate, russian);
    }
    static string FormatDateWithRelativeDay(DateTime date, CultureInfo culture)
    {
        string datePart = date.ToString("d MMMM", culture);

        if (date.Date == DateTime.Today)
            return $"{datePart}, сегодня";
        else if (date.Date == DateTime.Today.AddDays(1))
            return $"{datePart}, завтра";
        else if (date.Date == DateTime.Today.AddDays(2))
            return $"{datePart}, послезавтра";
        else if (date.Date == DateTime.Today.AddDays(-1))
            return $"{datePart}, вчера";
        else if (date.Date > DateTime.Today)
            return $"{datePart}, через {(int)(date-DateTime.Today).TotalDays} дн.";
        else if (date.Date < DateTime.Today)
            return $"{datePart}, {(int)(DateTime.Today-date).TotalDays} дн. назад";
        else
            return datePart;
    }



    [Header("CELLS")]
    [SerializeField] Transform calendarGrid;
    [SerializeField] GameObject dayCellPrefab;
    [SerializeField] GameObject selector;
    //public Text monthLabel;

    private DateTime today = DateTime.Today;
    private DateTime selectedDate;
    bool first = true;
    GameObject today_cell;

    IEnumerator ShowCalendar(int year, int month)
    {
        foreach (Transform child in calendarGrid)
            Destroy(child.gameObject);

        DateTime firstDay = new DateTime(year, month, 1);
        int daysInMonth = DateTime.DaysInMonth(year, month);
        int startOffset = ((int)firstDay.DayOfWeek + 6) % 7; // чтобы понедельник = 0

        DateTime startDate = firstDay.AddDays(-startOffset);

        for (int i = 0; i < 42; i++)
        {
            DateTime date = startDate.AddDays(i);
            GameObject cellObj = Instantiate(dayCellPrefab, calendarGrid);
            DayCell cell = cellObj.GetComponent<DayCell>();
            cell.Setup(date, date.Month == month, date == today, date == selectedDate, OnDaySelected);

            if(date == today)
            {
                today_cell = cell.gameObject;
            }
            else if (last_day.Month != selectedDate.Month)
            {
                first = true;
                if(selectedDate==date)
                    today_cell = cell.gameObject;
            }
            
        }
        if (first)
        {
            first = false;
            yield return new WaitForSeconds(1f);
            OnDaySelected(selectedDate, today_cell);
        }
    }


    [Header("BUTTON")]
    GameObject last_btn;
    DateTime last_day;
    void OnDaySelected(DateTime date, GameObject btn)
    {
        selectedDate = date;
        StartCoroutine(ShowCalendar(date.Year, date.Month));

        last_btn = btn;
        

        StartCoroutine(Animate_Selector(btn,date));
        last_day = date;

        fill_Page();
    }
    IEnumerator Animate_Selector(GameObject btn,DateTime date)
    {
        yield return Tween.Position(selector.transform, btn.transform.position, 0.4f);
        selector.GetComponentInChildren<TMP_Text>().text = date.Day + "";
    }
}
