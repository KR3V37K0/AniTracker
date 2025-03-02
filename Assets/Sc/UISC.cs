using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PrimeTween;
using UnityEngine.UI;
using TMPro;

public class UISC : MonoBehaviour
{
    ManagerSC manager;
[Header("----NAVIGATION----")]
    [SerializeField] GameObject popupEnter;
    [SerializeField] GameObject popupLoading;
[Header("----NAVIGATION----")]
    [SerializeField] GameObject panel_Navigation;
    [SerializeField] GameObject[]icons=new GameObject[6];
    [SerializeField] GameObject[] windows = new GameObject[6];
    [SerializeField] GameObject details;
    [SerializeField] GameObject selector;

    int active;

[Header("---HOME--")]
    public string onHome;
    [SerializeField] Transform home_slot;
    [SerializeField] GameObject panelAnime;

[Header("---DETAILS--")]
    [SerializeField] GameObject Scroll;
    [SerializeField] TMP_Text txt_name;
    [SerializeField] TMP_Text txt_nameEng;
    [SerializeField] Image img_poster;
    [SerializeField] Button btn_Details_List;
    [SerializeField] TMP_Text txt_type;
    [SerializeField] TMP_Text txt_series;
    [SerializeField] TMP_Text txt_status;
    [SerializeField] TMP_Text txt_year;
    [SerializeField] TMP_Text txt_genres;
    [SerializeField] TMP_Text txt_themes;
    [SerializeField] TMP_Text txt_age;
    [SerializeField] Image img_stars;
    [SerializeField] TMP_Text txt_stars;
    [SerializeField] Transform arr_studios;
    [SerializeField] TMP_Text txt_description;
    [SerializeField] Transform arr_authors;
    [SerializeField] Transform arr_screens;
    [SerializeField] Transform arr_related;
    [SerializeField] Transform arr_similar;

    [SerializeField] GameObject obj_people;
    [SerializeField] GameObject obj_screen;
    [SerializeField] GameObject obj_studio;
    [SerializeField] GameObject obj_miniAnime;
    [SerializeField] GameObject obj_miniAnimeRelated;
    [SerializeField] GameObject obj_TextButton;

    [Header("---SETTINGS--")]
    [SerializeField] Button btn_Enter;


    private void Start()
    {
        panel_Navigation.SetActive(true);
        manager = gameObject.transform.GetComponent<ManagerSC>();
        details.SetActive(false);
        activate_Window(0);
    }
    public void activate_Window(int i)
    {
        //animation
            Sequence.Create(cycles: 1)
                .Group(Tween.Scale(icons[int.Parse(selector.name)].transform, endValue: 1f, duration: 0.4f, endDelay: 0.1f))
                .Group(Tween.PositionX(selector.transform, icons[i].transform.position.x, 0.25f))
                .Group(Tween.Scale(icons[i].transform, endValue: 1.25f, duration: 0.4f, endDelay: 0.1f))
                .ChainCallback(() => selector.name = i.ToString())
                .ChainCallback(() => Swap_Windows(i));
        //logic       
            switch(i)
            {
                case 0://HOME
                if(active==0)
                {
                    if (details.activeSelf)
                    {
                        Sequence.Create(cycles: 1)
                            .Group(Tween.PositionX(details.transform, -1*Screen.width / 100f, 0.6f))
                            .ChainCallback(() => details.SetActive(false));
                    }
                }
                else if (active == 5)
                {
                    Sequence.Create(cycles: 1)
                        .Group(Tween.PositionX(details.transform, -1*Screen.width/100f, 0.05f))
                        .ChainCallback(() => details.SetActive(true))
                        .Chain(Tween.PositionX(details.transform, 0f, 0.6f));
                }
                break;

                case 1://SEARCH

                break;

                case 2: //LISTS

                break;

                case 3://CALENDAR

                break;

                case 4://SETTIGS 
                if (PlayerPrefs.HasKey("access_token")) btn_Enter.interactable = false;
                else btn_Enter.interactable = true;
                break;

            }
            if (active == 5) Tween.PositionY(Scroll.transform, 0f, 2f);
            active = i;     
    }

