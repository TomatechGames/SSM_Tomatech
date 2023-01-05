using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(ReferenceRegistry.AddressedObject<>))]
public class AddressedObjectPropertyDrawer : PropertyDrawer
{
    [SerializeField] VisualTreeAsset m_UXMLAsset;
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {

        var root = new VisualElement();
        root.style.flexDirection = FlexDirection.Row;
        root.Add(new TextField() { bindingPath = "key", style = { flexShrink = 1, flexGrow = 1, width = new Length(30, LengthUnit.Percent) } });
        root.Add(new PropertyField() { bindingPath = "value", label="", style = { flexShrink = 1, flexGrow = 1, width = new Length(70, LengthUnit.Percent) } });

        root.Bind(property.serializedObject);
        return root;
    }
}
