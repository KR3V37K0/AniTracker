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
        reader.Close();
        reader = null;
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
                                Anime.""all"" AS anime_all,
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
            if (change.list_id >= 6)
            {
                
                string sqlQuery = "";
                switch (change.status)
                {
                    case "delete DB"://OK
                        sqlQuery = @$"DELETE FROM 
                                                Link
                                            WHERE
                                                id_Anime={change.anime.id}";
                                                
                        break;


                    case "update":
                        sqlQuery = @$"UPDATE Link SET viewed = {change.anime.viewed} WHERE id_Anime = {change.anime.id} AND id_List = {change.list_id}";
                        Debug.Log("update DB");
                        break;


                    case "create":
                        sqlQuery = @$"INSERT INTO Link(id_Anime, id_List, viewed) VALUES({change.anime.id}, {change.list_id}, {change.anime.viewed})";
                        if (manager.ui_lists.FindAnimeInLists(change.anime.id).Count == 0) await createAnime(change.anime);
                        Debug.Log("create DB "+change.anime.name);
                        break;

                    default:
                        Debug.LogError(@$"неизвестный статус для сохранения в БД: {change.anime.name} {change.status}");
                        break;
                }
                OpenConnection();
                dbcmd.CommandText = sqlQuery;
                reader = dbcmd.ExecuteReader();

                while (reader.Read()) 
                { 

                }


                CloseConnection();
            }
        }
        Debug.Log("...saving offline lists");
    }
    async Task createAnime(DB_Anime anime)
    {
        Debug.Log("create new anime "+anime.name);
        OpenConnection();

        string sqlQuery = @$"INSERT INTO Anime(id, name, aired, all) VALUES({anime.id}, {anime.name}, {anime.aired},{anime.all})";
        dbcmd.CommandText = sqlQuery;
        reader = dbcmd.ExecuteReader();

        while (reader.Read())
        {

        }

        CloseConnection();
    }
}
