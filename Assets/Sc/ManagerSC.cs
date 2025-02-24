using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.UI;
using UnityEngine;

public class ManagerSC : MonoBehaviour
{
    [Header("----SYSTEM----")]
    public API api;
    public UISC ui;
    public NotificationSC noty;
    public LocalServer winServer;
    public DeepLinkHandler androidServer;
    void Start()
    {
        StartCoroutine(api.GetOngoingAnime());
    }
    void Update()
    {
        
    }
    public void EnterToShiki()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            androidServer.StartAuthorization();
        }
        else if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
        {
            winServer.StartAuthorization();
        }
    }
}
public class AnimeImage
{
    public string originalUrl;
}
public class Anime
{
    public string id;
    public string name;
    public string russian;
    public AnimeImage poster;
    public Sprite sprite;
}
public class detailResponse
{
    public Data data;
}
public class Data
{
    public List<AnimeDetails> animes;
}
public class AnimeDetails
{
    public Anime main;
    public string kind { get; set; }
    public int episodes { get; set; }
    public int episodesAired { get; set; }
    public string status { get; set; }
    public List<Genre> genres { get; set; }
    public string rating { get; set; }
    public double score { get; set; }
    public List<Studio> studios { get; set; }
    public string description { get; set; }
    public List<PersonRole> personRoles { get; set; }
    public List<Screenshot> screenshots { get; set; }
    public Related[] related;
    public Anime[] similar;

}
public class Genre
{
    public int id;
    public string name;
    public string russian;
    public string kind;
}
public class Studio
{
    public string id { get; set; }
    public string name { get; set; }
    public string imageUrl { get; set; }
    public Sprite sprite;
}
public class PersonRole
{
    public string id { get; set; }
    public List<string> rolesRu { get; set; }
    public Person person { get; set; }
}
public class Person
{
    public string id { get; set; }
    public string name { get; set; }
    public Poster poster { get; set; }
}
public class Poster
{
    public string id { get; set; }
}
public class Related
{
    public Anime anime;
    public string relationText;
}
public class Screenshot
{
    public string originalUrl { get; set; }
    public Sprite sprite;
}
