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
    PhysicsSimulator simulator;
    [SerializeField]
    SaveManager saveManager;

    [SerializeField]
    SSMThemeFilter themeFilter;
    [SerializeField]
    bool loadOnStart;
    [SerializeField]
    [TextArea]
    string dataToLoadOnStart;

    private void Start()
    {
        RepaletteResourceManager.SetThemeFilter(themeFilter);
        RepaletteResourceManager.onThemeUpdateCompleted += RefreshTilemaps;
        if (loadOnStart)
            StartLoad(dataToLoadOnStart);
        else
            UpdateTheme();
    }

    [ContextMenu("Update Theme")]
    public void UpdateTheme()
    {
        RepaletteResourceManager.UpdateRegisteredThemeables().JustRun();
    }

    void RefreshTilemaps()
    {
        foreach (var map in saveManager.SavableTilemaps)
        {
            map.RefreshAllTiles();
        }
    }

    [ContextMenu("Save")]
    public void StartSave()
    {
        GUIUtility.systemCopyBuffer = saveManager.Save().ToString();
    }

    [ContextMenu("Load")]
    public void StartLoad() => StartLoad(GUIUtility.systemCopyBuffer);
    public void StartLoad(string rawData) => LoadTask(JSON.Parse(rawData).AsObject).JustRun();

    async Task LoadTask(JSONObject levelData)
    {
        simulator.HaltSimulation();
        await saveManager.Load(levelData);
        await RepaletteResourceManager.UpdateRegisteredThemeables();
        simulator.ResumeSimulation();
    }
}
