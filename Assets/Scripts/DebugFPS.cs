using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DebugFPS : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI fpsDisplay;
    public void SetFPS(float newFPS)
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = (int)newFPS;
        if(fpsDisplay)
            fpsDisplay.text = Application.targetFrameRate.ToString();
    }
}
