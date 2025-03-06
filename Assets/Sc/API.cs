using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using UnityEngine.UI;
using System.Linq;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

public class API : MonoBehaviour
{
    ManagerSC manager;
    private string QL = "https://shikimori.one/api/graphql";
    private string Similar = "https://shikimori.one/api/animes/id/similar";
    private string url_Images = "https://shikimori.one";
    private const int BatchSize = 19;

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
                        //Debug.Log("...." + n + " " + anime.russian);
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
    public async Task<string> ToAPIAsync(string query,string url)
    {
        var jsonRequest = new JObject
        {
            ["query"] = query
        };

        string jsonRequestStr = jsonRequest.ToString();

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
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
                airedOn {{ year month day date }}
                genres {{ id name russian kind }}
                rating
                score
                studios {{ id name imageUrl }}
                description
                personRoles {{
                  id
                  rolesRu
                  person {{ id name poster {{ id originalUrl }} }}
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
        Task<string> apiTask = ToAPIAsync(query,QL);
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
            //ПОЧИСТИТЬ ОПИСАНИЕ
            details.description = RemoveTextInBrackets(details.description);
            foreach (Studio s in details.studios)
                StartCoroutine(DownloadImage(s.imageUrl,
                    (sprite) => s.sprite=sprite));
            details.screenshots=details.screenshots.OrderBy(x => Guid.NewGuid()).ToList();
            foreach (Screenshot s in details.screenshots)
                StartCoroutine(DownloadImage(s.originalUrl,
                    (sprite) => s.sprite = sprite));
            foreach (PersonRole p in details.personRoles)
            {
                if (p.person.poster != null)
                {
                    StartCoroutine(DownloadImage(p.person.poster.originalUrl,
                        (sprite) => p.person.poster.sprite = sprite));
                }
            }
            manager.ui.ViewDetails(details);

            // ДОБАВИТЬ ДОП ИНФУ О СВЯЗАННЫХ
            List<string> a = new List<string>();
            foreach (Related r in details.related)
            {                        
                if (r.anime != null)
                {
                    a.Add(r.anime.id);
                }
            }
            yield return StartCoroutine(Get_BIGminiAnime(a, (o) =>
            {
                for (int i = details.related.Count - 1; i >= 0; i--)
                {
                    if (details.related[i].anime == null)
                    {
                        details.related.RemoveAt(i);
                    }
                }
                for (int i = 0; i < details.related.Count; i++)
                {
                    foreach (Anime ani in o)
                    {
                        if (details.related[i].anime.id == ani.id)
                        {
                            details.related[i].anime = ani;
                        }
                    }
                }
            }));
            foreach (Related r in details.related)
            {
                if (r.anime != null)
                {
                    yield return StartCoroutine(DownloadImage(r.anime.poster.originalUrl, (sprite) => r.anime.sprite = sprite));
                }
            }
            manager.ui.ViewDetails_Related(details);

            // ДОБАВИТЬ ПОХОЖИЕ
            Task<List<SimilarAnime>> SimTask = GetSimilar(details.main.id);
            while (!SimTask.IsCompleted)
            {
                yield return null;
            }
            if (SimTask.Result != null)
            {
                a = new List<string>();
                foreach (SimilarAnime s in SimTask.Result)
                {
                    if (s != null)
                    {
                        a.Add(s.id);
                    }
                }
                yield return StartCoroutine(Get_BIGminiAnime(a,
                    (o) =>
                    {
                        details.similar = new List<Anime>();
                        for (int i = 0; i < o.Count; i++)
                        {
                            details.similar.Add(o[i]);
                        }
                    }));
                foreach (Anime r in details.similar)
                {
                    yield return StartCoroutine(DownloadImage(r.poster.originalUrl,
                        (sprite) => r.sprite = sprite));
                }
                manager.ui.ViewDetails_Similar(details);
            }


        }
        yield return null;
    }
    private async Task<List<SimilarAnime>> GetSimilar(string animeId)
    {
        using (HttpClient client = new HttpClient())
        {
            // Устанавливаем заголовок User-Agent (обязательно для Shikimori API)
            client.DefaultRequestHeaders.Add("User-Agent", "AniTracker");

            // Отправляем GET-запрос
            HttpResponseMessage response = await client.GetAsync($"https://shikimori.one/api/animes/{animeId}/similar");

            if (response.IsSuccessStatusCode)
            {
                // Читаем JSON-ответ
                string jsonResponse = await response.Content.ReadAsStringAsync(); 
                return JsonConvert.DeserializeObject<List<SimilarAnime>>(jsonResponse);
            }
            else
            {
                // Обработка ошибок
                throw new Exception($"Ошибка запроса: {response.StatusCode} - {response.ReasonPhrase}");
            }
        }
    }
    IEnumerator DownloadImage(string url,Action<Sprite> callback)
    {
        
        if (url != null)
        {
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
                    Debug.LogError("Ошибка загрузки постера: " + request.error + " " + url);
                }
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
    public class AnimeDataWrapperAlias
    {
        public Dictionary<string, List<Anime>> animes;
    }
    public class AnimeResponseAlias
    {
        public Dictionary<string, AnimeDataWrapper> data;
    }
    IEnumerator Get_miniAnime(string id, Action<Anime> callback)
    {
        string query = $@"
        query {{
            animes(ids:""{id}"") 
            {{  
                id
                name
                russian       
                poster {{ originalUrl }}
            }}
         }}";
        Task<string> apiTask = ToAPIAsync(query,QL);
        while (!apiTask.IsCompleted)
        {
            yield return null; 
        }
        AnimeResponse response = JsonConvert.DeserializeObject<AnimeResponse>(apiTask.Result);
        callback(response.data.animes[0]);       
    }
    public IEnumerator Get_BIGminiAnime(List<string> id, Action<List<Anime>> callback)
    {
        List<Anime> allAnimes = new List<Anime>(); // Общий список для всех аниме

        // Разбиваем список ID на пакеты
        for (int i = 0; i < id.Count; i += BatchSize)
        {
            // Получаем текущий пакет ID
            List<string> batchIds = id.GetRange(i, Mathf.Min(BatchSize, id.Count - i));

            // Формируем запрос для текущего пакета
            string query = FormQuery(batchIds);

            // Отправляем запрос
            Task<string> apiTask = ToAPIAsync(query, QL);
            Debug.Log("wait:  " + i);
            while (!apiTask.IsCompleted)
            {
                yield return null;
            }

            // Обрабатываем ответ
            if (apiTask.IsFaulted)
            {
                Debug.LogError("Ошибка при выполнении запроса: " + apiTask.Exception);
                yield break;
            }

            // Десериализуем ответ и добавляем аниме в общий список
            List<Anime> batchAnimes = ParseResponse(apiTask.Result);
            allAnimes.AddRange(batchAnimes);
        }

        // Возвращаем полный список через callback
        callback(allAnimes);
    }

    private string FormQuery(List<string> ids)
    {
        int count = 0;
        string query = "query {";
        foreach (string id in ids)
        {
            query += $@"
            AnImE{count}: animes(ids: ""{id}"") {{
                id
                name
                russian       
                poster {{ originalUrl }}
            }}";
            count++;
        }
        query += "}";
        return query;
    }

    private List<Anime> ParseResponse(string jsonResponse)
    {
        List<Anime> animes = new List<Anime>();

        JObject root = JObject.Parse(jsonResponse);
        JObject data = (JObject)root["data"];

        foreach (var entry in data)
        {
            string key = entry.Key.ToLower(); // Делаем ключи предсказуемыми ("anime0", "anime1", ...)
            if (key.StartsWith("anime")) // Проверяем, что это нужный ключ
            {
                List<Anime> animeList = JsonConvert.DeserializeObject<List<Anime>>(entry.Value.ToString());
                animes.AddRange(animeList);
            }
        }

        return animes;
    }
    public static string RemoveTextInBrackets(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        // Удаляем все фрагменты текста в квадратных скобках
        return Regex.Replace(input, @"\[.*?\]", string.Empty);
    }
    public async Task<List<GenreData>> Get_GenresList()
    {
        string query = @"
        query {
            genres(entryType: Anime) {
                id
                kind
                name
                russian
            }
        }";
        Task<string> apiTask = ToAPIAsync(query, QL);
        while (!apiTask.IsCompleted)
        {
            await Task.Yield();
        }
        if (apiTask.IsFaulted)
        {
            Debug.LogError("Ошибка: " + apiTask.Exception.Message);
        }
        else
        {
            detailResponse response = JsonConvert.DeserializeObject<detailResponse>(apiTask.Result);
            return response.data.genres;
        }
        return null;
    }
}

