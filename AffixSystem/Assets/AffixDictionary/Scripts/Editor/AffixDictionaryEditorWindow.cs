using SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class AffixDictionaryEditorWindow : EditorWindow
{
    private const string JSON_PATH = "Assets/AffixDictionary/Resources/affixes.json";

    Texture2D m_BackgroundTexture;
    string m_SearchResult = "";
    Vector2 m_SearchResultScrollbarPosition = Vector2.zero;

    private bool m_FoldAffixType = false;
    private bool[] m_AffixTypeToggles = new bool[Enum.GetValues(typeof(Affix.Type)).Length];

    private bool m_FoldAffixModifier = false;
    private bool[] m_AffixModifierToggles = new bool[9];

    private bool m_FoldAffixModifierType = false;
    private bool[] m_AffixModifierTypeToggles = new bool[Enum.GetValues(typeof(Affix.ModifierType)).Length];

    private bool m_FoldItemType = false;
    private bool[] m_ItemTypeToggles = new bool[Enum.GetValues(typeof(Item.Type)).Length];

    List<Affix> m_Affixes;
    List<Affix> m_SortedList;

    private bool SearchResultsDirty { get; set; }

    private void OnGUI()
    {
        if (m_Affixes == null)
            m_Affixes = LoadAffixListFromJsonFile(JSON_PATH);

        float headerHeight = 30;
        float areaWidth = position.width / 8;
        float dividerHeight = headerHeight + 5;

        m_BackgroundTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        m_BackgroundTexture.SetPixel(0, 0, new Color(0.9f, 0.9f, 0.9f));
        m_BackgroundTexture.Apply();

        GUI.DrawTexture(new Rect(0, 0, position.width, dividerHeight), m_BackgroundTexture);

        GUILayout.BeginArea(new Rect(0, 0, areaWidth * 2 - 2, headerHeight));
        GUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        string temp = m_SearchResult;
        m_SearchResult = EditorGUILayout.TextField(GUIContent.none, m_SearchResult, GUIStyles.TextField, GUILayout.Width(areaWidth * 2 - 12), GUILayout.Height(25));
        if (m_SearchResult != temp)
        {
            SearchResultsDirty = true;
        }
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(areaWidth * 2 - 1, 0, 1, dividerHeight));
        GUILayout.Box(GUIContent.none, GUIStyles.VerticalLine, GUILayout.Height(dividerHeight));
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(areaWidth * 2, 0, areaWidth * 3 - 2, headerHeight));
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginVertical();
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("Search Results", GUIStyles.HeaderLabel);
        EditorGUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(areaWidth * 2 + areaWidth * 3 + 1, 0, 1, dividerHeight));
        GUILayout.Box(GUIContent.none, GUIStyles.VerticalLine, GUILayout.Height(dividerHeight));
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(areaWidth * 2 + areaWidth * 3 + 2, 0, areaWidth * 3 - 2, headerHeight));
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        EditorGUILayout.BeginVertical();
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("Selected Affix", GUIStyles.HeaderLabel);
        EditorGUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(0, dividerHeight, position.width, 1));
        GUILayout.Box(GUIContent.none, GUIStyles.HorizontalLine, GUILayout.Width(position.width));
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(0, dividerHeight + 5, areaWidth * 2 - 2, position.height - dividerHeight - 5));

        CreateFoldoutToggleGroup<Affix.Type>(ref m_FoldAffixType, "Affix Type", ref m_AffixTypeToggles);
        EditorGUILayout.Space();
        CreateFoldoutToggleGroup<Affix.Modifier>(ref m_FoldAffixModifier, "Affix Modifier", ref m_AffixModifierToggles);
        EditorGUILayout.Space();
        CreateFoldoutToggleGroup<Affix.ModifierType>(ref m_FoldAffixModifierType, "Affix Modifier Type", ref m_AffixModifierTypeToggles);
        EditorGUILayout.Space();
        CreateFoldoutToggleGroup<Item.Type>(ref m_FoldItemType, "Item Type", ref m_ItemTypeToggles);
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(areaWidth * 2 - 1, dividerHeight, 1, position.height - dividerHeight));
        GUILayout.Box(GUIContent.none, GUIStyles.VerticalLine, GUILayout.Height(position.height - dividerHeight));
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(areaWidth * 2, dividerHeight + 5, areaWidth * 3 - 2, position.height - dividerHeight - 5));
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Name", GUIStyles.SearchResultsHeader, GUILayout.Width(150));
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("Type", GUIStyles.SearchResultsHeader, GUILayout.Width(75));
        GUILayout.FlexibleSpace();
        EditorGUILayout.LabelField("Modifier", GUIStyles.SearchResultsHeader, GUILayout.Width(150));
        GUILayout.FlexibleSpace();
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        GUILayout.Space(5);
        GUILayout.Box(GUIContent.none, GUIStyles.HorizontalLine, GUILayout.Width(areaWidth * 3 - 25), GUILayout.Height(1));
        EditorGUILayout.EndHorizontal();
        m_SearchResultScrollbarPosition = EditorGUILayout.BeginScrollView(m_SearchResultScrollbarPosition);
        for (int i = 0; i < m_SortedList.Count; i++)
        {
            Affix a = m_SortedList[i];
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(a.name, GUIStyles.SearchResultsLabel, GUILayout.Width(150), GUILayout.Height(20));
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(a.type.ToString(), GUIStyles.SearchResultsLabel, GUILayout.Width(75), GUILayout.Height(20));
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField(a.modifier.ToString(), GUIStyles.SearchResultsLabel, GUILayout.Width(150), GUILayout.Height(20));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(areaWidth * 2 + areaWidth * 3 + 1, dividerHeight, 1, position.height - dividerHeight));
        GUILayout.Box(GUIContent.none, GUIStyles.VerticalLine, GUILayout.Height(position.height - dividerHeight));
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(areaWidth * 2 + areaWidth * 3 + 2, dividerHeight + 5, areaWidth * 3 - 2, position.height - dividerHeight - 5));
        GUILayout.EndArea();

        if (SearchResultsDirty)
        {
            Debug.Log("Search results dirty! Gotta clean this mess!");
            m_SortedList = SortAffixesList();
        }

    }

    [MenuItem("Window/Affixes/AffixDictionary")]
    public static void ShowWindow()
    {
        GetWindow(typeof(AffixDictionaryEditorWindow));
    }

    private void CreateFoldoutToggleGroup<T>(ref bool foldout, string name, ref bool[] toggles) where T : struct, IConvertible
    {
        foldout = EditorGUILayout.Foldout(foldout, name, true);
        if (foldout)
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < toggles.Length; i++)
            {
                EditorGUILayout.BeginHorizontal();
                bool temp = toggles[i];
                toggles[i] = EditorGUILayout.Toggle(toggles[i], GUILayout.Width(25));

                if (toggles[i] != temp)
                    SearchResultsDirty = true;

                EditorGUILayout.LabelField(Enum.GetValues(typeof(T)).GetValue(i).ToString());
                EditorGUILayout.EndHorizontal();
            }
            EditorGUI.indentLevel--;
        }
    }

    private List<T> GetListOfSelectedEnumType<T>(bool[] toggles) where T : struct, IConvertible
    {
        List<T> list = new List<T>();
        for (int i = 0; i < toggles.Length; i++)
            if (toggles[i])
                list.Add((T)Enum.GetValues(typeof(T)).GetValue(i));

        return list;
    }

    private bool AffixCompliesToList<T>(T affixVariable, List<T> list) where T : struct, IConvertible
    {
        if (list.Count == 0)
            return true;

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Equals(affixVariable))
                return true;
        }

        return false;
    }

    private T GetAffixVariableByName<T>(string name, JSONObject o) where T : struct, IConvertible
    {
        Array t = Enum.GetValues(typeof(T));

        for (int i = 0; i < t.Length; i++)
            if (t.GetValue(i).ToString() == o[name])
                return (T)t.GetValue(i);

        return (T)t.GetValue(0);
    }

    private List<string> GetListOfWordsFromSearch(string result)
    {
        List<string> list = new List<string>();
        char[] r = result.ToCharArray();
        int lastindex = 0;

        for (int i = 0; i < r.Length; i++)
        {
            if (r[i] == ' ')
            {
                string s = "";
                for (int j = lastindex; j < i; j++)
                    s += r[j];

                list.Add(s);
                lastindex = i;
            }
        }

        string last = "";
        for (int i = lastindex; i < r.Length; i++)
        {
            last += r[i];
        }

        list.Add(last);

        return list;
    }

    private List<Affix> SortAffixesList()
    {
        List<Affix> list = new List<Affix>();
        List<Affix.Type> affixTypes = GetListOfSelectedEnumType<Affix.Type>(m_AffixTypeToggles);
        List<Affix.Modifier> affixModifiers = GetListOfSelectedEnumType<Affix.Modifier>(m_AffixModifierToggles);
        List<Affix.ModifierType> affixModifierTypes = GetListOfSelectedEnumType<Affix.ModifierType>(m_AffixModifierTypeToggles);
        List<Item.Type> itemTypes = GetListOfSelectedEnumType<Item.Type>(m_ItemTypeToggles);

        for (int i = 0; i < m_Affixes.Count; i++)
            if (AffixCompliesToList(m_Affixes[i].type, affixTypes) &&
                AffixCompliesToList(m_Affixes[i].modifier, affixModifiers) &&
                AffixCompliesToList(m_Affixes[i].modifier_type, affixModifierTypes) &&
                AffixCompliesToList(m_Affixes[i].item_type, itemTypes))
                list.Add(m_Affixes[i]);

        list = FilterAffixesFromResult(GetListOfWordsFromSearch(m_SearchResult).ToArray(), list);

        SearchResultsDirty = false;
        return list;
    }

    private List<Affix> FilterAffixesFromResult(string[] result, List<Affix> affixes)
    {
        List<Affix> a = new List<Affix>();

        for (int i = 0; i < affixes.Count; i++)
        {
            bool contains = true;

            for (int j = 0; j < result.Length; j++)
            {
                if (!affixes[i].name.ToLower().Contains(result[j].ToLower()))
                {
                    contains = false;
                    break;
                }
            }

            if (contains)
                a.Add(affixes[i]);
        }

        return a;
    }

    private List<Affix> LoadAffixListFromJsonFile(string path)
    {
        List<Affix> list = new List<Affix>();

        JSONNode json = JSON.Parse(File.ReadAllText(path));
        JSONObject root = (JSONObject)json;
        JSONArray objects = root["list"].AsArray;

        for (int i = 0; i < objects.Count; i++)
        {
            JSONObject o = objects[i].AsObject;
            Affix affix = new Affix()
            {
                name = o["name"],
                item_level = o["item_level"],
                min_amount = o["min_range"],
                max_amount = o["max_range"],
                type = GetAffixVariableByName<Affix.Type>("type", o),
                modifier = GetAffixVariableByName<Affix.Modifier>("modifier", o),
                modifier_type = GetAffixVariableByName<Affix.ModifierType>("modifier_type", o),
                item_type = GetAffixVariableByName<Item.Type>("item_type", o)
            };

            list.Add(affix);
        }

        SearchResultsDirty = true;
        return list;
    }
}

