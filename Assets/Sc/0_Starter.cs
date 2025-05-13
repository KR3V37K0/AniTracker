using System.Collections;
using UnityEngine;
using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;
using Unity.VisualScripting;
using Newtonsoft.Json;
using System.Threading;

public class A_Starter : MonoBehaviour
{
    [SerializeField]ManagerSC manager;
    [SerializeField] GameObject servers;
    private void Awake()
    {
        MobileDebug.Log("starter!");
        StartSequence();
    }
    async void StartSequence()
    {
        MobileDebug.Log("ищу БД");
        await manager.db.InitializeDatabase();
        MobileDebug.Log("БД найдена");

        MobileDebug.Log("server activating");
        servers.SetActive(false);
        manager.hasConnection = false;
        MobileDebug.Log("manager init");

        if (!PlayerPrefs.HasKey("local_id")) {PlayerPrefs.SetInt("local_id", 1); MobileDebug.Log("player pref has new");}
        manager.user = new ShikimoriUser("user", PlayerPrefs.GetInt("local_id"));
        MobileDebug.Log("player pref in user is "+manager.user.nickname+" "+manager.user.local_id);




        //инфо о локале
        MobileDebug.Log("getting user from DB...");
        Task task = manager.db.get_currentUser();
        while (!task.IsCompleted)
        {
            await task;
        }
        MobileDebug.Log("Task DB complete. start Lists");
        StartCoroutine(manager.ui_lists.setup_allList());

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
                MobileDebug.Log("Интернет доступен.");
                Connect();
            }
            else
            {
                MobileDebug.Log("Интернет недоступен. Ошибка: " + request.error);
                noConnect();
            }
        }
    }
    public void Connect()
    {
        manager.hasConnection = true;
        servers.SetActive(true);
        if (Application.isEditor)
        {
            servers.transform.GetChild(0).gameObject.SetActive(true);
        }
        else if (Application.platform == RuntimePlatform.Android)
        {
            servers.transform.GetChild(1).gameObject.SetActive(true);
        }
        

    }
    public void noConnect()
    {
        manager.hasConnection = false;
        servers.SetActive(false);
        manager.ui_settings.off_btn_Enter();
        StartCoroutine(wait_connection());
    }

    void GetUserInfo()
    {
        StartCoroutine(GetUserInfo_IENUM(0));
    }
    bool waitOnline = true;
    public void withoutOnlineUser()//связан с *servers
    {
        if (waitOnline)
        {
            waitOnline = false;
            StartCoroutine(withoutAutentify());
        }
        
    }
    IEnumerator withoutAutentify()
    {
        MobileDebug.Log("используем локального юзера");
        while (true)
        {
            if(manager.hasConnection && manager.user.id>0 && manager.ui_lists.hasOffline)
            {
                getOngoing();
                manager.ui_lists.fill_toList_panel();
                break;
            }
            yield return new WaitForSeconds(0.1f);
        }
    }
    IEnumerator GetUserInfo_IENUM(int tick)
    {
        if (manager.hasConnection)
        {
            if (!online_user_send)
            {
                online_user_send = true;
                string url = "https://shikimori.one/api/users/whoami";

                using (UnityWebRequest request = UnityWebRequest.Get(url))
                {
                    request.SetRequestHeader("Authorization", $"Bearer {PlayerPrefs.GetString("access_token")}");
                    request.SetRequestHeader("Content-Type", "application/json");
                    request.SetRequestHeader("User-Agent", "AniTracker");
                    yield return request.SendWebRequest();

                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        string jsonResponse = request.downloadHandler.text;

                        ShikimoriUser user = JsonConvert.DeserializeObject<ShikimoriUser>(jsonResponse);
                        manager.user.id = user.id;
                        manager.user.avatar = user.avatar;
                        manager.user.image = user.image;
                        manager.user.nickname = user.nickname;

                        manager.db.Enqueue(manager.db.set_currentUser_info);
                        //СКАЧАТЬ СПИСКИ ОНЛАЙН
                        StartCoroutine(manager.ui_lists.setup_allList());
                        manager.ui_settings.ViewUserInfo();
                        StopCoroutine(withoutAutentify());

                        //delete
                        // manager.db.WriteUpdate();
                    }
                    else
                    {
                        MobileDebug.LogError($"Ошибка: {request.error}");
                        PlayerPrefs.DeleteAll();
                        PlayerPrefs.SetInt("authorization_shown", 1);
                        PlayerPrefs.Save();

                        manager.ui_settings.show_popupEnter();
                    }
                }
            }
        }
        else if (tick < 4)
        {
            StartCoroutine(GetUserInfo_IENUM(tick + 1));
        }
    }


    bool ongoing_send = false;
    bool online_user_send = false;
    public void getOngoing()
    {
        if (!ongoing_send)
        {
            ongoing_send = true;
            //QUERY ONGOING
            manager.ui_search.getGenres();
            Query_Search query = new Query_Search();
            query.status.Add("ongoing");
            ConnectionData.currentSearch = query;
            StartCoroutine(manager.api.SearchResult());
            //StartCoroutine(manager.api.GetOngoingAnime());
        }
    }

    IEnumerator wait_connection()
    {
        using (UnityWebRequest request = UnityWebRequest.Get("https://shikimori.one"))
        {
            request.timeout = 2;
            yield return request.SendWebRequest();

            manager.ui.InternetSplash(request.result == UnityWebRequest.Result.Success);
            if (request.result == UnityWebRequest.Result.Success)
            {
                MobileDebug.Log("Интернет доступен.");
                Connect();
            }
            else
            {
                MobileDebug.Log("Интернет недоступен. Ошибка: " + request.error);
                yield return new WaitForSeconds(5f);
                StartCoroutine(wait_connection());
            }
        }
    }
}
