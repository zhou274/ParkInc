using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.Text;
using System.IO;
using Watermelon.List;

namespace Watermelon.SkinStore
{
    [CustomEditor(typeof(SkinsDatabase))]
    public class SkinsDatabaseEditor : Editor
    {
        //Settings section
        private SerializedProperty coinsForAdsAmountProp;
        private SerializedProperty currencyForAdsProp;
        private const int INSPECTOR_HEADER_SIZE = 114;

        //Tabs section
        private SerializedProperty tabsArrayProp;
        CustomList tabsList;
        private Rect workRect1;
        private Rect workRect2;
        private float previewFieldheight1;
        private float previewFieldHeight2;
        SerializedProperty previewTypeProp;

        //Products section
        private const int SPRITE_HEIGHT = 60;
        private const int LABEL_HEIGHT = 20;
        private const int DEFAULT_SPACE = 8;
        private SerializedProperty productsArrayProp;
        CustomList productList;
        int selectedProductTabIndex;
        int previousSelectedProductTabIndex;
        string[] tabTexts;
        List<SerializedProperty> tabProducts;
        bool productMovedToOtherTab = false;
        SerializedProperty tabProperty;
        SerializedProperty purchaseTypyProperty;
        private float previewPrefabFieldHeight;
        private float purchaseTypeFieldHeight;
        private float currencyFieldHeight;
        private GUIStyle productSectionStyle;
        
        //RecalculateProducts
        List<SerializedProperty> products;
        private Rect tabsRect;

        protected void OnEnable()
        {
            tabsArrayProp = serializedObject.FindProperty("tabs");
            productsArrayProp = serializedObject.FindProperty("products");
            coinsForAdsAmountProp = serializedObject.FindProperty("coinsForAdsAmount");
            currencyForAdsProp = serializedObject.FindProperty("currencyForAds");
            products = new List<SerializedProperty>();

            PrepareStyles();
            InitTabs();
            InitProducts();
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.BeginVertical(GUILayout.Height(Screen.height - INSPECTOR_HEADER_SIZE));
            DrawSettings();
            DrawTabs();
            DrawProducts();
            EditorGUILayout.EndVertical();
            serializedObject.ApplyModifiedProperties();
        }

        private void OnDestroy()
        {
            AssetDatabase.SaveAssets();
        }

        protected void PrepareStyles()
        {
            productSectionStyle = new GUIStyle(WatermelonEditor.Styles.Skin.box);
            productSectionStyle.stretchHeight = true;
        }

        public void RecalculateProducts()
        {
            for (int i = 0; i < productsArrayProp.arraySize; i++)
            {
                SerializedProperty iProp = productsArrayProp.GetArrayElementAtIndex(i);

                int iTabId = iProp.FindPropertyRelative("tabId").intValue;
                int iId = iProp.FindPropertyRelative("id").intValue;

                for (int j = i + 1; j < productsArrayProp.arraySize; j++)
                {
                    SerializedProperty jProp = productsArrayProp.GetArrayElementAtIndex(j);

                    int jTabId = jProp.FindPropertyRelative("tabId").intValue;
                    int jId = jProp.FindPropertyRelative("id").intValue;

                    if (iTabId > jTabId)
                    {
                        productsArrayProp.MoveArrayElement(i, j);
                        productsArrayProp.MoveArrayElement(j - 1, i);
                    }
                    else if (iTabId == jTabId)
                    {
                        if (iId > jId)
                        {
                            productsArrayProp.MoveArrayElement(i, j);
                            productsArrayProp.MoveArrayElement(j - 1, i);
                        }
                    }
                }
            }

            for (int i = 0; i < tabsArrayProp.arraySize; i++)
            {
                var products = GetProducts(i);

                for (int k = 0; k < products.Count; k++)
                {
                    SerializedProperty product = products[k];

                    //product.FindPropertyRelative("id").intValue = k;

                    SerializedProperty uniqueId = product.FindPropertyRelative("uniqueId");
                    if (uniqueId.stringValue == "")
                    {
                        uniqueId.stringValue = System.DateTime.Now.ToString() + "_" + UnityEngine.Random.Range(0, 1000000);
                    }
                }

                SerializedProperty tabProp = tabsArrayProp.GetArrayElementAtIndex(i);
                SerializedProperty tabUniqueId = tabProp.FindPropertyRelative("uniqueId");

                if (tabUniqueId.stringValue == "")
                {
                    tabUniqueId.stringValue = System.DateTime.Now.ToString() + "_" + UnityEngine.Random.Range(0, 1000000);
                }
            }
        }

