using UnityEngine;
using System;
using UnityEngine.Networking;
using System.Collections;
using JetBrains.Annotations;

public class DeepLinkHandler : MonoBehaviour
{
    private static string deepLinkURL; // Храним ссылку для обработки в Unity

    void Start()
    {
        
        Application.deepLinkActivated += OnDeepLinkActivated;

        if (!string.IsNullOrEmpty(Application.absoluteURL))
        {
            OnDeepLinkActivated(Application.absoluteURL);
        }
        Application.OpenURL(ConnectionData.URL_ANDROID);
    }

    void OnDeepLinkActivated(string url)
    {
        Debug.Log("?? Deep Link Activated: " + url);
        deepLinkURL = url; // Сохраняем ссылку для обработки в Update()
    }

    void Update()
    {
        if (!string.IsNullOrEmpty(deepLinkURL))
        {
            string code = GetQueryParameter(deepLinkURL, "code");

            if (!string.IsNullOrEmpty(code))
            {
                Debug.Log("? Получен OAuth2 код: " + code);
                deepLinkURL = null; // Сбрасываем ссылку после обработки
                StartCoroutine(GetAccessToken(code));
            }
        }
    }

    // Метод для парсинга параметров из URL (замена System.Web)
    private string GetQueryParameter(string url, string paramName)
    {
        Uri uri = new Uri(url);
        string[] queryParams = uri.Query.TrimStart('?').Split('&');

        foreach (string param in queryParams)
        {
            string[] keyValue = param.Split('=');
            if (keyValue.Length == 2 && keyValue[0] == paramName)
            {
                return Uri.UnescapeDataString(keyValue[1]); // Декодируем строку
            }
        }
        return null;
    }

    IEnumerator GetAccessToken(string authorizationCode)
    {
        string url = "https://shikimori.one/oauth/token";

        // Формируем запрос в правильном формате
        string postData = $"grant_type=authorization_code" +
                          $"&client_id={ConnectionData.CLIENT_ID}" +
                          $"&client_secret={ConnectionData.CLIENT_SECRET}" +
                          $"&code={authorizationCode}" +
                          $"&redirect_uri={ConnectionData.CALLBACK_ANDROID}";

        byte[] postDataBytes = System.Text.Encoding.UTF8.GetBytes(postData);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(postDataBytes);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string json = request.downloadHandler.text;
                ConnectionData.TOKEN = JsonUtility.FromJson<ConnectionData.TokenResponse>(json);

                PlayerPrefs.SetString("access_token", ConnectionData.TOKEN.access_token);
                PlayerPrefs.SetString("refresh_token", ConnectionData.TOKEN.refresh_token);
                PlayerPrefs.SetString("token_expiry", (Time.time + ConnectionData.TOKEN.expires_in).ToString());

                Debug.Log("? Access Token получен!");
                good.SetActive(true);
                ConnectionData.code = authorizationCode;              
                ConnectionData.Succes();
            }
            else
            {
                Debug.LogError($"? Ошибка получения токенов: {request.error}");
                Debug.LogError($"Ответ сервера: {request.downloadHandler.text}");
            }
        }
    }
    public GameObject good;
}
