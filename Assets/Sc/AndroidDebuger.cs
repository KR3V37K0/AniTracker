using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class MobileDebug : MonoBehaviour
{
    [Header("UI компонент для отображения лога")]
    [SerializeField] private TMP_Text debugText;

    [Header("Максимум строк в выводе")]
    [SerializeField] private int maxLines = 100;

    [Header("Показывать в билде")]
    [SerializeField] private bool showInBuild = true;

    private static MobileDebug _instance;
    private static List<string> _logs = new List<string>();

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);

            if (showInBuild)
                Application.logMessageReceived += HandleUnityLog;

            AddLog("Debug Console Initialized");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (_instance == this)
            Application.logMessageReceived -= HandleUnityLog;
    }

    private void HandleUnityLog(string message, string stackTrace, LogType type)
    {
        if (type == LogType.Warning) return;

        string prefix = type == LogType.Error ? "ERROR: " : "";
        AddLog(prefix + message);
    }

    private void AddLog(string message)
    {
        _logs.Add(message);
        if (_logs.Count > maxLines)
            _logs.RemoveAt(0);

        if (debugText != null)
        {
            debugText.text = string.Join("\n", _logs);
        }
    }

    // Внешний вызов
    public static void Log(string message)
    {
        Debug.Log(message);
        if (_instance != null)
        {
            _instance.AddLog(message);
        }
    }

    public static void LogError(string message)
    {
        Debug.LogError(message);
        if (_instance != null)
        {
            _instance.AddLog("<color=red>" + message + "</color>");
        }
    }

}
