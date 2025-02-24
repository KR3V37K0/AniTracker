using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;

public class API : MonoBehaviour
{
    ManagerSC manager;
    private string QL = "https://shikimori.one/api/graphql";

    void Start()
    {
       
        manager=gameObject.transform.GetComponent<ManagerSC>();
    }

    public IEnumerator GetOngoingAnime()
    {
        string query = @"
        query {
            animes(status: ""ongoing"", limit: 15, order: ranked) {
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

        using (UnityWebRequest request = new UnityWebRequest(QL, "POST"))
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
                            =>StartCoroutine( manager.ui.Anime_to_Home(anime, sprite,localIndex))));
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
    public async Task<string> ToAPIAsync(string query)
    {
        var jsonRequest = new JObject
        {
            ["query"] = query
        };

        string jsonRequestStr = jsonRequest.ToString();

        using (UnityWebRequest request = new UnityWebRequest(QL, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonRequestStr);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("User-Agent", "AniTracker");

            var operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield(); // Ждем завершения запроса
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                Debug.Log(jsonResponse);
                return jsonResponse;
            }
            else
            {
                Debug.LogError("Ошибка запроса: " + request.error);
                throw new System.Exception("Ошибка запроса: " + request.error);
            }
        }
    }
    public IEnumerator getDetails(Anime main)
    {
        //AnimeDetails details = new AnimeDetails();
        //details.mainData = main;

        string query = $@"
        query {{
            animes(ids:""{main.id}"") 
            {{
                kind
                episodes
                episodesAired
                status
                genres {{ id name russian kind }}
                rating
                score
                studios {{ id name imageUrl }}
                description
                personRoles {{
                  id
                  rolesRu
                  person {{ id name poster {{ id }} }}
                }}
                screenshots {{ originalUrl }}
                related {{
                      anime {{
                        id
                        name
                      }}
                      relationText
                    }}
            }}
         }}";
        Task<string> apiTask = ToAPIAsync(query);
        while (!apiTask.IsCompleted)
        {
            yield return null; // Ждем завершения задачи
        }
        if (apiTask.IsFaulted)
        {
            Debug.LogError("Ошибка: " + apiTask.Exception.Message);
        }
        else
        {
            detailResponse response = JsonConvert.DeserializeObject<detailResponse>(apiTask.Result);
            AnimeDetails details = response.data.animes[0];
            details.main = main;
            //Debug.Log(d_details.data.animes[0].related[0].anime.name);
            foreach(Studio s in details.studios)
                StartCoroutine(DownloadImage(s.imageUrl,
                    (sprite) => s.sprite=sprite));
            foreach (Screenshot s in details.screenshots)
                StartCoroutine(DownloadImage(s.originalUrl,
                    (sprite) => s.sprite = sprite));

// ДОБАВИТЬ ПОСТЕРЫ ЛЮДЕЙ
// ДОБАВИТЬ ДОП ИНФУ О СВЯЗАННЫХ
// ДОБАВИТЬ СВЯЗАННЫЕ
            manager.ui.ViewDetails(details);
        }
        yield return null;
    }
    private async Task<string> GetSimilar(int animeId)
    {
        using (HttpClient client = new HttpClient())
        {
            // Устанавливаем заголовок User-Agent (обязательно для Shikimori API)
            client.DefaultRequestHeaders.Add("User-Agent", "YourAppName");

            // Отправляем GET-запрос
            HttpResponseMessage response = await client.GetAsync($"https://shikimori.one/api/animes/{animeId}/similar");
            return await response.Content.ReadAsStringAsync();
        }
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
            }
            else
            {
                Debug.LogError("Ошибка загрузки постера: " + request.error+" "+url);
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

