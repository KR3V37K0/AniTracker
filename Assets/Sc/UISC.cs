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
    public void but_ViewDetails(GameObject i)
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
            
                
    }
    public void show_popupEnter()
    {
        Sequence.Create(cycles: 1)
            .Group(Tween.Scale(popupEnter.transform, 0.0f, 0.01f))
            .ChainCallback(() => popupEnter.SetActive(true))
            .Chain(Tween.Scale(popupEnter.transform, 1f, 0.3f));
    }
    public IEnumerator Anime_to_Home(string id,string name,string russian,Sprite sprite,int number)
    {
        Transform anime = Instantiate(panelAnime.transform, home_slot);
        anime.transform.Find("poster/img").GetComponent<Image>().sprite=sprite;
        anime.transform.Find("txt").GetComponent<TMP_Text>().text = russian;
        anime.SetSiblingIndex(number);
        Debug.Log(number+" "+russian);
        yield return null;
        
    }
}
