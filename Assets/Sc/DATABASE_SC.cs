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

    List<DB_List> Get_AllLists_preview()
    {
        OpenConnection();

        List<DB_List> list = new List<DB_List>();

        string sqlQuery = "Select id,name,color,place FROM List";
        dbcmd.CommandText = sqlQuery;
        reader = dbcmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new DB_List(reader.GetInt32(0), reader.GetString(1), Color.white, reader.GetInt32(3)));
        }
        //ДОДЕЛАТЬ
        CloseConnection();
        
        return list;
    }

    async void WriteUpdate()
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
