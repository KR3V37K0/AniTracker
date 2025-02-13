using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PrimeTween;
using UnityEngine.UI;

public class UISC : MonoBehaviour
{
    //NAVIGATION
    [SerializeField] GameObject[]icons=new GameObject[5];
    [SerializeField] GameObject[] windows = new GameObject[5];
    [SerializeField] GameObject details;
    [SerializeField] GameObject selector;
    [SerializeField] GameObject popupEnter;

    private void Start()
    {   
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
                .ChainCallback(() => activate_Window(0));
    }
    public void show_popupEnter()
    {
        Sequence.Create(cycles: 1)
            .Group(Tween.Scale(popupEnter.transform, 0.0f, 0.01f))
            .ChainCallback(() => popupEnter.SetActive(true))
            .Chain(Tween.Scale(popupEnter.transform, 1f, 0.4f));
    }
}
