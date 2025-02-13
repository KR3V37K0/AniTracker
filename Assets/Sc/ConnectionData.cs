
using System;
using Unity.VisualScripting;
using UnityEngine;

public class ConnectionData:MonoBehaviour
{
    public static event Action OnUserAuthenticated;
    public static string code = "";

    public static string CLIENT_ID = "9sx1ahR0clhL6lWvED6gjeLlKRcsABJAj9SwW33BFMM";
    public static string CLIENT_SECRET = "xkOW1ufttEbgksfUJE2owqYlSD_aX4d1T0OhUqxBhCA";

    public static string CALLBACK_WINDOWS = "http://localhost:5000/callback";
    public static string CALLBACK_ANDROID = "myanimeapp://callback";

    public static string URL_WINDOWS = "https://shikimori.one/oauth/authorize?client_id=9sx1ahR0clhL6lWvED6gjeLlKRcsABJAj9SwW33BFMM&response_type=code&redirect_uri=http://localhost:5000/callback";
    public static string URL_ANDROID = "https://shikimori.one/oauth/authorize?client_id=9sx1ahR0clhL6lWvED6gjeLlKRcsABJAj9SwW33BFMM&redirect_uri=myanimeapp://callback&response_type=code";

    public static TokenResponse TOKEN = new TokenResponse();


    [System.Serializable]
    public class TokenResponse
    {
        public string access_token;
        public string refresh_token;
        public int expires_in;
        public string token_type;
    }

    public static void Succes()
    {
        TOKEN.access_token = PlayerPrefs.GetString("access_token");
        TOKEN.refresh_token = PlayerPrefs.GetString("refresh_token");
        
        PlayerPrefs.Save();
        OnUserAuthenticated?.Invoke();
    }
}
