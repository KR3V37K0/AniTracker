using System;
using UnityEngine;
using System.Collections;
using System.Net;
using System.Threading;
using UnityEngine.Networking;
using System.Collections.Concurrent; // Добавляем поддержку потокобезопасной очереди
using Unity.VisualScripting;

public class LocalServer : MonoBehaviour
{
    private HttpListener _listener;
    private Thread _listenerThread;

    private ConcurrentQueue<string> authCodeQueue = new ConcurrentQueue<string>(); // Очередь для передачи кода в главный поток

    void Start()
    {
        _listenerThread = new Thread(StartServer);
        _listenerThread.Start();
        Application.OpenURL(ConnectionData.URL_WINDOWS);
    }

    void StartServer()
    {
        _listener = new HttpListener();
        _listener.Prefixes.Add("http://localhost:5000/"); // Локальный адрес
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
        string code = context.Request.QueryString["code"];  // Получаем код авторизации

        if (!string.IsNullOrEmpty(code))
        {
            authCodeQueue.Enqueue(code); // Добавляем код в очередь, чтобы обработать в главном потоке
        }

        context.Response.ContentType = "text/html";
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
        context.Response.OutputStream.Write(buffer, 0, buffer.Length);
        context.Response.OutputStream.Close();
    }

    void Update()
    {
        // Обрабатываем очередь кодов в главном потоке
        while (authCodeQueue.TryDequeue(out string authCode))
        {
            StartCoroutine(GetAccessToken(authCode));
        }
    }

    IEnumerator GetAccessToken(string authorizationCode)
    {
        string url = "https://shikimori.one/oauth/token";

        // Данные для запроса в строковом формате
        string postData = $"grant_type=authorization_code" +
                          $"&client_id={ConnectionData.CLIENT_ID}" +
                          $"&client_secret={ConnectionData.CLIENT_SECRET}" +
                          $"&code={authorizationCode}" +
                          $"&redirect_uri={ConnectionData.CALLBACK_WINDOWS}";

        byte[] postDataBytes = System.Text.Encoding.UTF8.GetBytes(postData);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(postDataBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("? Access Token получен!");
                string json = request.downloadHandler.text;
                ConnectionData.TOKEN = JsonUtility.FromJson<ConnectionData.TokenResponse>(json);

                PlayerPrefs.SetString("access_token", ConnectionData.TOKEN.access_token);
                PlayerPrefs.SetString("refresh_token", ConnectionData.TOKEN.refresh_token);
                PlayerPrefs.SetString("token_expiry", (Time.time + 86400).ToString());

                ConnectionData.code = authorizationCode;
                ConnectionData.Succes();
            }
            else
            {
                Debug.LogError($"? Ошибка получения токена: {request.error}");
                Debug.LogError($"Ответ сервера: {request.downloadHandler.text}");
            }
        }
    }

    void OnApplicationQuit()
    {
        _listener.Stop();
        _listenerThread.Abort();
    }
}
