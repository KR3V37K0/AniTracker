using PrimeTween;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_Settings : MonoBehaviour
{
    ManagerSC manager;
    [SerializeField] GameObject popupEnter,viewer;
    [SerializeField] Image img_Ava;
    [SerializeField] TMP_Text txt_name;
    [SerializeField] Button btn_Enter;
    [SerializeField] string URL_Rules;
    private void Start()
    {
        manager = GetComponent<ManagerSC>();
        viewer.SetActive(false);
    }
    public void show_popupEnter()
    {
        Sequence.Create(cycles: 1)
            .Group(Tween.Scale(popupEnter.transform, 0.0f, 0.01f))
            .ChainCallback(() => popupEnter.SetActive(true))
            .Chain(Tween.Scale(popupEnter.transform, 1f, 0.3f));
    }
    public void btn_openRules()
    {
        Application.OpenURL(URL_Rules);
    }
    public void ViewUserInfo()
    {
        if(manager.user.avatar!=null)   manager.api.DownloadImage(manager.user.avatar, (img) => img_Ava.sprite = img);
        txt_name.text = manager.user.nickname;
        viewer.SetActive(true);
        btn_Enter.interactable=false;
    }
}
