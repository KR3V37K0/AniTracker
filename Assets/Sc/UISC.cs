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
    [SerializeField] GameObject[]icons=new GameObject[6];
    [SerializeField] GameObject[] windows = new GameObject[6];
    [SerializeField] GameObject details;
    [SerializeField] GameObject selector;
    [SerializeField] GameObject popupEnter;
    int active;

[Header("---HOME--")]
    public string onHome;
    [SerializeField] Transform home_slot;
    [SerializeField] GameObject panelAnime;

[Header("---DETAILS--")]
    [SerializeField] TMP_Text txt_name;
    [SerializeField] TMP_Text txt_nameEng;
    [SerializeField] Image img_poster;
    [SerializeField] TMP_Text txt_type;
    [SerializeField] TMP_Text txt_series;
    [SerializeField] TMP_Text txt_status;
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

    [Header("---SETTINGS--")]
    [SerializeField] Button btn_Enter;


    private void Start()
    {
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
        txt_name.text = details.main.russian;
        txt_nameEng.text = details.main.name;
        img_poster.sprite=details.main.sprite;
        txt_type.text = details.kind;
        txt_series.text=details.episodesAired+" / ";
        if (details.episodes == 0) txt_series.text += "??";
        else txt_series.text += details.episodes;

        txt_genres.text = "";
        txt_themes.text = "";
        foreach(Genre g in details.genres)
        {
            if (g.kind == "genre") txt_genres.text += g.russian + "   ";
            else if (g.kind == "theme") txt_themes.text += g.russian + "   ";
        }

        txt_age.text = details.rating;
        img_stars.fillAmount = (float)details.score / 10f;
        txt_stars.text = details.score+"";
        //STUDIOS
        txt_description.text = details.description;
        if (details.description == null) txt_description.text = "нет описания";
        txt_description.gameObject.transform.position = txt_description.gameObject.transform.position + new Vector3(0, 0, 0);
        //PERSON
        //SCREENS
        //RELATES
        //SIMILAR
    }
}
