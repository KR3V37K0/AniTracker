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
    [SerializeField]ManagerSC manager;
    string conn = SetDataBaseClass.SetDataBase("DATABASE.db");
    IDbConnection dbconn;
    IDbCommand dbcmd;
    IDataReader reader;
    public Dictionary<string,string> basic_List_name = new Dictionary<string, string>()
    {
        {"planned","�������������"},
        {"watching","������"},
        {"rewatching","�������������"},
        {"completed","�����������"},
        {"on_hold","��������"},
        {"dropped","�������"}      
    };

    private Queue<Func<Task>> _queue = new Queue<Func<Task>>();
    private bool _isRunning = false; 

    //�������
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
            Color listColor = Color.white; // �����������, ��� ���� �������� ��� ������
            int listPlace = reader.GetInt32(3);

            // ���� ������ � ����� id ��� �� ��������, ������ ���
            if (!listDict.ContainsKey(listId))
            {
                DB_List dbList = new DB_List(listId, listName, listColor, listPlace);
                listDict[listId] = dbList;
                lists.Add(dbList);
            }

            // ���� ���� ��������� �����, ��������� ��� � ������
            if (!reader.IsDBNull(4)) // ���������, ���� �� ����� � ������� Anime
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
        //��������
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
}
