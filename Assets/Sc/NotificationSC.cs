using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Notifications.Android;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;

public class NotificationSC : MonoBehaviour
{
    public List<Tracker> trackers;
    public List<Anime> trackes_anime;
    ManagerSC manager;
    private void Awake()
    {
        manager = GetComponent<ManagerSC>();
        CreateChannel();
        //SendNotification();
    }
    public async Task get_Trackers()
    {
        // Получаем список отслеживаемых аниме, если еще не загружен
        trackers = await manager.db.get_Tracking(); 

        List<string> ids = new List<string>();
        foreach(Tracker tracker in trackers)
        {
            ids.Add(tracker.id_Anime+"");
        }
        StartCoroutine( manager.api.Get_BIGminiAnime(ids,(animes)=>trackes_anime=animes));
        while (trackes_anime == null) await Task.Delay(100);

    }
    void CreateChannel()
    {
        var channel = new AndroidNotificationChannel()
        {
            Id = "channel_id",
            Name = "default",
            Importance = Importance.High,
            Description = "generic",
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);
    }
    public void SendNotification(int id, string name, int seria,DateTime date)
    {
        var not = new AndroidNotification();
        if(seria != 0)
        {
            not.Title = "Вышла новая серия!";
            not.Text = seria+" серия "+name;
        }
        else
        {
            not.Title = "Оно вышло!";
            not.Text = name;
        }
        not.FireTime = date;
        id = id * 10000 + seria;
        AndroidNotificationCenter.SendNotificationWithExplicitID(not, "channel_id", id);
    }

    public async Task changes(AnimeDetails anime, bool bell)
    {


        // Проверяем, есть ли уже это аниме в уведомлениях
        var existingTracker = trackers.Find(track => track.id_Anime.ToString() == anime.main.id);
        bool alreadyTracking = existingTracker != null;

        // Если статус не изменился
        if (bell == alreadyTracking)
        {
            Debug.LogError(anime.main.russian + " уже " + (bell ? "в уведомлениях" : "не в уведомлениях"));
            return;
        }

        if (anime.episodes == 0) anime.episodes = anime.episodesAired + 10;

        if (bell)
        {
            // Добавляем уведомления
            Debug.Log($" уведомления для {anime.main.name} в процессе");
            await AddAnimeNotifications(anime);
            Debug.Log($"Добавлены уведомления для {anime.main.name}");
        }
        else
        {
            Debug.Log($"Удаление уведомления для {anime.main.name} в процессе");
            // Удаляем все уведомления для этого аниме
            RemoveAnimeNotifications(anime);
            Debug.Log($"Удалены уведомления для {anime.main.name}");
        }
    }

    private async Task AddAnimeNotifications(AnimeDetails anime)
    {
        DateTime anime_first_seria = DateTime.Parse(anime.airedOn.date);

        //await manager.db.AddTracker(anime.main.id, anime.main.name, anime.main.aired_on, anime.main.episodes);

        // Обновляем локальный список трекеров
        if (trackers == null) trackers = new List<Tracker>();
        Tracker track = new Tracker(

            int.Parse(anime.main.id),
            anime_first_seria,
            anime.episodes
                );
        trackers.Add(track);
        manager.db.AddTracking(track,anime);

        // Создаем уведомления для каждой серии
        for (int seria = 1; seria <= anime.episodes; seria++)
        {
            // Рассчитываем дату выхода серии (первая серия + недели)
            DateTime seriaDate = anime_first_seria.AddDays(7 * (seria - 1));

            // Отправляем уведомление в день выхода
            SendNotification(
                id: int.Parse(anime.main.id),
                name: anime.main.russian ?? anime.main.name,
                seria: seria,
                date: seriaDate // Уведомление в день выхода
            );
        }
    }

    private void RemoveAnimeNotifications(AnimeDetails anime)
    {
        DateTime anime_first_seria = DateTime.Parse(anime.airedOn.date);
        Tracker track = new Tracker(

                            int.Parse(anime.main.id),
                            anime_first_seria,
                            anime.episodes
                                );
        // Удаляем из базы
        manager.db.DeleteTracking(track);

        // Удаляем из локального списка
        trackers.RemoveAll(t => t.id_Anime.ToString() == anime.main.id);

        // Отменяем все запланированные уведомления для этого аниме
        int baseId = int.Parse(anime.main.id) * 10000;
        for (int i = 0; i < 100; i++) // Предполагаем максимум 100 серий
        {
            AndroidNotificationCenter.CancelNotification(baseId + i);
        }
    }
    
    public async Task<bool> hasNotify(string opened_s)
    {
        int opened = int.Parse(opened_s);
        foreach(Tracker track in trackers)
        {
            if(track.id_Anime==opened)return true;
        }
        return false;
    }

}
