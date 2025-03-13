using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Threading.Tasks;
using UnityEditor.Search;
using System.ComponentModel;
using System.Linq;
using UnityEngine.Analytics;

public class UI_Search : MonoBehaviour
{
    ManagerSC manager;
    [SerializeField] GameObject container;
    [SerializeField] GameObject obj_checkbox;

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
            obj.name = g.name;
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
        StartCoroutine(manager.api.SearchResult(query.apply()));
    }
    public void input_search(TMP_InputField txt)
    {
        query.search = txt.text;
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
    //LIST


}
