using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Numerics;

public class UI_Lists : MonoBehaviour
{
    [SerializeField] ManagerSC manager;
    [SerializeField] GameObject obj_list,obj_view, obj_toList_container, obj_toList_panel, obj_toList_shiki_panel;
    [SerializeField] Transform list_container,view_container;
    bool firstOpen=true;
    public bool hasOnline = false, hasOffline = false;

    public List<DB_List> allList=new List<DB_List>();
    public List<Changes> changes = new List<Changes>();

    private void Start()
    {
        obj_view.SetActive(false);      
    }
    public IEnumerator setup_allList()
    {
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
            fill_toList_panel();
            manager.ui_search.fill_List_toSearch();
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
        manager.ui_search.fill_List_toSearch();
        yield return null;

    }


    public void open_window(bool switched)
    {
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
        manager.ui.sort_children(list_container);

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


    public List<int> FindAnimeInLists( int animeId)
    {
        List<int> result = new List<int>();
        for (int i = 0; i < allList.Count; i++)
        {
            if (allList[i].animes.Exists(anime => anime.id == animeId))
            {
                result.Add(i); 
            }
        }
        return result;
    }

    private List<GameObject> shiki_list_panel=new List<GameObject>();
    public void fill_toList_panel()
    {
        foreach(DB_List list in allList)
        {
            Transform pan;
            if (list.place < 6)
            {
                pan = Instantiate(obj_toList_shiki_panel.transform, obj_toList_container.transform);
                shiki_list_panel.Add(pan.gameObject);
            }
            else pan = Instantiate(obj_toList_panel.transform, obj_toList_container.transform);
            pan.GetComponentInChildren<TMP_Text>().text=list.name;
            pan.GetComponent<Image>().color=list.color;
            pan.name = list.place+"";                    
        }
        manager.ui.sort_children(obj_toList_container.transform);
    }
    void control_shikiCheck(string place,bool isOn)
    {
        foreach(GameObject toggle in shiki_list_panel)
        {
            if(toggle.name==place)toggle.GetComponentInChildren<Toggle>().isOn = isOn;
            else toggle.GetComponentInChildren<Toggle>().isOn = false;
        }
    }


    public AnimeDetails currentAnime;
    public void set_ToggleFor(int idA)
    {
        List<int> lists = FindAnimeInLists(idA);
        foreach (DB_List i in allList)
        {
            Toggle toggle = obj_toList_container.transform.Find(i.place + "").GetComponentInChildren<Toggle>();
            toggle.isOn = (lists.Contains(allList.IndexOf(i)));
            toggle.onValueChanged.AddListener(delegate {
                                                            change_inLists(toggle);
                                                        });
        }
        
    }
    public void change_inLists(Toggle toggle)
    {
        int n = int.Parse(toggle.gameObject.transform.parent.gameObject.name);
        if (toggle.isOn) add_inLists(n);
        else delete_inLists(n);
        if (n < 6) 
        {
            onlineChanged = true;
            control_shikiCheck(n + "", toggle.isOn); 
        }
        else offlineChanged = true;


        if (timer != null) StopCoroutine(timer);
        timer=StartCoroutine(wait_save());
    }
    void add_inLists(int place)
    {

        //addChanges(true,allList.IndexOf( allList.Find(list => list.place == place)));
        addChanges(true, place);
        DB_Anime ani = new DB_Anime(int.Parse(currentAnime.main.id), currentAnime.main.russian, currentAnime.episodesAired, currentAnime.episodes, 0);
        allList.Find(list => list.place == place).animes.Add(ani);
    }
    void delete_inLists(int list_id)
    {
        addChanges(false, allList.IndexOf(allList.Find(list => list.place == list_id)));
        allList.Find(list => list.place == list_id).animes.Remove(allList.Find(list => list.place == list_id).animes.Find(anime => (anime.id + "") == currentAnime.main.id));
    }

    int save_delay = 10;
    Coroutine timer;
    bool offlineChanged = false, onlineChanged = false;
    void addChanges(bool action,int list_index)
    {
        Changes past = changes.Find(change => change.anime.id == int.Parse(currentAnime.main.id));
        if (action)  //update or create
        {
            if (past != null) 
            { 
                past.status = "update";
//ДОДЕЛАТЬ добавить ID
                past.list_id = list_index;
                Debug.Log(past.status + " " + past.anime.name + " in " + allList[list_index].name);
            }
            else 
            {
                Changes cha = new Changes();
                cha.status = "create";
                cha.list_id = list_index;
                cha.anime=new DB_Anime(int.Parse(currentAnime.main.id), currentAnime.main.russian, currentAnime.episodesAired, currentAnime.episodes,
                    allList
                        .SelectMany(list => list.animes)  // "Разворачиваем" все аниме из всех списков
                        .FirstOrDefault(anime => anime.id == int.Parse(currentAnime.main.id)) // Ищем первое совпадение
                        ?.viewed ?? 0); // Возвращаем viewed или 0, если не найдено
                changes.Add(cha);
                Debug.Log(cha.status + " " + cha.anime.name + "  viewed: "+cha.anime.viewed + " in " + allList[list_index].name);
            }
            
        }
        else         //delete
        {
            if (past != null) past.status = "delete";
            else changes.Add(new Changes(-1,"delete", new DB_Anime(int.Parse(currentAnime.main.id))));
            Debug.Log(changes[changes.Count-1].status + " " + changes[changes.Count - 1].anime.name + " ");
        }
    }
    IEnumerator wait_save()
    {
        Debug.Log("want to save");
        yield return new WaitForSeconds(save_delay);

        foreach(Changes change in changes)
        {
            Debug.Log(change.anime.name + " in " + change.list_id);
        }

        if (offlineChanged) manager.db.save_Lists(changes);
        if (onlineChanged) manager.api.save_onlineList(changes);

        Debug.Log("SAVED SAVED SAVED SAVED SAVED");


        timer = null;
        offlineChanged = false;
        onlineChanged = false;
        changes = new List<Changes> { };
    }
}