        private List<SerializedProperty> GetProducts(int tabId)
        {
            products.Clear();

            for (int i = 0; i < productsArrayProp.arraySize; i++)
            {
                var productProperty = productsArrayProp.GetArrayElementAtIndex(i);

                if (productProperty.FindPropertyRelative("tabId").intValue == tabId)
                {
                    products.Add(productProperty);
                }
            }

            return products;
        }


        #region Settings
        private void DrawSettings()
        {
            EditorGUILayout.BeginVertical(WatermelonEditor.Styles.Skin.box, GUILayout.ExpandHeight(false));

            EditorGUILayoutCustom.Header("Settings");

            EditorGUILayout.BeginVertical();

            GUILayout.Space(4);

            float defaultValue = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = GUI.skin.label.CalcSize(new GUIContent(coinsForAdsAmountProp.displayName)).x;
            EditorGUILayout.PropertyField(coinsForAdsAmountProp);
            EditorGUILayout.PropertyField(currencyForAdsProp);
            EditorGUIUtility.labelWidth = defaultValue;

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();
        }
        #endregion

        #region Products
        private void InitProducts()
        {
            selectedProductTabIndex = 0;
            tabTexts = new string[2];
            tabProducts = new List<SerializedProperty>();
            UpdateTabProducts();

            productList = new CustomList(serializedObject, tabProducts, "name");
            productList.MinHeight = 400f;
            productList.addElementCallback = ProductAddElementCallback;
            productList.removeElementCallback = ProductRemoveElementCallback;
            productList.listChangedCallback = ListChangedCallback;
            productList.AddPropertyField("name");
            productList.AddSpace();

            productList.AddCustomField(DrawSpriteAndPreviewField, GetHeightForSpriteAndPreviewField);
            productList.AddPropertyField("prefab");
            productList.AddSpace();
            productList.AddSeparator();
            productList.AddCustomField(DrawPurchaseTypeAndCurrencyField, GetHeightForPurchaseTypeAndCurrencyField);
            productList.AddSpace();
            productList.AddPropertyField("cost");
            productList.AddSpace();
            productList.AddSeparator();
            productList.AddSpace();
            productList.AddCustomField(DrawTabField, GetHeightForTabField);
            productList.AddPropertyField("isDummy", new GUIContent("Is Dummy:"));
            productList.AddSpace();
            productList.AddSeparator();
            productList.AddSpace();
            productList.AddPropertyField("uniqueId");
        }

        

        private void ProductAddElementCallback()
        {
            productsArrayProp.arraySize++;
            SerializedProperty newPoductProp = productsArrayProp.GetArrayElementAtIndex(productsArrayProp.arraySize - 1);
            newPoductProp.ClearProperty();
            newPoductProp.FindPropertyRelative("tabId").intValue = selectedProductTabIndex;
            newPoductProp.isExpanded = false;
            newPoductProp.FindPropertyRelative("name").stringValue = "New Element";
            newPoductProp.FindPropertyRelative("uniqueId").stringValue = System.DateTime.Now.ToString() + "_" + UnityEngine.Random.Range(0, 1000000);

            UpdateTabProducts(false);
        }

