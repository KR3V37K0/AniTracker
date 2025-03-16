using System.Collections;
using UnityEngine;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;

public class A_Starter : MonoBehaviour
{
    ManagerSC manager;
    [SerializeField]GameObject servers;
    private void Awake()
    {
        servers.SetActive(false);
        manager = GetComponent<ManagerSC>();
        manager.hasConnection = false;
    }
    private void OnEnable()
    {
        ConnectionData.OnUserAuthenticated += GetUserInfo;
    }
    private void OnDisable()
    {
        ConnectionData.OnUserAuthenticated -= GetUserInfo;
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

            manager.ui.InternetSplash(request.result == UnityWebRequest.Result.Success);
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
    public void Connect()
    {
        manager.hasConnection=true;
        servers.SetActive(true);
        if (Application.isEditor)
        {
            servers.transform.GetChild(0).gameObject.SetActive(true);
        }
        else if (Application.platform == RuntimePlatform.Android)
        {
            servers.transform.GetChild(1).gameObject.SetActive(true);
        }


        //QUERY ONGOING
        manager.ui_search.getGenres();
        Query_Search query = new Query_Search();
        query.status.Add("ongoing");
        ConnectionData.currentSearch = query;
        StartCoroutine(manager.api.SearchResult());
        //StartCoroutine(manager.api.GetOngoingAnime());
    }
    public void noConnect()
    {
        manager.hasConnection = false;
        servers.SetActive(false);
    }

    void GetUserInfo()
    {
        StartCoroutine(GetUserInfo_IENUM(0));
    }
    IEnumerator GetUserInfo_IENUM(int tick)
    {
        if (manager.hasConnection)
        {

            string url = "https://shikimori.one/api/users/whoami";

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString("access_token")}");
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponse = request.downloadHandler.text;

                    ShikimoriUser user = JsonUtility.FromJson<ShikimoriUser>(jsonResponse);
                    manager.user = user;
                    manager.ui_settings.ViewUserInfo();
                }
                else
                {
                    Debug.LogError($"Ошибка: {request.error}");
                    PlayerPrefs.DeleteAll();
                    PlayerPrefs.SetInt("authorization_shown", 1);
                    PlayerPrefs.Save();

                    manager.ui_settings.show_popupEnter();
                }
            }          
        }
        else if (tick < 2)
        {
            StartCoroutine(GetUserInfo_IENUM(tick + 1));
        }
    }
}
