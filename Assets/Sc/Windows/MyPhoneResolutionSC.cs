using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyPhoneResolutionSC : MonoBehaviour
{
    private void Start()
    {
        // ������ ���������� ���� 
        Screen.SetResolution(720, 1280,FullScreenMode.Windowed);
    }
}
