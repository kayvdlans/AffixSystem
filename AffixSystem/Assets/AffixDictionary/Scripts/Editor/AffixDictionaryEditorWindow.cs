using SimpleJSON;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

//TODO: Save to .json on quit
//TODO: Selection window
//TODO: Save the state of the editor window
public class AffixDictionaryEditorWindow : EditorWindow
{
    private enum SelectionWindowState
    {
        Add,
        Edit
    }

    /// <summary>
    /// Instead of creating a million variables for each toggle button per enumeration
    /// Create them automatically based on T. This way we don't have to repeat code as much.
    /// </summary>
    /// <typeparam name="T">The enumeration to create a toggle group from.</typeparam>
    private struct FilterToggleGroup<T> where T : struct, IConvertible
    {
        public string Name;
        public bool FoldGroup;
        public bool[] Toggles;

        public FilterToggleGroup(string name)
        {
            Name = name;
            FoldGroup = true;
            Toggles = new bool[Enum.GetValues(typeof(T)).Length];
        }
    }

    string editName;

    private const string JSON_PATH = "Assets/AffixDictionary/Resources/affixes.json";
    Texture2D m_BackgroundTexture;
    string m_SearchResult = "";
    Vector2 m_SearchResultScrollbarPosition = Vector2.zero;

    FilterToggleGroup<Affix.Type> m_AffixTypeGroup = new FilterToggleGroup<Affix.Type>("Affix Type");
    FilterToggleGroup<Affix.Modifier> m_AffixModifierGroup = new FilterToggleGroup<Affix.Modifier>("Affix Modifier");
    FilterToggleGroup<Affix.ModifierType> m_AffixModifierTypeGroup = new FilterToggleGroup<Affix.ModifierType>("Affix Modifier Type");
    FilterToggleGroup<Item.Type> m_ItemTypeGroup = new FilterToggleGroup<Item.Type>("Item Type");

    List<Affix> m_Affixes;
    List<Affix> m_SortedList;
    bool[] m_SortedListSelections;

    private bool SearchResultsDirty { get; set; }

    /// <summary>
    /// Draw the search bar area in the top left corner of the window.
    /// </summary>
    /// <param name="areaWidth">The width of this area</param>
    /// <param name="headerHeight">The height of this header</param>
    private void DrawSearchBar(float areaWidth, float headerHeight)
    {
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
    }

    /// <summary>
    /// Draw the search results header top middle of the window.
    /// </summary>
    /// <param name="areaWidth">The width of this area</param>
    /// <param name="headerHeight">The height of this header</param>
    private void DrawSearchResultsHeader(float areaWidth, float headerHeight)
    {
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
    }

    /// <summary>
    /// Draw the selection header top right of the window.
    /// </summary>
    /// <param name="areaWidth">The width of this area</param>
    /// <param name="headerHeight">The height of this header</param>
    private void DrawSelectionHeader(float areaWidth, float headerHeight)
    {
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
    }

    /// <summary>
    /// Create toggle groups for each filter option and draw them
    /// </summary>
    /// <param name="areaWidth">The width of this area</param>
    /// <param name="dividerHeight">The height of the divider between the heads and bodies</param>
    private void DrawFilterOptions(float areaWidth, float dividerHeight)
    {
        GUILayout.BeginArea(new Rect(0, dividerHeight + 5, areaWidth * 2 - 2, position.height - dividerHeight - 5));
        CreateFoldoutToggleGroup<Affix.Type>(ref m_AffixTypeGroup.FoldGroup, m_AffixTypeGroup.Name, ref m_AffixTypeGroup.Toggles);
        EditorGUILayout.Space();
        CreateFoldoutToggleGroup<Affix.Modifier>(ref m_AffixModifierGroup.FoldGroup, m_AffixModifierGroup.Name, ref m_AffixModifierGroup.Toggles);
        EditorGUILayout.Space();
        CreateFoldoutToggleGroup<Affix.ModifierType>(ref m_AffixModifierTypeGroup.FoldGroup, m_AffixModifierTypeGroup.Name, ref m_AffixModifierTypeGroup.Toggles);
        EditorGUILayout.Space();
        CreateFoldoutToggleGroup<Item.Type>(ref m_ItemTypeGroup.FoldGroup, m_ItemTypeGroup.Name, ref m_ItemTypeGroup.Toggles);
        GUILayout.EndArea();
    }

