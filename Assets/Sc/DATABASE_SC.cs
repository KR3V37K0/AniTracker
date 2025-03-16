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

public class DATABASE_SC : MonoBehaviour
{
    ManagerSC manager;
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
    public List<FastList> allLists= new List<FastList>();  

    private void Start()
    {
        manager = GetComponent<ManagerSC>();
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

    public List<DB_List> Get_AllLists_preview()
    {
        OpenConnection();

        List<DB_List> lists = new List<DB_List>();
        Dictionary<int, DB_List> listDict = new Dictionary<int, DB_List>();

        string sqlQuery = @"SELECT 
                                List.id AS list_id,
                                List.name AS list_name,
                                List.color AS list_color,
                                List.place AS list_place,
                                Anime.id AS anime_id,
                                Anime.name AS anime_name,
                                Anime.series AS anime_series
                            FROM 
                                List
                            LEFT JOIN 
                                Link 
                            ON 
                                List.id = Link.id_List
                            LEFT JOIN 
                                Anime 
                            ON 
                                Link.id_Anime = Anime.id;";
        dbcmd.CommandText = sqlQuery;
        reader = dbcmd.ExecuteReader();
        while (reader.Read())
        {
            int listId = reader.GetInt32(0);
            string listName = reader.GetString(1);
            Color listColor = Color.white; // Предположим, что цвет хранится как строка
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
                string animeSeries = reader.GetString(6);

                DB_Anime dbAnime = new DB_Anime(animeId, animeName, animeSeries);
                listDict[listId].animes.Add(dbAnime);
            }
        }
        //ДОДЕЛАТЬ
        CloseConnection();
        return lists;
    }

    public async void WriteUpdate()
    {
        OpenConnection();

        foreach(var status in basic_List_name)
        {
            Task<List<Anime>> apiTask = manager.api.getList(status.Key);
            while (!apiTask.IsCompleted)
            {
                await apiTask;
            }
            FastList fast=new FastList();
            fast.name = status.Key;
            fast.russian = status.Value;
            fast.animes = apiTask.Result;
        }


        CloseConnection();
    }
}
