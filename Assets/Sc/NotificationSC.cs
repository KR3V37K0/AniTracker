using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Notifications.Android;
using UnityEngine;

public class NotificationSC : MonoBehaviour
{
    void Start()
    {
        CreateChannel();
        //SendNotification();
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
        not.FireTime = System.DateTime.Now.AddSeconds(10);
        id = id * 10000 + seria;
        AndroidNotificationCenter.SendNotificationWithExplicitID(not, "channel_id", id);
    }
}
