using UnityEngine;
using System.Data;
using Mono.Data;
using Mono.Data.Sqlite;
using System.IO;
using System.EnterpriseServices;
using static Unity.Burst.Intrinsics.X86;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Globalization;
using System;
using System.Threading;
using UnityEngine.UIElements;
using Unity.VisualScripting;
using System.Threading.Tasks;
using System.Collections;
using UnityEngine.PlayerLoop;
using UnityEngine.Assertions;
using UnityEngine.Networking;
using UnityEngine.Android;

public class DATABASE_SC : MonoBehaviour
{
    [SerializeField]ManagerSC manager;
    string conn = SetDataBaseClass.SetDataBase("DATABASE.db");
    IDbConnection dbconn;
    IDbCommand dbcmd;
    IDataReader reader;
    int writer;
    public Dictionary<string,string> basic_List_name = new Dictionary<string, string>()
    {
        {"planned","запланировано"},
        {"watching","смотрю"},
        {"rewatching","пересматриваю"},
        {"completed","просмотрено"},
        {"on_hold","отложено"},
        {"dropped","брошено"}      
    };

    private Queue<Func<Task>> _queue = new Queue<Func<Task>>();
    private bool _isRunning = false;
    List<int> savedAnime;

    public DATABASE_SC()
    {
        conn = SetDataBaseClass.SetDataBase("DATABASE.db");
    }

    private void Awake()
    {
        Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

#if UNITY_ANDROID
        if (!Permission.HasUserAuthorizedPermission(Permission.ExternalStorageWrite))
        {
            Permission.RequestUserPermission(Permission.ExternalStorageWrite);
        }
#endif

        //InitializeDatabase();
    }

    public async Task InitializeDatabase()
    {
        await CheckAndCopyDatabase();

        // Только теперь подключаемся к БД
        conn = SetDataBaseClass.SetDataBase("DATABASE.db");
        MobileDebug.Log("Подключение к БД выполнено");
    }
    private async Task CheckAndCopyDatabase()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
       /* string targetPath = Path.Combine(Application.persistentDataPath, "DATABASE.db");
        if (!File.Exists(targetPath))
        {
            string sourcePath = Path.Combine(Application.streamingAssetsPath, "DATABASE.db");
            var www = new UnityEngine.Networking.UnityWebRequest(sourcePath);
            await www.SendWebRequest();
            File.WriteAllBytes(targetPath, www.downloadHandler.data);
        }*/
        //StartCoroutine(CopyDatabaseAndroid());
        string targetPath = Path.Combine(Application.persistentDataPath, "DATABASE.db");
        if (!File.Exists(targetPath))
        {
            string sourcePath = Path.Combine(Application.streamingAssetsPath, "DATABASE.db");
            using (var www = UnityEngine.Networking.UnityWebRequest.Get(sourcePath))
            {
                var operation = www.SendWebRequest();
                
                // Ожидаем завершения через TaskCompletionSource
                var tcs = new TaskCompletionSource<bool>();
                operation.completed += _ => tcs.SetResult(true);
                await tcs.Task;
                
                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    File.WriteAllBytes(targetPath, www.downloadHandler.data);
                    MobileDebug.Log("БД успешно скопирована");
                }
                else
                {
                    MobileDebug.LogError($"Ошибка копирования БД: {www.error}");
                }
            }
        }
