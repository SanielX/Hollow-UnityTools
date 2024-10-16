#if UNITY_EDITOR
using Hollow;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace HollowEditor
{
    /// 
    /// It would not make much sense to make special drawer for UniqueTag alone since it will not contain original string representation,
    /// and that one doesn't live for too long. I tried to make a file that would serialize all used UniqueTags and then read again
    /// The problem is that with such approach you would annoy git too much
    /// 
    /// PropertyName is serialized by unity as string, special case. I can't do the same without adding string parameter
    /// Unity add custom serialization please!
    /// 

    [CustomPropertyDrawer(typeof(UniqueTagProperty))]
    public class UniqueTagPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            label = EditorGUI.BeginProperty(position, label, property);
            
            var textField = property.FindPropertyRelative("m_Text");

            EditorGUI.BeginChangeCheck();
            EditorGUI.DelayedTextField(position, textField, label);

            if (EditorGUI.EndChangeCheck())
            {
                textField.stringValue = textField.stringValue.Trim();
                property.serializedObject.ApplyModifiedProperties();
            }

            label.text = textField.stringValue;
            
            EditorGUI.EndProperty();
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var textFieldProperty = property.FindPropertyRelative("m_Text");
            var textField = new TextField(property.displayName, 16, false, false, '*');
            textField.BindProperty(textFieldProperty);

            return textField;
        }

        public static VisualElement CreatePropertyGUI(SerializedProperty property, string label)
        {
            var textFieldProperty = property.FindPropertyRelative("m_Text");
            var textField = new TextField(label, 16, false, false, '*');
            textField.BindProperty(textFieldProperty);

            return textField;
        }
    }

    /// This won't work
    // [CustomPropertyDrawer(typeof(UniqueTag))]
    // internal class UniqueTagDrawer : PropertyDrawer
    // {
    //     public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    //     {
    //         var a = property.FindPropertyRelative("_a");
    //         var b = property.FindPropertyRelative("_b");
    // 
    //         UniqueTag tagValue = default;
    //         tagValue._a = a.intValue;
    //         tagValue._b = b.intValue;
    // 
    //         string tagValueString = tagValue == UniqueTag.Null? "" : tagValue.ToString();
    //         var result = EditorGUI.DelayedTextField(position, label, tagValueString);
    //          
    //         if(result != tagValueString)
    //         {
    //             result = result.Trim();
    //             UniqueTag tag;
    //              
    //             if(string.IsNullOrEmpty(result))
    //             {
    //                 tag = default;
    //             }
    //             else if (int.TryParse(result, out int resultAsInt))
    //             {
    //                 tag = new(resultAsInt);
    //             }
    //             else
    //             {
    //                 tag = new(result);
    //             }
    // 
    //             a.intValue = tag._a;
    //             b.intValue = tag._b;
    // 
    //             property.serializedObject.ApplyModifiedProperties();
    //         }
    //     }
    // }
}
#endif 