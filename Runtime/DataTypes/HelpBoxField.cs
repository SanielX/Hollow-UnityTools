using UnityEngine;

namespace Hollow
{
#if UNITY_EDITOR
using UnityEditor;
using HollowEditor;

[CustomPropertyDrawer(typeof(HelpBoxField))]
class HelpBoxFieldEditor : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing * 2;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var value = property.GetValue<HelpBoxField>();
        EditorGUI.HelpBox(position, value.text, MessageType.Info);
    }
}
    
#endif 
    
    [System.Serializable]
    public struct HelpBoxField
    {
        [SerializeField] int dummy;
        
        [System.NonSerialized] internal string text;
        
        public static implicit operator HelpBoxField(string s) => new() { text = s };
    }
}