using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class MobileDebug : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private TMP_Text debugText;
    [SerializeField] private int maxLines = 20;
    [SerializeField] private bool showInBuild = true;

    private static MobileDebug _instance;
    private static List<string> _logs = new List<string>();
    private static bool _initialized = false;

    // Статический конструктор для ранней инициализации
    static MobileDebug()
    {
        _logs = new List<string>();
    }

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            _initialized = true;
            
            // Перенаправляем стандартные логи
            if (showInBuild)
            {
                Application.logMessageReceived += HandleUnityLog;
            }
            
            UpdateDebugText();
            debugText.text = "Debug Console Initialized\n" + debugText.text;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (_instance == this)
        {
            Application.logMessageReceived -= HandleUnityLog;
            _instance = null;
            _initialized = false;
        }
    }

    private void HandleUnityLog(string message, string stackTrace, LogType type)
    {
        if (type == LogType.Warning) return;
        AddLog(type == LogType.Error ? $"ERROR: {message}" : message);
    }

    private static void AddLog(string message)
    {
        _logs.Add(message);
        
        if (_logs.Count > _instance.maxLines)
        {
            _logs.RemoveAt(0);
        }
        
        if (_initialized && _instance.debugText != null)
        {
            _instance.debugText.text = string.Join("\n", _logs);
            
            // Автопрокрутка
            /*Canvas.ForceUpdateCanvases();
            var scrollRect = _instance.debugText.GetComponentInParent<ScrollRect>();
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 0;
            }*/
        }
    }

    public static void Log(string message)
    {
        Debug.Log( "   "+ message);
        if (_initialized)
        {
            AddLog(message);
        }
        else
        {
            Debug.LogWarning("MobileDebug not initialized! Message: " + message);
        }
    }

    public static void LogError(string message)
    {
#if UNITY_EDITOR
        Debug.LogError(message);
#endif
        if (_initialized)
        {
            AddLog($"<color=red>{message}</color>");
        }
        else
        {
            Debug.LogError("MobileDebug not initialized! Error: " + message);
        }
    }

    private void UpdateDebugText()
    {
        if (debugText != null)
        {
            debugText.text = string.Join("\n", _logs);
        }
    }

    public void ClearLogs()
    {
        _logs.Clear();
        UpdateDebugText();
    }
}