        private void ProductRemoveElementCallback()
        {
            string uniqueId = tabProducts[productList.SelectedIndex].FindPropertyRelative("uniqueId").stringValue;

            for (int i = 0; i < productsArrayProp.arraySize; i++)
            {
                if (productsArrayProp.GetArrayElementAtIndex(i).FindPropertyRelative("uniqueId").stringValue.Equals(uniqueId))
                {
                    productsArrayProp.DeleteArrayElementAtIndex(i);
                    break;
                }
            }

            productList.SelectedIndex = -1;
            UpdateTabProducts();
        }

        private void ListChangedCallback()
        {
            for (int i = 0; i < tabProducts.Count; i++)
            {
                tabProducts[i].FindPropertyRelative("id").intValue = i;
            }
        }

        private void DrawSpriteAndPreviewField(SerializedProperty elementProperty, Rect rect, CustomListStyle style)
        {
            SerializedProperty openedSpriteProp = elementProperty.FindPropertyRelative("openedSprite");
            SerializedProperty lockedSpriteProp = elementProperty.FindPropertyRelative("lockedSprite");

            workRect1.Set(rect.x, rect.y, EditorGUIUtility.labelWidth, LABEL_HEIGHT);
            GUI.Label(workRect1, "Opened Sprite:");
            workRect1.y += LABEL_HEIGHT;
            GUI.Label(workRect1, "Locked Sprite:");
            

            workRect2.Set(rect.x + EditorGUIUtility.labelWidth + DEFAULT_SPACE, rect.y, SPRITE_HEIGHT, SPRITE_HEIGHT);
            openedSpriteProp.objectReferenceValue = EditorGUI.ObjectField(workRect2, openedSpriteProp.objectReferenceValue, typeof(Sprite), false);

            workRect2.x += SPRITE_HEIGHT;
            lockedSpriteProp.objectReferenceValue = EditorGUI.ObjectField(workRect2, lockedSpriteProp.objectReferenceValue, typeof(Sprite), false);

            if (IsProductIn2DPreviewMode(elementProperty))
            {
                SerializedProperty preview2DSpriteProp = elementProperty.FindPropertyRelative("preview2DSprite");

                workRect1.y += LABEL_HEIGHT;
                GUI.Label(workRect1, "2D Sprite Preview:");

                workRect2.x += SPRITE_HEIGHT;
                preview2DSpriteProp.objectReferenceValue = EditorGUI.ObjectField(workRect2, preview2DSpriteProp.objectReferenceValue, typeof(Sprite), false);
            }
            else
            {
                workRect1.Set(rect.x, rect.y + SPRITE_HEIGHT + DEFAULT_SPACE, rect.width, previewPrefabFieldHeight);
                EditorGUI.PropertyField(workRect1, elementProperty.FindPropertyRelative("previewPrefab"));
            }

            this.Repaint();

        }

        private float GetHeightForSpriteAndPreviewField(SerializedProperty elementProperty, CustomListStyle style)
        {
            if (IsProductIn2DPreviewMode(elementProperty))
            {
                return SPRITE_HEIGHT;
            }
            else
            {
                previewPrefabFieldHeight = EditorGUI.GetPropertyHeight(elementProperty.FindPropertyRelative("previewPrefab"));
                return SPRITE_HEIGHT + previewPrefabFieldHeight + DEFAULT_SPACE;
            }
        }

        private void DrawPurchaseTypeAndCurrencyField(SerializedProperty elementProperty, Rect rect, CustomListStyle style)
        {
            workRect1.Set(rect.x, rect.y, rect.width, purchaseTypeFieldHeight);
            EditorGUI.PropertyField(workRect1, elementProperty.FindPropertyRelative("purchaseType"));

            if (currencyFieldHeight != -1)
            {
                workRect1.y += purchaseTypeFieldHeight + DEFAULT_SPACE;
                workRect1.height = currencyFieldHeight;
                EditorGUI.PropertyField(workRect1, elementProperty.FindPropertyRelative("currency"));
            }
        }

