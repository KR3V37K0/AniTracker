using UnityEngine;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Networking;
using System.Collections;

public class ConvertDataBaseToMultiPlatform : MonoBehaviour
{
    public string DataBaseName;
    public string filepath;


    public void Awake()
    {
        
        GenerateConnectionString(DataBaseName+".db");
        
    }
    
public void GenerateConnectionString(string DatabaseName)
    {
        UnityWebRequest loadDb;
        string dbPath = "";
#if UNITY_EDITOR
        dbPath = Application.dataPath + "/StreamingAssets/" + DatabaseName;
#else
        //check if file exists in Application.persistentDataPath
        string filepath = Application.persistentDataPath + "/" + DatabaseName;

        if (!File.Exists(filepath) || new System.IO.FileInfo(filepath).Length == 0)
        {
            // if it doesn't ->
            // open StreamingAssets directory and load the db ->
#if UNITY_ANDROID

        filepath = Path.Combine(Application.persistentDataPath, DataBaseName);
        StartCoroutine(LoadDatabaseFromStreamingAssets());

        IEnumerator LoadDatabaseFromStreamingAssets()
    {
        dbPath = "jar:file://" + Application.dataPath + "!/assets/" + DataBaseName;
        loadDb = UnityWebRequest.Get(dbPath);

        yield return loadDb.SendWebRequest();

        if (loadDb.result == UnityWebRequest.Result.Success)
        {
            File.WriteAllBytes(filepath, loadDb.downloadHandler.data);
            Debug.Log("Database successfully loaded and saved to: " + filepath);
        }
        else
        {
            Debug.LogError("Failed to load database: " + loadDb.error);
        }
    }

        //dbPath = "jar:file://" + Application.dataPath + "!/assets/" + DataBaseName;
        //loadDb = UnityWebRequest.Get(dbPath);


        //File.WriteAllBytes(filepath, loadDb.downloadHandler.data);



/*
                UnityWebRequest loadDb = new UnityWebRequest("jar:file://" + Application.dataPath + "!/assets/" + DatabaseName);  // this is the path to your StreamingAssets in android
                while (!loadDb.isDone) { }  // CAREFUL here, for safety reasons you shouldn't let this while loop unattended, place a timer and error check
                // then save to Application.persistentDataPath
                File.WriteAllBytes(filepath, loadDb.bytes);
                */
#endif
        }
        
        dbPath = filepath;
#endif



    }




}