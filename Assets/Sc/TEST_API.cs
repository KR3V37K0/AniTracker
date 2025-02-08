using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class ShikimoriAPI : MonoBehaviour
{
    void OnEnable()
    {
        // Подписываемся на событие успешной авторизации
        ConnectionData.OnUserAuthenticated += OnUserAuthenticated;
        Debug.Log("Sub");
    }

    void OnDisable()
    {
        // Отписываемся от события, чтобы избежать утечек памяти
        ConnectionData.OnUserAuthenticated -= OnUserAuthenticated;
        Debug.Log("UnSub");
    }


    void OnUserAuthenticated()
    {
        // Запуск корутины для запроса данных
        StartCoroutine(GetUserData());
    }

    IEnumerator GetUserData()
    {
        string url = "https://shikimori.one/api/users/whoami";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            // Добавляем заголовок с access_token
            request.SetRequestHeader("Authorization", "Bearer " + ConnectionData.TOKEN.access_token);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"? Данные пользователя: {request.downloadHandler.text}");
            }
            else
            {
                Debug.LogError($"? Ошибка запроса: {request.error}");
            }
        }
    }

    // Класс для десериализации данных пользователя
    [System.Serializable]
    public class UserData
    {
        public string nickname;
        // Добавь другие поля, которые тебе нужны из ответа
    }
}