    /// <summary>
    /// Create the search results area containing a sub-header and all the affixes added in a scrollview
    /// </summary>
    /// <param name="areaWidth">The width of this area/param>
    private void DrawSearchResults(float areaWidth)
    {
        //Create the sub-header showing important information for the affixes
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

        //Create a scrollview with all the affixes
        m_SearchResultScrollbarPosition = EditorGUILayout.BeginScrollView(m_SearchResultScrollbarPosition);
        if (m_SortedList != null && m_SortedListSelections != null && m_SortedList.Count != 0)
        {
            for (int i = 0; i < m_SortedList.Count; i++)
            {
                Affix a = m_SortedList[i];
                EditorGUILayout.BeginHorizontal();
                m_SortedListSelections[i] = EditorGUILayout.ToggleLeft(a.name, m_SortedListSelections[i], GUIStyles.SearchResultsLabel, GUILayout.Width(150), GUILayout.Height(20));
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(a.type.ToString(), GUIStyles.SearchResultsLabel, GUILayout.Width(75), GUILayout.Height(20));
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(a.modifier.ToString(), GUIStyles.SearchResultsLabel, GUILayout.Width(150), GUILayout.Height(20));
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndScrollView();
    }

    /// <summary>
    /// Draw the add and remove buttons below the search results scrollview
    /// </summary>
    /// <param name="areaWidth">The width of this area</param>
    //  TODO: Delete from .json file and add functionality to new affix button
    private void DrawAddRemoveButtons(float areaWidth)
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add new affix", GUILayout.Width((areaWidth * 3 - 2) / 2 - 4)))
        {
            //create new thing in selection window > press apply when done > create new affix > save state to json
        }
        if (GUILayout.Button("Remove selected affixes", GUILayout.Width((areaWidth * 3 - 2) / 2 - 4)))
        {
            //TODO: ALSO DELETE FROM .json FILE.
            for (int i = 0; i < m_SortedList.Count; i++)
            {
                if (m_SortedListSelections[i])
                {
                    for (int j = 0; j < m_Affixes.Count; j++)
                    {
                        if (m_SortedList[i].Equals(m_Affixes[j]))
                        {
                            m_Affixes.Remove(m_Affixes[j]);
                            SearchResultsDirty = true;
                            break;
                        }
                    }
                }
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void OnGUI()
    { 
        //If the list of affixes is not initialized yet, load it in from a .json file.
        if (m_Affixes == null)
            m_Affixes = LoadAffixListFromJsonFile(JSON_PATH);

        float headerHeight = 30;
        float areaWidth = position.width / 8;
        float dividerHeight = headerHeight + 5;

        //Create a texture representing the background.
        m_BackgroundTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        m_BackgroundTexture.SetPixel(0, 0, new Color(0.9f, 0.9f, 0.9f));
        m_BackgroundTexture.Apply();

        //Draw the background before anything else.
        GUI.DrawTexture(new Rect(0, 0, position.width, dividerHeight), m_BackgroundTexture);

        DrawSearchBar(areaWidth, headerHeight);

        //Draw the divider between search bar and search results header
        GUILayout.BeginArea(new Rect(areaWidth * 2 - 1, 0, 1, dividerHeight));
        GUILayout.Box(GUIContent.none, GUIStyles.VerticalLine, GUILayout.Height(dividerHeight));
        GUILayout.EndArea();

        DrawSearchResultsHeader(areaWidth, headerHeight);

        //Draw the divider between search results header and selection header
        GUILayout.BeginArea(new Rect(areaWidth * 2 + areaWidth * 3 + 1, 0, 1, dividerHeight));
        GUILayout.Box(GUIContent.none, GUIStyles.VerticalLine, GUILayout.Height(dividerHeight));
        GUILayout.EndArea();

        DrawSelectionHeader(areaWidth, headerHeight);

        //Draw the divider between the headers and the bodies
        GUILayout.BeginArea(new Rect(0, dividerHeight, position.width, 1));
        GUILayout.Box(GUIContent.none, GUIStyles.HorizontalLine, GUILayout.Width(position.width));
        GUILayout.EndArea();

        DrawFilterOptions(areaWidth, dividerHeight);

        //Draw the divider between the filter options and the search results
        GUILayout.BeginArea(new Rect(areaWidth * 2 - 1, dividerHeight, 1, position.height - dividerHeight));
        GUILayout.Box(GUIContent.none, GUIStyles.VerticalLine, GUILayout.Height(position.height - dividerHeight));
        GUILayout.EndArea();

        //Draw search results body
        GUILayout.BeginArea(new Rect(areaWidth * 2, dividerHeight + 5, areaWidth * 3 - 2, position.height - dividerHeight - 5));
        DrawSearchResults(areaWidth);
        DrawAddRemoveButtons(areaWidth);
        GUILayout.EndArea();

        //Draw the divider between the search results and selection
        GUILayout.BeginArea(new Rect(areaWidth * 2 + areaWidth * 3 + 1, dividerHeight, 1, position.height - dividerHeight));
        GUILayout.Box(GUIContent.none, GUIStyles.VerticalLine, GUILayout.Height(position.height - dividerHeight));
        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(areaWidth * 2 + areaWidth * 3 + 2, dividerHeight + 5, areaWidth * 3 - 2, position.height - dividerHeight - 5));

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Name", GUIStyles.SearchResultsHeader, GUILayout.Width(75));
        editName = EditorGUILayout.TextField(editName);
        EditorGUILayout.EndHorizontal();

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

    public void CreateFoldoutToggleGroup<T>(ref bool foldout, string name, ref bool[] toggles) where T : struct, IConvertible
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

    /// <summary>
    /// Create a list of values from enumeration T of which the toggles, which have been created earlier are selected.
    /// This way we can filter out certain affixes which do not meet the criteria of the selected values.
    /// </summary>
    /// <typeparam name="T">The enumeration to get the values from</typeparam>
    /// <param name="toggles">The toggles representing the enumeration values</param>
    /// <returns>A list with the values of enumeration T which are selected in the filter options.</returns>
    private List<T> GetListOfSelectedEnumType<T>(bool[] toggles) where T : struct, IConvertible
    {
        List<T> list = new List<T>();
        for (int i = 0; i < toggles.Length; i++)
            if (toggles[i])
                list.Add((T)Enum.GetValues(typeof(T)).GetValue(i));

        return list;
    }

    /// <summary>
    /// Check whether a certain affix variable complies to a list of enumeration T. 
    /// This way we can find out whether an affix should be shown based on filter option results.
    /// </summary>
    /// <typeparam name="T">The enumeration to check</typeparam>
    /// <param name="affixVariable">The variable to check</param>
    /// <param name="list">The list to check inside</param>
    /// <returns></returns>
    private bool AffixCompliesToList<T>(T affixVariable, List<T> list) where T : struct, IConvertible
    {
        //If none of the filter options of a certain category are selected just show all of the affixes
        if (list.Count == 0)
            return true;

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Equals(affixVariable))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Finds an Affix's type, item_type, modifier or modifier_type in the .json file by searching for it by its name.
    /// This is a temporary method, since the enums will be abolished at some point in time.
    /// </summary>
    /// <typeparam name="T">The enum that will be used to find its value</typeparam>
    /// <param name="name">The name of the value inside the .json file</param>
    /// <param name="o">the object to get the value from</param>
    /// <returns>the enum value of the object</returns>
    private T GetAffixVariableByName<T>(string name, JSONObject o) where T : struct, IConvertible
    {
        Array t = Enum.GetValues(typeof(T));

        for (int i = 0; i < t.Length; i++)
            if (t.GetValue(i).ToString() == o[name])
                return (T)t.GetValue(i);

        return (T)t.GetValue(0);
    }

    /// <summary>
    /// Instead of having to search things directly from a string, turn the string into a list of words,
    /// which can then be easily checked if the affix contains all of the words. If words are not in a chronological order,
    /// or if a certain word is left out in between, you will still be able to find the affix, because other words are 
    /// included in the search result.
    /// </summary>
    /// <param name="result">The search result</param>
    /// <returns>A list of words that were inside the search result</returns>
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

        //Get a list for each enumeration that contains the selected values in the filter options.
        List<Affix.Type> affixTypes = GetListOfSelectedEnumType<Affix.Type>(m_AffixTypeGroup.Toggles);
        List<Affix.Modifier> affixModifiers = GetListOfSelectedEnumType<Affix.Modifier>(m_AffixModifierGroup.Toggles);
        List<Affix.ModifierType> affixModifierTypes = GetListOfSelectedEnumType<Affix.ModifierType>(m_AffixModifierTypeGroup.Toggles);
        List<Item.Type> itemTypes = GetListOfSelectedEnumType<Item.Type>(m_ItemTypeGroup.Toggles);

        //Check whether the enumerations of each affix comply to the lists defined earlier.
        for (int i = 0; i < m_Affixes.Count; i++)
            if (AffixCompliesToList(m_Affixes[i].type, affixTypes) &&
                AffixCompliesToList(m_Affixes[i].modifier, affixModifiers) &&
                AffixCompliesToList(m_Affixes[i].modifier_type, affixModifierTypes) &&
                AffixCompliesToList(m_Affixes[i].item_type, itemTypes))
                list.Add(m_Affixes[i]);

        //
        if (list != null && list.Count >= 2)
        {
            list = FilterAffixesFromResult(GetListOfWordsFromSearch(m_SearchResult).ToArray(), list);
            list.Sort((a, b) => { return a.CompareTo(b); });
        }

        m_SortedListSelections = new bool[list.Count];

        SearchResultsDirty = false;
        return list;
    }

    private List<Affix> FilterAffixesFromResult(string[] result,  List<Affix> affixes)
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