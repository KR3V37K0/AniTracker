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

    //ОЧЕРЕДИ
    public void Enqueue(Func<Task> function)
    {
        _queue.Enqueue(function);
        ProcessQueue(); 
    }
    private async void ProcessQueue()
    {
        if (_isRunning) return; 

        _isRunning = true;

        while (_queue.Count > 0)
        {
            Func<Task> function = _queue.Dequeue();
            await function();
        }

        _isRunning = false;
    }


    private void Start()
    {   
        Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
    }
    void OpenConnection()
    {
        dbconn = new SqliteConnection(conn);
        dbconn.Open();
        dbcmd = dbconn.CreateCommand();
    }
    void CloseConnection()
    {
        dbcmd.Parameters.Clear();
        if (reader != null)
        {
            reader.Close();
            reader = null;
        }

        dbcmd.Dispose();
        dbcmd = null;
        dbconn.Close();
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
            Color listColor = Color.HSVToRGB(float.Parse(col[0]), float.Parse(col[1]), float.Parse(col[2]));

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
        foreach (Changes change in changes)
        {
            Debug.Log(change.list_id+" "+change.status+"  "+change.anime.name);
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
                        Debug.Log("update DB");
                        break;*/
                        sqlQuery = @$"INSERT INTO Link(id_Anime, id_List, viewed) VALUES({change.anime.id}, {real_id}, {change.anime.viewed})";
                        try
                        {
                            await createAnime(change.anime);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError("Ошибка при добавлении аниме: " + ex.Message);
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
                            Debug.LogError("Ошибка при добавлении аниме: " + ex.Message);
                        }
                        break;

                    default:
                        Debug.LogError(@$"неизвестный статус для сохранения в БД: {change.anime.name} {change.status}");
                        break;
                }
                if(await list_has_anime(real_id, change.anime.id)) break;

                OpenConnection();
                dbcmd.CommandText = sqlQuery;
                //reader = dbcmd.ExecuteReader();

                writer = dbcmd.ExecuteNonQuery();

                if (writer > 0)
                    Debug.Log("Запись успешно добавлена!");


                CloseConnection();
            }
        }
        Debug.Log("...saving offline lists");
    }
    async Task createAnime(DB_Anime anime)
    {
        if (savedAnime == null) await ReadAnime();
        if (savedAnime.Contains(anime.id)) { Debug.Log("Аниме уже существует " + anime.name); return; }

        Debug.Log("create new anime in DATABASE "+anime.name+" "+anime.id);
        OpenConnection();

        //string sqlQuery = @$"INSERT INTO Anime(id, name, aired, all) VALUES({anime.id}, {anime.name}, {anime.aired},{anime.all})";

        string sqlQuery = @$"INSERT INTO Anime(id, name, aired, all_ep) VALUES({anime.id}, ""{anime.name}"", {anime.aired},{anime.all})";
        dbcmd.CommandText = sqlQuery;

        //dbcmd.CommandText = sqlQuery;
        //reader = dbcmd.ExecuteReader();
        writer = dbcmd.ExecuteNonQuery();

        if (writer > 0)
            Debug.Log("Запись ОБ АНИМЕ успешно добавлена!");

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
        return false;
    }

    public async Task get_Tracking()
    {
        OpenConnection();
        string sqlQuery = @$"SELECT 
                                id_Anime,
                                1s_Date,
                                all_ep
                            FROM 
                                Track";
        dbcmd.CommandText = sqlQuery;
        reader = dbcmd.ExecuteReader();
        List<Tracker> trackers = new List<Tracker>();
        while (reader.Read())
        {
            Tracker track = new Tracker(reader.GetInt32(0), DateTime.Parse(reader.GetString(1)), reader.GetInt32(2));
            trackers.Add(track);

        }
        CloseConnection();
    }
}
