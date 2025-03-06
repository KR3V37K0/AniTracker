using System.Collections;
using UnityEngine;
using System;
using System.Threading.Tasks;
using UnityEditor.Search;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;

public class A_Starter : MonoBehaviour
{
    ManagerSC manager;
    private void Awake()
    {
        manager = GetComponent<ManagerSC>();
    }
    private void Start()
    {
        StartCoroutine(CheckInternetConnection());
    }
    IEnumerator CheckInternetConnection()
    {
        using (UnityWebRequest request = UnityWebRequest.Get("https://shikimori.one"))
        {
            request.timeout = 2; 
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Интернет доступен.");
                Connect();
            }
            else
            {        
                Debug.Log("Интернет недоступен. Ошибка: " + request.error);
                noConnect();
            }
        }
    }
    void Connect()
    {
        // manager.api.EnterToShiki();
        StartCoroutine(manager.api.GetOngoingAnime());
    }
    void noConnect()
    {

    }


}
