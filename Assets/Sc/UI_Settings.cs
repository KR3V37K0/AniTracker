using PrimeTween;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_Settings : MonoBehaviour
{
    [SerializeField] ManagerSC manager;
    [SerializeField] GameObject popupEnter,viewer;
    [SerializeField] Image img_Ava;
    [SerializeField] TMP_Text txt_name,txt_id;
    [SerializeField] Button btn_Enter;
    [SerializeField] string URL_Rules;
    private void Start()
    {
        //viewer.SetActive(false);
    }
    public void show_popupEnter()
    {
        Sequence.Create(cycles: 1)
            .Group(Tween.Scale(popupEnter.transform, 0.0f, 0.01f))
            .ChainCallback(() => popupEnter.SetActive(true))
            .Chain(Tween.Scale(popupEnter.transform, 1f, 0.3f));
    }
    public void btn_discardUser()
    {
        manager.starter.withoutOnlineUser();
    }
    public void btn_openRules()
    {
        Application.OpenURL(URL_Rules);
    }
    public void off_btn_Enter()
    {
        btn_Enter.interactable = false;
    }
    public void ViewUserInfo()
    {      
        if ((manager.user.image!=null))  StartCoroutine( manager.api.DownloadImage(manager.user.image.x160, (img) => 
            { 
                img_Ava.sprite = img;
                manager.user.sprite = img;
            }));
        txt_name.text = manager.user.nickname;
        txt_id.text = manager.user.id+"";
        viewer.SetActive(true);
        if (manager.user.id != 0) off_btn_Enter();
    }
}
