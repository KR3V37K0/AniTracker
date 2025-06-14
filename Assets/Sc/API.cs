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
using static UnityEngine.EventSystems.EventTrigger;

public class API : MonoBehaviour
{
    ManagerSC manager;
    private string QL = "https://shikimori.one/api/graphql";
    private string Similar = "https://shikimori.one/api/animes/id/similar";
    private string url_Images = "https://shikimori.one";
    private const int BatchSize = 19;
    public int querys = 0;

    void Start()
    {
        manager=gameObject.transform.GetComponent<ManagerSC>();
    }

    public void EnterToShiki()//do not delete
    {
        if (Application.isEditor)
        {
            manager.winServer.StartAuthorization();
        }
        else if (Application.platform == RuntimePlatform.Android)
        {
            manager.androidServer.StartAuthorization();
        }
    }
    public IEnumerator SearchResult()
    {
        string query = ConnectionData.currentSearch.apply();
        Task<string> apiTask = ToAPIAsync(query, QL);
        while (!apiTask.IsCompleted)
        {
            yield return null; // ���� ���������� ������
        }
        if (apiTask.IsFaulted)
        {
            Debug.LogError("������: " + apiTask.Exception.Message);
        }
        else
        {           
            AnimeResponse response = JsonConvert.DeserializeObject<AnimeResponse>(apiTask.Result);

            if (response?.data?.animes != null)
            {

                if(ConnectionData.currentSearch.page==1) manager.ui.clear_Home();
                manager.ui.activate_Window(0);
                manager.ui.activate_Window(0);
                int n = (BatchSize* ConnectionData.currentSearch.page)-BatchSize;
                foreach (Anime anime in response.data.animes)
                {
                    int localIndex = n;

                    StartCoroutine(DownloadImage(anime.poster.originalUrl, sprite
                        => StartCoroutine(manager.ui.Anime_to_Home(anime, sprite, localIndex))));
                    n++;
                }
            }
            else
            {
                Debug.LogError("? �� ������� ������� ������ ����� �� JSON.");
                Debug.LogError(apiTask.Result);
            }
        }
    }
    public async Task<string> ToAPIAsync(string query,string url)
    {
        querys++;
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
                await Task.Yield(); // ���� ���������� �������
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = request.downloadHandler.text;
                return jsonResponse;
            }
            else
            {
                manager.starter.noConnect();
                Debug.LogError("������ �������: " + request.error+"  "+query);
                throw new System.Exception("������ �������: " + request.error);
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
                name
                russian
                poster {{ originalUrl }}
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
            yield return null; // ���� ���������� ������
        }
        if (apiTask.IsFaulted)
        {
            Debug.LogError("������: " + apiTask.Exception.Message);
        }
        else
        {
            detailResponse response = JsonConvert.DeserializeObject<detailResponse>(apiTask.Result);
            AnimeDetails details = response.data.animes[0];

            if (main.name != null && main.name!="")
                details.main = main;
            else 
            {
                details.main = new Anime(main.id);
                details.main.name = response.data.animes[0].name;
                details.main.russian = response.data.animes[0].russian;
                details.main.poster = response.data.animes[0].poster;
                yield return StartCoroutine(DownloadImage(details.main.poster.originalUrl,
                    (sprite) => details.main.sprite = sprite));
            }

            //Debug.Log(d_details.data.animes[0].related[0].anime.name);
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

            // �������� ��� ���� � ���������
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

            // �������� �������
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
            // ������������� ��������� User-Agent (����������� ��� Shikimori API)
            client.DefaultRequestHeaders.Add("User-Agent", "AniTracker");

            // ���������� GET-������
            HttpResponseMessage response = await client.GetAsync($"https://shikimori.one/api/animes/{animeId}/similar");

            if (response.IsSuccessStatusCode)
            {
                // ������ JSON-�����
                string jsonResponse = await response.Content.ReadAsStringAsync(); 
                return JsonConvert.DeserializeObject<List<SimilarAnime>>(jsonResponse);
            }
            else
            {
                // ��������� ������
                throw new Exception($"������ �������: {response.StatusCode} - {response.ReasonPhrase}");
            }
        }
    }
    public IEnumerator DownloadImage(string url,Action<Sprite> callback)
    {       
        if (url != null && url!="")
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.downloadHandler = new DownloadHandlerTexture(true); // ������������� ������� Texture2D
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("User-Agent", "AniTracker");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Texture2D texture = ((DownloadHandlerTexture)request.downloadHandler).texture;
                    Sprite sprite = SpriteFromTexture(texture);
                    callback(sprite);
                }
                else
                {
                    Debug.LogError("������ �������� �������: " + request.error + " " + url);
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
        List<Anime> allAnimes = new List<Anime>(); // ����� ������ ��� ���� �����

        // ��������� ������ ID �� ������
        for (int i = 0; i < id.Count; i += BatchSize)
        {
            // �������� ������� ����� ID
            List<string> batchIds = id.GetRange(i, Mathf.Min(BatchSize, id.Count - i));

            // ��������� ������ ��� �������� ������
            string query = FormQuery(batchIds);

            // ���������� ������
            Task<string> apiTask = ToAPIAsync(query, QL);
            Debug.Log("wait:  " + i);
            while (!apiTask.IsCompleted)
            {
                yield return null;
            }

            // ������������ �����
            if (apiTask.IsFaulted)
            {
                Debug.LogError("������ ��� ���������� �������: " + apiTask.Exception);
                yield break;
            }

            // ������������� ����� � ��������� ����� � ����� ������
            List<Anime> batchAnimes = ParseResponse(apiTask.Result);
            allAnimes.AddRange(batchAnimes);
        }

        // ���������� ������ ������ ����� callback
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
            string key = entry.Key.ToLower(); // ������ ����� �������������� ("anime0", "anime1", ...)
            if (key.StartsWith("anime")) // ���������, ��� ��� ������ ����
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

        // ������� ��� ��������� ������ � ���������� �������
        return Regex.Replace(input, @"\[.*?\]", string.Empty);
    }
    public async Task<List<GenreData>> Get_GenresList()
    {
        await Task.Delay(1);
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
            Debug.LogError("������: " + apiTask.Exception.Message);
        }
        else
        {
            detailResponse response = JsonConvert.DeserializeObject<detailResponse>(apiTask.Result);
            return response.data.genres;
        }
        return null;
    }



    //LIST
    public async Task<List<DB_Anime>> getList(string status)
    {
        
        int page = 1;
        List<DB_Anime> animes = new List<DB_Anime>();
        
        while (true)
        { 
            string query = $@"
            query {{
                  userRates(limit: 50, page: {page}, targetType: Anime, status: {status}, userId: {manager.user.id}, order: {{ field: updated_at, order: desc }}) 
                    {{
                        id
                        anime {{ id russian episodes episodesAired}}
                        episodes
                    }}
             }}";
            Task<string> apiTask = ToAPIAsync(query, QL);
            while (!apiTask.IsCompleted)
            {
                await Task.Yield();
            }
           // Debug.Log(apiTask.Result);

            detailResponse respo = JsonConvert.DeserializeObject<detailResponse>(apiTask.Result.ToString());
            List<respo_list> animeList = respo.data.userRates;


            foreach (respo_list r in animeList)
            {
                animes.Add(new DB_Anime(int.Parse(r.anime.id),r.anime.russian,r.anime.episodesAired,r.anime.episodes,r.episodes));
            }
            if (animeList.Count < 50) 
            {
                return animes;
            }
            await Task.Delay(100);
            page++;
        }
    }
    public async Task save_onlineList(List<Changes> changes)
    {
        Debug.Log("...saving online lists");
    }

}

