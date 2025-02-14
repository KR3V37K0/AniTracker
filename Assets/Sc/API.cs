using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using System;

public class API : MonoBehaviour
{
    ManagerSC manager;
    private string apiUrl = "https://shikimori.one/api/graphql";

    void Start()
    {
        manager=gameObject.transform.GetComponent<ManagerSC>();
    }

    public IEnumerator GetOngoingAnime()
    {
        string query = @"
        query {
            animes(status: ""ongoing"", limit: 4, order: ranked) {
                id
                name
                russian       
                poster { originalUrl }
            }
        }";
        var jsonRequest = new JObject
        {
            ["query"] = query
        };

        string jsonRequestStr = jsonRequest.ToString();

        using (UnityWebRequest request = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonRequestStr);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("User-Agent", "AniTracker");

            yield return request.SendWebRequest();

            if(request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                AnimeResponse response = JsonConvert.DeserializeObject<AnimeResponse>(jsonResponse);

                if (response?.data?.animes != null)
                {
                    int n = 0;
                    foreach (Anime anime in response.data.animes)
                    {
                        int localIndex = n;
                        /*
                        Debug.Log($"Название: {anime.name}");
                        Debug.Log($"Русское название: {anime.russian}");
                        Debug.Log($"ID: {anime.id}");
                        Debug.Log($"Постер: {anime.poster.originalUrl}");   
                        */
                        Debug.Log("...." + n + " " + anime.russian);
                        StartCoroutine(DownloadImage(anime.poster.originalUrl, sprite 
                            =>StartCoroutine( manager.ui.Anime_to_Home(anime.id, anime.name, anime.russian, sprite,localIndex))));
                        n++;
                        
                    }
                }
                else
                {
                    Debug.LogError("? Не удалось извлечь список аниме из JSON.");
                }
                
            }
            else
            {
                Debug.LogError("? Ошибка запроса: " + request.error);
            }
        }
    
    }
    [System.Serializable]
    public class AnimeImage
    {
        public string originalUrl;
    }

    [System.Serializable]
    public class Anime
    {
        public string id;
        public string name;
        public string russian;
        public AnimeImage poster;
    }

    IEnumerator DownloadImage(string url,Action<Sprite> callback)
    {
        //url = "https://shikimori.one" + url;
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.downloadHandler = new DownloadHandlerTexture(true); // Автоматически создает Texture2D
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                
                Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                Sprite sprite = SpriteFromTexture(texture);
                callback(sprite);
                //obj.transform.GetComponent<Image>().sprite = SpriteFromTexture(texture); // Преобразуем в Sprite и вставляем в Image
            }
            else
            {
                Debug.LogError("Ошибка загрузки постера: " + request.error);
            }
        }
    }
    private Sprite SpriteFromTexture(Texture2D texture)
    {
        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }
    public class AnimeDataWrapper
    {
        public List<Anime> animes;
    }

    public class AnimeResponse
    {
        public AnimeDataWrapper data;
    }
}