    private void Swap_Windows(int i)
    {
        foreach (GameObject go in windows)
        {
            go.SetActive(false);
        }
        windows[i].SetActive(true);
    }
    public void but_ViewDetails(GameObject i,Anime anime)
    {
        StartLoad();
        //animation
        Sequence.Create(cycles: 1)
            .Group(Tween.Scale(i.transform, 0.9f, 0.2f))
            .Group(Tween.Color(i.transform.Find("poster/img").GetComponent<Image>(), Color.gray, 0.2f))
            .Chain(Tween.Scale(i.transform, 1f, 0.2f))
            .Group(Tween.Color(i.transform.Find("poster/img").GetComponent<Image>(), Color.white, 0.2f))
            .ChainCallback(() => details.SetActive(true))
            .ChainCallback(()=>active=5)
            .ChainCallback(() => activate_Window(0));

        StartCoroutine(manager.api.getDetails(anime));
            
        
    }
    public void show_popupEnter()
    {
        Sequence.Create(cycles: 1)
            .Group(Tween.Scale(popupEnter.transform, 0.0f, 0.01f))
            .ChainCallback(() => popupEnter.SetActive(true))
            .Chain(Tween.Scale(popupEnter.transform, 1f, 0.3f));
    }
    public IEnumerator Anime_to_Home(Anime Data,Sprite sprite,int number)
    {
        Data.sprite = sprite;

        Transform anime = Instantiate(panelAnime.transform, home_slot);
        anime.name = number+":"+Data.id;
        anime.transform.Find("poster/img").GetComponent<Image>().sprite=sprite;
        anime.transform.Find("txt").GetComponent<TMP_Text>().text = Data.russian;
        anime.GetComponent<Button>().onClick.AddListener(() => but_ViewDetails(anime.gameObject,Data));
        anime.SetSiblingIndex(number);
        sort_children(home_slot);
        yield return null;   
    }
    private void sort_children(Transform parent)
    {
        foreach (Transform child in parent)
        {
            child.SetSiblingIndex(fromName(child.gameObject.name,"state"));
        }
    }
    private int fromName(string me,string what)
    {
        string[] words = me.Split(':');
        switch (what)
        {
            case ("state"):
                return(int.Parse(words[0]));               

            case ("id"):
                return (int.Parse(words[1]));               
        }
        return -1;
    }
    public void ViewDetails(AnimeDetails details)
    {
        StopLoad();
        //List BUTT
        btn_Details_List.onClick.RemoveAllListeners();
        btn_Details_List.onClick.AddListener(() => butt_Anime_in_List(details.main.id));

        //MAIN
        txt_name.text = details.main.russian;
        txt_nameEng.text = details.main.name;
        img_poster.sprite=details.main.sprite;
        txt_type.text = details.kind;
        txt_series.text=details.episodesAired+" / ";
        if (details.episodes == 0) txt_series.text += "??";
        else txt_series.text += details.episodes;

        txt_status.text=details.status;
        txt_year.text=details.airedOn.date;

        //GENRES
        /*txt_genres.text = "";
        txt_themes.text = "";
        foreach(Genre g in details.genres)
        {
            if (g.kind == "genre") txt_genres.text += g.russian + "   ";
            else if (g.kind == "theme") txt_themes.text += g.russian + "   ";
        }*/
        DeleteChildren(txt_genres.transform);
        DeleteChildren(txt_themes.transform);
        foreach (Genre g in details.genres)
        {
            if (g.kind == "genre") 
            {
                GameObject obj = Instantiate(obj_TextButton, txt_genres.transform);
                obj.name = g.id+"";
                obj.transform.GetComponent<TMP_Text>().text=g.russian;
                obj.transform.GetComponent<Button>().onClick.AddListener(() => butt_SearchGenre(obj));
            }
            else if (g.kind == "theme")
            {
                GameObject obj = Instantiate(obj_TextButton, txt_themes.transform);
                obj.name = g.id+"";
                obj.transform.GetComponent<TMP_Text>().text = g.russian;
                obj.transform.GetComponent<Button>().onClick.AddListener(() => butt_SearchGenre(obj));
            }
        }
        ReloadContainer(txt_genres.gameObject);
        ReloadContainer(txt_themes.gameObject);



        txt_age.text = details.rating;

        //SCORE
        img_stars.fillAmount = (float)details.score / 10f;
        txt_stars.text = details.score+"";

        //STUDIOS
        DeleteChildren(arr_studios);
        foreach (Studio s in details.studios)
        {
            GameObject g = Instantiate(obj_studio, arr_studios);
            if(s.imageUrl==null) g.GetComponentInChildren<TMP_Text>().text=s.name;
            else
            {
                g.GetComponentInChildren<Image>().color = Color.white;
                StartCoroutine(waitImages(() => s.sprite, g.GetComponentInChildren<Image>()));
            }           
        }
        ReloadContainer(arr_studios.gameObject);

        //DESCRIPTON
        txt_description.text = details.description;
        if (details.description == null) txt_description.text = "нет описания";
        ReloadContainer(txt_description.gameObject);

        //PERSON
        DeleteChildren(arr_authors);
        int count = 1;
        foreach (PersonRole r in details.personRoles)
        {
            if((r.rolesRu.Contains("Автор оригинала"))|| (r.rolesRu.Contains("Главный продюсер"))|| (r.rolesRu.Contains("Композитор гл. муз. темы"))|| (r.rolesRu.Contains("Композитор гл. муз. темы"))|| (r.rolesRu.Contains("Музыка")))
            {
                if (count > 9) break;
                GameObject g = Instantiate(obj_people, arr_authors);
                if (r.person.poster != null) StartCoroutine(waitImages(() => r.person.poster.sprite, g.GetComponentInChildren<Image>()));
                g.transform.Find("txt_name").GetComponent<TMP_Text>().text = r.person.name;
                foreach (string rank in r.rolesRu)
                {
                    g.transform.Find("txt_rank").GetComponent<TMP_Text>().text += rank + "  ";
                }
                count++;

            }
        }
        ReloadContainer(arr_authors.gameObject);

        //SCREENSHOTS
        DeleteChildren(arr_screens);
        count = 1;
        foreach (Screenshot s in details.screenshots)
        {
            if (count > 9) break;
            GameObject g = Instantiate(obj_screen, arr_screens);
            StartCoroutine(waitImages(()=>s.sprite,g.GetComponent<Image>()));
            count++;
        }

        //RELATES


        //SIMILAR

    }
    public void ViewDetails_Related(AnimeDetails details)
    {
        DeleteChildren(arr_related);
        foreach (Related re in details.related)
        {
            GameObject pan = Instantiate(obj_miniAnimeRelated, arr_related);
            pan.transform.GetChild(0).GetChild(0).GetComponent<Image>().sprite = re.anime.sprite;
            pan.transform.GetChild(1).transform.GetComponent<TMP_Text>().text = re.relationText;
            pan.transform.GetChild(2).transform.GetComponent<TMP_Text>().text = re.anime.russian;
            pan.GetComponent<Button>().onClick.AddListener(() => but_ViewDetails(pan.gameObject, re.anime));
        }
        ReloadContainer(arr_related.gameObject);
    }
    public void ViewDetails_Similar(AnimeDetails details)
    {
        DeleteChildren(arr_similar);
        Debug.Log(details.similar.Count);
        foreach (Anime sim in details.similar)
        {
            GameObject pan = Instantiate(obj_miniAnime, arr_similar);
            pan.transform.Find("poster/img").GetComponent<Image>().sprite = sim.sprite;
            pan.GetComponentInChildren<TMP_Text>().text = sim.russian;
            pan.GetComponent<Button>().onClick.AddListener(() => but_ViewDetails(pan.gameObject, sim));
        }
        ReloadContainer(arr_similar.gameObject);
    }
    IEnumerator waitImages(System.Func<Sprite> getSprite, Image slot)
    {
        while (getSprite() == null) yield return new WaitForEndOfFrame();
        ReloadContainer(arr_screens.gameObject);
        slot.sprite = getSprite();
    }
    void ReloadContainer(GameObject container)
    {
        LayoutRebuilder.ForceRebuildLayoutImmediate(container.GetComponent<RectTransform>());
        //container.transform.localScale = new Vector3(1.01f, 1.01f, 1f);
    }
    void DeleteChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
            Destroy(child.gameObject);
        }
    }
    void butt_Anime_in_List(string id)
    {
        Debug.Log(id + " в список!");
    }
    void butt_SearchGenre(GameObject b)
    {
        Debug.Log("search by " + b.GetComponent<TMP_Text>().text);
    }
    void StartLoad()
    {
        popupLoading.SetActive(true);
    }
    void StopLoad()
    {
        popupLoading.SetActive(false);
    }
    public void but_Split(GameObject container)
    {
        ContentSizeFitter fit = container.GetComponent<ContentSizeFitter>();
        RectTransform rect = container.GetComponent<RectTransform>();
        if (fit.verticalFit == ContentSizeFitter.FitMode.PreferredSize)
        {
            fit.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, 450f);
            ReloadContainer(container);
        }
        else
        {
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            ReloadContainer(container);
        }
            
    }
    public void anim_Rotate(Transform obj)
    {
        if (obj.name == "0")
        {
            obj.name = "1";
            Tween.Rotation(obj, new Quaternion(0f, 0f, -90f, 0f), 0.6f);
        }
        else 
        {
            obj.name = "0";
            Tween.Rotation(obj, new Quaternion(0f, 0f, 90f, 0f), 0.6f); 
        }
    }
}
