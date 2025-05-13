using UnityEngine;
using System;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.Android;

public class DeepLinkHandler : MonoBehaviour
{
    [SerializeField] ManagerSC manager;
    private static string deepLinkURL;
    Coroutine en;
    private void OnEnable()
    {
        MobileDebug.Log("-- ������� ������ �����������");
        en=StartCoroutine(onEnable_cor());
    }
    IEnumerator onEnable_cor(){

        Application.deepLinkActivated += OnDeepLinkActivated;
        MobileDebug.Log("-- ������ �� �������� ������");
        yield return new WaitForSeconds(2f);
        if (PlayerPrefs.HasKey("access_token") && PlayerPrefs.HasKey("token_expiry"))
        {
            MobileDebug.Log("-- ����� �������");
                float expiryTime = float.Parse(PlayerPrefs.GetString("token_expiry"));

                if (Time.time < expiryTime)
                {
                    // ? ����� ��� ������������
                    ConnectionData.TOKEN.access_token = PlayerPrefs.GetString("access_token");
                    MobileDebug.Log("? ���������� ����������� �����.");
                    ConnectionData.Succes();
                    StopCoroutine(en);
                }
                else
                {
                    // ?? ����� �����, ������� ��������
                    StartCoroutine(RefreshToken());
                    StopCoroutine(en);
                }
        }

        // ���������, ��� �� ������������ ��������� �� �����������
        if (PlayerPrefs.GetInt("authorization_shown", 0) == 0)
        {
            PanelAuthorize();
        }
        else
        {
            manager.starter.withoutOnlineUser();
        }
    }

    void PanelAuthorize()
    {
        MobileDebug.Log("?? ���������� ������������ ��������������.");
        PlayerPrefs.SetInt("authorization_shown", 1); // ����������, ��� ����������
        PlayerPrefs.Save();

        manager.ui_settings.show_popupEnter();
    }

    public void StartAuthorization()
    {
        MobileDebug.Log("?? ��������� ������� ��� �����������.");
        Application.OpenURL(ConnectionData.URL_ANDROID);
    }

    void OnDeepLinkActivated(string url)
    {
        MobileDebug.Log("?? Deep Link Activated: " + url);
        deepLinkURL = url;
    }

    void Update()
    {
        if (!string.IsNullOrEmpty(deepLinkURL))
        {
            string code = GetQueryParameter(deepLinkURL, "code");

            if (!string.IsNullOrEmpty(code))
            {
                MobileDebug.Log("? ������� OAuth2 ���: " + code);
                deepLinkURL = null;
                StartCoroutine(GetAccessToken(code));
            }
        }
    }

    private string GetQueryParameter(string url, string paramName)
    {
        Uri uri = new Uri(url);
        string[] queryParams = uri.Query.TrimStart('?').Split('&');

        foreach (string param in queryParams)
        {
            string[] keyValue = param.Split('=');
            if (keyValue.Length == 2 && keyValue[0] == paramName)
            {
                return Uri.UnescapeDataString(keyValue[1]);
            }
        }
        return null;
    }

    IEnumerator GetAccessToken(string authorizationCode)
    {
        MobileDebug.Log("-- get access token");
        string url = "https://shikimori.one/oauth/token";

        WWWForm form = new WWWForm();
        form.AddField("grant_type", "authorization_code");
        form.AddField("client_id", ConnectionData.CLIENT_ID);
        form.AddField("client_secret", ConnectionData.CLIENT_SECRET);
        form.AddField("code", authorizationCode);
        form.AddField("redirect_uri", ConnectionData.CALLBACK_ANDROID);

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

                MobileDebug.Log("? Access Token �������!");
                ConnectionData.Succes();
            }
            else
            {
                MobileDebug.LogError($"? ������ ��������� �������: {request.error}");
                MobileDebug.LogError($"����� �������: {request.downloadHandler.text}");
            }
        }
    }

    IEnumerator RefreshToken()
    {
        string refreshToken = PlayerPrefs.GetString("refresh_token");

        if (string.IsNullOrEmpty(refreshToken))
        {
            MobileDebug.Log("? ��� refresh_token, ��������� ��������� �����������.");
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

                MobileDebug.Log("? ����� ������� ��������.");
                ConnectionData.Succes();
            }
            else
            {
                MobileDebug.Log("? ������ ���������� ������, ��������� ��������� ��������������.");
                PanelAuthorize();
            }
        }
    }
}

