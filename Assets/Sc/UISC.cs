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
    [SerializeField] GameObject selector;
    //[SerializeField] Dictionary<string, GameObject> icons = new Dictionary<string, GameObject>();

    public void activate_Window(int i)
    {
        //animation
            //navigation
            Tween.Scale(icons[int.Parse(selector.name)].transform, endValue: 1f, duration: 0.4f, endDelay: 0.1f);
            Tween.PositionX(selector.transform, icons[i].transform.position.x, 0.25f);
            Tween.Scale(icons[i].transform, endValue: 1.25f, duration:0.4f, endDelay: 0.1f);
            selector.name = i.ToString();
        //windows
            foreach(GameObject go in windows)
            {
                go.SetActive(false);
            }
            windows[i].SetActive(true);

    }
    public void but_ViewDetails(GameObject i)
    {
        //animation
            Tween.Scale(i.transform, 0.9f, 0.2f).OnComplete(()=>Tween.Scale(i.transform, 1f, 0.2f));
            Tween.Color(i.transform.Find("poster/img").GetComponent<Image>(),Color.gray,0.2f).OnComplete(() => Tween.Color(i.transform.Find("poster/img").GetComponent<Image>(), Color.white, 0.2f));
    }
}
