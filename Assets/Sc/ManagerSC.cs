using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class ManagerSC : MonoBehaviour
{
   // public static ManagerSC info { get; private set; }

[Header("----SYSTEM----")]
    public API api;
    public NotificationSC noty;
    public LocalServer winServer;
    public DeepLinkHandler androidServer;
    public bool hasConnection;
    public ShikimoriUser user;

[Header("----UI----")]
    public UISC ui;
    public UI_Search ui_search;
    public UI_Lists ui_lists;
    public UI_Settings ui_settings;
    
}
//USER
public class ShikimoriUser
{
    public int id;
    public string nickname;
    public string avatar;
}
//ANIME
public class AnimeImage
{
    public string originalUrl;
    public string original;
}
public class Anime
{
    public string id;
    public string name;
    public string russian;
    public AnimeImage poster;
    public Sprite sprite;
}
public class SimilarAnime
{
    public string id;
    public string name;
    public string russian;
    public AnimeImage image;
    public Sprite sprite;
}
public class detailResponse
{
    public Data data;
}
public class Data
{
    public List<AnimeDetails> animes;
    public List<GenreData> genres;
    public List<UserRate> userRates;
}
public class UserRate
{
    public Anime anime;
}
public class GenreData
{
    public string id;
    public string kind;
    public string name;
    public string russian;
}

public class AnimeDetails
{
    public Anime main;
    public string kind { get; set; }
    public int episodes { get; set; }
    public int episodesAired { get; set; }
    public string status { get; set; }
    public A_Date airedOn;
    public List<Genre> genres { get; set; }
    public string rating { get; set; }
    public double score { get; set; }
    public List<Studio> studios { get; set; }
    public string description { get; set; }
    public List<PersonRole> personRoles { get; set; }
    public List<Screenshot> screenshots { get; set; }
    public List<Related> related;
    public List<Anime> similar;

}
public class A_Date
{
    public int year;
    public int month;
    public int day;
    public string date;
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
    public string originalUrl;
    public Sprite sprite;
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

//DATABASE
public class Series
{
    public int viewved;
    public int aired;
    public int all;
}

public class DB_Anime
{
    public int id;
    public string name;
    public Series series;
}
public class DB_List
{
    public int id;
    public string name;
    public Color color;
    public int place;
    public DB_List(int id, string name, Color color, int place)
    {
        this.id = id;
        this.name = name;
        this.color = color;
        this.place = place;
    }
}
public class DB_Link
{
    public int id_Anime;
    public int id_List;
}

