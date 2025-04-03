using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;


public class FlyOut : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    bool _isPressed = false;
    [SerializeField] RectTransform panel;
    float panel_10_pos, newY=0;
    Vector2 screenSize=new Vector2(0,0);

    public void OnPointerDown(PointerEventData eventData)
    {
        _isPressed = true;
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        _isPressed = false;
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (_isPressed)
        {
            if(screenSize.y==0)screenSize = new Vector2(Screen.width, Screen.height);
            panel_10_pos = panel.position.y + 5f;
            newY = Mathf.Clamp(eventData.position.y / screenSize.y * 10f - 4.8f, -5f, 4.8f);
            if (newY < -3f) close_panel();
            panel.position = new Vector3(0 ,newY,0);
            panel.sizeDelta = new Vector2(panel.sizeDelta.x,eventData.position.y);
        }
    }
    void open_panel()
    {

    }
    void close_panel()
    {
        gameObject.transform.parent.gameObject.SetActive(false);
    }
}
