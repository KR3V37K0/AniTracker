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
    public GameObject my_canvas;
    [SerializeField] TMP_Text txt_thisMonth, txt_detailed;
    CultureInfo russian = CultureInfo.GetCultureInfo("ru-RU");

    [Header("DETAILS")]
    [SerializeField] Transform container_Details;
    [SerializeField] GameObject pref_Anime;
    private void Awake()
    {
        Init();
    }
    async Task Init()
    {
        manager = GetComponent<ManagerSC>();
        manager.calendar = manager.GetComponent<CalendarSC>();

        //await Task.Delay(3000);
        await fill_Page();
        StartCoroutine(ShowCalendar(today.Year, today.Month));

        selectedDate = DateTime.Today;
    }

    async Task fill_Page()
    {
        await update_Details();
        txt_thisMonth.text = selectedDate.ToString("MMMM yyyy", russian);
    }
    async Task update_Details()
    {
        txt_detailed.text = FormatDateWithRelativeDay(selectedDate, russian);

        manager.ui.DeleteChildren(container_Details);
        foreach (EpisodeInfo info in await GetEpisodesOnDate())
        {
            await create_Anime_Detail(info);
        }
    }
    async Task create_Anime_Detail(EpisodeInfo info)
    {
        while (manager == null)
        {
            await Task.Delay(100);
        }
        while (manager.noty == null)
        {
            await Task.Delay(100);
        }
        while (manager.noty.trackes_anime == null)
        {
            await Task.Delay(100);
        }
        Anime anime = manager.noty.trackes_anime.Find(ani => ani.id == (info.AnimeId+""));
        GameObject obj = Instantiate(pref_Anime, container_Details);
        obj.GetComponentsInChildren<TMP_Text>()[0].text=anime.russian;
        obj.GetComponentsInChildren<TMP_Text>()[1].text = info.EpisodeNumber+" из "+info.track.all;
        if (anime.poster != null)
        {
            Sprite spri = null;
            StartCoroutine(manager.api.DownloadImage(anime.poster.originalUrl, (sprite) => spri = sprite));
            while (spri == null) { await Task.Delay(100); }
            obj.transform.Find("poster/img").GetComponent<Image>().sprite = spri;
            anime.sprite = spri;
        }
        obj.GetComponent<Button>().onClick.AddListener(()=>manager.ui.but_ViewDetails(obj, anime));


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
            dayCellPrefab.name = "daycel_" + date.Day;
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
        yield return new WaitForSeconds(0.5f);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(calendarGrid as RectTransform);
        if (first)
        {
            first = false;

            // Подождать один кадр, чтобы layout успел обновиться
            yield return new WaitForSeconds(0.5f);

            OnDaySelected(selectedDate, today_cell);

            yield return new WaitForSeconds(2f);
        }
    }


    [Header("BUTTON")]
    GameObject last_btn;
    DateTime last_day;
    async void OnDaySelected(DateTime date, GameObject btn)
    {
        if(!my_canvas.activeSelf)return;
        selectedDate = date;
        StartCoroutine(ShowCalendar(date.Year, date.Month));

        last_btn = btn;
        

        
        last_day = date;

        StartCoroutine(Animate_Selector(btn, date));

        await fill_Page();
    }
    IEnumerator Animate_Selector(GameObject btn,DateTime date)
    {
        MobileDebug.Log("CAlendar set date: " + date +" on position "+ btn.gameObject.name+" : "+btn.gameObject.transform.position);
        yield return Tween.Position(selector.transform, btn.transform.position, 0.4f);
        selector.GetComponentInChildren<TMP_Text>().text = date.Day + "";
    }
    public async Task<List<EpisodeInfo>> GetEpisodesOnDate()
    {
        while (manager.noty.trackers == null) await Task.Delay(100);

        List<Tracker> trackers = manager.noty.trackers;
        List<EpisodeInfo> result = new List<EpisodeInfo>();

        foreach (var tracker in trackers)
        {
            int daysDiff = (selectedDate.Date - tracker.start.Date).Days;

            if (daysDiff < 0) continue; // еще не вышло

            if (daysDiff % 7 == 0)
            {
                int episodeNumber = daysDiff / 7 + 1;

                if (episodeNumber <= tracker.all)
                {
                    result.Add(new EpisodeInfo(tracker.id_Anime, episodeNumber, selectedDate,tracker));
                }
            }
        }
        return result;
    }
    public class EpisodeInfo
    {
        public int AnimeId;
        public int EpisodeNumber;
        public DateTime Date;
        public Tracker track;

        public EpisodeInfo(int animeId, int episodeNumber, DateTime date, Tracker track)
        {
            AnimeId = animeId;
            EpisodeNumber = episodeNumber;
            Date = date;
            this.track = track;
        }
    }

}
