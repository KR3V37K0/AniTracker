using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static System.Net.Mime.MediaTypeNames;

public class UI_Lists : MonoBehaviour
{
    ManagerSC manager;
    [SerializeField] GameObject obj_list,obj_view;
    [SerializeField] Transform list_container,view_container;
    bool firstOpen=true;

    List<DB_List> allList;

    private void Start()
    {
        obj_view.SetActive(false);
        manager = GetComponent<ManagerSC>();
        allList = manager.db.Get_AllLists_preview();
    }
    public void open_window(bool switched)
    {
        if (firstOpen)
        {
            foreach (DB_List list in allList)
            {
                GameObject butt = Instantiate(obj_list, list_container);
                butt.GetComponent<Button>().onClick.AddListener(() => open_list(list.id));
                butt.transform.GetComponentsInChildren<TMP_Text>()[0].text = list.name;
                butt.transform.GetComponentsInChildren<TMP_Text>()[1].text = "("+list.animes.Count+")";
            }
            firstOpen = false;
        }
        if (!switched)obj_view.SetActive(false);

    }
    public void open_list(int id)
    {
        obj_view.SetActive(true);
        obj_view.GetComponentInChildren<TMP_Text>().text= allList.Find(list => list.id == id).name;
        foreach (DB_Anime anime in allList.Find(list => list.id == id).animes)
        {
            GameObject butt = Instantiate(obj_list, view_container);
            butt.GetComponent<Button>().onClick.AddListener(() => open_list(anime.id));
            butt.transform.GetComponentsInChildren<TMP_Text>()[0].text = anime.name;

            string s = @$"{anime.series.viewved}/{anime.series.aired}";
            if (anime.series.aired != anime.series.all) s = s + @$"({anime.series.all})";
            butt.transform.GetComponentsInChildren<TMP_Text>()[1].text = s;
        }
        firstOpen = false;
    }
}
