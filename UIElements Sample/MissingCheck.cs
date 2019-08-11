using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

public struct MissingData
{
    public string MissingPath { get; private set; }
    public Object MissingPrefab { get; private set; }

    public MissingData(string path, Object obj)
    {
        this.MissingPath = path;
        this.MissingPrefab = obj;
    }
}

public class MissingCheck : EditorWindow
{
    private List<MissingData> _missingData = new List<MissingData>();
    private VisualElement _resultVisualElement;
    
    private const string ExtensionPath = ".prefab";
    private const string DirectoryName = "Assets/Prefabs";

    [MenuItem("UIElements/MissingCheck")]
    public static void ShowExample()
    {
        MissingCheck window = GetWindow<MissingCheck>();
        window.titleContent = new GUIContent("MissingCheck");
    }
    
    public void OnEnable()
    {
        _resultVisualElement = new VisualElement();
        
        // Each editor window contains a root VisualElement object
        VisualElement root = rootVisualElement;
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/MissingCheck.uss");
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/MissingCheck.uxml");
        VisualElement labelFromUXML = visualTree.CloneTree();
        labelFromUXML.styleSheets.Add(styleSheet);
        root.Add(labelFromUXML);
        
        // ボタンのアクションを登録
        var button = root.Query<Button>("button").First();
        button.clickable.clicked += Search;
    }

    private void Search()
    {
        // リセット処理
        if (_missingData.Count > 0)
        {
            _missingData.Clear();
            _resultVisualElement.Clear();
        }

        var allPaths = AssetDatabase.GetAllAssetPaths()
            .Where(_ => Path.GetExtension(_) == ExtensionPath && Path.GetDirectoryName(_) == DirectoryName).ToList();

        foreach (var path in allPaths)
        {
            SearchMissing(path);
        }

        // Missingがある時
        if (_missingData.Count <= 0)
        {
            return;
        }

        // UXMLのUIを表示
        var uxmlVisualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Editor/MissingCheckResult.uxml");
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Editor/MissingCheck.uss");
        VisualElement ve = uxmlVisualTree.CloneTree();
        ve.styleSheets.Add(styleSheet);
        _resultVisualElement.Add(ve);

        var scrollView = new ScrollView();
        scrollView.name = "scroll";
        scrollView.verticalPageSize = 200;
        _resultVisualElement.Add(scrollView);
        scrollView.styleSheets.Add(styleSheet);

        for (int i = 0; i < _missingData.Count; i++)
        {
            Label pathLabel = new Label(_missingData[i].MissingPath);
            scrollView.Add(pathLabel);
            ObjectField objectField = new ObjectField("該当Prefab:");
            objectField.objectType = typeof(GameObject);
            objectField.SetValueWithoutNotify(_missingData[i].MissingPrefab);
            scrollView.Add(objectField);
        }

        rootVisualElement.Add(_resultVisualElement);
    }


    private void SearchMissing(string path)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath(path);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        foreach (var asset in assets)
        {
            if (asset == null)
            {
                continue;
            }

            var serializedObject = new SerializedObject(asset);
            var property = serializedObject.GetIterator();
            while (property.Next(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (property.objectReferenceValue != null) continue;
                if (!property.hasChildren) continue;
                var fileId = property.FindPropertyRelative("m_FileID");
                if (fileId == null) continue;
                if (fileId.intValue == 0) continue;

                _missingData.Add(new MissingData(path, prefab));
            }
        }
    }    
}