        private float GetHeightForPurchaseTypeAndCurrencyField(SerializedProperty elementProperty, CustomListStyle style)
        {
            purchaseTypyProperty = elementProperty.FindPropertyRelative("purchaseType");
            purchaseTypeFieldHeight = EditorGUI.GetPropertyHeight(purchaseTypyProperty);

            if (purchaseTypyProperty.intValue == (int)SkinData.PurchaseType.InGameCurrency)
            {
                currencyFieldHeight = EditorGUI.GetPropertyHeight(elementProperty.FindPropertyRelative("currency"));
                return purchaseTypeFieldHeight + currencyFieldHeight + DEFAULT_SPACE;
            }
            else
            {
                currencyFieldHeight = -1;
                return purchaseTypeFieldHeight;
            }
        }

        private void DrawTabField(SerializedProperty elementProperty, Rect rect, CustomListStyle style)
        {
            tabProperty = elementProperty.FindPropertyRelative("tabId");
            tabProperty.intValue = EditorGUI.Popup(rect, "Tab:", tabProperty.intValue, tabTexts);

            if (tabProperty.intValue != previousSelectedProductTabIndex)
            {
                productMovedToOtherTab = true;
            }
        }

        private float GetHeightForTabField(SerializedProperty elementProperty, CustomListStyle style)
        {
            return EditorGUIUtility.singleLineHeight;
        }

        private bool IsProductIn2DPreviewMode(SerializedProperty elementProperty)
        {
            return (tabsArrayProp.GetArrayElementAtIndex(previousSelectedProductTabIndex).FindPropertyRelative("previewType").intValue == (int)PreviewType.Preview_2D);
        }

        private void DrawProducts()
        {
            EditorGUILayout.BeginVertical(WatermelonEditor.Styles.Skin.box);
            EditorGUILayoutCustom.Header("Product");

            if (productMovedToOtherTab && (Event.current.type == EventType.Repaint))
            {
                productMovedToOtherTab = false;
                productList.SelectedIndex = -1;
                UpdateTabProducts();
            }

            HandleProductTabs();
            productList.Display();
            EditorGUILayout.EndVertical();
        }

        private void HandleProductTabs()
        {
            if(tabTexts.Length != tabsArrayProp.arraySize)
            {
                tabTexts = new string[tabsArrayProp.arraySize];
            }

            for (int i = 0; i < tabsArrayProp.arraySize; i++)
            {
                tabTexts[i] = tabsArrayProp.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue;
            }

            selectedProductTabIndex = GUILayout.Toolbar(selectedProductTabIndex, tabTexts);


            if((selectedProductTabIndex != previousSelectedProductTabIndex) && (Event.current.type == EventType.Repaint))
            {
                productList.SelectedIndex = -1;
                UpdateTabProducts();
                previousSelectedProductTabIndex = selectedProductTabIndex;
            }
        }

        private void UpdateTabProducts(bool recalculate = true)
        {
            tabProducts.Clear();
            tabProducts.AddRange(GetProducts(selectedProductTabIndex));

            if (recalculate)
            {
                RecalculateProducts();
            }
        }

        #endregion

        #region Tabs


        private void InitTabs()
        {
            if (tabsArrayProp.arraySize == 0)
            {
                tabsArrayProp.arraySize = 1;
                tabsArrayProp.GetArrayElementAtIndex(0).FindPropertyRelative("name").stringValue = "Default";
            }

            workRect1 = new Rect();
            workRect2 = new Rect();
            tabsList = new CustomList(serializedObject, tabsArrayProp, "name");
            tabsList.StretchHeight = false;
            tabsList.addElementCallback = TabsAddElementCallback;
            tabsList.removeElementCallback = TabsRemoveElementCallback;
            tabsList.MinHeight = 100;
            tabsList.AddPropertyField("name");
            tabsList.AddSpace();
            tabsList.AddCustomField(DrawTypeFieldWithButton, GetHeightForTypeFieldWithButton);
            tabsList.AddCustomField(DrawPreviewField, GetHeightForPreviewField);
            tabsList.AddSpace();
            tabsList.AddPropertyField("backgroundImage");
            tabsList.AddSpace();
            tabsList.AddPropertyField("backgroundColor");
            tabsList.AddSpace();
            tabsList.AddPropertyField("panelColor");
            tabsList.AddSpace();
            tabsList.AddPropertyField("tabActiveColor");
            tabsList.AddSpace();
            tabsList.AddPropertyField("tabDisabledColor");
            tabsList.AddSpace();
            tabsList.AddPropertyField("productBackgroundColor");
        }

