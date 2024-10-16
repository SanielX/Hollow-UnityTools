#if UNITY_EDITOR
using Hollow;
using UnityEngine;
using UnityEditor;
using System;

namespace HollowEditor
{
    [CustomPropertyDrawer(typeof(SGuid))]
    internal class SGuidDrawer : PropertyDrawer
    {
        readonly byte[] guidBytes = new byte[16];

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            var rect = EditorGUI.PrefixLabel(position, label);

            var children = property.Copy();
            children.NextVisible(true);

            var _a = children.intValue;
            children.NextVisible(false);
            var _b = children.intValue;
            children.NextVisible(false);
            var _c = children.intValue;
            children.NextVisible(false);
            var _d = children.intValue;
            children.NextVisible(false);
            var _e = children.intValue;
            children.NextVisible(false);
            var _f = children.intValue;
            children.NextVisible(false);
            var _g = children.intValue;
            children.NextVisible(false);
            var _h = children.intValue;
            children.NextVisible(false);
            var _i = children.intValue;
            children.NextVisible(false);
            var _j = children.intValue;
            children.NextVisible(false);
            var _k = children.intValue;

            System.Guid guid = new(_a, (short)_b, (short)_c, (byte)_d, (byte)_e, (byte)_f, (byte)_g, (byte)_h, (byte)_i, (byte)_j, (byte)_k);

            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
            
            EditorGUI.SelectableLabel(rect, guid.ToString("b"));
            
            EditorGUI.indentLevel = indent;
            
            EditorGUI.EndProperty();
        }
    }
}
#endif 