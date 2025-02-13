using System;
using UnityEngine;
using System.Collections;
using System.Net;
using System.Threading;
using UnityEngine.Networking;
using System.Collections.Concurrent;

public class LocalServer : MonoBehaviour
{
    [SerializeField] ManagerSC manager;
    private HttpListener _listener;
    private Thread _listenerThread;
    private ConcurrentQueue<string> authCodeQueue = new ConcurrentQueue<string>();

    void Start()
    {
        // Проверяем, есть ли сохраненный токен
        if (PlayerPrefs.HasKey("access_token") && PlayerPrefs.HasKey("token_expiry"))
        {
            float expiryTime = float.Parse(PlayerPrefs.GetString("token_expiry"));

            if (Time.time < expiryTime)
            {
                // ? Токен еще действителен
                ConnectionData.TOKEN.access_token = PlayerPrefs.GetString("access_token");
                Debug.Log("? Используем сохраненный токен.");
                ConnectionData.Succes();
                return;
            }
            else
            {
                // ?? Токен истек, пробуем обновить
                StartCoroutine(RefreshToken());
                return;
            }
        }

        // Проверяем, предлагали ли авторизацию ранее
        if (PlayerPrefs.GetInt("authorization_shown", 0) == 0)
        {
            PanelAuthorize();
        }
    }

    void PanelAuthorize()
    {
        Debug.Log("?? Предлагаем пользователю авторизоваться.");
        PlayerPrefs.SetInt("authorization_shown", 1);
        PlayerPrefs.Save();

        manager.ui.show_popupEnter();
    }

    public void StartAuthorization()
    {
        Debug.Log("?? Открываем браузер для авторизации.");
        _listenerThread = new Thread(StartServer);
        _listenerThread.Start();
        Application.OpenURL(ConnectionData.URL_WINDOWS);
    }

    void StartServer()
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add("http://localhost:5000/");
        _listener.Start();

        while (true)
        {
            var context = _listener.GetContext();
            HandleRequest(context);
        }
    }

    void HandleRequest(HttpListenerContext context)
    {
        string responseString = "Авторизация успешна!";
        string code = context.Request.QueryString["code"];

        if (!string.IsNullOrEmpty(code))
        {
            authCodeQueue.Enqueue(code);
        }

        context.Response.ContentType = "text/html";
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
        context.Response.OutputStream.Close();
    }

    void Update()
    {
        while (authCodeQueue.TryDequeue(out string authCode))
        {
            StartCoroutine(GetAccessToken(authCode));
        }
    }

    IEnumerator GetAccessToken(string authorizationCode)
    {
        string url = "https://shikimori.one/oauth/token";

        WWWForm form = new WWWForm();
        form.AddField("grant_type", "authorization_code");
        form.AddField("client_id", ConnectionData.CLIENT_ID);
        form.AddField("client_secret", ConnectionData.CLIENT_SECRET);
        form.AddField("code", authorizationCode);
        form.AddField("redirect_uri", ConnectionData.CALLBACK_WINDOWS);

        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                ConnectionData.TOKEN = JsonUtility.FromJson<ConnectionData.TokenResponse>(json);
                
                PlayerPrefs.SetString("access_token", ConnectionData.TOKEN.access_token);
                PlayerPrefs.SetString("refresh_token", ConnectionData.TOKEN.refresh_token);
                PlayerPrefs.SetString("token_expiry", (Time.time + ConnectionData.TOKEN.expires_in).ToString());

                Debug.Log("? Access Token получен!");
                ConnectionData.Succes();
            }
            else
            {
                Debug.LogError($"? Ошибка получения токена: {request.error}");
                Debug.LogError($"Ответ сервера: {request.downloadHandler.text}");
            }
        }
    }

    IEnumerator RefreshToken()
    {
        string refreshToken = PlayerPrefs.GetString("refresh_token");

        if (string.IsNullOrEmpty(refreshToken))
        {
            Debug.Log("? Нет refresh_token, требуется повторная авторизация.");
            PanelAuthorize();
            yield break;
        }

        string url = "https://shikimori.one/oauth/token";
        WWWForm form = new WWWForm();
        form.AddField("grant_type", "refresh_token");
        form.AddField("client_id", ConnectionData.CLIENT_ID);
        form.AddField("client_secret", ConnectionData.CLIENT_SECRET);
        form.AddField("refresh_token", refreshToken);

        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                ConnectionData.TOKEN = JsonUtility.FromJson<ConnectionData.TokenResponse>(json);

                PlayerPrefs.SetString("access_token", ConnectionData.TOKEN.access_token);
                PlayerPrefs.SetString("refresh_token", ConnectionData.TOKEN.refresh_token);
                PlayerPrefs.SetString("token_expiry", (Time.time + ConnectionData.TOKEN.expires_in).ToString());

                Debug.Log("? Токен успешно обновлен.");
                ConnectionData.Succes();
            }
            else
            {
                Debug.Log("? Ошибка обновления токена, требуется повторная аутентификация.");
                PanelAuthorize();
            }
        }
    }

    void OnApplicationQuit()
    {
        if (_listener != null)
        {
            _listener.Stop();
            _listenerThread.Abort();
        }
    }
}
