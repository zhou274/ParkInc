using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Editor = UnityEditor.Editor;

namespace Watermelon
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(UnityEngine.Object), true)]
    public sealed class WatermelonEditor : Editor
    {
        private IEnumerable<FieldInfo> fields;

        private HashSet<FieldInfo> groupedFields;
        private Dictionary<string, List<FieldInfo>> groupedFieldsByGroupName;

        private IEnumerable<FieldInfo> nonSerializedFields;
        private IEnumerable<MethodInfo> methods;

        private Dictionary<string, SerializedProperty> serializedPropertiesByFieldName;

        private bool useDefaultInspector;

        private static EditorCustomStyles styles;
        public static EditorCustomStyles Styles
        {
            get
            {
                if (styles == null)
                    styles = new EditorCustomStyles();

                return styles;
            }
        }

        private void OnEnable()
        {
            // Cache serialized fields
            fields = GetFields(f => serializedObject.FindProperty(f.Name) != null);

            if (fields.All(f => f.GetCustomAttributes(typeof(ExtendedEditorAttribute), true).Length == 0))
            {
                useDefaultInspector = true;
            }
            else
            {
                useDefaultInspector = false;

                // Cache grouped fields
                groupedFields = new HashSet<FieldInfo>(fields.Where(f => f.GetCustomAttributes(typeof(GroupAttribute), true).Length > 0));

                // Cache grouped fields by group name
                groupedFieldsByGroupName = new Dictionary<string, List<FieldInfo>>();
                foreach (var groupedField in groupedFields)
                {
                    string groupName = (groupedField.GetCustomAttributes(typeof(GroupAttribute), true)[0] as GroupAttribute).Name;

                    if (groupedFieldsByGroupName.ContainsKey(groupName))
                    {
                        groupedFieldsByGroupName[groupName].Add(groupedField);
                    }
                    else
                    {
                        groupedFieldsByGroupName[groupName] = new List<FieldInfo>()
                        {
                            groupedField
                        };
                    }
                }

                // Cache serialized properties by field name
                serializedPropertiesByFieldName = new Dictionary<string, SerializedProperty>();
                foreach (var field in fields)
                {
                    serializedPropertiesByFieldName[field.Name] = serializedObject.FindProperty(field.Name);
                }
            }

            // Cache non-serialized fields
            nonSerializedFields = GetFields(f => f.GetCustomAttributes(typeof(DrawerAttribute), true).Length > 0 && serializedObject.FindProperty(f.Name) == null);

            // Cache methods with DrawerAttribute
            methods = GetMethods(m => m.GetCustomAttributes(typeof(DrawerAttribute), true).Length > 0);
        }

        private void OnDisable()
        {
            CustomAttributesDatabase.Clear();
        }

        public override void OnInspectorGUI()
        {
            if (useDefaultInspector)
            {
                DrawDefaultInspector();
            }
            else
            {
                serializedObject.Update();

                SerializedProperty script = serializedObject.FindProperty("m_Script");
                if (script != null)
                {
                    using (new EditorGUI.DisabledScope(disabled: true))
                    {
                        EditorGUILayout.PropertyField(script);
                    }
                }

                // Draw fields
                HashSet<string> drawnGroups = new HashSet<string>();
                foreach (var field in fields)
                {
                    if (groupedFields.Contains(field))
                    {
                        // Draw grouped fields
                        string groupName = (field.GetCustomAttributes(typeof(GroupAttribute), true)[0] as GroupAttribute).Name;
                        if (!drawnGroups.Contains(groupName))
                        {
                            drawnGroups.Add(groupName);

                            PropertyGrouper grouper = GetPropertyGrouperForField(field);
                            if (grouper != null)
                            {
                                grouper.BeginGroup(groupName);

                                DrawFields(groupedFieldsByGroupName[groupName]);

                                grouper.EndGroup();
                            }
                            else
                            {
                                DrawFields(groupedFieldsByGroupName[groupName]);
                            }
                        }
                    }
                    else
                    {
                        // Draw non-grouped field
                        ApplyFieldMeta(field);
                        DrawField(field);
                    }
                }

                serializedObject.ApplyModifiedProperties();
            }

            // Draw non-serialized fields
            foreach (var field in nonSerializedFields)
            {
                DrawerAttribute drawerAttribute = (DrawerAttribute)field.GetCustomAttributes(typeof(DrawerAttribute), true)[0];
                FieldDrawer drawer = CustomAttributesDatabase.GetFieldAttribute(drawerAttribute.GetType());
                if (drawer != null)
                {
                    drawer.DrawField(target, field);
                }
            }

            // Draw methods
            foreach (var method in methods)
            {
                DrawerAttribute drawerAttribute = (DrawerAttribute)method.GetCustomAttributes(typeof(DrawerAttribute), true)[0];
                MethodDrawer methodDrawer = CustomAttributesDatabase.GetMethodAttribute(drawerAttribute.GetType());
                if (methodDrawer != null)
                {
                    methodDrawer.DrawMethod(target, method);
                }
            }
        }

        private void DrawFields(IEnumerable<FieldInfo> fields)
        {
            foreach (var field in fields)
            {
                ApplyFieldMeta(field);
                DrawField(field);
            }
        }

        private void DrawField(FieldInfo field)
        {
            // Check if the field has draw conditions
            PropertyDrawCondition drawCondition = GetPropertyDrawConditionForField(field);
            if (drawCondition != null)
            {
                bool canDrawProperty = drawCondition.CanDrawProperty(serializedPropertiesByFieldName[field.Name]);
                if (!canDrawProperty)
                {
                    return;
                }
            }

            // Check if the field has HideInInspectorAttribute
            HideInInspector[] hideInInspectorAttributes = (HideInInspector[])field.GetCustomAttributes(typeof(HideInInspector), true);
            if (hideInInspectorAttributes.Length > 0)
            {
                return;
            }

            // Draw the field
            EditorGUI.BeginChangeCheck();
            PropertyDrawer drawer = GetPropertyDrawerForField(field);
            if (drawer != null)
            {
                drawer.DrawProperty(serializedPropertiesByFieldName[field.Name]);
            }
            else
            {
                EditorDrawUtility.DrawPropertyField(serializedPropertiesByFieldName[field.Name]);
            }

            if (EditorGUI.EndChangeCheck())
            {
                OnValueChangedAttribute[] onValueChangedAttributes = (OnValueChangedAttribute[])field.GetCustomAttributes(typeof(OnValueChangedAttribute), true);
                foreach (var onValueChangedAttribute in onValueChangedAttributes)
                {
                    PropertyMeta meta = CustomAttributesDatabase.GetMetaAttribute(onValueChangedAttribute.GetType());
                    if (meta != null)
                    {
                        meta.ApplyPropertyMeta(serializedPropertiesByFieldName[field.Name], onValueChangedAttribute);
                    }
                }
            }
        }

        private void ApplyFieldMeta(FieldInfo field)
        {
            // Apply custom meta attributes
            MetaAttribute[] metaAttributes = field.GetCustomAttributes(typeof(MetaAttribute), true).Where(attr => attr.GetType() != typeof(OnValueChangedAttribute)).Select(obj => obj as MetaAttribute).ToArray();

            Array.Sort(metaAttributes, (x, y) => { return x.Order - y.Order; });

            foreach (var metaAttribute in metaAttributes)
            {
                PropertyMeta meta = CustomAttributesDatabase.GetMetaAttribute(metaAttribute.GetType());
                if (meta != null)
                {
                    meta.ApplyPropertyMeta(serializedPropertiesByFieldName[field.Name], metaAttribute);
                }
            }
        }

        private PropertyDrawer GetPropertyDrawerForField(FieldInfo field)
        {
            DrawerAttribute[] drawerAttributes = (DrawerAttribute[])field.GetCustomAttributes(typeof(DrawerAttribute), true);
            if (drawerAttributes.Length > 0)
            {
                PropertyDrawer drawer = CustomAttributesDatabase.GetPropertyAttribute(drawerAttributes[0].GetType());
                return drawer;
            }
            else
            {
                return null;
            }
        }

        private PropertyGrouper GetPropertyGrouperForField(FieldInfo field)
        {
            GroupAttribute[] groupAttributes = (GroupAttribute[])field.GetCustomAttributes(typeof(GroupAttribute), true);
            if (groupAttributes.Length > 0)
            {
                PropertyGrouper grouper = CustomAttributesDatabase.GetGroupAttribute(groupAttributes[0].GetType());

                return grouper;
            }
            else
            {
                return null;
            }
        }

        private PropertyDrawCondition GetPropertyDrawConditionForField(FieldInfo field)
        {
            DrawConditionAttribute[] drawConditionAttributes = (DrawConditionAttribute[])field.GetCustomAttributes(typeof(DrawConditionAttribute), true);
            if (drawConditionAttributes.Length > 0)
            {
                PropertyDrawCondition drawCondition = CustomAttributesDatabase.GetDrawConditionAttribute(drawConditionAttributes[0].GetType());

                return drawCondition;
            }
            else
            {
                return null;
            }
        }

        private List<FieldInfo> GetFields(Func<FieldInfo, bool> predicate)
        {
            List<Type> types = new List<Type>() { target.GetType() };

            while (types.Last().BaseType != null)
            {
                types.Add(types.Last().BaseType);
            }

            List<FieldInfo> fields = new List<FieldInfo>();
            for (int i = types.Count - 1; i >= 0; i--)
            {
                IEnumerable<FieldInfo> fieldInfos = types[i].GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly).Where(predicate);

                fields.AddRange(fieldInfos);
            }

            return fields;
        }

        private List<PropertyInfo> GetProperties(Func<PropertyInfo, bool> predicate)
        {
            List<Type> types = new List<Type>() { target.GetType() };

            while (types.Last().BaseType != null)
            {
                types.Add(types.Last().BaseType);
            }

            List<PropertyInfo> properties = new List<PropertyInfo>();
            for (int i = types.Count - 1; i >= 0; i--)
            {
                IEnumerable<PropertyInfo> propertyInfos = types[i].GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly).Where(predicate);

                properties.AddRange(propertyInfos);
            }

            return properties;
        }

        private List<MethodInfo> GetMethods(Func<MethodInfo, bool> predicate)
        {
            List<Type> types = new List<Type>() { target.GetType() };

            while (types.Last().BaseType != null)
            {
                types.Add(types.Last().BaseType);
            }

            List<MethodInfo> methods = new List<MethodInfo>();
            for (int i = types.Count - 1; i >= 0; i--)
            {
                IEnumerable<MethodInfo> methodInfos = types[i].GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static).Where(predicate);

                methods.AddRange(methodInfos);
            }

            return methods;
        }
    }
}