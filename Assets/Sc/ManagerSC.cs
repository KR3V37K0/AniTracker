using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Windows;

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
    public A_Starter starter;
    public DATABASE_SC db;

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

public class FastList
{
    public string id_list,name,russian;
    public List<Anime> animes;
}
public class DB_Anime
{
    public int id;
    public string name;
    public Series series;
    public DB_Anime(int id, string name, string series)
    {
        this.id = id;
        this.name = name;

        this.series=new Series();
        string[] parts = series.Split('/');
        this.series.viewved = int.Parse(parts[0]);
        this.series.aired = int.Parse(parts[1]);
        this.series.all = int.Parse(parts[2]);
    }
}
public class DB_List
{
    public int id;
    public string name;
    public Color color;
    public int place;
    public List<DB_Anime> animes;
    public DB_List(int id, string name, Color color, int place)
    {
        this.id = id;
        this.name = name;
        this.color = color;
        this.place = place;
        this.animes = new List<DB_Anime>();
    }
}
public class DB_Link
{
    public int id_Anime;
    public int id_List;
}

//API
public class Query_Search
{
    public string search, mylist, excludeids, franchise,season,order= "ranked",censure="true";
    public float score=1;
    public int page=1;
    public List<string>kind=new List<string>();
    public List<GenreData> genre = new List<GenreData>();
    public List<string> status = new List<string>();
    public List<string> rating = new List<string>();

    public string title = "";
    public string genre_title="";
    public string apply()
    {
        generate_title();

        string query = $@"
        query {{
              animes(limit: 18 ";

        query += $@", page: {page}";
        query += $@", search: ""{search}""";
        if (status.Count > 0)  query += $@", status: ""{List_toString(status)}""";
        if (kind.Count>0) query += $@", kind: ""{List_toString(kind)}""";
        if (genre.Count > 0) query += $@", genre: ""{Genres_toString()}""";
        if (mylist != null) query += $@", mylist: ""{mylist}""";
        if (franchise != null) query += $@", excludeIds: ""{franchise}""";
        query += $@", score: {score}";
        if (season != null) query += $@", season: ""{season}""";
        if (rating.Count >0) query += $@", rating: ""{List_toString(rating)}""";
        query += $@", order: {order}";

        query +=$@", censored: {censure}) 
                    {{
                        id
                        name
                        russian
                        poster {{ originalUrl }}
                    }}                
             }}";
        return query;
    }
    public string List_toString(List<string>list) 
    {
        string s="";
        foreach (string item in list)
        {
            s += item + ",";
        }
        s = s.Remove(s.Length - 1);
        return s;
    }
    string Genres_toString() 
    {
        string s="";
        foreach(GenreData g in genre)
        {
            s += g.id + ",";
        }
        s = s.Remove(s.Length - 1);
        return s;
    }
    void generate_title()
    {
        title = "";

        if (status.Count == 1)
        {
            switch (status[0])
            {
                case "anons":
                    title += "анонсирован ";
                    break;
                case "ongoing":
                    title += "сейчас выходит ";
                    break;
                case "released":
                    title += "уже вышел ";
                    break;
            }
        }
        if (kind.Count == 1)
        {
            switch (kind[0])
            {
                case "movie":
                    title += "фильм ";
                    break;
                case "tv":
                    title += "сериал ";
                    break;
                case "ona":
                    title += "ona ";
                    break;
                case "ova":
                    title += "ova ";
                    break;
                case "special,tv_special":
                    title += "спешл ";
                    break;
                case "music":
                    title += "музыка ";
                    break;
                case "pv":
                    title += "промо ";
                    break;
            }
        }

        if (search != null)
        { 
            if(search!="")  title += "'" + search + "'"; 
        }
        else if(genre.Count == 1)
        {
            title += genre_title + " ";
        }

        if (title != "") title = char.ToUpper(title[0]) + title.Substring(1);
    }
}