        private void TabsAddElementCallback()
        {
            tabsArrayProp.arraySize++;
            tabsArrayProp.GetArrayElementAtIndex(tabsArrayProp.arraySize - 1).FindPropertyRelative("name").stringValue = "New Tab";
            tabsArrayProp.GetArrayElementAtIndex(tabsArrayProp.arraySize - 1).isExpanded = false;
            UpdateTabProducts();
        }

        private void TabsRemoveElementCallback()
        {
            if(tabsArrayProp.arraySize > 1)
            {
                tabsArrayProp.DeleteArrayElementAtIndex(tabsList.SelectedIndex);
                tabsList.SelectedIndex = -1;
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Tabs list should always have at least one tab.", "Ok");
            }
        }

        private void DrawTypeFieldWithButton(SerializedProperty elementProperty, Rect rect, CustomListStyle style)
        {
            workRect1.Set(rect.x, rect.y, rect.width - 36, rect.height);
            workRect2.Set(rect.x + rect.width - 32, rect.y, 32, rect.height);
            EditorGUI.PropertyField(workRect1, elementProperty.FindPropertyRelative("type"));

            if (GUI.Button(workRect2, "..."))
            {
                PopupWindow.Show(workRect2, new TabTypePopupWindow());
            }
        }

        private float GetHeightForTypeFieldWithButton(SerializedProperty elementProperty, CustomListStyle style)
        {
            return EditorGUI.GetPropertyHeight(elementProperty.FindPropertyRelative("type"));
        }

        private void DrawPreviewField(SerializedProperty elementProperty, Rect rect, CustomListStyle style)
        {
            workRect1.Set(rect.x, rect.y, rect.width, previewFieldheight1);
            EditorGUI.PropertyField(workRect1, elementProperty.FindPropertyRelative("previewType"));

            if (previewFieldHeight2 != -1)
            {
                workRect1.Set(rect.x, rect.y + previewFieldheight1, rect.width, previewFieldHeight2);
                EditorGUI.PropertyField(workRect1, elementProperty.FindPropertyRelative("previewPrefab"));
            }
        }

        private float GetHeightForPreviewField(SerializedProperty elementProperty, CustomListStyle style)
        {
            previewTypeProp = elementProperty.FindPropertyRelative("previewType");
            previewFieldheight1 = EditorGUI.GetPropertyHeight(previewTypeProp);

            if (previewTypeProp.intValue == (int)PreviewType.Preview_2D)
            {
                previewFieldHeight2 = -1;
                return previewFieldheight1;
            }
            else
            {
                previewFieldHeight2 = EditorGUI.GetPropertyHeight(elementProperty.FindPropertyRelative("previewPrefab"));
                return previewFieldheight1 + previewFieldHeight2;
            }
        }

        private void DrawTabs()
        {
            tabsRect = EditorGUILayout.BeginVertical(WatermelonEditor.Styles.Skin.box, GUILayout.ExpandHeight(false));
            EditorGUILayoutCustom.Header("Tabs");
            tabsList.Display();
            EditorGUILayout.EndVertical();
        }
        #endregion
    }

    public class TabTypePopupWindow : PopupWindowContent
    {
        private static readonly string TAB_TYPE_SCRIPT_NAME = "SkinTab";
        private static readonly string PATH_SUFFICS = "\\SkinTab.cs";

        private string fullEnumFilePath;
        private string enumAssetPath;

        List<string> typeNames = new List<string>();
        List<string> cache = new List<string>();