public static class GUIStyles
{
    private static GUIStyle m_HorizontalLine = null;
    private static GUIStyle m_VerticalLine = null;
    private static GUIStyle m_HeaderLabel = null;
    private static GUIStyle m_TextField = null;
    private static GUIStyle m_SearchResultsHeader = null;
    private static GUIStyle m_SearchResultsLabel = null;

    static GUIStyles()
    {
        m_HorizontalLine = new GUIStyle("box");
        m_HorizontalLine.border.top = m_HorizontalLine.border.bottom = 1;
        m_HorizontalLine.margin.top = m_HorizontalLine.margin.bottom = 1;
        m_HorizontalLine.padding.top = m_HorizontalLine.padding.bottom = 1;

        m_VerticalLine = new GUIStyle("box");
        m_VerticalLine.border.left = m_VerticalLine.border.right = 1;
        m_VerticalLine.margin.left = m_VerticalLine.margin.right = 1;
        m_VerticalLine.padding.left = m_VerticalLine.padding.right = 1;

        m_HeaderLabel = new GUIStyle("largeLabel");
        m_HeaderLabel.alignment = TextAnchor.MiddleCenter;
        m_HeaderLabel.fontSize = 14;

        m_TextField = new GUIStyle("textField");
        m_TextField.alignment = TextAnchor.MiddleLeft;
        m_TextField.border.top = 3;
        m_TextField.margin.top = 3;
        m_TextField.padding.top = 3;

        m_SearchResultsHeader = new GUIStyle("label");
        m_SearchResultsHeader.alignment = TextAnchor.LowerLeft;
        m_SearchResultsHeader.fontSize = 12;

        m_SearchResultsLabel = new GUIStyle("label");
        m_SearchResultsLabel.alignment = TextAnchor.UpperLeft;
        m_SearchResultsLabel.fontSize = 12;

    }

    public static GUIStyle HorizontalLine
    {
        get { return m_HorizontalLine; }
    }

    public static GUIStyle VerticalLine
    {
        get { return m_VerticalLine; }
    }

    public static GUIStyle HeaderLabel
    {
        get { return m_HeaderLabel; }
    }

    public static GUIStyle TextField
    {
        get { return m_TextField; }
    }

    public static GUIStyle SearchResultsHeader
    {
        get { return m_SearchResultsHeader; }
    }

    public static GUIStyle SearchResultsLabel
    {
        get { return m_SearchResultsLabel; }
    }
}