#endif
    }



    public void Enqueue(Func<Task> function)
    {
        _queue.Enqueue(function);
        ProcessQueue(); 
    }

    private async void ProcessQueue()
    {
        if (_isRunning) return; 

        _isRunning = true;

        try
        {
            while (_queue.Count > 0)
            {
                try
                {
                    Func<Task> function = _queue.Dequeue();
                    await function().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    MobileDebug.LogError($"Ошибка в очереди: {ex.Message}");
                }
            }
        }
        finally
        {
            _isRunning = false;
        }
    }

    private void OpenConnection()
    {
        if (dbconn?.State == ConnectionState.Open) return;
        
        dbconn = new SqliteConnection(conn);
        dbconn.Open();
        dbcmd = dbconn.CreateCommand();
    }

    private void CloseConnection()
    {
        reader?.Close();
        reader = null;
        
        dbcmd?.Dispose();
        dbcmd = null;
        
        dbconn?.Close();
        dbconn?.Dispose();
        dbconn = null;
    }


    public async Task<List<DB_List>> Get_AllLists_preview()
    {
        OpenConnection();

        List<DB_List> lists = new List<DB_List>();
        Dictionary<int, DB_List> listDict = new Dictionary<int, DB_List>();

        string sqlQuery = @$"SELECT 
                                List.id AS list_id,
                                List.name AS list_name,
                                List.color AS list_color,
                                List.place AS list_place,
                                Anime.id AS anime_id,
                                Anime.name AS anime_name,
                                Anime.aired AS anime_aired,
                                Anime.""all_ep"" AS anime_all_ep,
                                Link.viewed AS anime_viewed
                            FROM 
                                List
                            LEFT JOIN 
                                Link 
                            ON 
                                List.id = Link.id_List
                            LEFT JOIN 
                                Anime 
                            ON 
                                Link.id_Anime = Anime.id
                            WHERE 
                                List.id_user = {manager.user.local_id};";
        dbcmd.CommandText = sqlQuery;
        reader = dbcmd.ExecuteReader();
        while (reader.Read())
        {
            int listId = reader.GetInt32(0);
            string listName = reader.GetString(1);

            string[] col = reader.GetString(2).Split('/');
            //Color listColor = Color.HSVToRGB(float.Parse(col[0]), float.Parse(col[1]), float.Parse(col[2]));
            Color listColor = Color.white;

            int listPlace = reader.GetInt32(3);

            // Если список с таким id ещё не добавлен, создаём его
            if (!listDict.ContainsKey(listId))
            {
                DB_List dbList = new DB_List(listId, listName, listColor, listPlace);
                listDict[listId] = dbList;
                lists.Add(dbList);
            }

            // Если есть связанное аниме, добавляем его в список
            if (!reader.IsDBNull(4)) // Проверяем, есть ли аниме в таблице Anime
            {
                int animeId = reader.GetInt32(4);
                string animeName = reader.GetString(5);
                int animeAired = reader.GetInt32(6);
                int animeAll = reader.GetInt32(7);
                int animeViewed = reader.GetInt32(8);

                if (animeAired == 0) animeAired = animeAll;

                DB_Anime dbAnime = new DB_Anime(animeId, animeName, animeAired, animeAll, animeViewed);
                listDict[listId].animes.Add(dbAnime);
            }
        }
        //ДОДЕЛАТЬ
        CloseConnection();
        return lists;
        //manager.ui_lists.allList=lists;
    }

    public async Task get_currentUser()
    {
        OpenConnection();
        string sqlQuery = @$"SELECT 
                                name,
                                shiki
                            FROM 
                                Users                           
                            WHERE 
                                local_id = {manager.user.local_id}";
        dbcmd.CommandText = sqlQuery;
        reader = dbcmd.ExecuteReader();
        while (reader.Read())
        {
            manager.user.nickname = reader.GetString(0);
            if(reader.GetInt32(1)!=0) manager.user.id = reader.GetInt32(1);
        }
        CloseConnection();
        MobileDebug.Log("--из БД-- найдено: "+manager.user.nickname+" "+manager.user.id);
        MobileDebug.Log(" --из БД-- получаю трекеры");
        await manager.noty.get_Trackers();
        MobileDebug.Log("--из БД-- трекеры получены. ищу локальные аниме");
        //manager.noty.trackers = await get_Tracking();
        await ReadAnime();
        MobileDebug.Log("--из БД-- локальные аним проверены. заполняю UI Settings");
        manager.ui_settings.ViewUserInfo();
    }
    public async Task set_currentUser_info()
    {
        OpenConnection();
        string sqlQuery = @$"UPDATE
                                Users
                            SET
                                name = '{manager.user.nickname}',
                                shiki = {manager.user.id}                          
                            WHERE 
                                local_id = {manager.user.local_id}";
        dbcmd.CommandText = sqlQuery;
        reader = dbcmd.ExecuteReader();
        while (reader.Read()) { }
        CloseConnection();
    }

    public async Task save_Lists(List<Changes>changes)
    {
        _isRunning = true;
        foreach (Changes change in changes)
        {
            MobileDebug.Log(change.list_id+" "+change.status+"  "+change.anime.name);
            if (change.list_id >= 6)
            {
                int real_id = change.list_id - 5;
                string sqlQuery = "";
                switch (change.status)
                {                  
                    case "delete DB"://OK
                        sqlQuery = @$"DELETE FROM 
                                                Link
                                            WHERE
                                                id_Anime={change.anime.id}
                                            AND
                                                id_List={real_id}";
                                                
                        break;
                    case "delete"://OK
                        sqlQuery = @$"DELETE FROM 
                                                Link
                                            WHERE
                                                id_Anime={change.anime.id}
                                            AND
                                                id_List={real_id}";

                        break;


                    case "update":
                        /*sqlQuery = @$"UPDATE Link SET viewed = {change.anime.viewed} WHERE id_Anime = {change.anime.id} AND id_List = {real_id}";
                        MobileDebug.Log("update DB");
                        break;*/
                        sqlQuery = @$"INSERT INTO Link(id_Anime, id_List, viewed) VALUES({change.anime.id}, {real_id}, {change.anime.viewed})";
                        try
                        {
                            await createAnime(change.anime);
                        }
                        catch (Exception ex)
                        {
                            MobileDebug.LogError("Ошибка при добавлении аниме: " + ex.Message);
                        }
                        break;


                    case "create":
                        sqlQuery = @$"INSERT INTO Link(id_Anime, id_List, viewed) VALUES({change.anime.id}, {real_id}, {change.anime.viewed})";
                        try
                        {
                            await createAnime(change.anime);
                        }
                        catch (Exception ex)
                        {
                            MobileDebug.LogError("Ошибка при добавлении аниме: " + ex.Message);
                        }
                        break;

                    default:
                        MobileDebug.LogError(@$"неизвестный статус для сохранения в БД: {change.anime.name} {change.status}");
                        break;
                }
                if((await list_has_anime(real_id, change.anime.id))&&(change.status== "create"|| change.status == "update")) break;

                OpenConnection();
                dbcmd.CommandText = sqlQuery;
                //reader = dbcmd.ExecuteReader();

                writer = dbcmd.ExecuteNonQuery();

                if (writer > 0)
                    MobileDebug.Log("Запись успешно добавлена!");


                CloseConnection();
               
            }
        }
        MobileDebug.Log("...saving offline lists");
        _isRunning = false;
    }
    async Task createAnime(DB_Anime anime)
    {
        if (savedAnime == null) await ReadAnime();
        if (savedAnime.Contains(anime.id)) { MobileDebug.Log("Аниме уже существует " + anime.name); return; }

        MobileDebug.Log("create new anime in DATABASE "+anime.name+" "+anime.id);
        OpenConnection();

        //string sqlQuery = @$"INSERT INTO Anime(id, name, aired, all) VALUES({anime.id}, {anime.name}, {anime.aired},{anime.all})";

        string sqlQuery = @$"INSERT INTO Anime(id, name, aired, all_ep) VALUES({anime.id}, ""{anime.name}"", {anime.aired},{anime.all})";
        dbcmd.CommandText = sqlQuery;

        //dbcmd.CommandText = sqlQuery;
        //reader = dbcmd.ExecuteReader();
        writer = dbcmd.ExecuteNonQuery();

        if (writer > 0)
            MobileDebug.Log("Запись ОБ АНИМЕ успешно добавлена!");

        savedAnime.Add(anime.id);
        CloseConnection();
    }
    async Task ReadAnime()
    {
        savedAnime = new List<int>();
        OpenConnection();
        string sqlQuery = @$"SELECT 
                                id
                            FROM 
                                Anime";
        dbcmd.CommandText = sqlQuery;
        reader = dbcmd.ExecuteReader();
        while (reader.Read())
        {
            savedAnime.Add(reader.GetInt32(0));
        }
        CloseConnection();
    }
    async Task<bool> list_has_anime(int list, int anime)
    {
        OpenConnection();
        bool has = false;
        string sqlQuery = @$"SELECT 
                                id_Anime,
                                id_List
                            FROM 
                                Link
                            WHERE
                                id_Anime = {anime},
                                id_List = {list}";
        dbcmd.CommandText = sqlQuery;

        using (reader = dbcmd.ExecuteReader())
        {
            has = reader.Read(); // вернёт true, если есть хотя бы одна запись
        }

        CloseConnection();
        return has;
    }

    public async Task<List<Tracker>> get_Tracking()
    {
        await Task.Delay(5);
        MobileDebug.Log("DB work");
        List<Tracker> trackers = new List<Tracker>();
        try
        {
            OpenConnection();

            string sqlQuery = @$"SELECT 
                                id_Anime,
                                start_Date,
                                all_ep
                            FROM 
                                Tracking";
            dbcmd.CommandText = sqlQuery;

            reader = dbcmd.ExecuteReader();

            while (reader.Read())
            {
                Tracker track = new Tracker(
                    reader.GetInt32(0),
                    DateTime.ParseExact(reader.GetString(1), "MM/dd/yyyy HH:mm:ss", CultureInfo.InvariantCulture),
                    reader.GetInt32(2)
                );
                trackers.Add(track);
            }

            CloseConnection();
        }
        catch (Exception ex)
        {
            MobileDebug.LogError("Ошибка при получении трекеров из базы: " + ex.Message + "\n" + ex.StackTrace);
        }

        return trackers;
    }
    public async Task DeleteTracking(Tracker track)
    {
       /* if (_isRunning)
        {
            await Task.Delay(500);
        }*/
        //_isRunning = true;
        OpenConnection();
        string sqlQuery = @$"DELETE FROM 
                        Tracking
                    WHERE
                        id_Anime={track.id_Anime}";
        dbcmd.CommandText = sqlQuery;

        writer = dbcmd.ExecuteNonQuery();

        if (writer > 0)
            MobileDebug.Log("NOTY успешно удалена!");

        CloseConnection();
        //_isRunning=false;
    }
    public async Task AddTracking(Tracker track,AnimeDetails anime)
    {
        /*if (_isRunning)
        {
            await Task.Delay(500);
        }*/
        // _isRunning=true;
        if (savedAnime.Contains(int.Parse(anime.main.id))) await createAnime(
                                    new DB_Anime(
                                        int.Parse(anime.main.id),
                                        anime.main.russian,
                                        anime.episodesAired,
                                        anime.episodes,
                                        0));

        OpenConnection();
        string sqlQuery = @$"INSERT INTO Tracking(id_Anime, start_Date, all_ep) VALUES({track.id_Anime}, ""{track.start}"", {track.all})";
        dbcmd.CommandText = sqlQuery;
        writer = dbcmd.ExecuteNonQuery();

        if (writer > 0)
            MobileDebug.Log("уведомление добавлено в БД");

        CloseConnection();
       // _isRunning= false;
    }



    //List editing
    public async Task Change_List(DB_List list)
    {
        OpenConnection();

        string sqlQuery = @$"UPDATE List 
                    SET name = ""{list.name}""
                    WHERE
                        place = {list.place-6}";
        dbcmd.CommandText = sqlQuery;

        int rowsAffected = dbcmd.ExecuteNonQuery();

        if (rowsAffected > 0)
            MobileDebug.Log($"Обновлено записей: {rowsAffected}");
        else
            MobileDebug.LogError("Запись не найдена или не изменена");

        CloseConnection();
    }
    public async Task Delete_List(DB_List list)
    {
        OpenConnection();

        string sqlQuery = @$"DELETE FROM List 
                    WHERE
                        place = {list.place - 6}";
        dbcmd.CommandText = sqlQuery;

        int rowsAffected = dbcmd.ExecuteNonQuery();

        if (rowsAffected > 0)
            MobileDebug.Log($"Удалено записей: {rowsAffected}");
        else
            MobileDebug.LogError("Запись не найдена");

        CloseConnection();
    }
    public async Task Create_List(DB_List list)
    {
        OpenConnection();

        string sqlQuery = @$"INSERT INTO List (name, color, place, id_user)
                    VALUES
                       (""{list.name}"", ""{"0/0/0"}"", {list.place - 6}, {manager.user.local_id})";
        dbcmd.CommandText = sqlQuery;

        int rowsAffected = dbcmd.ExecuteNonQuery();

        if (rowsAffected > 0)
            MobileDebug.Log($"Добавлено: {rowsAffected}");
        else
            MobileDebug.LogError("Запись не найдена");

        CloseConnection();
    }
}
