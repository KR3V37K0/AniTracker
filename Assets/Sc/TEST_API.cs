using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class ShikimoriAPI : MonoBehaviour
{
    void OnEnable()
    {
        // ������������� �� ������� �������� �����������
        ConnectionData.OnUserAuthenticated += OnUserAuthenticated;
        Debug.Log("Sub");
    }

    void OnDisable()
    {
        // ������������ �� �������, ����� �������� ������ ������
        ConnectionData.OnUserAuthenticated -= OnUserAuthenticated;
        Debug.Log("UnSub");
    }


    void OnUserAuthenticated()
    {
        // ������ �������� ��� ������� ������
        StartCoroutine(GetUserData());
    }

    IEnumerator GetUserData()
    {
        string url = "https://shikimori.one/api/users/whoami";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            // ��������� ��������� � access_token
            request.SetRequestHeader("Authorization", "Bearer " + ConnectionData.TOKEN.access_token);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"? ������ ������������: {request.downloadHandler.text}");
            }
            else
            {
                Debug.LogError($"? ������ �������: {request.error}");
            }
        }
    }

    // ����� ��� �������������� ������ ������������
    [System.Serializable]
    public class UserData
    {
        public string nickname;
        // ������ ������ ����, ������� ���� ����� �� ������
    }
}
