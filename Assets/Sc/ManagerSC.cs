using System.Collections;
using System.Collections.Generic;
using UnityEditor.UI;
using UnityEngine;

public class ManagerSC : MonoBehaviour
{
    [Header("----SYSTEM----")]
    public API api;
    public UISC ui;
    public NotificationSC noty;
    public LocalServer winServer;
    public DeepLinkHandler androidServer;
    void Start()
    {
        StartCoroutine(api.GetOngoingAnime());
    }
    void Update()
    {
        
    }
    public void EnterToShiki()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            androidServer.StartAuthorization();
        }
        else if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
        {
            winServer.StartAuthorization();
        }
    }
}
