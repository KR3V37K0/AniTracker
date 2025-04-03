using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Linq;
using UnityEngine.Analytics;
using System;

public class UI_Search : MonoBehaviour
{
    ManagerSC manager;
    [SerializeField] GameObject container;
    [SerializeField] GameObject obj_checkbox, obj_pan_list, list_container;
    [SerializeField] Sprite[] triple_check;

    [Header("----Genres and Themes----")]
    [SerializeField] Transform view_Genres;
    [SerializeField] Transform view_Themes;

    public List<GenreData> genresList;

    bool splited_GaT;
    Query_Search query = new Query_Search();



    private void Start()
    {
        manager = gameObject.GetComponent<ManagerSC>();
        manager.ui_search = gameObject.GetComponent<UI_Search>();
    }

    //GENRES
    public async void getGenres()
    {
        Task<List<GenreData>> gTask = manager.api.Get_GenresList();
        while (!gTask.IsCompleted)
        {
            await Task.Yield();
        }
        genresList = gTask.Result.OrderBy(g => g.russian).ToList();
        Fill_GaT();
    }
    void Fill_GaT()
    {
        GameObject obj;
        foreach (GenreData g in genresList)
        {
            if (g.kind == "theme")
            {
                obj=Instantiate(obj_checkbox, view_Themes);
            }
            else
            {
                obj = Instantiate(obj_checkbox, view_Genres);
            }
            obj.GetComponentInChildren<TMP_Text>().text = g.russian;
            obj.name = g.id;
            obj.GetComponent<Toggle>().onValueChanged.AddListener((isOn) => check_genre(g.id,isOn));
        }
        //view_Genres.Find("btn_Split").SetSiblingIndex(genres.Length);

        view_Genres.GetComponent<ContentSizeFitter>().verticalFit= ContentSizeFitter.FitMode.Unconstrained;
        view_Themes.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.Unconstrained;

        RectTransform rect = view_Genres.parent.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(rect.rect.width, 406f);
        splited_GaT = false;
    }
    public void btn_Split_GaT(GameObject btn)
    {
        RectTransform rect = view_Genres.parent.GetComponent<RectTransform>();
        if (splited_GaT)
        {
            btn.GetComponent<TMP_Text>().text = "развернуть";

            view_Genres.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            view_Genres.GetComponent<RectTransform>().sizeDelta=new Vector2(465f, 288f);
            view_Themes.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            view_Themes.GetComponent<RectTransform>().sizeDelta = new Vector2(465f, 288f);
           
            rect.sizeDelta = new Vector2(rect.rect.width, 406f);
            splited_GaT = false;
        }
        else
        {
            btn.GetComponent<TMP_Text>().text = "свернуть";

            view_Genres.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            view_Themes.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            rect.sizeDelta = new Vector2(rect.rect.width, (view_Themes.childCount)
                * (obj_checkbox.GetComponent<RectTransform>().sizeDelta.y + view_Themes.GetComponent<VerticalLayoutGroup>().spacing) + 80f);

            splited_GaT = true;
            
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(container.GetComponent<RectTransform>());
        

    }

    //DATA
    public void btn_Enter()
    {
        Debug.Log( query.apply());
        ConnectionData.currentSearch = query;
        StartCoroutine(manager.api.SearchResult());
    }
    public void input_search(TMP_InputField txt)
    {
        query.search = txt.text;
    }
    string[] sort_value=new string[] {"ranked", "popularity", "name", "aired_on", "random", "status", "episodes"};
    public void drop_sort(TMP_Dropdown drop)
    {
        query.order = sort_value[drop.value];
    }
    public void check_type(Toggle toggle)
    {
        if (toggle.isOn)
        {
            query.kind.Add(toggle.name);
        }
        else query.kind.Remove(toggle.name);
    }
    public void check_status(Toggle toggle)
    {
        if (toggle.isOn)
        {
            query.status.Add(toggle.name);
        }
        else query.status.Remove(toggle.name);
    }
    public void slider_score(Slider slider)
    {        
        query.score =Mathf.Clamp(slider.value,1,10);
    }
    public void slider_year(float left, float right)
    {
        query.season=left+"_"+right;
        if(left<1980) query.season = "ancient," + left + "_" + right;
    }
    public void check_rating(Toggle toggle)
    {
        query.censure = "true";
        if (toggle.isOn)
        {
            query.rating.Add(toggle.name);
            if (toggle.name == "r_plus,rx") query.censure = "false";
        }
        else query.rating.Remove(toggle.name);
    }
    public void check_genre(string name, bool on)
    {        
        if (on)
        {  
            query.genre.Add(genresList.Find(genre => genre.id == name));
        }
        else query.genre.Remove(genresList.Find(genre => genre.id == name));
    }
    //LIST
    public void fill_List_toSearch()
    {
        manager.ui.DeleteChildren(list_container.transform);
        foreach(DB_List list in manager.ui_lists.allList)
        {
            GameObject obj = Instantiate(obj_pan_list, list_container.transform);
            obj.name = list.place+"";
            if(list.id == 0) obj.transform.Find("txt").name = manager.db.basic_List_name.FirstOrDefault(x => x.Value == list.name).Key;
            obj.GetComponentInChildren<TMP_Text>().text = list.name;
            obj.GetComponentInChildren<Button>().onClick.AddListener(() => switch_list_check(obj));

        }
        manager.ui.sort_children(list_container.transform);
    }
    public void switch_list_check(GameObject obj)
    {
        int i = int.Parse(obj.transform.GetChild(1).name)+1;
        if (i > 2)
        {
            i = 0;
            obj.transform.GetChild(1).GetChild(0).gameObject.SetActive(false);
        }
        else 
        { 
            obj.transform.GetChild(1).GetChild(0).GetComponent<Image>().sprite = triple_check[i];
            obj.transform.GetChild(1).GetChild(0).gameObject.SetActive(true);
        }
        obj.transform.GetChild(1).name = i + "";

        if (obj.transform.GetChild(0).name != "txt")
            ProcessListString(obj.transform.GetChild(0).name, i);

    }
    void ProcessListString(string listName, int operation)
    {
        if (string.IsNullOrEmpty(query.mylist))
        {
            // Если строка пустая, обрабатываем особо
            switch (operation)
            {
                case 1:
                    query.mylist = listName;
                    break;
                case 2:
                    query.mylist = $"!{listName}";
                    break;
            }
            Debug.Log("mylist: " + query.mylist);
            return;
        }

        // Разделяем строку на отдельные списки
        var lists = query.mylist.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                               .Select(x => x.Trim())
                               .ToList();

        // Обработка в зависимости от операции
        switch (operation)
        {
            case 0: // Удалить список
                lists.RemoveAll(x => x.Equals(listName) || x.Equals($"!{listName}"));
                break;

            case 1: // Добавить список
                if (!lists.Contains(listName) && !lists.Contains($"!{listName}"))
                {
                    lists.Add(listName);
                }
                break;

            case 2: // Добавить/модифицировать список с "!"
                    // Удаляем обычную версию, если есть
                lists.RemoveAll(x => x.Equals(listName));

                // Добавляем версию с "!" (если еще нет)
                string excludedList = $"!{listName}";
                if (!lists.Contains(excludedList))
                {
                    lists.Add(excludedList);
                }
                break;
        }

        query.mylist = string.Join(", ", lists);
    }

}
