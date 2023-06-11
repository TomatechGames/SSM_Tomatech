using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tomatech.AFASS;
using Tomatech.RePalette;
using UnityEngine;

public class CourseAreaManager : MonoBehaviour
{
    [SerializeField]
    SaveManager saveManager;

    [ContextMenu("Save")]
    public void StartSave()
    {
        GUIUtility.systemCopyBuffer = saveManager.Save().ToString();
    }

    [ContextMenu("Load")]
    public void StartLoad()
    {
        var task = LoadTask(JSON.Parse(GUIUtility.systemCopyBuffer).AsObject);
        if (task.Exception is not null)
            Debug.LogException(task.Exception);
    }

    async Task LoadTask(JSONObject levelData)
    {
        await RepaletteResourceManager.UpdateRegisteredThemeables();
        await saveManager.Load(levelData);
    }
}
