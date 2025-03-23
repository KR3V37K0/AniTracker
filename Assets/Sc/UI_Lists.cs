using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static System.Net.Mime.MediaTypeNames;
using System.Threading.Tasks;
using System.Linq;
using UnityEditor.VersionControl;

public class UI_Lists : MonoBehaviour
{
    [SerializeField] ManagerSC manager;
    [SerializeField] GameObject obj_list,obj_view;
    [SerializeField] Transform list_container,view_container;
    bool firstOpen=true,hasOnline=false,hasOffline=false;

    public List<DB_List> allList=new List<DB_List>();

    private void Start()
    {
        obj_view.SetActive(false);      
    }
    public IEnumerator setup_allList()
    {
        Debug.Log("online " + hasOnline); Debug.Log("offline " + hasOffline);

        if (hasOnline == false) yield return StartCoroutine(get_Online());
        if (hasOffline == false) yield return StartCoroutine(get_Offline());

        yield return null;
    }
    IEnumerator get_Online()
    {    
        //ONLINE LISTS
        if ((manager.hasConnection) && (manager.user.id != 0))
        {
            foreach (string s in manager.db.basic_List_name.Keys)
            {
                Task<List<DB_Anime>> apiTask= apiTask = manager.api.getList(s);

                while (!apiTask.IsCompleted)
                {
                    yield return new WaitForSeconds(0.1f);
                    if (apiTask.IsFaulted)
                    {
                        Debug.LogError("Задача завершена с ошибкой: " + apiTask.Exception.Message);
                        yield return new WaitForSeconds(0.3f);
                        apiTask = manager.api.getList(s);
                    }
                }
                DB_List current = new DB_List(0, manager.db.basic_List_name[s], Color.white, manager.db.basic_List_name.Keys.ToList().IndexOf(s));

                current.animes = apiTask.Result;
                allList.Add(current);
            }
            hasOnline = true;
            firstOpen = true;
            manager.starter.getOngoing();
        }
        yield return null;
    }
    IEnumerator get_Offline()
    {
        //OFFLINE LISTS
        Task<List<DB_List>> dbTask = manager.db.Get_AllLists_preview();
        while (!dbTask.IsCompleted)
        {
            yield return new WaitForEndOfFrame();
        }
        foreach (DB_List current in dbTask.Result)
        {
            current.place += 6;
            allList.Add(current);
        }
        hasOffline = true;
        firstOpen = true;
        yield return null;
    }
    public void open_window(bool switched)
    {
        Debug.Log(allList.Count);
        if (firstOpen)
        {
            manager.ui.DeleteChildren(list_container);
            foreach (DB_List list in allList)
            {
                GameObject butt = Instantiate(obj_list, list_container);
                butt.gameObject.name = list.place+"";
                butt.GetComponent<Button>().onClick.AddListener(() => open_list(list.place));
                butt.transform.GetComponentsInChildren<TMP_Text>()[0].text = list.name;
                butt.transform.GetComponentsInChildren<TMP_Text>()[1].text = "("+list.animes.Count+")";
            }
            manager.ui.sort_children(list_container);
            firstOpen = false;
        }
        if (!switched)obj_view.SetActive(false);

    }
    public void open_list(int place)
    {
        manager.ui.DeleteChildren(view_container);
        obj_view.SetActive(true);
        obj_view.GetComponentInChildren<TMP_Text>().text= allList.Find(list => list.place == place).name;
        foreach (DB_Anime anime in allList.Find(list => list.place == place).animes)
        {
            GameObject butt = Instantiate(obj_list, view_container);
            butt.GetComponent<Button>().onClick.AddListener(() => manager.ui.but_ViewDetails(butt,new Anime(anime.id+"")));
            butt.transform.GetComponentsInChildren<TMP_Text>()[0].text = anime.name;

            string s = @$"{anime.aired}";
            if (anime.viewed != 0) s = anime.viewed + "/" + s;
            if (anime.aired != anime.all) s = s + @$"({anime.all})";
            butt.transform.GetComponentsInChildren<TMP_Text>()[1].text = s;
        }
        firstOpen = false;
    }
}