        private bool changed = false;

        public TabTypePopupWindow()
        {
            MonoScript prefsSettingsScript = EditorUtils.GetAssetByName<MonoScript>(TAB_TYPE_SCRIPT_NAME);
            enumAssetPath = Path.GetDirectoryName(AssetDatabase.GetAssetPath(prefsSettingsScript)) + PATH_SUFFICS;
            fullEnumFilePath = EditorUtils.projectFolderPath + enumAssetPath;

            typeNames = new List<string>(Enum.GetNames(typeof(SkinTab)));

            cache = new List<string>();
            for(int i = 0; i < typeNames.Count; i++)
            {
                cache.Add(typeNames[i]);
            }
        }

        public override void OnGUI(Rect rect)
        {
            EditorGUILayout.BeginVertical();
            for (int i = 0; i < typeNames.Count; i++)
            {
                var name = typeNames[i];

                EditorGUILayout.BeginHorizontal();
                name = EditorGUILayout.TextField(name);

                EditorGUI.BeginDisabledGroup(typeNames.Count < 2);

                if (GUILayout.Button("x", GUILayout.Width(55)))
                {
                    typeNames.RemoveAt(i);
                    i--;
                    continue;
                }

                EditorGUI.EndDisabledGroup();



                EditorGUILayout.EndHorizontal();

                if (ValidateName(name, i)) typeNames[i] = name;
            }
            EditorGUILayout.EndVertical();

            var newType = EditorGUILayout.TextField("");

            if(ValidateName(newType, typeNames.Count))
            {
                typeNames.Add(newType);
            }

            if (typeNames.Count != cache.Count)
            {
                changed = true;
            } else
            {
                changed = false;
                for (int i = 0; i < cache.Count; i++)
                {
                    if(cache[i] != typeNames[i])
                    {
                        changed = true;
                        break;
                    }
                }
            }

            if (changed)
            {
                if (GUILayout.Button("Save", GUILayout.Width(55)))
                {
                    UpdateEnum();

                    AssetDatabase.ImportAsset(enumAssetPath, ImportAssetOptions.ForceUpdate);

                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    editorWindow.Close();
                }
            }
        }

        private void UpdateEnum()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("// GENERATED BY CODE! DO NOT MODIFY!");
            stringBuilder.AppendLine();
            stringBuilder.Append("namespace Watermelon.SkinStore");
            stringBuilder.AppendLine();
            stringBuilder.Append("{");
            stringBuilder.AppendLine();
            stringBuilder.Append("\tpublic enum SkinTab");
            stringBuilder.AppendLine();
            stringBuilder.Append("\t{");
            stringBuilder.AppendLine();

            for (int i = 0; i < typeNames.Count; i++)
            {
                stringBuilder.Append("\t\t\t");
                stringBuilder.Append(typeNames[i]);
                stringBuilder.Append(" = ");
                stringBuilder.Append(i);
                stringBuilder.Append(",");
                stringBuilder.AppendLine();
            }

            stringBuilder.AppendLine();
            stringBuilder.Append("\t}");
            stringBuilder.AppendLine();
            stringBuilder.Append("}");

            File.WriteAllText(fullEnumFilePath, stringBuilder.ToString(), Encoding.UTF8);

            cache = typeNames;
            typeNames = new List<string>();

            for(int i = 0; i < cache.Count; i++) {
                typeNames.Add(cache[i]);
            }
        }

        private bool ValidateName(string name, int i)
        {
            bool available = true;

            if (i >= typeNames.Count || typeNames[i] != name)
            {
                for (int j = 0; j < typeNames.Count; j++)
                {
                    if (i == j) continue;

                    if (name == typeNames[j])
                    {
                        available = false;
                        break;
                    }
                }

                if (available && name == "") available = false;
            }

            return available;
        }

        public override Vector2 GetWindowSize()
        {
            var height = (typeNames.Count + 1) * 25;

            if (changed) height += 25;

            return new Vector2(200, height);
        }
    }
}