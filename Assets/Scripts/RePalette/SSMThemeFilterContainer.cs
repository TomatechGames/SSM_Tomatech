using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tomatech.RePalette;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.UIElements;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

public class SSMThemeFilterContainer : ThemeFilterContainer<SSMThemeFilter> { }

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(SSMThemeFilter))]
public class SSMThemeFilterPropertyDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var root = new Box() { name="PropContainer"};
        var applyProps = false;
        root.Add(new Label("Theme Filter") { name="PropHeader"});

        var styleProp = property.FindPropertyRelative("gameStyle");
        if (!SSMThemeFilter.gameStyles.Contains(styleProp.stringValue))
        {
            styleProp.stringValue = SSMThemeFilter.gameStyles.First();
            applyProps = true;
        }
        var styleField = new PopupField<string>(styleProp.displayName, SSMThemeFilter.gameStyles, SSMThemeFilter.gameStyles.IndexOf(styleProp.stringValue));
        styleField.formatListItemCallback = s => s;
        styleField.formatSelectedValueCallback = s => s;
        styleField.RegisterValueChangedCallback(s =>
        {
            styleProp.stringValue = s.newValue;
            property.serializedObject.ApplyModifiedProperties();
        });
        styleField.AddToClassList(BaseField<string>.alignedFieldUssClassName);
        root.Add(styleField);

        var keyProp = property.FindPropertyRelative("theme");
        if (!SSMThemeFilter.themeKeys.Contains(keyProp.stringValue))
        {
            keyProp.stringValue = SSMThemeFilter.themeKeys.First();
            applyProps= true;
        }
        var keyField = new PopupField<string>(keyProp.displayName, SSMThemeFilter.themeKeys, SSMThemeFilter.themeKeys.IndexOf(keyProp.stringValue));
        keyField.formatListItemCallback = FormatTheme;
        keyField.formatSelectedValueCallback = FormatTheme;
        keyField.RegisterValueChangedCallback(s =>
        {
            keyProp.stringValue = s.newValue;
            property.serializedObject.ApplyModifiedProperties();
        });
        keyField.AddToClassList(BaseField<string>.alignedFieldUssClassName);
        root.Add(keyField);

        if(applyProps)
            property.serializedObject.ApplyModifiedProperties();
        return root;
    }

    string FormatTheme(string rawTheme) => rawTheme.Split(SSMThemeFilter.inh).First();
}
#endif

[System.Serializable]
public class SSMThemeFilter : ThemeFilterBase
{
    // splits themes and game styles
    internal const char ts = '-';
    // splits themes from their inherited themes
    internal const char inh = '~';

    public static List<string> gameStyles = new() { 
        "SMB1",
        "SMB3",
        "SMW",
        "NSMB"
    };
    public static List<string> themeKeys = new() {
        "Overworld",
        "Underground"+inh+"Overworld",
        "Water"+inh+"Overworld",
        "Ghost House"+inh+"Overworld",
        "Airship"+inh+"Overworld",
        "Castle"+inh+"Overworld"
    };

    public string gameStyle="";
    public string theme="";

    public override string ThemeKey =>  gameStyle+ ts + theme.Split(inh).First();

    public override string GetInheritedThemeKeys(Func<string, bool> validator) =>
        TryGetInheritedThemeKeys(theme, validator);

    string TryGetInheritedThemeKeys(string fromTheme, Func<string, bool> validator)
    {
        var keyList = gameStyle + ts + fromTheme;
        //Debug.Log(keyList);
        if (validator(keyList))
            return keyList;
        else if (fromTheme.Split(inh).Length == 2)
            return TryGetInheritedThemeKeys(themeKeys.First(t => t.Split(inh).First() == fromTheme.Split(inh).Last()), validator);
        else
            return null;
    }

    public override Task<IResourceLocation> GetThemeAssetLocation(string objectKey, Type typeFilter) =>
        TryGetThemeAssetLocation(theme, objectKey);

    async Task<IResourceLocation> TryGetThemeAssetLocation(string fromTheme, string objectKey)
    {
        var fullKey = gameStyle + ts + fromTheme.Split(inh).First();
        Debug.Log($"Scanning {fullKey} for {objectKey}");
        var locationHandle = Addressables.LoadResourceLocationsAsync(new List<string>() { "RPt_"+fullKey, "RPe_"+objectKey }, Addressables.MergeMode.Intersection).Task;
        await locationHandle;
        Debug.Log("awaited");
        if (locationHandle.Result.Count == 0)
        {
            if (fromTheme.Split(inh).Length == 2)
            {
                var parentHandle = TryGetThemeAssetLocation(themeKeys.First(t => t.Split(inh).First() == fromTheme.Split(inh).Last()), objectKey);
                await parentHandle;
                return parentHandle.Result;
            }
            else
            {
                Debug.Log("boomer");
                return null;
            }
        }
        locationHandle.Result.ToList().ForEach(location => Debug.Log(location.PrimaryKey));
        return locationHandle.Result[0];
    }
}
