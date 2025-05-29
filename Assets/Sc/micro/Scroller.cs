using System.Collections;
using System.Collections.Generic;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UI;

public class Scroller : MonoBehaviour
{
    public ManagerSC manager;
    public ScrollRect scrollRect;
    public float threshold = 0.01f; // насколько близко к низу
    private bool hasCalledEnd = false;
    void Start()
    {
        StartCoroutine(Ini());
    }
    IEnumerator Ini()
    {
        Debug.Log("____SCROLL start init");
        
        Debug.Log("____SCROLL init");
        scrollRect.onValueChanged.AddListener(OnScrollValueChanged);
        yield return new WaitForSeconds(4f);
    }

    void OnScrollValueChanged(Vector2 scrollPos)
    {
        // Только когда вниз (vertical scroll, horizontal будет scrollPos.x)
        if (!hasCalledEnd && scrollRect.verticalNormalizedPosition <= threshold)
        {
            hasCalledEnd = true;
            OnScrolledToBottom();
        }
        else if (scrollRect.verticalNormalizedPosition > threshold)
        {
            // Сброс, если пользователь отскроллил вверх
            StartCoroutine(cooldown());
        }
    }
    IEnumerator cooldown()
    {
        yield return new WaitForSeconds(3f);
        hasCalledEnd = false;
    }
    void OnScrolledToBottom()
    {
        Debug.Log("____SCROLL reached bottom!");
        if (ConnectionData.currentSearch != null)
        {
            ConnectionData.currentSearch.page++;
            StartCoroutine(manager.api.SearchResult());
        }

    }
}
