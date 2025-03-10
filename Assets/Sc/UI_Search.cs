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

    List<GenreData> genresList;
    bool splited_GaT;

    private void Start()
    {
        manager = gameObject.GetComponent<ManagerSC>();
        getGenres();
    }

    //GENRES
    async void getGenres()
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
        Debug.Log("click");
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


    //LIST


}
