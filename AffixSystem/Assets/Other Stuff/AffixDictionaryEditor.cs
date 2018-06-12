using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Runtime.Serialization;

[CustomEditor(typeof(AffixDictionary))]
public class AffixDictionaryEditor : Editor
{

    string filename = "";

    [CanEditMultipleObjects]
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty list = serializedObject.FindProperty("affixes");
        EditorGUILayout.PropertyField(list);

        EditorGUI.indentLevel += 1;
        if (list.isExpanded)
        {
            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty affix = list.GetArrayElementAtIndex(i);
                SerializedProperty Name = affix.FindPropertyRelative("name");
                SerializedProperty ItemLevel = affix.FindPropertyRelative("item_level");
                SerializedProperty MinAmount = affix.FindPropertyRelative("min_amount");
                SerializedProperty MaxAmount = affix.FindPropertyRelative("max_amount");
                SerializedProperty Type = affix.FindPropertyRelative("type");
                SerializedProperty Modifier = affix.FindPropertyRelative("modifier");
                SerializedProperty ModifierType = affix.FindPropertyRelative("modifier_type");
                SerializedProperty ItemType = affix.FindPropertyRelative("item_type");

                float oldlabelwidth = EditorGUIUtility.labelWidth;
                float oldfieldwidth = EditorGUIUtility.fieldWidth;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.PropertyField(affix);
                if (GUILayout.Button("\u2191", GUILayout.Width(50), GUILayout.Height(20)))
                {
                    list.MoveArrayElement(i, i - 1);
                }
                if (GUILayout.Button("\u2193", GUILayout.Width(50), GUILayout.Height(20)))
                {
                    list.MoveArrayElement(i, i + 1);
                }

                bool propertyDeleted = false;
                if (GUILayout.Button("-", GUILayout.Width(50), GUILayout.Height(20)))
                {
                    propertyDeleted = true;
                    list.DeleteArrayElementAtIndex(i);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel += 1;
                if (!propertyDeleted && affix.isExpanded)
                {
                    EditorGUILayout.LabelField("Basic Information", EditorStyles.boldLabel);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Name", GUILayout.Width(100));
                    EditorGUILayout.PropertyField(Name, new GUIContent(""));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Item Level", GUILayout.Width(100));
                    EditorGUILayout.PropertyField(ItemLevel, new GUIContent(""));
                    EditorGUILayout.LabelField("Item Type", GUILayout.Width(100));
                    EditorGUILayout.PropertyField(ItemType, new GUIContent(""));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space();
                    EditorGUILayout.LabelField("Affix Information", EditorStyles.boldLabel);
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.PropertyField(Type, new GUIContent(""));
                    EditorGUILayout.PropertyField(Modifier, new GUIContent(""));
                    EditorGUILayout.PropertyField(ModifierType, new GUIContent(""));
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.Space();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Amounts", EditorStyles.boldLabel);
                    EditorGUIUtility.labelWidth = 60;
                    EditorGUILayout.Space();
                    EditorGUILayout.PropertyField(MinAmount, new GUIContent("Min"));
                    EditorGUILayout.PropertyField(MaxAmount, new GUIContent("Max"));
                    float min = MinAmount.intValue;
                    float max = MaxAmount.intValue;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.Space();
                    EditorGUILayout.Space();
                    EditorGUILayout.MinMaxSlider(ref min, ref max, 0, 130);
                    EditorGUILayout.EndHorizontal();
                    EditorGUIUtility.labelWidth = oldlabelwidth;
                    MinAmount.intValue = (int)min;
                    MaxAmount.intValue = (int)max;
                    if (MinAmount.intValue > MaxAmount.intValue)
                    {
                        MinAmount.intValue = MaxAmount.intValue;
                    }
                }
                EditorGUI.indentLevel -= 1;
            }

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space();
            if (GUILayout.Button("+", GUILayout.Width(158)))
            {
                list.InsertArrayElementAtIndex(list.arraySize);
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUI.indentLevel -= 1;


        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Location: Resources/affixes.json", GUILayout.Width(250), GUILayout.Height(20));
   
        if (GUILayout.Button("Save"))
        {
            SimpleJSON.JSONObject root = new SimpleJSON.JSONObject();
            SimpleJSON.JSONArray objects = root["list"].AsArray;
            for (int i = 0; i < list.arraySize; i++) 
            {
                SerializedProperty affix = list.GetArrayElementAtIndex(i);
                SimpleJSON.JSONObject af = objects[-1].AsObject;
                af["name"] = affix.FindPropertyRelative("name").stringValue;
                af["item_level"] = affix.FindPropertyRelative("item_level").intValue;
                af["min_range"] = affix.FindPropertyRelative("min_amount").intValue;
                af["max_range"] = affix.FindPropertyRelative("max_amount").intValue;
                af["type"] = affix.FindPropertyRelative("type").enumNames[affix.FindPropertyRelative("type").enumValueIndex];
                af["modifier"] = affix.FindPropertyRelative("modifier").enumNames[affix.FindPropertyRelative("modifier").enumValueIndex];
                af["modifier_type"] = affix.FindPropertyRelative("modifier_type").enumNames[affix.FindPropertyRelative("modifier_type").enumValueIndex];
                af["item_type"] = affix.FindPropertyRelative("item_type").enumNames[affix.FindPropertyRelative("item_type").enumValueIndex];
            }
            
            File.WriteAllText("Assets/Resources/affixes.json", root.ToString(3));
            Debug.Log("Saved to Assets/Resources/affixes.json!");
        }

        if (GUILayout.Button("Load"))
        {
            SimpleJSON.JSONNode json = SimpleJSON.JSON.Parse(File.ReadAllText("Assets/Resources/affixes.json"));
            SimpleJSON.JSONObject root = (SimpleJSON.JSONObject) json;
            SimpleJSON.JSONArray objects = root["list"].AsArray;

            list.ClearArray();
            for (int i = 0; i < objects.Count; i++)
            {
                SimpleJSON.JSONObject af = objects[i].AsObject;
                list.InsertArrayElementAtIndex(list.arraySize);
                SerializedProperty affix = list.GetArrayElementAtIndex(list.arraySize - 1);
                affix.FindPropertyRelative("name").stringValue = af["name"];
                affix.FindPropertyRelative("item_level").intValue = af["item_level"];
                affix.FindPropertyRelative("min_amount").intValue = af["min_range"];
                affix.FindPropertyRelative("max_amount").intValue = af["max_range"];
                for (int j = 0; j < affix.FindPropertyRelative("type").enumNames.Length; j++)
                {
                    if (affix.FindPropertyRelative("type").enumNames[j] == af["type"])
                    {

                        affix.FindPropertyRelative("type").enumValueIndex = j;
                        break;
                    }

                }
                for (int j = 0; j < affix.FindPropertyRelative("modifier").enumNames.Length; j++)
                {
                    if (affix.FindPropertyRelative("modifier").enumNames[j] == af["modifier"])
                    {

                        affix.FindPropertyRelative("modifier").enumValueIndex = j;
                        break;
                    }

                }
                for (int j = 0; j < affix.FindPropertyRelative("modifier_type").enumNames.Length; j++)
                {
                    if (affix.FindPropertyRelative("modifier_type").enumNames[j] == af["modifier_type"])
                    {

                        affix.FindPropertyRelative("modifier_type").enumValueIndex = j;
                        break;
                    }

                }
                for (int j = 0; j < affix.FindPropertyRelative("item_type").enumNames.Length; j++)
                {
                    if (affix.FindPropertyRelative("item_type").enumNames[j] == af["item_type"])
                    {

                        affix.FindPropertyRelative("item_type").enumValueIndex = j;
                        break;
                    }

                }
            }

            Debug.Log("Succesfully loaded affixes.json!");
        }
        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }
}
