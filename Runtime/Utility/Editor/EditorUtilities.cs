#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using Hollow.Extensions;
using UnityEditor;

namespace HollowEditor
{
    public static class EditorUtilities
    {
        /// <summary>
        /// Makes sure that folder exists by creating them if needed and putting .gitkeep inside the last one
        /// </summary>
        /// <param name="folderPath">Folder path in format 'Assets/Folder'</param>
        public static bool MakeSureFolderIsValid(string folderPath, bool putgitkeep = false)
        {
            if(folderPath.IsNullOrEmpty() || !folderPath.StartsWith("Assets/"))
            {
                return false;
            }

            var splitPath = folderPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

            for (int i = 1; i < splitPath.Length; i++)
            {
                var subfolder = string.Join('/', splitPath, 0, i+1);
                if(!AssetDatabase.IsValidFolder(subfolder))
                {
                    AssetDatabase.CreateFolder(string.Join('/', splitPath, 0, i), splitPath[i]);
                }
            }

            if(putgitkeep)
                System.IO.File.Create(folderPath + "/.gitkeep").Close();

            return true;
        }

        public static T GetValue<T>(this SerializedProperty prop)
        {
            return GetObjectFromProperty<T>(prop, out _);
        }

        public static FieldInfo GetFieldInfo(this SerializedProperty prop)
        {
            GetObjectFromProperty<object>(prop, out var info);
            return info;
        }

        // https://forum.unity.com/threads/loop-through-serializedproperty-children.435119/
        /// <summary>
        /// Gets visible children of `SerializedProperty` at 1 level depth.
        /// </summary>
        /// <param name="serializedProperty">Parent `SerializedProperty`.</param>
        /// <returns>Collection of `SerializedProperty` children.</returns>
        public static IEnumerable<SerializedProperty> GetVisibleChildren(this SerializedProperty serializedProperty)
        {
            SerializedProperty currentProperty = serializedProperty.Copy();
            SerializedProperty nextSiblingProperty = serializedProperty.Copy();
            {
                nextSiblingProperty.NextVisible(false);
            }

            if (currentProperty.NextVisible(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(currentProperty, nextSiblingProperty))
                        break;

                    yield return currentProperty.Copy();
                }
                while (currentProperty.NextVisible(false));
            }
        }

        public static float GetHeight(this SerializedProperty prop, bool withVerticalSpacing = true)
        {
            return withVerticalSpacing ? EditorGUI.GetPropertyHeight(prop) + EditorGUIUtility.standardVerticalSpacing : EditorGUI.GetPropertyHeight(prop);
        }

        public static Type FindPropertyDrawer(Type propertyType)
        {
            Type drawerType = null;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = asm.GetType("UnityEditor.ScriptAttributeUtility");
                if (type != null)
                {
                    var method = type.GetMethod("GetDrawerTypeForType", BindingFlags.Static | BindingFlags.NonPublic, null,
                                                new Type[] { typeof(Type) }, null);
                    drawerType = (Type)method.Invoke(null, new object[] { propertyType });

                    if(drawerType is null)
                    {
                        var buildMethod = type.GetMethod("BuildDrawerTypeForTypeDictionary", BindingFlags.Static | BindingFlags.NonPublic);
                        buildMethod.Invoke(null, null);

                        drawerType = (Type)method.Invoke(null, new object[] { propertyType });
                    }
                    break;
                } 
            }

            return drawerType;
        }

        public static PropertyDrawer CreateDrawerForProperty(SerializedProperty prop)
        {
            _ = GetObjectFromProperty<object>(prop, out FieldInfo info);
            var propertyType = info.FieldType;

            Type drawerType = null;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = asm.GetType("UnityEditor.ScriptAttributeUtility");
                if (type != null)
                {
                    var method = type.GetMethod("GetDrawerTypeForPropertyAndType", BindingFlags.Static | BindingFlags.NonPublic, null,
                                                new Type[] { typeof(SerializedProperty), typeof(Type) }, null);
                    drawerType = (Type)method.Invoke(null, new object[] { prop, propertyType });
                    break;
                }
            }

            if (drawerType is null)
                return null;

            return (PropertyDrawer)Activator.CreateInstance(drawerType);
        }

        // Source: https://github.com/lordofduct/spacepuppy-unity-framework/blob/master/SpacepuppyBaseEditor/EditorHelper.cs

        public static T GetObjectFromProperty<T>(SerializedProperty prop, out FieldInfo fieldInfo)
        {
            fieldInfo = null;
            if (prop == null) return default(T);

            var path = prop.propertyPath.Replace(".Array.data[", "[");
            return GetObjectFromPath<T>(prop.serializedObject.targetObject, path, out fieldInfo);
        }

        public static T GetObjectFromProperty<T>(SerializedProperty prop, out FieldInfo fieldInfo, out object parent)
        {
            fieldInfo = null;
            parent = null;
            if (prop == null) return default(T);

            var path = prop.propertyPath.Replace(".Array.data[", "[");
            return GetObjectFromPath<T>(prop.serializedObject.targetObject, path, out fieldInfo, out parent);
        }

        public static T GetObjectFromPath<T>(UnityEngine.Object targetObject, string path, out FieldInfo fieldInfo, out object parent)
        {
            fieldInfo = null;
            object obj = targetObject;
            parent = obj;

            var elements = path.Split('.');
            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    parent = obj;
                    obj = GetValue_Imp(obj, elementName, index, out fieldInfo);
                }
                else
                {
                    parent = obj;
                    obj = GetValue_Imp(obj, element, out fieldInfo);
                }
            }
            return (T)obj;
        }


        public static T GetObjectFromPath<T>(UnityEngine.Object targetObject, string path, out FieldInfo fieldInfo) 
        {
            fieldInfo = null;
            object obj = targetObject;
            var elements = path.Split('.');
            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("["));
                    var index = System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
                    obj = GetValue_Imp(obj, elementName, index, out fieldInfo);
                }
                else
                {
                    obj = GetValue_Imp(obj, element, out fieldInfo);
                }
            }
            Type t = typeof(T);

            return (T)obj;
        }

        private static object GetValue_Imp(object source, string name, out FieldInfo fieldInfo)
        {
            fieldInfo = null;
            if (source == null)
                return null;
            var type = source.GetType();

            while (type != null)
            {
                var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
                if (f != null)
                {
                    fieldInfo = f;
                    return f.GetValue(source);
                }
                var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null)
                {
                    return p.GetValue(source, null);
                }

                type = type.BaseType;
            }
            return null;
        }
        private static object GetValue_Imp(object source, string name, int index, out FieldInfo info)
        {
            var enumerable = GetValue_Imp(source, name, out info) as System.Collections.IEnumerable;
            if (enumerable == null) return null;
            var enm = enumerable.GetEnumerator();
            //while (index-- >= 0)
            //    enm.MoveNext();
            //return enm.Current;

            for (int i = 0; i <= index; i++)
            {
                if (!enm.MoveNext()) return null;
            }
            return enm.Current;
        }
    }
}
#endif 