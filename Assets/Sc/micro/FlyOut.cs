using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using PrimeTween;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;

public class FlyOut : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    public bool canClose = true;
    bool _isPressed = false, _inAnim=false;
    [SerializeField] RectTransform panel;
    [SerializeField] Image img_back;
    float panel_10_pos, newY = 0;
    Vector2 screenSize = new Vector2(0, 0);

    public void OnPointerDown(PointerEventData eventData)
    {
        if(!_inAnim)
            _isPressed = true;
    }
    public void OnPointerUp(PointerEventData eventData)
    {
        _isPressed = false;
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (_isPressed && !_inAnim)
        {
            if (screenSize.y == 0) screenSize = new Vector2(Screen.width, Screen.height);
            move_to(eventData.position.y);
        }
    }
    public IEnumerator open_panel()
    {
        _inAnim = true;
        if (screenSize.y == 0) screenSize = new Vector2(Screen.width, Screen.height);

        move_to(0);

        float elapsed = 0f;
        Vector3 startPos = new Vector3(0, 0, 0);
        Vector3 targetPos = new Vector3(0, screenSize.y/2f, 0);

        Tween.Alpha(img_back, 0.8f,1f);

        while (elapsed < 0.7f)
        {
            move_to(Vector3.Lerp(startPos, targetPos, elapsed / 0.7f).y);
            elapsed += Time.deltaTime;
            yield return null;
        }

        _inAnim = false;
        yield return null;
    }
    public void btn_close()
    {
        StartCoroutine(close_panel());
    }
    public IEnumerator close_panel()
    {
        if (canClose)
        {
            _inAnim = true;
            float elapsed = 0.7f;
            Vector3 startPos = new Vector3(0, 0, 0);
            Vector3 targetPos = new Vector3(0, panel.rect.position.y, 0);

            Tween.Alpha(img_back, 0f, 1f);

            while (elapsed > 0)
            {
                move_to(Vector3.Lerp(startPos, targetPos, elapsed / 0.7f).y);
                elapsed -= Time.deltaTime;
                yield return null;
            }

            gameObject.transform.parent.gameObject.SetActive(false);
            _inAnim = false;
            yield return null;
        }
        yield return null;
    }
    void move_to(float Y)
    {
        panel_10_pos = panel.position.y + 5f;
        newY = Mathf.Clamp(Y / screenSize.y * 10f - 4.8f, -5f, 4.8f);
        if (newY < -3f && _isPressed && !_inAnim) StartCoroutine(close_panel());
        panel.position = new Vector3(0, newY, 0);
        panel.sizeDelta = new Vector2(panel.sizeDelta.x, Y);
    }
}