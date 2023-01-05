using Cinemachine.Editor;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static UnityEditor.Rendering.FilterWindow;

[CustomEditor(typeof(ReferenceRegistry))]
public class ReferenceRegistryEditor : Editor
{
    [SerializeField] VisualTreeAsset m_UXMLAsset;
    [SerializeField] VisualTreeAsset m_UXMLListElement;
    public override VisualElement CreateInspectorGUI()
    {
        var root = new VisualElement();

        /*
        //var tabContainer = new VisualElement();
        //tabContainer.style.flexGrow = 0;
        //tabContainer.style.flexDirection = FlexDirection.Row;
        //tabContainer.style.height = 20;
        //tabContainer.style.paddingBottom = 
        //    tabContainer.style.paddingTop = 
        //    tabContainer.style.paddingLeft = 
        //    tabContainer.style.paddingRight = 1;

        //var editorTab = new Button();
        //editorTab.style.flexGrow = 1;
        //editorTab.style.height = 30;
        //editorTab.style.paddingBottom =
        //    editorTab.style.paddingTop =
        //    editorTab.style.paddingLeft =
        //    editorTab.style.paddingRight = 0;
        //editorTab.style.marginBottom =
        //    editorTab.style.marginTop =
        //    editorTab.style.marginLeft =
        //    editorTab.style.marginRight = 1;
        //editorTab.text = "Editor";

        //var inspectorTab = new Button();
        //inspectorTab.style.flexGrow = 1;
        //inspectorTab.style.height = 30;
        //inspectorTab.style.paddingBottom =
        //    inspectorTab.style.paddingTop =
        //    inspectorTab.style.paddingLeft =
        //    inspectorTab.style.paddingRight = 0;
        //inspectorTab.style.marginBottom =
        //    inspectorTab.style.marginTop =
        //    inspectorTab.style.marginLeft =
        //    inspectorTab.style.marginRight = 1;
        //inspectorTab.text = "Inspector";
        */

        //InspectorElement.FillDefaultInspector(root, serializedObject, this);

        m_UXMLAsset.CloneTree(root);

        var inspectorParent = root.Q("unityInspector");
        if(inspectorParent!=null)
            InspectorElement.FillDefaultInspector(inspectorParent, serializedObject, this);

        //var prefabListView = root.Q<ListView>("prefabDict");
        //var prefabListProp = serializedObject.FindProperty("m_PrefabList");
        //prefabListView.itemsSource = (target as ReferenceRegistry).m_PrefabList;
        //prefabListView.bindingPath = "m_PrefabList";

        //prefabListView.makeItem = () =>
        //{
        //    var element = root.Q("listElement");
        //    var objField = element.Q<ObjectField>();
        //    objField.label = "Prefab";
        //    objField.objectType = typeof(GameObject);
        //    return element;
        //};

        //prefabListView.bindItem = (VisualElement e, int index) =>
        //{
        //    var addressField = e.Q<TextField>();
        //    addressField.bindingPath = "key";

        //    var objField = e.Q<ObjectField>();
        //    objField.bindingPath = "value";
        //};

        return root;
    }
}
