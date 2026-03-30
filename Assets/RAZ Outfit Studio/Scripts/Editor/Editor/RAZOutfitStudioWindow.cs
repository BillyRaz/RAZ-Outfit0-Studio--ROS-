using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.SceneManagement;

public class RAZOutfitStudioWindow : EditorWindow
{
    private int selectedTab = 0;
    private readonly string[] tabNames = { "Root Setup", "Outfit", "UI", "Studio", "Diagnosed" };
    private Vector2 mainScrollPosition;

    // Root Setup Tab Variables
    private GameObject meshForRoot;
    private string characterName = "PlayerCharacter";

    // Outfit Tab Variables
    private GameObject selectedOutfitMesh;
    private string newOutfitName = "New Outfit";
    private string outfitDescription = "";
    private string outfitAuthor = System.Environment.UserName;
    private bool autoCapturePreview = true;
    private Vector2 outfitScrollPosition;
    private List<OutfitData> existingOutfits = new List<OutfitData>();
    private OutfitData selectedOutfitData;

    // UI Tab Variables
    private GameObject playerRootPrefab;
    private string uiCanvasName = "OutfitUI";
    private bool createPlayerPreview = true;
    private int previewSize = 256;
    private bool autoRefreshOnSceneLoad = true;
    private bool autoScanOnStart = true;
    private bool spawnRootIfMissing = true;

    // Diagnostic Tab Variables
    private Vector2 scrollPosition;
    private List<string> diagnosticLog = new List<string>();
    private bool isDeepScanning = false;

    // Studio Tab Variables
    private GameObject currentPreviewObject;
    private Camera previewCamera;
    private Light[] previewLights;
    private bool isStudioSceneOpen = false;
    private string studioScenePath = "Assets/RAZ Outfit Studio/StudioScene.unity";
    private Vector2 studioScrollPosition;
    private OutfitData selectedOutfitForCapture;
    private int captureResolution = 512;
    private bool useTransparentBackground = true;
    private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f);
    private float cameraDistance = 3f;
    private float cameraHeight = 1f;
    private float cameraRotation = 0f;
    private bool showPreview = true;
    private Vector2 studioOutfitScrollPosition;
    private OutfitData currentStudioOutfit;
    private string studioStatusMessage = "";
    private float studioStatusTimer = 0f;
    private double studioStatusLastUpdateTime = 0d;

    private enum StudioPreset
    {
        Default,
        Product,
        Character,
        CloseUp,
        FullBody
    }

    private StudioPreset selectedPreset = StudioPreset.Default;
    private bool showAdvancedSettings = false;

    private enum ExportResolution
    {
        HD_720p = 1280,
        HD_1080p = 1920,
        QHD_1440p = 2560,
        UHD_4K = 3840,
        Custom = 0
    }

    private enum ExportFormat
    {
        PNG,
        JPG,
        WebP,
        TGA,
        EXR
    }

    private enum ExportMode
    {
        Single,
        Batch,
        Variants
    }

    private ExportResolution selectedResolution = ExportResolution.HD_1080p;
    private ExportFormat selectedFormat = ExportFormat.PNG;
    private int customResolution = 2048;
    private int jpgQuality = 90;
    private bool exportWithTransparency = true;
    private bool exportWithBackground = false;
    private Color exportBackgroundColor = Color.gray;
    private bool createSpriteAtlas = false;
    private bool exportMultipleAngles = false;
    private int numberOfAngles = 4;
    private bool exportThumbnail = true;
    private int thumbnailSize = 256;
    private bool autoOpenFolder = true;
    private bool showAdvancedExport = false;
    private float exportProgress = 0f;
    private string exportStatus = "";

    [MenuItem("Tools/RAZ Outfit Studio")]
    public static void ShowWindow()
    {
        RAZOutfitStudioWindow window = GetWindow<RAZOutfitStudioWindow>("RAZ Outfit Studio");
        window.minSize = new Vector2(450, 600);
        window.Show();
    }

    private void OnEnable()
    {
        RefreshOutfitList();
        EnsureDefaultPlayerRootPrefab();
    }

    private void OnGUI()
    {
        mainScrollPosition = EditorGUILayout.BeginScrollView(mainScrollPosition);
        EditorGUILayout.Space(10);

        // Tab System
        GUIStyle tabStyle = new GUIStyle(GUI.skin.button);
        tabStyle.fontSize = 12;
        tabStyle.fontStyle = FontStyle.Bold;

        selectedTab = GUILayout.Toolbar(selectedTab, tabNames, tabStyle);
        EditorGUILayout.Space(20);

        switch (selectedTab)
        {
            case 0:
                DrawRootSetupTab();
                break;
            case 1:
                DrawOutfitTab();
                break;
            case 2:
                DrawUITab();
                break;
            case 3:
                DrawStudioTab();
                break;
            case 4:
                DrawDiagnosedTab();
                break;
        }
        EditorGUILayout.EndScrollView();
    }

    #region Root Setup Tab

    private void DrawRootSetupTab()
    {
        EditorGUILayout.LabelField("Player Character Setup", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Select a mesh with SkinnedMeshRenderer to generate the player root prefab", MessageType.Info);
        EditorGUILayout.Space(10);

        meshForRoot = (GameObject)EditorGUILayout.ObjectField("Character Mesh/Prefab", meshForRoot, typeof(GameObject), false);
        characterName = EditorGUILayout.TextField("Character Name", characterName);

        EditorGUILayout.Space(20);

        GUI.enabled = meshForRoot != null && !string.IsNullOrEmpty(characterName);
        if (GUILayout.Button("Generate Root Prefab", GUILayout.Height(30)))
        {
            GenerateRootPrefab();
            RefreshOutfitList();
        }
        GUI.enabled = true;

        EditorGUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "This will create:\n" +
            "• A player prefab with OutfitManager component\n" +
            "• OutfitRoot transform for attaching outfits\n" +
            "• Automatic linking of SkinnedMeshRenderer",
            MessageType.None);
    }

    private void GenerateRootPrefab()
    {
        try
        {
            EnsureFolders();

            GameObject tempRoot = new GameObject(characterName + "_Root");
            Undo.RegisterCreatedObjectUndo(tempRoot, "Create Player Root");

            Animator animator = tempRoot.AddComponent<Animator>();
            OutfitManager outfitManager = tempRoot.AddComponent<OutfitManager>();

            GameObject meshInstance = Instantiate(meshForRoot, tempRoot.transform);
            meshInstance.name = characterName + "_Mesh";

            SkinnedMeshRenderer skinnedRenderer = meshInstance.GetComponent<SkinnedMeshRenderer>();
            if (skinnedRenderer == null)
            {
                EditorUtility.DisplayDialog("Error", "Selected object has no SkinnedMeshRenderer component!", "OK");
                DestroyImmediate(tempRoot);
                return;
            }

            GameObject outfitRoot = new GameObject("OutfitRoot");
            outfitRoot.transform.SetParent(tempRoot.transform);
            outfitRoot.transform.localPosition = Vector3.zero;
            outfitRoot.transform.localRotation = Quaternion.identity;

            SerializedObject so = new SerializedObject(outfitManager);
            so.FindProperty("baseMesh").objectReferenceValue = skinnedRenderer;
            so.FindProperty("outfitRoot").objectReferenceValue = outfitRoot.transform;
            so.ApplyModifiedProperties();

            string prefabPath = $"Assets/RAZ Outfit Studio/Characters/{characterName}_Root.prefab";
            PrefabUtility.SaveAsPrefabAsset(tempRoot, prefabPath);

            OutfitData defaultOutfitData = CreateOrUpdateDefaultOutfitForRoot();
            GameObject savedRootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (savedRootPrefab != null)
            {
                OutfitManager savedManager = savedRootPrefab.GetComponent<OutfitManager>();
                if (savedManager != null)
                {
                    SerializedObject savedManagerSO = new SerializedObject(savedManager);
                    savedManagerSO.FindProperty("defaultOutfit").objectReferenceValue = defaultOutfitData;
                    savedManagerSO.FindProperty("sourceRootPrefab").objectReferenceValue = savedRootPrefab;

                    SerializedProperty availableOutfitsProperty = savedManagerSO.FindProperty("availableOutfits");
                    bool alreadyLinked = false;
                    for (int i = 0; i < availableOutfitsProperty.arraySize; i++)
                    {
                        if (availableOutfitsProperty.GetArrayElementAtIndex(i).objectReferenceValue == defaultOutfitData)
                        {
                            alreadyLinked = true;
                            break;
                        }
                    }

                    if (!alreadyLinked && defaultOutfitData != null)
                    {
                        availableOutfitsProperty.InsertArrayElementAtIndex(availableOutfitsProperty.arraySize);
                        availableOutfitsProperty.GetArrayElementAtIndex(availableOutfitsProperty.arraySize - 1).objectReferenceValue = defaultOutfitData;
                    }

                    savedManagerSO.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(savedManager);
                    PrefabUtility.SavePrefabAsset(savedRootPrefab);
                }
            }

            DestroyImmediate(tempRoot);
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Success!",
                $"Root prefab successfully created at:\n{prefabPath}",
                "OK");

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            // Log to console
            Debug.Log($"✅ ROS: Root prefab created successfully at {prefabPath}");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to generate root prefab:\n{e.Message}", "OK");
            Debug.LogError($"❌ ROS Error: {e.Message}");
        }
    }

    #endregion

    #region Outfit Tab

    private void DrawOutfitTab()
    {
        EditorGUILayout.LabelField("Outfit Creation & Management", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);

        // Create New Outfit Section
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Create New Outfit", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        selectedOutfitMesh = (GameObject)EditorGUILayout.ObjectField("Outfit Mesh/Prefab", selectedOutfitMesh, typeof(GameObject), false);
        newOutfitName = EditorGUILayout.TextField("Outfit Name", newOutfitName);
        outfitDescription = EditorGUILayout.TextField("Description", outfitDescription);
        outfitAuthor = EditorGUILayout.TextField("Author", outfitAuthor);
        autoCapturePreview = EditorGUILayout.Toggle("Auto-capture Preview (Studio Tab)", autoCapturePreview);

        EditorGUILayout.Space(10);

        GUI.enabled = selectedOutfitMesh != null && !string.IsNullOrEmpty(newOutfitName);
        if (GUILayout.Button("Create Outfit", GUILayout.Height(30)))
        {
            CreateOutfitPrefab();
        }
        GUI.enabled = true;

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(20);

        // Existing Outfits Section
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Existing Outfits", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        if (existingOutfits.Count == 0)
        {
            EditorGUILayout.HelpBox("No outfits found. Create your first outfit above!", MessageType.Info);
        }
        else
        {
            outfitScrollPosition = EditorGUILayout.BeginScrollView(outfitScrollPosition, GUILayout.Height(250));

            foreach (OutfitData outfit in existingOutfits)
            {
                DrawOutfitListItem(outfit);
            }

            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // Quick Actions
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Refresh Outfit List"))
        {
            RefreshOutfitList();
        }
        if (GUILayout.Button("Open Outfit Folder"))
        {
            EditorUtility.RevealInFinder("Assets/RAZ Outfit Studio/Outfits");
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawOutfitListItem(OutfitData outfit)
    {
        EditorGUILayout.BeginHorizontal("box");

        // Preview
        if (outfit.previewSprite != null)
        {
            GUILayout.Box(outfit.previewSprite.texture, GUILayout.Width(50), GUILayout.Height(50));
        }
        else
        {
            GUILayout.Box("No Preview", GUILayout.Width(50), GUILayout.Height(50));
        }

        // Info
        EditorGUILayout.BeginVertical();
        EditorGUILayout.LabelField(outfit.outfitName, EditorStyles.boldLabel);
        EditorGUILayout.LabelField(outfit.description, EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"Author: {outfit.author}", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();

        // Actions
        EditorGUILayout.BeginVertical(GUILayout.Width(100));

        if (GUILayout.Button("Select", GUILayout.Width(80)))
        {
            Selection.activeObject = outfit;
            EditorGUIUtility.PingObject(outfit);
        }

        if (GUILayout.Button("Delete", GUILayout.Width(80)))
        {
            if (EditorUtility.DisplayDialog("Delete Outfit",
                $"Are you sure you want to delete {outfit.outfitName}?\nThis action cannot be undone.",
                "Delete", "Cancel"))
            {
                DeleteOutfit(outfit);
            }
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
    }

    private void CreateOutfitPrefab()
    {
        try
        {
            EnsureFolders();

            // Validate mesh
            SkinnedMeshRenderer skinnedRenderer = selectedOutfitMesh.GetComponent<SkinnedMeshRenderer>();
            if (skinnedRenderer == null)
            {
                EditorUtility.DisplayDialog("Error", "Selected outfit has no SkinnedMeshRenderer component!", "OK");
                return;
            }

            // Create temporary instance
            GameObject tempOutfit = Instantiate(selectedOutfitMesh);
            tempOutfit.name = newOutfitName;

            // Save prefab
            string prefabPath = $"Assets/RAZ Outfit Studio/Outfits/{newOutfitName}.prefab";
            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(tempOutfit, prefabPath);
            DestroyImmediate(tempOutfit);

            // Create OutfitData asset
            OutfitData data = ScriptableObject.CreateInstance<OutfitData>();
            data.outfitName = newOutfitName;
            data.description = outfitDescription;
            data.author = outfitAuthor;
            data.outfitPrefab = savedPrefab;
            data.creationDate = System.DateTime.Now;

            string dataPath = $"Assets/RAZ Outfit Studio/OutfitData/{newOutfitName}.asset";
            AssetDatabase.CreateAsset(data, dataPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"✅ ROS: Outfit '{newOutfitName}' created successfully!");
            Debug.Log($"   Prefab: {prefabPath}");
            Debug.Log($"   Data: {dataPath}");

            // Auto-capture preview if requested
            if (autoCapturePreview)
            {
                EditorUtility.DisplayDialog("Preview Capture",
                    $"Outfit created successfully!\n\nTo capture a preview sprite:\n" +
                    $"1. Go to the Studio tab\n" +
                    $"2. Load this outfit\n" +
                    $"3. Click 'Capture Sprite'\n\n" +
                    $"Or you can capture it later.",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Success!",
                    $"Outfit '{newOutfitName}' created successfully!\n\nPrefab: {prefabPath}\nData: {dataPath}",
                    "OK");
            }

            // Refresh lists
            RefreshOutfitList();

            // Clear fields
            selectedOutfitMesh = null;
            newOutfitName = "New Outfit";
            outfitDescription = "";
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to create outfit:\n{e.Message}", "OK");
            Debug.LogError($"❌ ROS Error: {e.Message}");
        }
    }

    private void DeleteOutfit(OutfitData outfit)
    {
        try
        {
            string dataPath = AssetDatabase.GetAssetPath(outfit);
            string prefabPath = AssetDatabase.GetAssetPath(outfit.outfitPrefab);

            // Delete the prefab if it exists
            if (!string.IsNullOrEmpty(prefabPath))
            {
                AssetDatabase.DeleteAsset(prefabPath);
                Debug.Log($"🗑️ ROS: Deleted prefab at {prefabPath}");
            }

            // Delete the data asset
            AssetDatabase.DeleteAsset(dataPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RefreshOutfitList();
            Debug.Log($"🗑️ ROS: Deleted outfit '{outfit.outfitName}'");
            EditorUtility.DisplayDialog("Success", $"Outfit '{outfit.outfitName}' deleted successfully.", "OK");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to delete outfit:\n{e.Message}", "OK");
            Debug.LogError($"❌ ROS Error: {e.Message}");
        }
    }

    private void RefreshOutfitList()
    {
        existingOutfits.Clear();

        string[] outfitDataGuids = AssetDatabase.FindAssets("t:OutfitData", new[] { "Assets/RAZ Outfit Studio/OutfitData" });

        foreach (string guid in outfitDataGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            OutfitData data = AssetDatabase.LoadAssetAtPath<OutfitData>(path);
            if (data != null)
            {
                existingOutfits.Add(data);
            }
        }

        Repaint();
    }

    private void EnsureDefaultPlayerRootPrefab()
    {
        if (playerRootPrefab != null)
        {
            return;
        }

        string[] rootPrefabGuids = AssetDatabase.FindAssets("t:Prefab _Root", new[] { "Assets/RAZ Outfit Studio/Characters" });
        if (rootPrefabGuids.Length == 0)
        {
            return;
        }

        string rootPrefabPath = AssetDatabase.GUIDToAssetPath(rootPrefabGuids[0]);
        playerRootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(rootPrefabPath);

        if (playerRootPrefab != null)
        {
            Debug.Log($"✅ ROS: Loaded default root prefab: {playerRootPrefab.name}");
        }
    }

    private OutfitData CreateOrUpdateDefaultOutfitForRoot()
    {
        if (meshForRoot == null)
        {
            return null;
        }

        string defaultOutfitName = $"{characterName}_DefaultOutfit";
        string outfitPrefabPath = $"Assets/RAZ Outfit Studio/Outfits/{defaultOutfitName}.prefab";
        string outfitDataPath = $"Assets/RAZ Outfit Studio/OutfitData/{defaultOutfitName}.asset";

        GameObject tempOutfit = Instantiate(meshForRoot);
        tempOutfit.name = defaultOutfitName;
        GameObject savedOutfitPrefab = PrefabUtility.SaveAsPrefabAsset(tempOutfit, outfitPrefabPath);
        DestroyImmediate(tempOutfit);

        OutfitData data = AssetDatabase.LoadAssetAtPath<OutfitData>(outfitDataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<OutfitData>();
            AssetDatabase.CreateAsset(data, outfitDataPath);
        }

        data.outfitName = defaultOutfitName;
        data.description = "Auto-generated default outfit from the root mesh.";
        data.author = outfitAuthor;
        data.outfitPrefab = savedOutfitPrefab;
        data.creationDate = System.DateTime.Now;
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();

        Debug.Log($"✅ ROS: Default outfit created/updated at {outfitDataPath}");
        return data;
    }

    #endregion

    #region UI Tab (Enhanced with Auto-Link)

    private void DrawUITab()
    {
        EditorGUILayout.LabelField("Runtime UI Generation & Auto-Link", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Generate UI system with auto-link capabilities for automatic outfit management", MessageType.Info);
        EditorGUILayout.Space(10);

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("UI Configuration", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        playerRootPrefab = (GameObject)EditorGUILayout.ObjectField("Player Root Prefab", playerRootPrefab, typeof(GameObject), false);
        uiCanvasName = EditorGUILayout.TextField("Canvas Name", uiCanvasName);
        createPlayerPreview = EditorGUILayout.Toggle("Create Player Preview Panel", createPlayerPreview);
        previewSize = EditorGUILayout.IntSlider("Preview Size", previewSize, 128, 512);

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Auto-Refresh Settings", EditorStyles.boldLabel);
        autoRefreshOnSceneLoad = EditorGUILayout.Toggle("Auto-Refresh on Scene Load", autoRefreshOnSceneLoad);
        autoScanOnStart = EditorGUILayout.Toggle("Auto-Scan Library on Start", autoScanOnStart);
        spawnRootIfMissing = EditorGUILayout.Toggle("Spawn Root If Missing", spawnRootIfMissing);

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("UI Layout Preview:", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal("box", GUILayout.Height(100));
        EditorGUILayout.LabelField("LEFT PANEL\n(Current Outfit Display)\n\nShows equipped outfit", GUILayout.Width(150), GUILayout.Height(80));
        EditorGUILayout.LabelField("RIGHT PANEL\n(Scrollable Outfit Library)\n\nAuto-refreshing buttons", GUILayout.Height(80));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        GUI.enabled = playerRootPrefab != null;
        if (GUILayout.Button("Generate and Show UI", GUILayout.Height(30)))
        {
            GenerateAndShowUI();
        }
        GUI.enabled = true;

        EditorGUILayout.Space(10);

        if (GUILayout.Button("Instantiate Existing UI Prefab", GUILayout.Height(25)))
        {
            InstantiateExistingUI();
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(20);

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Auto-Link & Fix UI", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Automatically link all UI components, sprites, and fix missing references", MessageType.Info);
        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Auto-Link & Fix All UI", GUILayout.Height(35)))
        {
            AutoLinkAndFixUI();
        }

        if (GUILayout.Button("Force Refresh UI Library", GUILayout.Height(35)))
        {
            ForceRefreshSceneUI();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        if (GUILayout.Button("Run UI Diagnostic Scan", GUILayout.Height(25)))
        {
            RunUIDiagnosticScan();
        }

        if (GUILayout.Button("Run UI Detailed Diagnostic", GUILayout.Height(25)))
        {
            RunUIDetailedDiagnosticScan();
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(20);

        // UI Management Section
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("UI Management", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Find Existing UI in Scene"))
        {
            FindExistingUI();
        }

        if (GUILayout.Button("Open UI Prefab Folder"))
        {
            EditorUtility.RevealInFinder("Assets/RAZ Outfit Studio/UI");
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Refresh Asset Database"))
        {
            AssetDatabase.Refresh();
            Debug.Log("✅ ROS: Asset database refreshed");
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        if (GUILayout.Button("Rescan Outfit Library"))
        {
            RefreshOutfitList();
            EditorUtility.DisplayDialog("Library Rescanned",
                $"Found {existingOutfits.Count} outfits in library.\n\nUse 'Force Refresh UI Library' to update the UI.",
                "OK");
        }

        EditorGUILayout.EndVertical();
    }

    private Font GetDefaultFont()
    {
        // Try to get LegacyRuntime font first (Unity 2020+)
        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Fallback to Arial if LegacyRuntime not found
        if (defaultFont == null)
        {
            defaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        // Final fallback - create a new font object
        if (defaultFont == null)
        {
            defaultFont = Font.CreateDynamicFontFromOSFont("Arial", 14);
        }

        return defaultFont;
    }

    private void GenerateAndShowUI()
    {
        GenerateUISystem();
        InstantiateExistingUI();
    }

    private void InstantiateExistingUI()
    {
        string prefabPath = $"Assets/RAZ Outfit Studio/UI/{uiCanvasName}.prefab";
        GameObject uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

        if (uiPrefab == null)
        {
            string[] uiPrefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/RAZ Outfit Studio/UI" });
            string latestCandidatePath = null;
            System.DateTime latestWriteTime = System.DateTime.MinValue;
            foreach (string guid in uiPrefabGuids)
            {
                string candidatePath = AssetDatabase.GUIDToAssetPath(guid);
                GameObject candidate = AssetDatabase.LoadAssetAtPath<GameObject>(candidatePath);
                if (candidate != null && candidate.GetComponent<OutfitUIManager>() != null)
                {
                    string fullPath = System.IO.Path.GetFullPath(candidatePath);
                    System.DateTime writeTime = System.IO.File.Exists(fullPath)
                        ? System.IO.File.GetLastWriteTime(fullPath)
                        : System.DateTime.MinValue;
                    if (writeTime >= latestWriteTime)
                    {
                        latestWriteTime = writeTime;
                        latestCandidatePath = candidatePath;
                    }
                }
            }

            if (!string.IsNullOrEmpty(latestCandidatePath))
            {
                prefabPath = latestCandidatePath;
                uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            }

            if (uiPrefab == null)
            {
                EditorUtility.DisplayDialog("No UI Found",
                    "No UI prefab found. Please generate one first using 'Generate and Show UI'.",
                    "OK");
                return;
            }
        }

        OutfitUIManager existingUI = Object.FindFirstObjectByType<OutfitUIManager>();
        if (existingUI != null)
        {
            if (EditorUtility.DisplayDialog("UI Already Exists",
                "There's already an Outfit UI in the scene. Do you want to replace it?",
                "Replace", "Cancel"))
            {
                DestroyImmediate(existingUI.gameObject);
            }
            else
            {
                return;
            }
        }

        GameObject uiInstance = PrefabUtility.InstantiatePrefab(uiPrefab) as GameObject;
        if (uiInstance == null)
        {
            EditorUtility.DisplayDialog("Instantiation Failed",
                "Unity could not instantiate the UI prefab.",
                "OK");
            return;
        }

        Undo.RegisterCreatedObjectUndo(uiInstance, "Instantiate Outfit UI");
        EnsureEventSystemInScene();

        OutfitManager playerManager = Object.FindFirstObjectByType<OutfitManager>();
        OutfitUIManager uiManager = uiInstance.GetComponent<OutfitUIManager>();

        if (uiManager != null)
        {
            Transform leftPanel = uiInstance.transform.Find("MainPanel/LeftPanel");
            Transform contentContainer = uiInstance.transform.Find("MainPanel/RightPanel/ScrollView/Viewport/Content");
            if (leftPanel != null)
            {
                uiManager.currentOutfitPanel = leftPanel.gameObject;
                uiManager.currentOutfitNameText = leftPanel.Find("OutfitName")?.GetComponent<Text>();
                uiManager.currentOutfitPreviewImage = leftPanel.Find("OutfitPreview")?.GetComponent<Image>();
            }
            if (contentContainer != null)
            {
                uiManager.outfitButtonContainer = contentContainer;
            }

            uiManager.outfitButtonPrefab = CreateStyledButtonPrefab();
            uiManager.playerRootPrefabAsset = playerRootPrefab;
            uiManager.spawnPlayerIfMissing = spawnRootIfMissing;
            uiManager.keepSearchingForPlayer = true;
            uiManager.outfitLibrary = ScanOutfitLibrary();
            List<string> uiFixLog = new List<string>();
            WireSceneUIButtonActions(uiManager, uiFixLog);
        }

        if (playerManager != null && uiManager != null)
        {
            uiManager.playerOutfitManager = playerManager;
            if (playerRootPrefab != null)
            {
                playerManager.SetSourceRootPrefab(playerRootPrefab);
                EditorUtility.SetDirty(playerManager);
            }
            uiManager.ManualRefresh();
            Debug.Log($"✅ ROS: UI instantiated and linked to player: {playerManager.gameObject.name}");
        }
        else if (playerManager == null)
        {
            Debug.LogWarning("⚠️ ROS: No OutfitManager found in scene. UI will need manual assignment.");
            EditorUtility.DisplayDialog("Manual Assignment Required",
                "No OutfitManager found in the scene.\n\nPlease assign the player's OutfitManager to the UI Manager manually.",
                "OK");
        }

        Selection.activeGameObject = uiInstance;
        Debug.Log($"✅ ROS: UI instantiated at position: {uiInstance.transform.position}");

        EditorUtility.DisplayDialog("UI Created",
            "UI has been created and added to your scene!\n\nYou can now run the game to test outfit selection.\n\nThe UI will automatically populate with all available outfits.",
            "OK");
    }

    private void GenerateUISystem()
    {
        try
        {
            EnsureFolders();

            // Create canvas
            GameObject canvasObj = new GameObject(uiCanvasName);
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            GameObject mainPanel = new GameObject("MainPanel");
            mainPanel.transform.SetParent(canvasObj.transform);
            RectTransform mainRect = mainPanel.AddComponent<RectTransform>();
            mainRect.anchorMin = Vector2.zero;
            mainRect.anchorMax = Vector2.one;
            mainRect.offsetMin = Vector2.zero;
            mainRect.offsetMax = Vector2.zero;

            Image mainBg = mainPanel.AddComponent<Image>();
            mainBg.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

            GameObject leftPanel = CreateLeftPanel(mainPanel);
            GameObject rightPanel = CreateRightPanel(mainPanel);
            GameObject buttonPrefab = CreateStyledButtonPrefab();

            OutfitUIManager uiManager = canvasObj.AddComponent<OutfitUIManager>();

            Transform contentContainer = rightPanel.transform.Find("ScrollView/Viewport/Content");
            if (contentContainer == null)
            {
                Debug.LogError("Could not find content container in right panel");
                DestroyImmediate(canvasObj);
                return;
            }

            List<OutfitData> library = ScanOutfitLibrary();

            uiManager.outfitButtonContainer = contentContainer;
            uiManager.outfitButtonPrefab = buttonPrefab;
            uiManager.playerRootPrefabAsset = playerRootPrefab;
            uiManager.currentOutfitPanel = leftPanel;
            uiManager.currentOutfitNameText = leftPanel.transform.Find("OutfitName")?.GetComponent<Text>();
            uiManager.currentOutfitPreviewImage = leftPanel.transform.Find("OutfitPreview")?.GetComponent<Image>();
            uiManager.autoRefreshOnStart = autoScanOnStart;
            uiManager.showOutfitNames = true;
            uiManager.showOutfitPreviews = true;
            uiManager.autoLinkPlayer = true;
            uiManager.spawnPlayerIfMissing = spawnRootIfMissing;
            uiManager.keepSearchingForPlayer = true;
            uiManager.outfitLibrary = library;
            uiManager.refreshInterval = autoRefreshOnSceneLoad ? 0f : 0f;

            CreateCloseButton(canvasObj);
            CreateRefreshButton(canvasObj, uiManager);

            string prefabPath = $"Assets/RAZ Outfit Studio/UI/{uiCanvasName}.prefab";
            if (!AssetDatabase.IsValidFolder("Assets/RAZ Outfit Studio/UI"))
            {
                AssetDatabase.CreateFolder("Assets/RAZ Outfit Studio", "UI");
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                AssetDatabase.DeleteAsset(prefabPath);
            }

            PrefabUtility.SaveAsPrefabAsset(canvasObj, prefabPath);
            DestroyImmediate(canvasObj);
            AssetDatabase.Refresh();

            Debug.Log($"✅ ROS: UI System created successfully at {prefabPath}");
            Debug.Log($"   Found {library.Count} outfits to populate in UI");
            Debug.Log($"   Auto-refresh on start: {autoScanOnStart}");

            GameObject verifyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (verifyPrefab != null)
            {
                Debug.Log($"✅ ROS: Verified UI prefab exists at {prefabPath}");
            }
            else
            {
                Debug.LogError($"❌ ROS: Failed to create UI prefab at {prefabPath}");
            }

            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to generate UI:\n{e.Message}", "OK");
            Debug.LogError($"❌ ROS Error: {e.Message}\n{e.StackTrace}");
        }
    }

    private void ForceRefreshSceneUI()
    {
        OutfitUIManager uiManager = Object.FindFirstObjectByType<OutfitUIManager>();
        if (uiManager != null)
        {
            EnsureEventSystemInScene();
            uiManager.outfitLibrary = ScanOutfitLibrary();
            if (uiManager.outfitButtonPrefab == null)
            {
                uiManager.outfitButtonPrefab = CreateStyledButtonPrefab();
            }
            uiManager.ManualRefresh();
            EditorUtility.SetDirty(uiManager);
            EditorUtility.DisplayDialog("UI Refreshed",
                "Outfit UI has been refreshed!\n\n" +
                $"Found {uiManager.outfitLibrary.Count} outfits in library.\n\n" +
                "The UI should now show all available outfits.",
                "OK");
            Debug.Log("✅ ROS: Force refreshed scene UI");
        }
        else
        {
            EditorUtility.DisplayDialog("No UI Found",
                "No OutfitUI found in the current scene.\n\nPlease generate and instantiate UI first.",
                "OK");
        }
    }

    private void AutoLinkAndFixUI()
    {
        Debug.Log("🔧 ROS: Starting Auto-Link & Fix UI...");

        OutfitUIManager uiManager = Object.FindFirstObjectByType<OutfitUIManager>();
        if (uiManager == null)
        {
            EditorUtility.DisplayDialog("No UI Found",
                "No OutfitUI found in the current scene.\n\nPlease generate and instantiate UI first using 'Generate and Show UI'.",
                "OK");
            return;
        }

        int fixesApplied = 0;
        List<string> fixLog = new List<string>();
        EditorUtility.DisplayProgressBar("Auto-Linking UI", "Analyzing UI components...", 0f);

        try
        {
            EnsureEventSystemInScene();

            if (uiManager.playerOutfitManager == null)
            {
                OutfitManager player = Object.FindFirstObjectByType<OutfitManager>();
                if (player != null)
                {
                    uiManager.playerOutfitManager = player;
                    fixesApplied++;
                    fixLog.Add("Linked to Player OutfitManager");
                }
                else
                {
                    fixLog.Add("No Player OutfitManager found in scene");
                }
            }

            if (uiManager.playerOutfitManager != null && playerRootPrefab != null)
            {
                uiManager.playerOutfitManager.SetSourceRootPrefab(playerRootPrefab);
                EditorUtility.SetDirty(uiManager.playerOutfitManager);
                fixesApplied++;
                fixLog.Add($"Linked source root prefab: {playerRootPrefab.name}");
            }

            uiManager.playerRootPrefabAsset = playerRootPrefab;
            uiManager.spawnPlayerIfMissing = spawnRootIfMissing;
            uiManager.keepSearchingForPlayer = true;

            Transform mainPanel = uiManager.transform.Find("MainPanel");
            Transform leftPanel = uiManager.transform.Find("MainPanel/LeftPanel") ?? uiManager.transform.Find("LeftPanel");
            Transform rightPanel = uiManager.transform.Find("MainPanel/RightPanel") ?? uiManager.transform.Find("RightPanel");
            Transform content = uiManager.transform.Find("MainPanel/RightPanel/ScrollView/Viewport/Content")
                ?? uiManager.transform.Find("RightPanel/ScrollView/Viewport/Content");

            if (mainPanel == null)
            {
                fixLog.Add("WARNING: MainPanel not found on instantiated UI");
            }

            if (leftPanel == null)
            {
                fixLog.Add("WARNING: LeftPanel not found on instantiated UI");
            }

            if (rightPanel == null)
            {
                fixLog.Add("WARNING: RightPanel not found on instantiated UI");
            }

            if (uiManager.outfitButtonContainer == null && content != null)
            {
                uiManager.outfitButtonContainer = content;
                fixesApplied++;
                fixLog.Add("Fixed Outfit Button Container");
            }

            GameObject buttonPrefabAsset = CreateStyledButtonPrefab();
            if (buttonPrefabAsset != null && uiManager.outfitButtonPrefab != buttonPrefabAsset)
            {
                uiManager.outfitButtonPrefab = buttonPrefabAsset;
                fixesApplied++;
                fixLog.Add("Fixed Outfit Button Prefab");
            }

            if (uiManager.currentOutfitPanel == null && leftPanel != null)
            {
                uiManager.currentOutfitPanel = leftPanel.gameObject;
                fixesApplied++;
                fixLog.Add("Fixed Current Outfit Panel");
            }

            if (uiManager.currentOutfitNameText == null && leftPanel != null)
            {
                uiManager.currentOutfitNameText = leftPanel.Find("OutfitName")?.GetComponent<Text>()
                    ?? leftPanel.GetComponentsInChildren<Text>(true)
                        .FirstOrDefault(text => text.gameObject.name == "OutfitName" || text.gameObject.name.Contains("Name"));
                if (uiManager.currentOutfitNameText != null)
                {
                    fixesApplied++;
                    fixLog.Add("Fixed Current Outfit Name Text");
                }
            }

            if (uiManager.currentOutfitPreviewImage == null && leftPanel != null)
            {
                uiManager.currentOutfitPreviewImage = leftPanel.Find("OutfitPreview")?.GetComponent<Image>()
                    ?? leftPanel.GetComponentsInChildren<Image>(true)
                        .FirstOrDefault(image => image.gameObject.name == "OutfitPreview");
                if (uiManager.currentOutfitPreviewImage != null)
                {
                    fixesApplied++;
                    fixLog.Add("Fixed Current Outfit Preview Image");
                }
            }

            if (uiManager.playerPreviewImage == null && leftPanel != null)
            {
                uiManager.playerPreviewImage = leftPanel.Find("PlayerPreview")?.GetComponent<Image>()
                    ?? leftPanel.GetComponentsInChildren<Image>(true)
                        .FirstOrDefault(image => image.gameObject.name == "PlayerPreview");
                if (uiManager.playerPreviewImage != null)
                {
                    fixesApplied++;
                    fixLog.Add("Fixed Player Preview Image");
                }
            }

            fixesApplied += WireSceneUIButtonActions(uiManager, fixLog);

            List<OutfitData> freshLibrary = ScanOutfitLibrary();
            if (freshLibrary.Count != uiManager.outfitLibrary.Count ||
                freshLibrary.Except(uiManager.outfitLibrary).Any())
            {
                uiManager.outfitLibrary = freshLibrary;
                fixesApplied++;
                fixLog.Add($"Updated outfit library: {freshLibrary.Count} outfits found");
            }
            else if (uiManager.outfitLibrary == null || uiManager.outfitLibrary.Count == 0)
            {
                uiManager.outfitLibrary = freshLibrary;
                fixesApplied++;
                fixLog.Add($"Reassigned outfit library: {freshLibrary.Count} outfits found");
            }

            if (uiManager.playerOutfitManager != null)
            {
                SerializedObject playerSO = new SerializedObject(uiManager.playerOutfitManager);
                SerializedProperty outfitsProperty = playerSO.FindProperty("availableOutfits");
                if (outfitsProperty != null)
                {
                    outfitsProperty.arraySize = freshLibrary.Count;
                    for (int i = 0; i < freshLibrary.Count; i++)
                    {
                        outfitsProperty.GetArrayElementAtIndex(i).objectReferenceValue = freshLibrary[i];
                    }

                    playerSO.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(uiManager.playerOutfitManager);
                    fixesApplied++;
                    fixLog.Add($"Synced player available outfits: {freshLibrary.Count}");
                }
            }

            if (uiManager.defaultOutfitSprite == null)
            {
                uiManager.defaultOutfitSprite = CreateDefaultPlaceholderSprite("DefaultOutfitPlaceholder.png", new Color(0.45f, 0.45f, 0.45f, 1f));
                fixesApplied++;
                fixLog.Add("Created default outfit placeholder sprite");
            }

            if (uiManager.defaultPlayerSprite == null)
            {
                uiManager.defaultPlayerSprite = CreateDefaultPlaceholderSprite("DefaultPlayerPlaceholder.png", new Color(0.35f, 0.45f, 0.6f, 1f));
                fixesApplied++;
                fixLog.Add("Created default player placeholder sprite");
            }

            uiManager.ManualRefresh();
            fixesApplied++;
            fixLog.Add("Refreshed outfit buttons and display");

            EditorUtility.SetDirty(uiManager);
            if (mainPanel != null)
            {
                EditorUtility.SetDirty(mainPanel.gameObject);
            }

            EditorUtility.ClearProgressBar();

            string resultMessage = $"Auto-Link & Fix Complete!\n\n" +
                                   $"Fixes Applied: {fixesApplied}\n\n" +
                                   $"Details:\n{string.Join("\n", fixLog)}\n\n" +
                                   $"UI is now linked and refreshed.";

            EditorUtility.DisplayDialog("Auto-Link Complete", resultMessage, "OK");
            Debug.Log($"✅ ROS: Auto-Link complete - {fixesApplied} fixes applied");
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"❌ ROS Error during Auto-Link: {e.Message}");
            EditorUtility.DisplayDialog("Error", $"Auto-Link failed:\n{e.Message}", "OK");
        }
    }

    private Sprite CreateDefaultPlaceholderSprite(string fileName, Color fillColor)
    {
        EnsureFolders();
        string path = $"Assets/RAZ Outfit Studio/UI/{fileName}";
        if (!System.IO.File.Exists(path))
        {
            Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            Color[] colors = new Color[64 * 64];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = fillColor;
            }

            texture.SetPixels(colors);
            texture.Apply();
            System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.Refresh();
        }

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private int WireSceneUIButtonActions(OutfitUIManager uiManager, List<string> fixLog)
    {
        int fixes = 0;

        Button sceneCloseButton = uiManager.transform.Find("CloseButton")?.GetComponent<Button>();
        if (sceneCloseButton != null)
        {
            sceneCloseButton.onClick = new Button.ButtonClickedEvent();
            sceneCloseButton.onClick.AddListener(uiManager.CloseUI);

            Text closeText = sceneCloseButton.GetComponentInChildren<Text>(true);
            if (closeText != null)
            {
                closeText.raycastTarget = false;
            }

            fixes++;
            fixLog?.Add("Rewired CloseButton to UI Manager");
        }

        Button sceneRefreshButton = uiManager.transform.Find("RefreshButton")?.GetComponent<Button>();
        if (sceneRefreshButton != null)
        {
            sceneRefreshButton.onClick = new Button.ButtonClickedEvent();
            sceneRefreshButton.onClick.AddListener(uiManager.ForceRefresh);

            Text refreshText = sceneRefreshButton.GetComponentInChildren<Text>(true);
            if (refreshText != null)
            {
                refreshText.raycastTarget = false;
            }

            fixes++;
            fixLog?.Add("Rewired RefreshButton to UI Manager");
        }

        return fixes;
    }

    private void RunUIDiagnosticScan()
    {
        OutfitUIManager uiManager = Object.FindFirstObjectByType<OutfitUIManager>();
        if (uiManager == null)
        {
            EditorUtility.DisplayDialog("No UI Found",
                "No OutfitUI found in the current scene.\n\nPlease generate UI first.",
                "OK");
            return;
        }

        List<string> lines = new List<string>();
        lines.Add("=======================================");
        lines.Add("      UI DIAGNOSTIC SCAN");
        lines.Add("=======================================");
        lines.Add($"Scan Time: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        lines.Add("");

        lines.Add("1. PLAYER LINK CHECK");
        lines.Add("---------------------------------------");
        if (uiManager.playerOutfitManager != null)
        {
            lines.Add($"  OK: Player linked: {uiManager.playerOutfitManager.gameObject.name}");
            lines.Add($"    Position: {uiManager.playerOutfitManager.transform.position}");
        }
        else
        {
            lines.Add("  ERROR: No Player OutfitManager linked");
            lines.Add("  -> Use Auto-Link & Fix All UI");
        }
        lines.Add("");

        lines.Add("2. UI COMPONENTS CHECK");
        lines.Add("---------------------------------------");
        lines.Add($"  EventSystem Present: {(Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null ? "OK" : "Missing")}");
        lines.Add($"  MainPanel: {(uiManager.transform.Find("MainPanel") != null ? "OK" : "Missing")}");
        lines.Add($"  LeftPanel: {(uiManager.transform.Find("MainPanel/LeftPanel") != null || uiManager.transform.Find("LeftPanel") != null ? "OK" : "Missing")}");
        lines.Add($"  RightPanel: {(uiManager.transform.Find("MainPanel/RightPanel") != null || uiManager.transform.Find("RightPanel") != null ? "OK" : "Missing")}");
        lines.Add($"  Button Container: {(uiManager.outfitButtonContainer != null ? "OK" : "Missing")}");
        lines.Add($"  Button Prefab: {(uiManager.outfitButtonPrefab != null ? "OK" : "Missing")}");
        lines.Add($"  Current Outfit Text: {(uiManager.currentOutfitNameText != null ? "OK" : "Missing")}");
        lines.Add($"  Current Outfit Preview: {(uiManager.currentOutfitPreviewImage != null ? "OK" : "Missing")}");
        lines.Add($"  Player Preview: {(uiManager.playerPreviewImage != null ? "OK" : "Missing")}");
        lines.Add($"  Default Outfit Sprite: {(uiManager.defaultOutfitSprite != null ? uiManager.defaultOutfitSprite.name : "Missing")}");
        lines.Add($"  Default Player Sprite: {(uiManager.defaultPlayerSprite != null ? uiManager.defaultPlayerSprite.name : "Missing")}");
        lines.Add("");

        lines.Add("3. OUTFIT LIBRARY CHECK");
        lines.Add("---------------------------------------");
        lines.Add($"  Outfits in Library: {uiManager.outfitLibrary.Count}");
        int missingPrefabs = 0;
        int missingSprites = 0;
        foreach (OutfitData outfit in uiManager.outfitLibrary)
        {
            if (outfit == null)
            {
                continue;
            }

            if (outfit.outfitPrefab == null)
            {
                missingPrefabs++;
                lines.Add($"  WARNING: {outfit.outfitName} is missing outfit prefab");
            }

            if (outfit.previewSprite == null && outfit.icon == null)
            {
                missingSprites++;
                lines.Add($"  WARNING: {outfit.outfitName} has no preview sprite or icon");
            }
            else
            {
                string previewSource = outfit.previewSprite != null ? $"sprite '{outfit.previewSprite.name}'" : $"icon '{outfit.icon.name}'";
                lines.Add($"  OK: {outfit.outfitName} preview source = {previewSource}");
            }
        }
        lines.Add($"  Missing Prefabs: {missingPrefabs}");
        lines.Add($"  Missing Previews: {missingSprites}");
        lines.Add("");

        lines.Add("4. BUTTON DISPLAY CHECK");
        lines.Add("---------------------------------------");
        if (uiManager.outfitButtonContainer != null)
        {
            lines.Add($"  Button Count: {uiManager.outfitButtonContainer.childCount}");
            if (uiManager.outfitButtonContainer.childCount > 0)
            {
                Transform firstButton = uiManager.outfitButtonContainer.GetChild(0);
                Image previewImage = firstButton.Find("PreviewImage")?.GetComponent<Image>()
                    ?? firstButton.Find("Preview")?.GetComponent<Image>();
                Text buttonText = firstButton.GetComponentInChildren<Text>(true);
                lines.Add($"  First Button Name: {firstButton.name}");
                lines.Add($"  First Button Preview: {(previewImage != null && previewImage.sprite != null ? previewImage.sprite.name : "Missing")}");
                lines.Add($"  First Button Text: {(buttonText != null ? buttonText.text : "Missing")}");
                lines.Add($"  First Button Click Handler Count: {firstButton.GetComponent<Button>()?.onClick.GetPersistentEventCount() ?? 0}");
            }
        }
        else
        {
            lines.Add("  WARNING: No button container found");
        }
        lines.Add("");

        lines.Add("5. CURRENT DISPLAY CHECK");
        lines.Add("---------------------------------------");
        lines.Add($"  Current Outfit Text: {(uiManager.currentOutfitNameText != null ? uiManager.currentOutfitNameText.text : "Missing")}");
        lines.Add($"  Current Preview Sprite: {(uiManager.currentOutfitPreviewImage != null && uiManager.currentOutfitPreviewImage.sprite != null ? uiManager.currentOutfitPreviewImage.sprite.name : "Missing")}");
        lines.Add($"  Player Preview Sprite: {(uiManager.playerPreviewImage != null && uiManager.playerPreviewImage.sprite != null ? uiManager.playerPreviewImage.sprite.name : "Missing")}");
        lines.Add("");

        lines.Add("6. SUMMARY");
        lines.Add("---------------------------------------");
        int issues = 0;
        if (uiManager.playerOutfitManager == null) issues++;
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null) issues++;
        if (uiManager.outfitButtonContainer == null) issues++;
        if (uiManager.outfitButtonPrefab == null) issues++;
        if (uiManager.currentOutfitNameText == null) issues++;
        if (uiManager.currentOutfitPreviewImage == null) issues++;
        if (uiManager.outfitButtonContainer != null && uiManager.outfitButtonContainer.childCount != uiManager.outfitLibrary.Count) issues++;
        lines.Add($"  Issues Found: {issues}");
        lines.Add($"  Warnings: {missingPrefabs + missingSprites}");
        lines.Add(issues == 0 ? "  UI is operational" : "  Use Auto-Link & Fix All UI");

        string resultText = string.Join("\n", lines);
        EditorUtility.DisplayDialog("UI Diagnostic Scan", resultText, "OK");

        foreach (string line in lines)
        {
            if (line.Contains("ERROR"))
            {
                Debug.LogError(line);
            }
            else if (line.Contains("WARNING"))
            {
                Debug.LogWarning(line);
            }
            else
            {
                Debug.Log(line);
            }
        }
    }

    private void RunUIDetailedDiagnosticScan()
    {
        OutfitUIManager uiManager = Object.FindFirstObjectByType<OutfitUIManager>();
        if (uiManager == null)
        {
            EditorUtility.DisplayDialog("No UI Found",
                "No OutfitUI found in the current scene.\n\nPlease generate UI first.",
                "OK");
            return;
        }

        List<string> lines = new List<string>();
        lines.Add("=======================================");
        lines.Add("      UI DETAILED DIAGNOSTIC SCAN");
        lines.Add("=======================================");
        lines.Add($"Scan Time: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        lines.Add("");

        Canvas canvas = uiManager.GetComponent<Canvas>() ?? uiManager.GetComponentInParent<Canvas>(true);
        Transform mainPanel = uiManager.transform.Find("MainPanel");
        Transform leftPanel = uiManager.transform.Find("MainPanel/LeftPanel") ?? uiManager.transform.Find("LeftPanel");
        Transform rightPanel = uiManager.transform.Find("MainPanel/RightPanel") ?? uiManager.transform.Find("RightPanel");
        Transform scrollView = uiManager.transform.Find("MainPanel/RightPanel/ScrollView") ?? uiManager.transform.Find("RightPanel/ScrollView");
        Transform viewport = uiManager.transform.Find("MainPanel/RightPanel/ScrollView/Viewport") ?? uiManager.transform.Find("RightPanel/ScrollView/Viewport");
        Transform content = uiManager.transform.Find("MainPanel/RightPanel/ScrollView/Viewport/Content") ?? uiManager.transform.Find("RightPanel/ScrollView/Viewport/Content");

        lines.Add("1. UI STRUCTURE");
        lines.Add("---------------------------------------");
        lines.Add($"  Canvas: {(canvas != null ? "Present" : "Missing")}");
        if (canvas != null)
        {
            lines.Add($"    Render Mode: {canvas.renderMode}");
            lines.Add($"    Enabled: {canvas.enabled}");
        }
        lines.Add($"  MainPanel: {(mainPanel != null ? "Found" : "Missing")}");
        lines.Add($"  LeftPanel: {(leftPanel != null ? "Found" : "Missing")}");
        lines.Add($"  RightPanel: {(rightPanel != null ? "Found" : "Missing")}");
        lines.Add($"  ScrollView: {(scrollView != null ? "Found" : "Missing")}");
        lines.Add($"  Viewport: {(viewport != null ? "Found" : "Missing")}");
        lines.Add($"  Content: {(content != null ? "Found" : "Missing")}");
        if (content != null)
        {
            RectTransform contentRect = content as RectTransform;
            lines.Add($"    Content Child Count: {content.childCount}");
            lines.Add($"    Content Local Position: {content.localPosition}");
            if (contentRect != null)
            {
                lines.Add($"    Content Size: {contentRect.sizeDelta}");
                lines.Add($"    Content Rect: {contentRect.rect.size}");
            }
        }
        lines.Add("");

        lines.Add("2. REFERENCES");
        lines.Add("---------------------------------------");
        lines.Add($"  Player OutfitManager: {(uiManager.playerOutfitManager != null ? uiManager.playerOutfitManager.gameObject.name : "Missing")}");
        lines.Add($"  Outfit Button Container: {(uiManager.outfitButtonContainer != null ? uiManager.outfitButtonContainer.name : "Missing")}");
        lines.Add($"  Outfit Button Prefab: {(uiManager.outfitButtonPrefab != null ? uiManager.outfitButtonPrefab.name : "Missing")}");
        lines.Add($"  Current Outfit Text: {(uiManager.currentOutfitNameText != null ? uiManager.currentOutfitNameText.name : "Missing")}");
        lines.Add($"  Current Outfit Preview: {(uiManager.currentOutfitPreviewImage != null ? uiManager.currentOutfitPreviewImage.name : "Missing")}");
        lines.Add($"  Player Preview: {(uiManager.playerPreviewImage != null ? uiManager.playerPreviewImage.name : "Missing")}");
        lines.Add($"  Default Outfit Sprite: {(uiManager.defaultOutfitSprite != null ? uiManager.defaultOutfitSprite.name : "Missing")}");
        lines.Add($"  Default Player Sprite: {(uiManager.defaultPlayerSprite != null ? uiManager.defaultPlayerSprite.name : "Missing")}");
        lines.Add("");

        lines.Add("3. OUTFIT DATA COUNTS");
        lines.Add("---------------------------------------");
        List<OutfitData> scannedLibrary = ScanOutfitLibrary();
        lines.Add($"  Scanned OutfitData Assets: {scannedLibrary.Count}");
        lines.Add($"  UI Manager Library Count: {uiManager.outfitLibrary.Count}");

        int validPrefabCount = 0;
        int validPreviewCount = 0;
        int fullCardReadyCount = 0;
        foreach (OutfitData outfit in uiManager.outfitLibrary)
        {
            if (outfit == null)
            {
                lines.Add("  WARNING: Null outfit entry in UI library");
                continue;
            }

            bool hasPrefab = outfit.outfitPrefab != null;
            bool hasPreview = outfit.previewSprite != null || outfit.icon != null;
            if (hasPrefab) validPrefabCount++;
            if (hasPreview) validPreviewCount++;
            if (hasPrefab && hasPreview) fullCardReadyCount++;

            string previewSource = outfit.previewSprite != null
                ? $"sprite '{outfit.previewSprite.name}'"
                : outfit.icon != null
                    ? $"icon '{outfit.icon.name}'"
                    : "none";
            lines.Add($"  {outfit.outfitName}: prefab={(hasPrefab ? "Yes" : "No")}, preview={previewSource}");
        }
        lines.Add($"  Outfits With Prefab: {validPrefabCount}");
        lines.Add($"  Outfits With Preview: {validPreviewCount}");
        lines.Add($"  Outfits Ready For Card: {fullCardReadyCount}");
        lines.Add("");

        lines.Add("4. GENERATED CARD COUNTS");
        lines.Add("---------------------------------------");
        int expectedCards = uiManager.outfitLibrary.Count;
        int actualCards = uiManager.outfitButtonContainer != null ? uiManager.outfitButtonContainer.childCount : 0;
        lines.Add($"  Expected Cards: {expectedCards}");
        lines.Add($"  Actual Content Children: {actualCards}");
        lines.Add($"  Count Match: {(expectedCards == actualCards ? "Yes" : "No")}");
        if (uiManager.outfitButtonContainer != null)
        {
            for (int i = 0; i < uiManager.outfitButtonContainer.childCount; i++)
            {
                Transform child = uiManager.outfitButtonContainer.GetChild(i);
                Image preview = child.Find("PreviewImage")?.GetComponent<Image>() ?? child.Find("Preview")?.GetComponent<Image>();
                Text label = child.GetComponentInChildren<Text>(true);
                lines.Add($"  [{i}] {child.name}: preview={(preview != null && preview.sprite != null ? preview.sprite.name : "Missing")}, text={(label != null ? label.text : "Missing")}");
            }
        }
        lines.Add("");

        lines.Add("5. PREFAB HEALTH");
        lines.Add("---------------------------------------");
        if (uiManager.outfitButtonPrefab != null)
        {
            Button prefabButton = uiManager.outfitButtonPrefab.GetComponent<Button>();
            Image prefabBackground = uiManager.outfitButtonPrefab.GetComponent<Image>();
            Transform prefabPreview = uiManager.outfitButtonPrefab.transform.Find("PreviewImage") ?? uiManager.outfitButtonPrefab.transform.Find("Preview");
            Text prefabText = uiManager.outfitButtonPrefab.GetComponentInChildren<Text>(true);
            lines.Add($"  Button Component: {(prefabButton != null ? "Present" : "Missing")}");
            lines.Add($"  Background Image: {(prefabBackground != null ? "Present" : "Missing")}");
            lines.Add($"  Preview Node: {(prefabPreview != null ? prefabPreview.name : "Missing")}");
            lines.Add($"  Text Node: {(prefabText != null ? prefabText.name : "Missing")}");
        }
        else
        {
            lines.Add("  OutfitButton prefab is missing");
        }
        lines.Add("");

        lines.Add("6. SUMMARY");
        lines.Add("---------------------------------------");
        int issues = 0;
        if (uiManager.outfitButtonContainer == null) issues++;
        if (uiManager.outfitButtonPrefab == null) issues++;
        if (expectedCards > 0 && actualCards == 0) issues++;
        if (expectedCards != actualCards) issues++;
        if (uiManager.playerOutfitManager == null) issues++;
        lines.Add($"  Issues Found: {issues}");
        lines.Add(issues == 0 ? "  UI card pipeline looks healthy" : "  UI card pipeline still has missing links");

        string resultText = string.Join("\n", lines);
        EditorUtility.DisplayDialog("UI Detailed Diagnostic",
            resultText.Length > 5000 ? resultText.Substring(0, 5000) + "\n\n... (truncated)" : resultText,
            "OK");

        foreach (string line in lines)
        {
            if (line.Contains("Missing") || line.Contains("WARNING") || line.Contains("Issues Found:"))
            {
                if (line.Contains("Issues Found: 0"))
                {
                    Debug.Log(line);
                }
                else
                {
                    Debug.LogWarning(line);
                }
            }
            else
            {
                Debug.Log(line);
            }
        }
    }

    private GameObject CreateLeftPanel(GameObject parent)
    {
        GameObject leftPanel = new GameObject("LeftPanel");
        leftPanel.transform.SetParent(parent.transform);

        RectTransform leftRect = leftPanel.AddComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0, 0);
        leftRect.anchorMax = new Vector2(0.35f, 1);
        leftRect.offsetMin = new Vector2(10, 10);
        leftRect.offsetMax = new Vector2(-10, -10);

        Image leftImage = leftPanel.AddComponent<Image>();
        leftImage.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

        // Add title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(leftPanel.transform);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.sizeDelta = new Vector2(0, 50);
        titleRect.anchoredPosition = new Vector2(0, -5);

        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "CURRENT OUTFIT";
        titleText.font = GetDefaultFont();
        titleText.fontSize = 18;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;

        GameObject outfitNameObj = new GameObject("OutfitName");
        outfitNameObj.transform.SetParent(leftPanel.transform);
        RectTransform nameRect = outfitNameObj.AddComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.7f);
        nameRect.anchorMax = new Vector2(1, 0.85f);
        nameRect.offsetMin = new Vector2(10, 0);
        nameRect.offsetMax = new Vector2(-10, 0);

        Text outfitNameText = outfitNameObj.AddComponent<Text>();
        outfitNameText.text = "No Outfit Equipped";
        outfitNameText.font = GetDefaultFont();
        outfitNameText.fontSize = 16;
        outfitNameText.alignment = TextAnchor.MiddleCenter;
        outfitNameText.color = Color.yellow;
        outfitNameText.fontStyle = FontStyle.Bold;

        // Add preview area
        GameObject previewObj = new GameObject("OutfitPreview");
        previewObj.transform.SetParent(leftPanel.transform);
        RectTransform previewRect = previewObj.AddComponent<RectTransform>();
        previewRect.anchorMin = new Vector2(0.5f, 0.2f);
        previewRect.anchorMax = new Vector2(0.5f, 0.65f);
        previewRect.sizeDelta = new Vector2(previewSize, previewSize);
        previewRect.anchoredPosition = Vector2.zero;

        Image previewImage = previewObj.AddComponent<Image>();
        previewImage.color = new Color(0.3f, 0.3f, 0.3f);

        return leftPanel;
    }

    private GameObject CreateRightPanel(GameObject parent)
    {
        GameObject rightPanel = new GameObject("RightPanel");
        rightPanel.transform.SetParent(parent.transform);

        RectTransform rightRect = rightPanel.AddComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(0.35f, 0);
        rightRect.anchorMax = new Vector2(1, 1);
        rightRect.offsetMin = new Vector2(10, 10);
        rightRect.offsetMax = new Vector2(-10, -10);

        Image rightImage = rightPanel.AddComponent<Image>();
        rightImage.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);

        // Add title
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(rightPanel.transform);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 1);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.pivot = new Vector2(0.5f, 1);
        titleRect.sizeDelta = new Vector2(0, 50);
        titleRect.anchoredPosition = new Vector2(0, -5);

        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "OUTFIT LIBRARY";
        titleText.font = GetDefaultFont();
        titleText.fontSize = 20;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;

        // Add scroll view
        GameObject scrollView = new GameObject("ScrollView");
        scrollView.transform.SetParent(rightPanel.transform);
        RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0, 0);
        scrollRect.anchorMax = new Vector2(1, 1);
        scrollRect.offsetMin = new Vector2(10, 10);
        scrollRect.offsetMax = new Vector2(-10, -60);

        ScrollRect scroll = scrollView.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;

        // Viewport
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(scrollView.transform);
        RectTransform viewRect = viewport.AddComponent<RectTransform>();
        viewRect.anchorMin = Vector2.zero;
        viewRect.anchorMax = Vector2.one;
        viewRect.offsetMin = Vector2.zero;
        viewRect.offsetMax = Vector2.zero;

        Image viewImage = viewport.AddComponent<Image>();
        viewImage.color = new Color(0.1f, 0.1f, 0.1f);
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        // Content
        GameObject content = new GameObject("Content");
        content.transform.SetParent(viewport.transform);
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.sizeDelta = new Vector2(0, 100);

        GridLayoutGroup grid = content.AddComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(120, 140);
        grid.spacing = new Vector2(10, 10);
        grid.padding = new RectOffset(15, 15, 15, 15);
        grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment = TextAnchor.UpperCenter;

        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport = viewRect;
        scroll.content = contentRect;
        return rightPanel;
    }

    private GameObject CreateStyledButtonPrefab()
    {
        string path = "Assets/RAZ Outfit Studio/UI/OutfitButton.prefab";

        GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existingPrefab != null &&
            existingPrefab.GetComponent<Button>() != null &&
            existingPrefab.GetComponent<Image>() != null &&
            existingPrefab.transform.Find("PreviewImage") != null &&
            existingPrefab.transform.Find("ButtonText") != null)
        {
            return existingPrefab;
        }

        if (existingPrefab != null)
        {
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.Refresh();
        }

        GameObject buttonObj = new GameObject("OutfitButton");

        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(120, 140);

        Image img = buttonObj.AddComponent<Image>();
        img.color = new Color(0.25f, 0.25f, 0.25f);

        Button btn = buttonObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.25f, 0.25f, 0.25f);
        colors.highlightedColor = new Color(0.35f, 0.35f, 0.35f);
        colors.pressedColor = new Color(0.15f, 0.15f, 0.15f);
        colors.selectedColor = new Color(0.4f, 0.5f, 0.6f);
        btn.colors = colors;

        GameObject previewObj = new GameObject("PreviewImage");
        previewObj.transform.SetParent(buttonObj.transform);
        RectTransform previewRect = previewObj.AddComponent<RectTransform>();
        previewRect.anchorMin = new Vector2(0.5f, 0.6f);
        previewRect.anchorMax = new Vector2(0.5f, 0.95f);
        previewRect.sizeDelta = new Vector2(80, 80);
        previewRect.anchoredPosition = Vector2.zero;

        Image previewImage = previewObj.AddComponent<Image>();
        previewImage.color = new Color(0.5f, 0.5f, 0.5f);
        previewImage.type = Image.Type.Simple;
        previewImage.preserveAspect = true;

        GameObject textObj = new GameObject("ButtonText");
        textObj.transform.SetParent(buttonObj.transform);
        Text text = textObj.AddComponent<Text>();
        text.text = "Outfit Name";
        text.font = GetDefaultFont();
        text.fontSize = 12;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 0);
        textRect.anchorMax = new Vector2(1, 0.4f);
        textRect.offsetMin = new Vector2(5, 5);
        textRect.offsetMax = new Vector2(-5, -5);

        PrefabUtility.SaveAsPrefabAsset(buttonObj, path);
        DestroyImmediate(buttonObj);
        AssetDatabase.Refresh();

        Debug.Log($"✅ ROS: Created button prefab with sprite support at {path}");
        return AssetDatabase.LoadAssetAtPath<GameObject>(path);
    }

    private void CreateCloseButton(GameObject canvasObj)
    {
        GameObject closeButton = new GameObject("CloseButton");
        closeButton.transform.SetParent(canvasObj.transform);

        RectTransform closeRect = closeButton.AddComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1, 1);
        closeRect.anchorMax = new Vector2(1, 1);
        closeRect.pivot = new Vector2(1, 1);
        closeRect.sizeDelta = new Vector2(40, 40);
        closeRect.anchoredPosition = new Vector2(-10, -10);

        Image closeImage = closeButton.AddComponent<Image>();
        closeImage.color = new Color(0.8f, 0.2f, 0.2f);
        closeImage.type = Image.Type.Simple;

        Button closeBtn = closeButton.AddComponent<Button>();

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(closeButton.transform);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text closeText = textObj.AddComponent<Text>();
        closeText.text = "X";
        closeText.font = GetDefaultFont();
        closeText.fontSize = 20;
        closeText.fontStyle = FontStyle.Bold;
        closeText.alignment = TextAnchor.MiddleCenter;
        closeText.color = Color.white;
        closeText.raycastTarget = false;

        OutfitUIManager uiManager = canvasObj.GetComponent<OutfitUIManager>();
        if (uiManager != null)
        {
            closeBtn.onClick.AddListener(uiManager.CloseUI);
        }
        else
        {
            closeBtn.onClick.AddListener(() =>
            {
                Canvas canvas = canvasObj.GetComponent<Canvas>();
                if (canvas != null)
                {
                    canvas.enabled = false;
                }
                else
                {
                    canvasObj.SetActive(false);
                }
                Debug.Log("✅ ROS: UI closed by user");
            });
        }

        ButtonHoverEffect hoverEffect = closeButton.AddComponent<ButtonHoverEffect>();
        hoverEffect.normalColor = new Color(0.8f, 0.2f, 0.2f);
        hoverEffect.hoverColor = new Color(1f, 0.3f, 0.3f);
    }

    private void CreateRefreshButton(GameObject canvasObj, OutfitUIManager uiManager)
    {
        GameObject refreshButton = new GameObject("RefreshButton");
        refreshButton.transform.SetParent(canvasObj.transform);

        RectTransform refreshRect = refreshButton.AddComponent<RectTransform>();
        refreshRect.anchorMin = new Vector2(1, 1);
        refreshRect.anchorMax = new Vector2(1, 1);
        refreshRect.pivot = new Vector2(1, 1);
        refreshRect.sizeDelta = new Vector2(60, 40);
        refreshRect.anchoredPosition = new Vector2(-70, -10);

        Image refreshImage = refreshButton.AddComponent<Image>();
        refreshImage.color = new Color(0.3f, 0.3f, 0.5f);

        Button refreshBtn = refreshButton.AddComponent<Button>();

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(refreshButton.transform);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text refreshText = textObj.AddComponent<Text>();
        refreshText.text = "R";
        refreshText.font = GetDefaultFont();
        refreshText.fontSize = 24;
        refreshText.fontStyle = FontStyle.Bold;
        refreshText.alignment = TextAnchor.MiddleCenter;
        refreshText.color = Color.white;

        refreshBtn.onClick.AddListener(() =>
        {
            if (uiManager != null)
            {
                uiManager.ManualRefresh();
                Debug.Log("🔄 ROS: UI manually refreshed by user");
            }
        });
    }

    private void EnsureEventSystemInScene()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
        Undo.RegisterCreatedObjectUndo(eventSystemObject, "Create EventSystem");
        Debug.Log("✅ ROS: Created EventSystem for UI interaction");
    }

    private List<OutfitData> ScanOutfitLibrary()
    {
        List<OutfitData> library = new List<OutfitData>();

        string[] outfitDataGuids = AssetDatabase.FindAssets("t:OutfitData", new[] { "Assets/RAZ Outfit Studio/OutfitData" });

        foreach (string guid in outfitDataGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            OutfitData data = AssetDatabase.LoadAssetAtPath<OutfitData>(path);
            if (data != null && data.outfitPrefab != null)
            {
                library.Add(data);
            }
        }

        return library;
    }

    private void FindExistingUI()
    {
        OutfitUIManager uiManager = Object.FindFirstObjectByType<OutfitUIManager>();

        if (uiManager != null)
        {
            Selection.activeGameObject = uiManager.gameObject;
            EditorGUIUtility.PingObject(uiManager.gameObject);
            EditorUtility.DisplayDialog("UI Found",
                $"Found OutfitUI in scene: {uiManager.gameObject.name}\n\n" +
                $"It has been selected in the hierarchy.",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("No UI Found",
                "No OutfitUI found in the current scene.\n\n" +
                "Use the 'Generate UI System' button to create one.",
                "OK");
        }
    }

    #endregion

    #region Studio Tab (Phase 3)

    private void DrawStudioTab()
    {
        UpdateStudioStatusTimer();

        GUILayout.BeginVertical();

        EditorGUILayout.LabelField("Outfit Studio - Professional Asset Export", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("High-quality sprite capture with in-studio outfit switching", MessageType.Info);
        EditorGUILayout.Space(10);

        GUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Studio Setup", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        EnsureDefaultPlayerRootPrefab();
        EditorGUILayout.ObjectField("Root Prefab", playerRootPrefab, typeof(GameObject), false);
        EditorGUILayout.Space(5);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Open/Setup Studio Scene", GUILayout.Height(30)))
        {
            SetupStudioScene();
        }

        GUI.enabled = isStudioSceneOpen;
        if (GUILayout.Button("Close Studio Scene", GUILayout.Height(30)))
        {
            CloseStudioScene();
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();

        if (isStudioSceneOpen)
        {
            EditorGUILayout.HelpBox("Studio Active - Right-click + drag to orbit | Scroll to zoom | Click outfits to preview", MessageType.Info);

            if (currentStudioOutfit != null)
            {
                EditorGUILayout.LabelField($"Current Preview: {currentStudioOutfit.outfitName}", EditorStyles.miniBoldLabel);
            }
            else if (currentPreviewObject != null && currentPreviewObject.name.Contains("Character"))
            {
                EditorGUILayout.LabelField("Current Preview: Character Model", EditorStyles.miniBoldLabel);
            }

            if (!string.IsNullOrEmpty(studioStatusMessage) && studioStatusTimer > 0f)
            {
                EditorGUILayout.HelpBox(studioStatusMessage, MessageType.Info);
            }
        }
        GUILayout.EndVertical();

        EditorGUILayout.Space(10);

        if (isStudioSceneOpen && existingOutfits.Count > 0)
        {
            GUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("In-Studio Outfit Library", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Click any outfit to preview in studio | Click 'Snap' to capture and assign", MessageType.Info);
            EditorGUILayout.Space(5);

            studioOutfitScrollPosition = EditorGUILayout.BeginScrollView(studioOutfitScrollPosition, GUILayout.Height(220));

            const int itemsPerRow = 4;
            int currentItem = 0;
            GUILayout.BeginHorizontal();
            foreach (OutfitData outfit in existingOutfits)
            {
                if (currentItem > 0 && currentItem % itemsPerRow == 0)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }

                DrawStudioOutfitCard(outfit);
                currentItem++;
            }

            if (currentItem % itemsPerRow != 0)
            {
                for (int i = currentItem % itemsPerRow; i < itemsPerRow; i++)
                {
                    GUILayout.FlexibleSpace();
                }
            }

            GUILayout.EndHorizontal();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(5);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh Library", GUILayout.Height(25)))
            {
                RefreshOutfitList();
                ShowStatusMessage("Outfit library refreshed!", 2f);
            }

            if (GUILayout.Button("Load Character", GUILayout.Height(25)))
            {
                LoadCharacterInStudio();
            }

            if (GUILayout.Button("Clear Preview", GUILayout.Height(25)))
            {
                ClearStudioPreview();
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            EditorGUILayout.Space(10);
        }

        GUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Lighting Presets", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        GUILayout.BeginHorizontal();
        selectedPreset = (StudioPreset)EditorGUILayout.EnumPopup("Preset", selectedPreset);
        GUI.enabled = isStudioSceneOpen;
        if (GUILayout.Button("Apply", GUILayout.Width(80)))
        {
            ApplyStudioPreset(selectedPreset);
            ShowStatusMessage($"Applied {selectedPreset} lighting preset", 1.5f);
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();

        showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Lighting Settings");
        if (showAdvancedSettings && isStudioSceneOpen && previewLights != null && previewLights.Length >= 4)
        {
            EditorGUILayout.Space(5);
            previewLights[0].intensity = EditorGUILayout.Slider("Key Light Intensity", previewLights[0].intensity, 0f, 2f);
            previewLights[1].intensity = EditorGUILayout.Slider("Fill Light Intensity", previewLights[1].intensity, 0f, 1.5f);
            previewLights[2].intensity = EditorGUILayout.Slider("Rim Light Intensity", previewLights[2].intensity, 0f, 1.5f);
            previewLights[3].intensity = EditorGUILayout.Slider("Bounce Light Intensity", previewLights[3].intensity, 0f, 1.5f);
            previewLights[0].color = EditorGUILayout.ColorField("Key Light Color", previewLights[0].color);
            previewLights[1].color = EditorGUILayout.ColorField("Fill Light Color", previewLights[1].color);
            previewLights[2].color = EditorGUILayout.ColorField("Rim Light Color", previewLights[2].color);
            previewLights[3].color = EditorGUILayout.ColorField("Bounce Light Color", previewLights[3].color);
        }
        GUILayout.EndVertical();

        EditorGUILayout.Space(10);

        GUILayout.BeginVertical("box");
        showAdvancedExport = EditorGUILayout.Foldout(showAdvancedExport, "Export Settings", true);
        if (showAdvancedExport)
        {
            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("Resolution", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            selectedResolution = (ExportResolution)EditorGUILayout.EnumPopup("Preset", selectedResolution);
            if (selectedResolution == ExportResolution.Custom)
            {
                customResolution = EditorGUILayout.IntField("Custom", customResolution);
                customResolution = Mathf.Clamp(customResolution, 128, 8192);
            }
            GUILayout.EndHorizontal();

            int currentResolution = GetCurrentResolution();
            EditorGUILayout.LabelField($"  -> Export Size: {currentResolution}x{currentResolution} pixels");
            EditorGUILayout.Space(5);

            EditorGUILayout.LabelField("Format", EditorStyles.boldLabel);
            selectedFormat = (ExportFormat)EditorGUILayout.EnumPopup("File Format", selectedFormat);
            if (selectedFormat == ExportFormat.JPG)
            {
                jpgQuality = EditorGUILayout.IntSlider("JPG Quality", jpgQuality, 1, 100);
            }

            EditorGUILayout.Space(5);
            exportWithTransparency = EditorGUILayout.Toggle("Transparent Background", exportWithTransparency);
            if (!exportWithTransparency)
            {
                exportWithBackground = EditorGUILayout.Toggle("Custom Background", exportWithBackground);
                if (exportWithBackground)
                {
                    exportBackgroundColor = EditorGUILayout.ColorField("Background Color", exportBackgroundColor);
                }
            }

            EditorGUILayout.Space(5);
            exportMultipleAngles = EditorGUILayout.Toggle("Capture Multiple Angles", exportMultipleAngles);
            if (exportMultipleAngles)
            {
                EditorGUI.indentLevel++;
                numberOfAngles = EditorGUILayout.IntSlider("Number of Angles", numberOfAngles, 2, 12);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);
            exportThumbnail = EditorGUILayout.Toggle("Generate Thumbnail", exportThumbnail);
            if (exportThumbnail)
            {
                EditorGUI.indentLevel++;
                thumbnailSize = EditorGUILayout.IntSlider("Thumbnail Size", thumbnailSize, 64, 512);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space(5);
            createSpriteAtlas = EditorGUILayout.Toggle("Create Sprite Atlas", createSpriteAtlas);
            autoOpenFolder = EditorGUILayout.Toggle("Auto-Open Export Folder", autoOpenFolder);
            EditorGUI.indentLevel--;
        }
        GUILayout.EndVertical();

        EditorGUILayout.Space(10);

        GUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Camera Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        cameraDistance = EditorGUILayout.Slider("Camera Distance", cameraDistance, 1f, 10f);
        cameraHeight = EditorGUILayout.Slider("Camera Height", cameraHeight, -2f, 4f);
        cameraRotation = EditorGUILayout.Slider("Camera Rotation", cameraRotation, -180f, 180f);

        if (isStudioSceneOpen && previewCamera != null)
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Update Camera Position"))
            {
                UpdateCameraPosition();
            }

            if (GUILayout.Button("Reset Camera"))
            {
                cameraDistance = 3f;
                cameraHeight = 1f;
                cameraRotation = 0f;
                UpdateCameraPosition();
                ShowStatusMessage("Camera reset", 1f);
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();

        EditorGUILayout.Space(10);

        GUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Export Current Preview", EditorStyles.boldLabel);

        if (exportProgress > 0f && exportProgress < 1f)
        {
            EditorGUI.ProgressBar(GUILayoutUtility.GetRect(18, 18), exportProgress, exportStatus);
            EditorGUILayout.Space(5);
        }

        GUI.enabled = isStudioSceneOpen && (currentStudioOutfit != null || currentPreviewObject != null);
        if (currentStudioOutfit != null)
        {
            if (GUILayout.Button($"Export Current Outfit: {currentStudioOutfit.outfitName} ({GetCurrentResolution()}x{GetCurrentResolution()})", GUILayout.Height(40)))
            {
                ExportSingleOutfitAdvanced(currentStudioOutfit);
            }
        }
        else if (currentPreviewObject != null && currentPreviewObject.name.Contains("Character"))
        {
            if (GUILayout.Button($"Export Character Preview ({GetCurrentResolution()}x{GetCurrentResolution()})", GUILayout.Height(40)))
            {
                CaptureCharacterSprite();
            }
        }
        GUI.enabled = isStudioSceneOpen && existingOutfits.Count > 0;
        if (GUILayout.Button($"Batch Export All Outfits ({existingOutfits.Count} items)", GUILayout.Height(40)))
        {
            BatchExportAllOutfits();
        }
        GUI.enabled = true;
        GUILayout.EndVertical();

        EditorGUILayout.Space(10);

        if (showPreview && currentPreviewObject != null && isStudioSceneOpen)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("Live Preview", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            if (previewCamera != null)
            {
                RenderTexture previewRT = new RenderTexture(256, 256, 24);
                RenderTexture oldTarget = previewCamera.targetTexture;
                previewCamera.targetTexture = previewRT;
                previewCamera.Render();

                Rect rect = GUILayoutUtility.GetRect(256, 256);
                GUI.DrawTexture(rect, previewRT, ScaleMode.ScaleToFit);

                previewCamera.targetTexture = oldTarget;
                DestroyImmediate(previewRT);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(10);
        }

        GUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Export Info", EditorStyles.boldLabel);
        long estimatedSize = CalculateEstimatedFileSize(GetCurrentResolution());
        EditorGUILayout.LabelField($"  Resolution: {GetResolutionName()} ({GetCurrentResolution()}x{GetCurrentResolution()})");
        EditorGUILayout.LabelField($"  Est. File Size: {estimatedSize / 1024f / 1024f:F1} MB (PNG)");
        EditorGUILayout.LabelField($"  Format: {selectedFormat}");
        if (exportMultipleAngles)
        {
            EditorGUILayout.LabelField($"  Angles: {numberOfAngles}");
        }
        if (exportThumbnail)
        {
            EditorGUILayout.LabelField($"  Thumbnail: {thumbnailSize}x{thumbnailSize}");
        }
        GUILayout.EndVertical();

        GUILayout.EndVertical();
    }

    private void SetupStudioScene()
    {
        try
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EnsureFolders();

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(studioScenePath) != null)
            {
                EditorSceneManager.OpenScene(studioScenePath, OpenSceneMode.Single);
            }
            else
            {
                var studioScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                EditorSceneManager.SaveScene(studioScene, studioScenePath);
            }

            isStudioSceneOpen = true;
            studioStatusMessage = "";
            studioStatusTimer = 0f;
            studioStatusLastUpdateTime = 0d;
            SetupStudioEnvironment();
            ShowStatusMessage("Studio ready! Click outfits to preview.", 3f);

            Debug.Log("✅ ROS: Studio scene setup complete");
            EditorUtility.DisplayDialog("Studio Ready",
                "Studio scene has been set up with professional lighting and camera.\n\n" +
                "Features:\n" +
                "• 3-point lighting system (Key, Fill, Rim)\n" +
                "• Adjustable camera with orbit controls\n" +
                "• Multiple lighting presets\n" +
                "• High-quality render settings\n\n" +
                "Tips:\n" +
                "• Right-click + drag to orbit around objects\n" +
                "• Scroll wheel to zoom\n" +
                "• Try different lighting presets for different looks\n" +
                "• Adjust camera settings for better angles\n\n" +
                "You can now capture preview sprites for your outfits and character.",
                "OK");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ ROS Error setting up studio scene: {e.Message}");
            ShowStatusMessage($"Error: {e.Message}", 3f);
            EditorUtility.DisplayDialog("Error", $"Failed to setup studio scene:\n{e.Message}", "OK");
        }
    }

    private void SetupStudioEnvironment()
    {
        SetupStudioLighting();
        SetupStudioCamera();
        SetupStudioBackground();
        SetupRenderSettings();
        SetupRotationPlatform();
        ApplyStudioPreset(selectedPreset);
        SetupDefaultStudioPreview();
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }

    private void SetupDefaultStudioPreview()
    {
        LoadCharacterInStudio();
    }

    private void SetupStudioLighting()
    {
        Light[] existingLights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (Light light in existingLights)
        {
            DestroyImmediate(light.gameObject);
        }

        GameObject lightingRig = new GameObject("LightingRig");

        GameObject keyLight = new GameObject("KeyLight");
        keyLight.transform.SetParent(lightingRig.transform);
        keyLight.transform.position = new Vector3(3, 2.5f, 2);
        keyLight.transform.LookAt(Vector3.zero);
        Light keyLightComp = keyLight.AddComponent<Light>();
        keyLightComp.type = LightType.Spot;
        keyLightComp.intensity = 1.5f;
        keyLightComp.color = new Color(1f, 0.95f, 0.85f);
        keyLightComp.shadows = LightShadows.Soft;
        keyLightComp.shadowStrength = 0.6f;
        keyLightComp.range = 10f;
        keyLightComp.spotAngle = 60f;

        GameObject fillLight = new GameObject("FillLight");
        fillLight.transform.SetParent(lightingRig.transform);
        fillLight.transform.position = new Vector3(-2.5f, 1.5f, 2.5f);
        fillLight.transform.LookAt(Vector3.zero);
        Light fillLightComp = fillLight.AddComponent<Light>();
        fillLightComp.type = LightType.Spot;
        fillLightComp.intensity = 0.6f;
        fillLightComp.color = new Color(0.7f, 0.85f, 1f);
        fillLightComp.shadows = LightShadows.None;
        fillLightComp.range = 8f;
        fillLightComp.spotAngle = 70f;

        GameObject rimLight = new GameObject("RimLight");
        rimLight.transform.SetParent(lightingRig.transform);
        rimLight.transform.position = new Vector3(0, 2f, -3);
        rimLight.transform.LookAt(Vector3.zero);
        Light rimLightComp = rimLight.AddComponent<Light>();
        rimLightComp.type = LightType.Spot;
        rimLightComp.intensity = 1f;
        rimLightComp.color = new Color(1f, 0.9f, 0.7f);
        rimLightComp.shadows = LightShadows.None;
        rimLightComp.range = 8f;
        rimLightComp.spotAngle = 45f;

        GameObject bounceLight = new GameObject("BounceLight");
        bounceLight.transform.SetParent(lightingRig.transform);
        bounceLight.transform.position = new Vector3(0, -1f, 0);
        bounceLight.transform.rotation = Quaternion.Euler(90, 0, 0);
        Light bounceLightComp = bounceLight.AddComponent<Light>();
        bounceLightComp.type = LightType.Point;
        bounceLightComp.intensity = 0.3f;
        bounceLightComp.color = new Color(0.9f, 0.85f, 0.8f);
        bounceLightComp.range = 5f;

        previewLights = new[] { keyLightComp, fillLightComp, rimLightComp, bounceLightComp };

        Selection.activeGameObject = lightingRig;
        if (SceneView.lastActiveSceneView != null)
        {
            SceneView.lastActiveSceneView.FrameSelected();
        }

        Debug.Log("✅ ROS: Professional 4-point lighting setup complete");
    }

    private void SetupStudioCamera()
    {
        Camera[] existingCameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (Camera cam in existingCameras)
        {
            DestroyImmediate(cam.gameObject);
        }

        GameObject cameraObj = new GameObject("Preview Camera");
        previewCamera = cameraObj.AddComponent<Camera>();
        previewCamera.clearFlags = useTransparentBackground ? CameraClearFlags.SolidColor : CameraClearFlags.Skybox;
        previewCamera.backgroundColor = useTransparentBackground ? Color.clear : backgroundColor;
        previewCamera.orthographic = false;
        previewCamera.fieldOfView = 35f;
        previewCamera.nearClipPlane = 0.1f;
        previewCamera.farClipPlane = 100f;

        UpdateCameraPosition();
        CameraOrbitController orbitController = cameraObj.AddComponent<CameraOrbitController>();
        orbitController.targetPosition = Vector3.zero;
        orbitController.rotateSpeed = 3f;
        orbitController.zoomSpeed = 1.5f;
        orbitController.minDistance = 1.5f;
        orbitController.maxDistance = 8f;
        orbitController.SetCameraPosition(cameraDistance, cameraRotation, cameraHeight);

        Debug.Log("✅ ROS: Professional camera setup complete");
    }

    private void UpdateCameraPosition()
    {
        if (previewCamera != null)
        {
            Vector3 cameraPosition = new Vector3(
                Mathf.Sin(cameraRotation * Mathf.Deg2Rad) * cameraDistance,
                cameraHeight,
                Mathf.Cos(cameraRotation * Mathf.Deg2Rad) * cameraDistance
            );

            previewCamera.transform.position = cameraPosition;
            previewCamera.transform.LookAt(Vector3.zero);

            CameraOrbitController orbitController = previewCamera.GetComponent<CameraOrbitController>();
            if (orbitController != null)
            {
                orbitController.SetCameraPosition(cameraDistance, cameraRotation, cameraHeight);
            }
        }
    }

    private void SetupStudioBackground()
    {
        GameObject backdrop = GameObject.Find("StudioBackdrop");
        if (backdrop == null)
        {
            backdrop = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            backdrop.name = "StudioBackdrop";
            backdrop.transform.position = new Vector3(0, 0, -3);
            backdrop.transform.localScale = new Vector3(4, 0.5f, 0.5f);

            Renderer backdropRenderer = backdrop.GetComponent<Renderer>();
            backdropRenderer.enabled = false;
            backdropRenderer.receiveShadows = false;
        }

        GameObject ground = GameObject.Find("ShadowCatcher");
        if (ground == null)
        {
            ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "ShadowCatcher";
            ground.transform.position = new Vector3(0, -1.2f, 0);
            ground.transform.localScale = new Vector3(3, 1, 3);

            Renderer renderer = ground.GetComponent<Renderer>();
            renderer.enabled = false;
            renderer.receiveShadows = true;
        }

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.2f, 0.2f, 0.25f);

        Debug.Log("✅ ROS: Studio background setup complete");
    }

    private void SetupRenderSettings()
    {
        RenderSettings.ambientIntensity = 0.8f;
        RenderSettings.reflectionIntensity = 0.5f;
        RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;

        Shader standardShader = Shader.Find("Standard");
        if (standardShader != null)
        {
            RenderSettings.defaultReflectionResolution = 128;
        }

        Debug.Log("✅ ROS: Render settings optimized");
    }

    private void SetupRotationPlatform()
    {
        GameObject rotationPlatform = GameObject.Find("RotationPlatform");
        if (rotationPlatform == null)
        {
            rotationPlatform = new GameObject("RotationPlatform");
            rotationPlatform.transform.position = Vector3.zero;
        }
    }

    private void ApplyStudioPreset(StudioPreset preset)
    {
        if (!isStudioSceneOpen || previewLights == null || previewLights.Length < 4 || previewCamera == null)
        {
            EditorUtility.DisplayDialog("Studio Not Ready",
                "Please open the studio scene first before applying presets.",
                "OK");
            return;
        }

        switch (preset)
        {
            case StudioPreset.Default:
                previewLights[0].intensity = 1.2f;
                previewLights[1].intensity = 0.5f;
                previewLights[2].intensity = 0.8f;
                previewLights[3].intensity = 0.3f;
                previewLights[0].color = new Color(1f, 0.95f, 0.85f);
                previewLights[1].color = new Color(0.7f, 0.85f, 1f);
                previewLights[2].color = new Color(1f, 0.9f, 0.7f);
                previewLights[3].color = new Color(0.9f, 0.85f, 0.8f);
                cameraDistance = 3f;
                cameraHeight = 1f;
                previewCamera.fieldOfView = 35f;
                break;

            case StudioPreset.Product:
                previewLights[0].intensity = 1.5f;
                previewLights[1].intensity = 0.8f;
                previewLights[2].intensity = 1.0f;
                previewLights[3].intensity = 0.5f;
                previewLights[0].color = Color.white;
                previewLights[1].color = new Color(0.9f, 0.95f, 1f);
                previewLights[2].color = new Color(1f, 0.95f, 0.9f);
                previewLights[3].color = new Color(0.95f, 0.95f, 0.95f);
                cameraDistance = 3.5f;
                cameraHeight = 0.8f;
                previewCamera.fieldOfView = 35f;
                break;

            case StudioPreset.Character:
                previewLights[0].intensity = 1.3f;
                previewLights[1].intensity = 0.4f;
                previewLights[2].intensity = 1.0f;
                previewLights[3].intensity = 0.2f;
                previewLights[0].color = new Color(1f, 0.9f, 0.8f);
                previewLights[1].color = new Color(0.6f, 0.7f, 1f);
                previewLights[2].color = new Color(1f, 0.85f, 0.7f);
                previewLights[3].color = new Color(0.9f, 0.85f, 0.8f);
                cameraDistance = 2.8f;
                cameraHeight = 1.2f;
                previewCamera.fieldOfView = 30f;
                break;

            case StudioPreset.CloseUp:
                previewLights[0].intensity = 1.4f;
                previewLights[1].intensity = 0.3f;
                previewLights[2].intensity = 1.2f;
                previewLights[3].intensity = 0.2f;
                previewLights[0].color = new Color(1f, 0.95f, 0.9f);
                previewLights[1].color = new Color(0.8f, 0.85f, 1f);
                previewLights[2].color = new Color(1f, 0.85f, 0.75f);
                previewLights[3].color = new Color(0.9f, 0.85f, 0.8f);
                cameraDistance = 2.2f;
                cameraHeight = 1f;
                previewCamera.fieldOfView = 25f;
                break;

            case StudioPreset.FullBody:
                previewLights[0].intensity = 1.1f;
                previewLights[1].intensity = 0.6f;
                previewLights[2].intensity = 0.7f;
                previewLights[3].intensity = 0.4f;
                previewLights[0].color = new Color(1f, 0.95f, 0.9f);
                previewLights[1].color = new Color(0.75f, 0.85f, 1f);
                previewLights[2].color = new Color(1f, 0.9f, 0.8f);
                previewLights[3].color = new Color(0.9f, 0.85f, 0.8f);
                cameraDistance = 4.5f;
                cameraHeight = 1.5f;
                previewCamera.fieldOfView = 40f;
                break;
        }

        UpdateCameraPosition();
        Debug.Log($"✅ ROS: Applied {preset} lighting preset");
    }

    private void CloseStudioScene()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            ClearStudioPreview(false);
            EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            isStudioSceneOpen = false;
            previewCamera = null;
            previewLights = null;
            studioStatusMessage = "";
            studioStatusTimer = 0f;
            studioStatusLastUpdateTime = 0d;
            Debug.Log("✅ ROS: Closed studio scene");
        }
    }

    private void DrawStudioOutfitCard(OutfitData outfit)
    {
        GUILayout.BeginVertical("box", GUILayout.Width(120), GUILayout.Height(155));

        if (outfit.previewSprite != null && outfit.previewSprite.texture != null)
        {
            GUILayout.Box(outfit.previewSprite.texture, GUILayout.Width(100), GUILayout.Height(100));
        }
        else
        {
            GUILayout.Box("No Preview", GUILayout.Width(100), GUILayout.Height(100));
        }

        GUILayout.Label(outfit.outfitName, EditorStyles.miniBoldLabel, GUILayout.Width(100));

        GUILayout.BeginHorizontal();
        GUI.backgroundColor = currentStudioOutfit == outfit ? new Color(0.3f, 0.7f, 0.3f) : Color.white;
        if (GUILayout.Button("Preview", GUILayout.Width(55), GUILayout.Height(20)))
        {
            LoadOutfitInStudio(outfit);
        }
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("Snap", GUILayout.Width(38), GUILayout.Height(20)))
        {
            CaptureAndAssignToOutfit(outfit);
        }
        GUILayout.EndHorizontal();

        GUILayout.EndVertical();
    }

    private void LoadOutfitInStudio(OutfitData outfit)
    {
        if (!isStudioSceneOpen)
        {
            ShowStatusMessage("Please open the studio scene first.", 2f);
            return;
        }

        if (outfit == null || outfit.outfitPrefab == null)
        {
            ShowStatusMessage("Selected outfit is missing a prefab.", 2f);
            return;
        }

        try
        {
            if (currentPreviewObject != null)
            {
                DestroyImmediate(currentPreviewObject);
            }

            currentPreviewObject = Instantiate(outfit.outfitPrefab, Vector3.zero, Quaternion.identity);
            currentPreviewObject.name = outfit.outfitName + "_StudioPreview";
            CenterAndScaleObject(currentPreviewObject);

            currentStudioOutfit = outfit;
            selectedOutfitForCapture = outfit;

            ShowStatusMessage($"Loaded: {outfit.outfitName}", 2f);
            Debug.Log($"✅ ROS: Loaded {outfit.outfitName} in studio");
            Repaint();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ ROS Error loading outfit: {e.Message}");
            ShowStatusMessage($"Failed to load {outfit.outfitName}", 2f);
        }
    }

    private void CaptureAndAssignToOutfit(OutfitData outfit)
    {
        if (!isStudioSceneOpen)
        {
            ShowStatusMessage("Please open the studio scene first.", 2f);
            return;
        }

        if (outfit == null)
        {
            ShowStatusMessage("Invalid outfit selected.", 2f);
            return;
        }

        try
        {
            EnsureFolders();
            if (currentStudioOutfit != outfit || currentPreviewObject == null)
            {
                LoadOutfitInStudio(outfit);
            }

            System.Threading.Thread.Sleep(100);

            int originalResolution = captureResolution;
            captureResolution = 512;
            Texture2D capturedTexture = CaptureRenderTexture();
            captureResolution = originalResolution;

            if (capturedTexture == null)
            {
                ShowStatusMessage("Failed to capture snapshot.", 2f);
                return;
            }

            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string savePath = $"Assets/RAZ Outfit Studio/Sprites/{outfit.outfitName}_Preview_{timestamp}.png";
            byte[] bytes = capturedTexture.EncodeToPNG();
            System.IO.File.WriteAllBytes(savePath, bytes);
            AssetDatabase.Refresh();

            TextureImporter importer = AssetImporter.GetAtPath(savePath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spritePixelsPerUnit = 100;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport();

                Sprite capturedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(savePath);
                if (capturedSprite != null)
                {
                    outfit.previewSprite = capturedSprite;
                    EditorUtility.SetDirty(outfit);
                    AssetDatabase.SaveAssets();
                    ShowStatusMessage($"Snapshot assigned to {outfit.outfitName}.", 3f);
                    Debug.Log($"✅ ROS: Captured and assigned snapshot to {outfit.outfitName}");
                }
            }

            DestroyImmediate(capturedTexture);
            Repaint();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ ROS Error capturing snapshot: {e.Message}");
            ShowStatusMessage($"Error: {e.Message}", 3f);
        }
    }

    private void LoadCharacterInStudio()
    {
        EnsureDefaultPlayerRootPrefab();
        if (playerRootPrefab == null)
        {
            return;
        }

        try
        {
            if (currentPreviewObject != null)
            {
                DestroyImmediate(currentPreviewObject);
            }

            currentPreviewObject = Instantiate(playerRootPrefab, Vector3.zero, Quaternion.identity);
            currentPreviewObject.name = "Character_StudioPreview";
            CenterAndScaleObject(currentPreviewObject);

            currentStudioOutfit = null;
            ShowStatusMessage("Character loaded in studio.", 2f);
            Debug.Log("✅ ROS: Loaded character in studio");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ ROS Error loading character: {e.Message}");
            ShowStatusMessage("Failed to load character.", 2f);
        }
    }

    private void ClearStudioPreview()
    {
        ClearStudioPreview(true);
    }

    private void ClearStudioPreview(bool showMessage)
    {
        if (currentPreviewObject != null)
        {
            DestroyImmediate(currentPreviewObject);
            currentPreviewObject = null;
        }

        currentStudioOutfit = null;

        if (showMessage)
        {
            ShowStatusMessage("Preview cleared", 1f);
        }

        Debug.Log("✅ ROS: Cleared studio preview");
        Repaint();
    }

    private void ShowStatusMessage(string message, float duration)
    {
        studioStatusMessage = message;
        studioStatusTimer = duration;
        studioStatusLastUpdateTime = EditorApplication.timeSinceStartup;
        Repaint();
    }

    private void UpdateStudioStatusTimer()
    {
        if (studioStatusTimer <= 0f)
        {
            return;
        }

        double now = EditorApplication.timeSinceStartup;
        if (studioStatusLastUpdateTime <= 0d)
        {
            studioStatusLastUpdateTime = now;
            return;
        }

        float delta = (float)(now - studioStatusLastUpdateTime);
        studioStatusLastUpdateTime = now;
        studioStatusTimer = Mathf.Max(0f, studioStatusTimer - delta);

        if (studioStatusTimer <= 0f)
        {
            studioStatusMessage = "";
        }
        else
        {
            Repaint();
        }
    }

    private void CaptureOutfitSprite(OutfitData outfit)
    {
        if (outfit == null || outfit.outfitPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Invalid outfit selected", "OK");
            return;
        }

        try
        {
            if (currentPreviewObject != null)
            {
                DestroyImmediate(currentPreviewObject);
            }

            currentPreviewObject = Instantiate(outfit.outfitPrefab, Vector3.zero, Quaternion.identity);
            currentPreviewObject.name = outfit.outfitName + "_StudioPreview";

            CenterAndScaleObject(currentPreviewObject);
            System.Threading.Thread.Sleep(50);

            Texture2D capturedTexture = CaptureRenderTexture();
            if (capturedTexture == null)
            {
                EditorUtility.DisplayDialog("Capture Failed", "Failed to capture render texture.", "OK");
                return;
            }

            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filename = $"{outfit.outfitName}_Preview_{timestamp}.png";
            string savePath = EditorUtility.SaveFilePanel("Save Outfit Sprite",
                "Assets/RAZ Outfit Studio/Sprites",
                filename,
                "png");

            if (!string.IsNullOrEmpty(savePath))
            {
                byte[] bytes = capturedTexture.EncodeToPNG();
                System.IO.File.WriteAllBytes(savePath, bytes);
                string fileSizeStr = FormatFileSize(new System.IO.FileInfo(savePath).Length);

                AssetDatabase.Refresh();
                string relativePath = GetRelativePath(savePath);

                TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spritePixelsPerUnit = 100;
                    importer.mipmapEnabled = false;
                    importer.SaveAndReimport();

                    Sprite capturedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(relativePath);
                    if (capturedSprite != null)
                    {
                        outfit.previewSprite = capturedSprite;
                        EditorUtility.SetDirty(outfit);
                        AssetDatabase.SaveAssets();

                        EditorUtility.DisplayDialog("Capture Successful!",
                            $"Outfit: {outfit.outfitName}\n" +
                            $"Saved to: {relativePath}\n" +
                            $"Resolution: {captureResolution}x{captureResolution}\n" +
                            $"File Size: {fileSizeStr}\n" +
                            $"Format: PNG\n" +
                            $"Time: {System.DateTime.Now:HH:mm:ss}\n\n" +
                            $"Preview sprite has been automatically assigned to the outfit.",
                            "OK");

                        Debug.Log($"✅ ROS: Captured outfit '{outfit.outfitName}'");
                        Debug.Log($"   Path: {relativePath}");
                        Debug.Log($"   Resolution: {captureResolution}x{captureResolution}");
                        Debug.Log($"   File Size: {fileSizeStr}");
                    }
                }
            }

            DestroyImmediate(capturedTexture);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ ROS Error capturing outfit sprite: {e.Message}");
            EditorUtility.DisplayDialog("Error", $"Failed to capture sprite:\n{e.Message}", "OK");
        }
    }

    private void BatchCaptureOutfits()
    {
        if (existingOutfits.Count == 0)
        {
            EditorUtility.DisplayDialog("No Outfits", "No outfits found to capture.", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Batch Capture",
            $"This will capture preview sprites for {existingOutfits.Count} outfits.\n\nEach sprite will be saved with a unique timestamp.\n\nContinue?",
            "Yes", "No"))
        {
            return;
        }

        try
        {
            int successCount = 0;
            int failCount = 0;
            List<string> capturedFiles = new List<string>();

            for (int i = 0; i < existingOutfits.Count; i++)
            {
                OutfitData outfit = existingOutfits[i];
                EditorUtility.DisplayProgressBar("Batch Capture",
                    $"Capturing {outfit.outfitName}... ({i + 1}/{existingOutfits.Count})",
                    (float)i / existingOutfits.Count);

                try
                {
                    if (outfit == null || outfit.outfitPrefab == null)
                    {
                        failCount++;
                        continue;
                    }

                    if (currentPreviewObject != null)
                    {
                        DestroyImmediate(currentPreviewObject);
                    }

                    currentPreviewObject = Instantiate(outfit.outfitPrefab, Vector3.zero, Quaternion.identity);
                    currentPreviewObject.name = outfit.outfitName + "_BatchPreview";

                    CenterAndScaleObject(currentPreviewObject);
                    System.Threading.Thread.Sleep(50);

                    Texture2D capturedTexture = CaptureRenderTexture();
                    if (capturedTexture == null)
                    {
                        failCount++;
                        capturedFiles.Add($"{outfit.outfitName}: FAILED");
                        continue;
                    }

                    string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string filename = $"{outfit.outfitName}_Preview_{timestamp}.png";
                    string savePath = $"Assets/RAZ Outfit Studio/Sprites/{filename}";
                    byte[] bytes = capturedTexture.EncodeToPNG();
                    System.IO.File.WriteAllBytes(savePath, bytes);
                    long fileSize = new System.IO.FileInfo(savePath).Length;

                    AssetDatabase.Refresh();
                    TextureImporter importer = AssetImporter.GetAtPath(savePath) as TextureImporter;
                    if (importer != null)
                    {
                        importer.textureType = TextureImporterType.Sprite;
                        importer.spritePixelsPerUnit = 100;
                        importer.mipmapEnabled = false;
                        importer.SaveAndReimport();

                        Sprite capturedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(savePath);
                        if (capturedSprite != null)
                        {
                            outfit.previewSprite = capturedSprite;
                            EditorUtility.SetDirty(outfit);
                            successCount++;
                            capturedFiles.Add($"{outfit.outfitName}: {filename} ({FormatFileSize(fileSize)})");
                        }
                    }

                    DestroyImmediate(capturedTexture);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to capture {outfit.outfitName}: {e.Message}");
                    failCount++;
                    capturedFiles.Add($"{outfit.outfitName}: ERROR - {e.Message}");
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"✅ ROS: Batch capture complete - {successCount} success, {failCount} failed");
            EditorUtility.DisplayDialog("Batch Capture Complete",
                $"Batch Capture Complete!\n\n" +
                $"Success: {successCount}\n" +
                $"Failed: {failCount}\n" +
                $"Total: {existingOutfits.Count}\n\n" +
                $"Details:\n{string.Join("\n", capturedFiles)}\n\n" +
                $"Resolution: {captureResolution}x{captureResolution}\n" +
                $"Saved to: Assets/RAZ Outfit Studio/Sprites/",
                "OK");
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError($"❌ ROS Error in batch capture: {e.Message}");
            EditorUtility.DisplayDialog("Error", $"Batch capture failed:\n{e.Message}", "OK");
        }
    }

    private void CaptureCharacterSprite()
    {
        if (playerRootPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "No character prefab selected.", "OK");
            return;
        }

        try
        {
            if (currentPreviewObject != null)
            {
                DestroyImmediate(currentPreviewObject);
            }

            currentPreviewObject = Instantiate(playerRootPrefab, Vector3.zero, Quaternion.identity);
            currentPreviewObject.name = "Character_StudioPreview";

            CenterAndScaleObject(currentPreviewObject);
            System.Threading.Thread.Sleep(50);

            Texture2D capturedTexture = CaptureRenderTexture();
            if (capturedTexture == null)
            {
                return;
            }

            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string filename = $"Character_Preview_{timestamp}.png";
            string savePath = EditorUtility.SaveFilePanel("Save Character Sprite",
                "Assets/RAZ Outfit Studio/Sprites",
                filename,
                "png");

            if (!string.IsNullOrEmpty(savePath))
            {
                byte[] bytes = capturedTexture.EncodeToPNG();
                System.IO.File.WriteAllBytes(savePath, bytes);
                string fileSizeStr = FormatFileSize(new System.IO.FileInfo(savePath).Length);
                AssetDatabase.Refresh();
                string relativePath = GetRelativePath(savePath);

                EditorUtility.DisplayDialog("Capture Successful!",
                    $"Character Preview Captured\n\n" +
                    $"Saved to: {relativePath}\n" +
                    $"Resolution: {captureResolution}x{captureResolution}\n" +
                    $"File Size: {fileSizeStr}\n" +
                    $"Format: PNG\n" +
                    $"Time: {System.DateTime.Now:HH:mm:ss}",
                    "OK");

                Debug.Log($"✅ ROS: Character sprite saved to {relativePath}");
                Debug.Log($"   Resolution: {captureResolution}x{captureResolution}");
                Debug.Log($"   File Size: {fileSizeStr}");
            }

            DestroyImmediate(capturedTexture);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ ROS Error capturing character sprite: {e.Message}");
            EditorUtility.DisplayDialog("Error", $"Failed to capture character sprite:\n{e.Message}", "OK");
        }
    }

    private Texture2D CaptureRenderTexture()
    {
        if (previewCamera == null)
        {
            Debug.LogError("Preview camera is null!");
            return null;
        }

        UpdateCameraPosition();

        RenderTexture renderTexture = new RenderTexture(captureResolution, captureResolution, 24, RenderTextureFormat.ARGB32);
        RenderTexture oldRT = previewCamera.targetTexture;

        previewCamera.targetTexture = renderTexture;
        previewCamera.clearFlags = CameraClearFlags.SolidColor;
        previewCamera.backgroundColor = useTransparentBackground ? new Color(0, 0, 0, 0) : backgroundColor;
        previewCamera.Render();

        RenderTexture.active = renderTexture;
        Texture2D texture = new Texture2D(captureResolution, captureResolution, TextureFormat.ARGB32, false);
        texture.ReadPixels(new Rect(0, 0, captureResolution, captureResolution), 0, 0);
        texture.Apply();

        previewCamera.targetTexture = oldRT;
        RenderTexture.active = null;
        renderTexture.Release();
        DestroyImmediate(renderTexture);

        return texture;
    }

    private void CenterAndScaleObject(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return;
        }

        obj.transform.position = Vector3.zero;
        obj.transform.rotation = Quaternion.identity;
        obj.transform.localScale = Vector3.one;

        Bounds initialBounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
        {
            initialBounds.Encapsulate(renderer.bounds);
        }

        float maxDimension = Mathf.Max(initialBounds.size.x, initialBounds.size.y, initialBounds.size.z);
        if (maxDimension > 0.001f)
        {
            float targetSize = 2.5f;
            float scale = targetSize / maxDimension;
            obj.transform.localScale = Vector3.one * scale;
        }

        renderers = obj.GetComponentsInChildren<Renderer>();
        Bounds scaledBounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers)
        {
            scaledBounds.Encapsulate(renderer.bounds);
        }

        float groundY = -1.2f;
        Vector3 offset = new Vector3(
            -scaledBounds.center.x,
            groundY - scaledBounds.min.y,
            -scaledBounds.center.z);

        obj.transform.position += offset;

        Debug.Log($"✅ ROS: Centered preview '{obj.name}' at {obj.transform.position} with scale {obj.transform.localScale}");
    }

    private void SaveTextureAsPNG(Texture2D texture, string path)
    {
        byte[] bytes = texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(path, bytes);
        string fileSize = FormatFileSize(new System.IO.FileInfo(path).Length);
        Debug.Log($"✅ ROS: Saved texture to {path}");
        Debug.Log($"   Resolution: {texture.width}x{texture.height}");
        Debug.Log($"   File Size: {fileSize}");
    }

    private string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private async void ExportSingleOutfitAdvanced(OutfitData outfit)
    {
        if (outfit == null || outfit.outfitPrefab == null)
        {
            EditorUtility.DisplayDialog("Error", "Invalid outfit selected", "OK");
            return;
        }

        try
        {
            exportProgress = 0f;
            exportStatus = "Preparing...";
            Repaint();

            EnsureFolders();
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string defaultExportRoot = GetDefaultExportsFolderAbsolute();
            string exportFolder = EditorUtility.SaveFolderPanel("Select Export Folder",
                defaultExportRoot,
                $"{outfit.outfitName}_Export_{timestamp}");

            if (string.IsNullOrEmpty(exportFolder))
            {
                exportProgress = 0f;
                exportStatus = string.Empty;
                return;
            }

            System.IO.Directory.CreateDirectory(exportFolder);

            int resolution = GetCurrentResolution();
            int totalSteps = 1;
            if (exportMultipleAngles) totalSteps += numberOfAngles;
            if (exportThumbnail) totalSteps++;

            int currentStep = 0;
            List<string> capturedFiles = new List<string>();

            if (currentPreviewObject != null) DestroyImmediate(currentPreviewObject);
            currentPreviewObject = Instantiate(outfit.outfitPrefab, Vector3.zero, Quaternion.identity);
            currentPreviewObject.name = outfit.outfitName + "_Export";
            CenterAndScaleObject(currentPreviewObject);

            currentStep++;
            exportStatus = $"Capturing main image... ({currentStep}/{totalSteps})";
            exportProgress = (float)currentStep / totalSteps;
            Repaint();
            await System.Threading.Tasks.Task.Delay(100);

            string mainFilePath = CaptureWithSettings(exportFolder, outfit.outfitName, resolution, "main");
            if (!string.IsNullOrEmpty(mainFilePath))
            {
                capturedFiles.Add(mainFilePath);
            }

            if (exportMultipleAngles)
            {
                float angleStep = 360f / numberOfAngles;
                float originalRotation = cameraRotation;

                for (int i = 0; i < numberOfAngles; i++)
                {
                    currentStep++;
                    exportStatus = $"Capturing angle {i + 1}/{numberOfAngles}... ({currentStep}/{totalSteps})";
                    exportProgress = (float)currentStep / totalSteps;
                    Repaint();

                    cameraRotation = i * angleStep;
                    UpdateCameraPosition();
                    await System.Threading.Tasks.Task.Delay(50);

                    string angleFilePath = CaptureWithSettings(exportFolder, outfit.outfitName, resolution, $"angle_{i:00}");
                    if (!string.IsNullOrEmpty(angleFilePath))
                    {
                        capturedFiles.Add(angleFilePath);
                    }
                }

                cameraRotation = originalRotation;
                UpdateCameraPosition();
            }

            if (exportThumbnail)
            {
                currentStep++;
                exportStatus = $"Generating thumbnail... ({currentStep}/{totalSteps})";
                exportProgress = (float)currentStep / totalSteps;
                Repaint();
                await System.Threading.Tasks.Task.Delay(50);

                string thumbPath = CaptureThumbnail(exportFolder, outfit.outfitName);
                if (!string.IsNullOrEmpty(thumbPath))
                {
                    capturedFiles.Add(thumbPath);
                }
            }

            exportProgress = 1f;
            exportStatus = "Complete!";
            Repaint();

            long totalSize = 0;
            foreach (string file in capturedFiles)
            {
                if (System.IO.File.Exists(file))
                {
                    totalSize += new System.IO.FileInfo(file).Length;
                }
            }

            EditorUtility.DisplayDialog("Export Successful!",
                $"Export Complete!\n\n" +
                $"Outfit: {outfit.outfitName}\n" +
                $"Resolution: {resolution}x{resolution}\n" +
                $"Format: {selectedFormat}\n" +
                $"Files: {capturedFiles.Count}\n" +
                $"Total Size: {FormatFileSize(totalSize)}\n\n" +
                $"Saved to: {exportFolder}",
                "OK");

            if (autoOpenFolder && System.IO.Directory.Exists(exportFolder))
            {
                System.Diagnostics.Process.Start(exportFolder);
            }

            Debug.Log($"✅ ROS: Advanced export complete for {outfit.outfitName}");
            Debug.Log($"   Resolution: {resolution}x{resolution}");
            Debug.Log($"   Format: {selectedFormat}");
            Debug.Log($"   Files: {capturedFiles.Count}");
            Debug.Log($"   Total Size: {FormatFileSize(totalSize)}");

            if (currentPreviewObject != null) DestroyImmediate(currentPreviewObject);
            currentPreviewObject = null;
            exportProgress = 0f;
            exportStatus = string.Empty;
            Repaint();
        }
        catch (System.Exception e)
        {
            exportProgress = 0f;
            exportStatus = string.Empty;
            EditorUtility.DisplayDialog("Error", $"Failed to export:\n{e.Message}", "OK");
            Debug.LogError($"❌ ROS Error: {e.Message}");
        }
    }

    private async void BatchExportAllOutfits()
    {
        if (existingOutfits.Count == 0)
        {
            EditorUtility.DisplayDialog("No Outfits", "No outfits found to export.", "OK");
            return;
        }

        EnsureFolders();
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string defaultExportRoot = GetDefaultExportsFolderAbsolute();
        string batchFolder = EditorUtility.SaveFolderPanel("Select Batch Export Folder",
            defaultExportRoot,
            $"Outfit_Batch_Export_{timestamp}");

        if (string.IsNullOrEmpty(batchFolder))
        {
            return;
        }

        System.IO.Directory.CreateDirectory(batchFolder);

        if (!EditorUtility.DisplayDialog("Batch Export",
            $"This will export {existingOutfits.Count} outfits.\n\n" +
            $"Resolution: {GetResolutionName()} ({GetCurrentResolution()}x{GetCurrentResolution()})\n" +
            $"Format: {selectedFormat}\n" +
            $"Multi-angle: {(exportMultipleAngles ? $"Yes ({numberOfAngles} angles)" : "No")}\n" +
            $"Thumbnails: {(exportThumbnail ? "Yes" : "No")}\n\n" +
            $"Estimated total files: {CalculateTotalFiles()}\n" +
            $"Estimated total size: {CalculateEstimatedTotalSize()}\n\n" +
            $"Continue?",
            "Yes", "No"))
        {
            return;
        }

        try
        {
            int totalOutfits = existingOutfits.Count;
            int filesPerOutfit = 1;
            if (exportMultipleAngles) filesPerOutfit += numberOfAngles;
            if (exportThumbnail) filesPerOutfit++;

            int totalFiles = totalOutfits * filesPerOutfit;
            int currentFile = 0;
            List<string> allExportedFiles = new List<string>();
            int successCount = 0;

            for (int i = 0; i < totalOutfits; i++)
            {
                OutfitData outfit = existingOutfits[i];
                EditorUtility.DisplayProgressBar("Batch Export",
                    $"Exporting {outfit.outfitName}... ({i + 1}/{totalOutfits})",
                    (float)i / totalOutfits);

                try
                {
                    if (currentPreviewObject != null) DestroyImmediate(currentPreviewObject);

                    currentPreviewObject = Instantiate(outfit.outfitPrefab, Vector3.zero, Quaternion.identity);
                    currentPreviewObject.name = outfit.outfitName + "_Export";
                    CenterAndScaleObject(currentPreviewObject);

                    string outfitFolder = System.IO.Path.Combine(batchFolder, outfit.outfitName);
                    System.IO.Directory.CreateDirectory(outfitFolder);

                    int resolution = GetCurrentResolution();
                    string mainPath = CaptureWithSettings(outfitFolder, outfit.outfitName, resolution, "main");
                    if (!string.IsNullOrEmpty(mainPath))
                    {
                        allExportedFiles.Add(mainPath);
                    }
                    currentFile++;

                    if (exportMultipleAngles)
                    {
                        float angleStep = 360f / numberOfAngles;
                        float originalRotation = cameraRotation;

                        for (int angleIdx = 0; angleIdx < numberOfAngles; angleIdx++)
                        {
                            cameraRotation = angleIdx * angleStep;
                            UpdateCameraPosition();
                            await System.Threading.Tasks.Task.Delay(30);

                            string anglePath = CaptureWithSettings(outfitFolder, outfit.outfitName, resolution, $"angle_{angleIdx:00}");
                            if (!string.IsNullOrEmpty(anglePath))
                            {
                                allExportedFiles.Add(anglePath);
                            }
                            currentFile++;
                        }

                        cameraRotation = originalRotation;
                        UpdateCameraPosition();
                    }

                    if (exportThumbnail)
                    {
                        string thumbPath = CaptureThumbnail(outfitFolder, outfit.outfitName);
                        if (!string.IsNullOrEmpty(thumbPath))
                        {
                            allExportedFiles.Add(thumbPath);
                        }
                        currentFile++;
                    }

                    string previewPath = System.IO.Path.Combine(outfitFolder, $"{outfit.outfitName}_main.{GetFormatExtension()}");
                    if (!System.IO.File.Exists(previewPath))
                    {
                        previewPath = System.IO.Path.Combine(outfitFolder, $"{outfit.outfitName}_main.png");
                    }

                    if (System.IO.File.Exists(previewPath) && previewPath.StartsWith(Application.dataPath))
                    {
                        AssetDatabase.Refresh();
                        string relativePath = GetRelativePath(previewPath);
                        TextureImporter importer = AssetImporter.GetAtPath(relativePath) as TextureImporter;
                        if (importer != null)
                        {
                            importer.textureType = TextureImporterType.Sprite;
                            importer.SaveAndReimport();

                            Sprite capturedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(relativePath);
                            if (capturedSprite != null)
                            {
                                outfit.previewSprite = capturedSprite;
                                EditorUtility.SetDirty(outfit);
                            }
                        }
                    }

                    successCount++;
                    EditorUtility.DisplayProgressBar("Batch Export",
                        $"Exporting {outfit.outfitName}... Files: {currentFile}/{totalFiles}",
                        (float)currentFile / totalFiles);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to export {outfit.outfitName}: {e.Message}");
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            long totalSize = 0;
            foreach (string file in allExportedFiles)
            {
                if (System.IO.File.Exists(file))
                {
                    totalSize += new System.IO.FileInfo(file).Length;
                }
            }

            EditorUtility.DisplayDialog("Batch Export Complete",
                $"Batch Export Complete!\n\n" +
                $"Success: {successCount}/{totalOutfits} outfits\n" +
                $"Total Files: {allExportedFiles.Count}\n" +
                $"Total Size: {FormatFileSize(totalSize)}\n" +
                $"Resolution: {GetResolutionName()}\n" +
                $"Format: {selectedFormat}\n\n" +
                $"Saved to: {batchFolder}",
                "OK");

            if (autoOpenFolder && System.IO.Directory.Exists(batchFolder))
            {
                System.Diagnostics.Process.Start(batchFolder);
            }

            Debug.Log($"✅ ROS: Batch export complete - {successCount}/{totalOutfits} outfits");
            Debug.Log($"   Total files: {allExportedFiles.Count}");
            Debug.Log($"   Total size: {FormatFileSize(totalSize)}");

            if (currentPreviewObject != null) DestroyImmediate(currentPreviewObject);
            currentPreviewObject = null;
            exportProgress = 0f;
            exportStatus = string.Empty;
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            exportProgress = 0f;
            exportStatus = string.Empty;
            Debug.LogError($"❌ ROS Error in batch export: {e.Message}");
            EditorUtility.DisplayDialog("Error", $"Batch export failed:\n{e.Message}", "OK");
        }
    }

    private string CaptureWithSettings(string folder, string baseName, int resolution, string suffix)
    {
        bool originalTransparency = useTransparentBackground;
        Color originalBgColor = backgroundColor;
        int originalResolution = captureResolution;

        useTransparentBackground = exportWithTransparency;
        if (exportWithBackground && !exportWithTransparency)
        {
            backgroundColor = exportBackgroundColor;
        }

        captureResolution = resolution;
        Texture2D texture = CaptureRenderTexture();

        captureResolution = originalResolution;
        useTransparentBackground = originalTransparency;
        backgroundColor = originalBgColor;

        if (texture == null)
        {
            return null;
        }

        string extension = GetFormatExtension();
        string filename = $"{baseName}_{suffix}.{extension}";
        string fullPath = System.IO.Path.Combine(folder, filename);

        byte[] bytes = null;
        switch (selectedFormat)
        {
            case ExportFormat.PNG:
                bytes = texture.EncodeToPNG();
                break;
            case ExportFormat.JPG:
                bytes = texture.EncodeToJPG(jpgQuality);
                break;
            case ExportFormat.TGA:
                bytes = texture.EncodeToTGA();
                break;
            case ExportFormat.EXR:
                bytes = texture.EncodeToEXR();
                break;
            case ExportFormat.WebP:
                Debug.LogWarning("WebP format requires WebP package. Falling back to PNG.");
                bytes = texture.EncodeToPNG();
                fullPath = System.IO.Path.Combine(folder, $"{baseName}_{suffix}.png");
                break;
        }

        if (bytes != null)
        {
            System.IO.File.WriteAllBytes(fullPath, bytes);
            Debug.Log($"✅ ROS: Saved {System.IO.Path.GetFileName(fullPath)} ({resolution}x{resolution})");
        }

        DestroyImmediate(texture);
        return fullPath;
    }

    private string CaptureThumbnail(string folder, string baseName)
    {
        int originalResolution = captureResolution;
        bool originalTransparency = useTransparentBackground;

        captureResolution = thumbnailSize;
        useTransparentBackground = true;

        Texture2D texture = CaptureRenderTexture();

        captureResolution = originalResolution;
        useTransparentBackground = originalTransparency;

        if (texture == null)
        {
            return null;
        }

        string fullPath = System.IO.Path.Combine(folder, $"{baseName}_thumb.png");
        byte[] bytes = texture.EncodeToPNG();
        System.IO.File.WriteAllBytes(fullPath, bytes);

        DestroyImmediate(texture);
        return fullPath;
    }

    private string GetDefaultExportsFolderAbsolute()
    {
        string exportsFolder = System.IO.Path.Combine(Application.dataPath, "RAZ Outfit Studio", "Exports");
        System.IO.Directory.CreateDirectory(exportsFolder);
        return exportsFolder;
    }

    private int GetCurrentResolution()
    {
        return selectedResolution == ExportResolution.Custom ? customResolution : (int)selectedResolution;
    }

    private string GetResolutionName()
    {
        switch (selectedResolution)
        {
            case ExportResolution.HD_720p: return "720p HD";
            case ExportResolution.HD_1080p: return "1080p Full HD";
            case ExportResolution.QHD_1440p: return "1440p QHD";
            case ExportResolution.UHD_4K: return "4K UHD";
            case ExportResolution.Custom: return $"{customResolution}px Custom";
            default: return "Unknown";
        }
    }

    private string GetFormatExtension()
    {
        switch (selectedFormat)
        {
            case ExportFormat.PNG: return "png";
            case ExportFormat.JPG: return "jpg";
            case ExportFormat.WebP: return "webp";
            case ExportFormat.TGA: return "tga";
            case ExportFormat.EXR: return "exr";
            default: return "png";
        }
    }

    private float GetResolutionMegapixels(int resolution)
    {
        return (resolution * resolution) / 1000000f;
    }

    private long CalculateEstimatedFileSize(int resolution)
    {
        long uncompressed = (long)resolution * resolution * 4;
        return uncompressed / 10;
    }

    private int CalculateTotalFiles()
    {
        int filesPerOutfit = 1;
        if (exportMultipleAngles) filesPerOutfit += numberOfAngles;
        if (exportThumbnail) filesPerOutfit++;
        return existingOutfits.Count * filesPerOutfit;
    }

    private string CalculateEstimatedTotalSize()
    {
        long perFile = CalculateEstimatedFileSize(GetCurrentResolution());
        long total = perFile * CalculateTotalFiles();
        return FormatFileSize(total);
    }

    private string GetRelativePath(string absolutePath)
    {
        if (absolutePath.StartsWith(Application.dataPath))
        {
            return "Assets" + absolutePath.Substring(Application.dataPath.Length);
        }

        return absolutePath;
    }

    #endregion

    #region Diagnostic Tab (With Console Logging)

    private void DrawDiagnosedTab()
    {
        EditorGUILayout.LabelField("System Diagnostics", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Deep Scan will check all ROS components, including scene and studio positions.", MessageType.Info);
        EditorGUILayout.Space(10);

        GUI.enabled = !isDeepScanning;
        if (GUILayout.Button(isDeepScanning ? "Scanning..." : "Run Deep Scan", GUILayout.Height(30)))
        {
            RunDeepScan();
        }
        GUI.enabled = true;

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Quick Status:", EditorStyles.boldLabel);
        DisplayQuickStatus();

        EditorGUILayout.Space(10);

        EditorGUILayout.LabelField("Detailed Scan Results:", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));

        if (diagnosticLog.Count == 0)
        {
            EditorGUILayout.HelpBox("Click 'Run Deep Scan' to perform a complete system analysis", MessageType.Info);
        }
        else
        {
            foreach (string logEntry in diagnosticLog)
            {
                if (logEntry.StartsWith("ERROR:"))
                    EditorGUILayout.HelpBox(logEntry, MessageType.Error);
                else if (logEntry.StartsWith("WARNING:"))
                    EditorGUILayout.HelpBox(logEntry, MessageType.Warning);
                else if (logEntry.StartsWith("SUCCESS:"))
                    EditorGUILayout.HelpBox(logEntry, MessageType.Info);
                else if (logEntry.StartsWith("→"))
                    EditorGUILayout.LabelField(logEntry, EditorStyles.miniLabel);
                else if (logEntry.StartsWith("  "))
                    EditorGUILayout.LabelField(logEntry, EditorStyles.miniLabel);
                else
                    EditorGUILayout.LabelField(logEntry, EditorStyles.boldLabel);
            }
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();
        if (diagnosticLog.Count > 0 && GUILayout.Button("Export Scan Log"))
        {
            ExportDiagnosticLog();
        }
        if (GUILayout.Button("Reset Preview Position"))
        {
            if (currentPreviewObject != null)
            {
                CenterAndScaleObject(currentPreviewObject);
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DisplayQuickStatus()
    {
        EditorGUILayout.BeginVertical("box");

        int rootPrefabCount = AssetDatabase.FindAssets("t:Prefab _Root", new[] { "Assets/RAZ Outfit Studio/Characters" }).Length;
        int outfitCount = existingOutfits.Count;

        EditorGUILayout.LabelField($"✓ Root Prefabs: {rootPrefabCount}", rootPrefabCount > 0 ? EditorStyles.miniLabel : EditorStyles.miniLabel);
        EditorGUILayout.LabelField($"✓ Outfits: {outfitCount}", outfitCount > 0 ? EditorStyles.miniLabel : EditorStyles.miniLabel);

        // Check for UI
        string[] uiPrefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/RAZ Outfit Studio/UI" });
        int uiSystemCount = 0;
        foreach (string guid in uiPrefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject candidate = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (candidate != null && candidate.GetComponent<OutfitUIManager>() != null)
            {
                uiSystemCount++;
            }
        }
        EditorGUILayout.LabelField($"✓ UI Systems: {uiSystemCount}", EditorStyles.miniLabel);

        OutfitManager playerManager = Object.FindFirstObjectByType<OutfitManager>();
        if (playerManager != null)
        {
            EditorGUILayout.LabelField($"✓ Player In Scene: {playerManager.gameObject.name}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"  Position: {playerManager.transform.position}", EditorStyles.miniLabel);
        }

        if (currentPreviewObject != null)
        {
            EditorGUILayout.LabelField($"✓ Studio Preview: {currentPreviewObject.name}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"  Position: {currentPreviewObject.transform.position}", EditorStyles.miniLabel);
        }

        EditorGUILayout.EndVertical();
    }

    private void RunDeepScan()
    {
        isDeepScanning = true;
        diagnosticLog.Clear();

        // Clear Unity console and start fresh
        Debug.ClearDeveloperConsole();
        Debug.Log("═══════════════════════════════════════");
        Debug.Log("      RAZ Outfit Studio Deep Scan");
        Debug.Log("═══════════════════════════════════════");

        try
        {
            diagnosticLog.Add("═══════════════════════════════════════");
            diagnosticLog.Add("      RAZ Outfit Studio Deep Scan");
            diagnosticLog.Add("═══════════════════════════════════════");
            diagnosticLog.Add($"Scan Time: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            diagnosticLog.Add("");

            CheckFolderStructure();
            diagnosticLog.Add("");

            CheckRootPrefabs();
            diagnosticLog.Add("");

            CheckCurrentSceneObjects();
            diagnosticLog.Add("");

            CheckOutfitData();
            diagnosticLog.Add("");

            CheckUISystem();
            diagnosticLog.Add("");

            CheckStudioScene();
            diagnosticLog.Add("");

            CheckComponentIntegrity();
            diagnosticLog.Add("");

            GeneratePerformanceSummary();

            diagnosticLog.Add("");
            diagnosticLog.Add("═══════════════════════════════════════");
            diagnosticLog.Add("            SCAN COMPLETE");
            diagnosticLog.Add("═══════════════════════════════════════");

            Debug.Log("═══════════════════════════════════════");
            Debug.Log("            SCAN COMPLETE");
            Debug.Log("═══════════════════════════════════════");
        }
        catch (System.Exception e)
        {
            diagnosticLog.Add($"ERROR: Scan failed - {e.Message}");
            Debug.LogError($"❌ ROS Error: {e.Message}");
        }
        finally
        {
            isDeepScanning = false;
            Repaint();
        }
    }

    private void CheckFolderStructure()
    {
        diagnosticLog.Add("1. FOLDER STRUCTURE CHECK");
        diagnosticLog.Add("───────────────────────────────────────");
        Debug.Log("1. FOLDER STRUCTURE CHECK");
        Debug.Log("───────────────────────────────────────");

        string[] requiredFolders = {
            "Assets/RAZ Outfit Studio",
            "Assets/RAZ Outfit Studio/Characters",
            "Assets/RAZ Outfit Studio/Outfits",
            "Assets/RAZ Outfit Studio/OutfitData",
            "Assets/RAZ Outfit Studio/UI",
            "Assets/RAZ Outfit Studio/Sprites"
        };

        foreach (string folder in requiredFolders)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                diagnosticLog.Add($"  ✓ {folder}");
                Debug.Log($"  ✓ {folder}");
            }
            else
            {
                diagnosticLog.Add($"  ✗ {folder} - MISSING");
                Debug.LogWarning($"  ✗ {folder} - MISSING");
            }
        }
    }

    private void CheckRootPrefabs()
    {
        diagnosticLog.Add("2. ROOT PREFABS CHECK");
        diagnosticLog.Add("───────────────────────────────────────");
        Debug.Log("2. ROOT PREFABS CHECK");
        Debug.Log("───────────────────────────────────────");

        string[] rootPrefabGuids = AssetDatabase.FindAssets("t:Prefab _Root", new[] { "Assets/RAZ Outfit Studio/Characters" });

        if (rootPrefabGuids.Length == 0)
        {
            diagnosticLog.Add("  WARNING: No root prefabs found");
            diagnosticLog.Add("  → Use Root Setup tab to create a player root prefab");
            Debug.LogWarning("  WARNING: No root prefabs found");
            Debug.Log("  → Use Root Setup tab to create a player root prefab");
            return;
        }

        diagnosticLog.Add($"  Found {rootPrefabGuids.Length} root prefab(s):");
        Debug.Log($"  Found {rootPrefabGuids.Length} root prefab(s):");

        foreach (string guid in rootPrefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null)
            {
                diagnosticLog.Add($"    • {System.IO.Path.GetFileName(path)}");
                Debug.Log($"    • {System.IO.Path.GetFileName(path)}");

                OutfitManager manager = prefab.GetComponent<OutfitManager>();
                if (manager == null)
                {
                    diagnosticLog.Add($"      → WARNING: Missing OutfitManager component");
                    Debug.LogWarning($"      → WARNING: Missing OutfitManager component");
                }
                else
                {
                    SerializedObject so = new SerializedObject(manager);
                    var baseMesh = so.FindProperty("baseMesh").objectReferenceValue;
                    var outfitRoot = so.FindProperty("outfitRoot").objectReferenceValue;

                    if (baseMesh == null)
                    {
                        diagnosticLog.Add($"      → WARNING: BaseMesh not assigned");
                        Debug.LogWarning($"      → WARNING: BaseMesh not assigned");
                    }
                    if (outfitRoot == null)
                    {
                        diagnosticLog.Add($"      → WARNING: OutfitRoot not assigned");
                        Debug.LogWarning($"      → WARNING: OutfitRoot not assigned");
                    }
                    if (baseMesh != null && outfitRoot != null)
                    {
                        diagnosticLog.Add($"      → SUCCESS: OutfitManager properly configured");
                        Debug.Log($"      → SUCCESS: OutfitManager properly configured");
                    }
                }
            }
        }
    }

    private void CheckOutfitData()
    {
        diagnosticLog.Add("3. OUTFIT DATA CHECK");
        diagnosticLog.Add("───────────────────────────────────────");
        Debug.Log("3. OUTFIT DATA CHECK");
        Debug.Log("───────────────────────────────────────");

        if (existingOutfits.Count == 0)
        {
            diagnosticLog.Add("  INFO: No outfit data found");
            diagnosticLog.Add("  → Use Outfit tab to create outfits");
            Debug.Log("  INFO: No outfit data found");
            Debug.Log("  → Use Outfit tab to create outfits");
            return;
        }

        diagnosticLog.Add($"  Found {existingOutfits.Count} outfit(s):");
        Debug.Log($"  Found {existingOutfits.Count} outfit(s):");

        foreach (OutfitData data in existingOutfits)
        {
            string status = "";
            if (data.outfitPrefab == null)
            {
                status = "ERROR: Missing prefab";
                diagnosticLog.Add($"    • {data.outfitName} - {status}");
                diagnosticLog.Add($"      → ERROR: Outfit prefab reference missing");
                Debug.LogError($"    • {data.outfitName} - {status}");
                Debug.LogError($"      → ERROR: Outfit prefab reference missing");
            }
            else if (data.previewSprite == null)
            {
                status = "WARNING: No preview sprite";
                diagnosticLog.Add($"    • {data.outfitName} - {status}");
                diagnosticLog.Add($"      → Use Studio tab to capture preview sprite");
                Debug.LogWarning($"    • {data.outfitName} - {status}");
                Debug.Log($"      → Use Studio tab to capture preview sprite");
            }
            else
            {
                status = "OK";
                diagnosticLog.Add($"    • {data.outfitName} - {status}");
                Debug.Log($"    • {data.outfitName} - {status}");
            }
        }
    }

    private void CheckCurrentSceneObjects()
    {
        diagnosticLog.Add("3. CURRENT SCENE OBJECTS CHECK");
        diagnosticLog.Add("───────────────────────────────────────");
        Debug.Log("3. CURRENT SCENE OBJECTS CHECK");
        Debug.Log("───────────────────────────────────────");

        OutfitManager playerManager = Object.FindFirstObjectByType<OutfitManager>();
        if (playerManager != null)
        {
            diagnosticLog.Add($"  ✓ Player found: {playerManager.gameObject.name}");
            diagnosticLog.Add($"    → Position: {playerManager.transform.position}");
            diagnosticLog.Add($"    → Rotation: {playerManager.transform.rotation.eulerAngles}");
            diagnosticLog.Add($"    → Scale: {playerManager.transform.localScale}");

            if (playerManager.transform.position != Vector3.zero)
            {
                diagnosticLog.Add("WARNING: Player not at origin. This may affect previews.");
            }

            if (playerManager.OutfitRoot != null)
            {
                diagnosticLog.Add($"    → OutfitRoot: {playerManager.OutfitRoot.position}");
                diagnosticLog.Add($"    → OutfitRoot children: {playerManager.OutfitRoot.childCount}");
            }
        }
        else
        {
            diagnosticLog.Add("  INFO: No player in current scene");
        }

        OutfitUIManager uiManager = Object.FindFirstObjectByType<OutfitUIManager>();
        if (uiManager != null)
        {
            diagnosticLog.Add($"  ✓ UI found: {uiManager.gameObject.name}");
            diagnosticLog.Add($"    → Position: {uiManager.transform.position}");
            diagnosticLog.Add(uiManager.playerOutfitManager != null
                ? $"    → Linked player: {uiManager.playerOutfitManager.gameObject.name}"
                : "WARNING: UI not linked to player");
        }
    }

    private void CheckUISystem()
    {
        diagnosticLog.Add("4. UI SYSTEM CHECK");
        diagnosticLog.Add("───────────────────────────────────────");
        Debug.Log("4. UI SYSTEM CHECK");
        Debug.Log("───────────────────────────────────────");

        string[] uiPrefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/RAZ Outfit Studio/UI" });
        List<GameObject> uiPrefabs = new List<GameObject>();

        foreach (string guid in uiPrefabGuids)
        {
            string candidatePath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject candidate = AssetDatabase.LoadAssetAtPath<GameObject>(candidatePath);
            if (candidate != null && candidate.GetComponent<OutfitUIManager>() != null)
            {
                uiPrefabs.Add(candidate);
            }
        }

        if (uiPrefabs.Count == 0)
        {
            diagnosticLog.Add("  INFO: No UI prefab found");
            diagnosticLog.Add("  → Use UI tab to generate the outfit UI system");
            Debug.Log("  INFO: No UI prefab found");
            Debug.Log("  → Use UI tab to generate the outfit UI system");
            return;
        }

        foreach (GameObject uiPrefab in uiPrefabs)
        {
            string path = AssetDatabase.GetAssetPath(uiPrefab);

            if (uiPrefab != null)
            {
                diagnosticLog.Add($"  ✓ UI prefab found: {System.IO.Path.GetFileName(path)}");
                Debug.Log($"  ✓ UI prefab found: {System.IO.Path.GetFileName(path)}");

                OutfitUIManager uiManager = uiPrefab.GetComponent<OutfitUIManager>();
                if (uiManager == null)
                {
                    diagnosticLog.Add($"    → WARNING: OutfitUIManager component missing");
                    Debug.LogWarning($"    → WARNING: OutfitUIManager component missing");
                }
                else
                {
                    diagnosticLog.Add($"    → SUCCESS: OutfitUIManager present");
                    Debug.Log($"    → SUCCESS: OutfitUIManager present");
                    diagnosticLog.Add($"    → Player linked: {(uiManager.playerOutfitManager != null ? "Yes" : "No")}");
                    diagnosticLog.Add($"    → Button container: {(uiManager.outfitButtonContainer != null ? "Yes" : "No")}");
                    diagnosticLog.Add($"    → Button prefab: {(uiManager.outfitButtonPrefab != null ? "Yes" : "No")}");
                    diagnosticLog.Add($"    → Current outfit text: {(uiManager.currentOutfitNameText != null ? "Yes" : "No")}");
                    diagnosticLog.Add($"    → Current preview image: {(uiManager.currentOutfitPreviewImage != null ? "Yes" : "No")}");
                    diagnosticLog.Add($"    → Player preview image: {(uiManager.playerPreviewImage != null ? "Yes" : "No")}");
                    diagnosticLog.Add($"    → Outfit count in prefab: {uiManager.outfitLibrary.Count}");
                    diagnosticLog.Add($"    → Default outfit sprite: {(uiManager.defaultOutfitSprite != null ? uiManager.defaultOutfitSprite.name : "Missing")}");
                    diagnosticLog.Add($"    → Default player sprite: {(uiManager.defaultPlayerSprite != null ? uiManager.defaultPlayerSprite.name : "Missing")}");

                    GameObject buttonPrefab = uiManager.outfitButtonPrefab;
                    if (buttonPrefab != null)
                    {
                        Transform previewNode = buttonPrefab.transform.Find("PreviewImage") ?? buttonPrefab.transform.Find("Preview");
                        Text buttonText = buttonPrefab.GetComponentInChildren<Text>(true);
                        diagnosticLog.Add($"    → Button preview node: {(previewNode != null ? previewNode.name : "Missing")}");
                        diagnosticLog.Add($"    → Button text node: {(buttonText != null ? buttonText.gameObject.name : "Missing")}");
                    }

                    foreach (OutfitData outfit in uiManager.outfitLibrary)
                    {
                        if (outfit == null)
                        {
                            diagnosticLog.Add("      → WARNING: Null outfit entry in UI library");
                            continue;
                        }

                        string previewReason;
                        if (outfit.previewSprite != null)
                        {
                            previewReason = $"preview sprite '{outfit.previewSprite.name}'";
                        }
                        else if (outfit.icon != null)
                        {
                            previewReason = $"icon '{outfit.icon.name}'";
                        }
                        else
                        {
                            previewReason = "missing both preview sprite and icon";
                        }

                        diagnosticLog.Add($"      → {outfit.outfitName}: prefab={(outfit.outfitPrefab != null ? "Yes" : "No")}, preview={previewReason}");
                    }
                }
            }
        }

        OutfitUIManager liveUI = Object.FindFirstObjectByType<OutfitUIManager>();
        if (liveUI == null)
        {
            diagnosticLog.Add("  → Scene UI instance: Not found");
            return;
        }

        diagnosticLog.Add("  Scene UI Instance:");
        diagnosticLog.Add($"    → Name: {liveUI.gameObject.name}");
        diagnosticLog.Add($"    → Player linked: {(liveUI.playerOutfitManager != null ? liveUI.playerOutfitManager.gameObject.name : "No")}");
        diagnosticLog.Add($"    → Button container linked: {(liveUI.outfitButtonContainer != null ? liveUI.outfitButtonContainer.name : "No")}");
        diagnosticLog.Add($"    → Current outfit text linked: {(liveUI.currentOutfitNameText != null ? liveUI.currentOutfitNameText.name : "No")}");
        diagnosticLog.Add($"    → Current preview linked: {(liveUI.currentOutfitPreviewImage != null ? liveUI.currentOutfitPreviewImage.name : "No")}");
        diagnosticLog.Add($"    → Player preview linked: {(liveUI.playerPreviewImage != null ? liveUI.playerPreviewImage.name : "No")}");
        diagnosticLog.Add($"    → Library outfit count: {liveUI.outfitLibrary.Count}");
        diagnosticLog.Add($"    → Scene button count: {(liveUI.outfitButtonContainer != null ? liveUI.outfitButtonContainer.childCount : 0)}");
        diagnosticLog.Add($"    → Expected cards match actual: {(liveUI.outfitButtonContainer != null && liveUI.outfitButtonContainer.childCount == liveUI.outfitLibrary.Count ? "Yes" : "No")}");
        diagnosticLog.Add($"    → EventSystem present: {(Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null ? "Yes" : "No")}");

        if (liveUI.outfitButtonContainer != null && liveUI.outfitButtonContainer.childCount > 0)
        {
            Transform firstButton = liveUI.outfitButtonContainer.GetChild(0);
            Image previewImage = firstButton.Find("PreviewImage")?.GetComponent<Image>()
                ?? firstButton.Find("Preview")?.GetComponent<Image>();
            Text buttonText = firstButton.GetComponentInChildren<Text>(true);
            diagnosticLog.Add($"    → First scene button: {firstButton.name}");
            diagnosticLog.Add($"      → Preview image component: {(previewImage != null ? "Yes" : "No")}");
            diagnosticLog.Add($"      → Preview sprite: {(previewImage != null && previewImage.sprite != null ? previewImage.sprite.name : "Missing")}");
            diagnosticLog.Add($"      → Text: {(buttonText != null ? buttonText.text : "Missing")}");
        }
        else if (liveUI.outfitLibrary.Count > 0)
        {
            diagnosticLog.Add("    → WARNING: Outfits exist in library but no scene buttons were generated");
        }
    }

    private void CheckStudioScene()
    {
        diagnosticLog.Add("5. STUDIO SCENE CHECK");
        diagnosticLog.Add("───────────────────────────────────────");
        Debug.Log("5. STUDIO SCENE CHECK");
        Debug.Log("───────────────────────────────────────");

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(studioScenePath) == null)
        {
            diagnosticLog.Add("  INFO: Studio scene not created yet");
            return;
        }

        diagnosticLog.Add($"  ✓ Studio scene exists: {studioScenePath}");
        diagnosticLog.Add(isStudioSceneOpen ? "  → Studio scene is open" : "  → Studio scene is not open");

        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        diagnosticLog.Add($"  → Lights in current scene: {lights.Length}");
        foreach (Light light in lights)
        {
            diagnosticLog.Add($"    • {light.name}: {light.type} at {light.transform.position}");
        }

        if (previewCamera != null)
        {
            diagnosticLog.Add($"  → Preview camera position: {previewCamera.transform.position}");
            diagnosticLog.Add($"  → Preview camera rotation: {previewCamera.transform.rotation.eulerAngles}");
        }

        if (currentPreviewObject != null)
        {
            diagnosticLog.Add($"  → Preview object: {currentPreviewObject.name}");
            diagnosticLog.Add($"    → Position: {currentPreviewObject.transform.position}");
            diagnosticLog.Add($"    → Scale: {currentPreviewObject.transform.localScale}");
        }
    }

    private void CheckComponentIntegrity()
    {
        diagnosticLog.Add("5. COMPONENT INTEGRITY CHECK");
        diagnosticLog.Add("───────────────────────────────────────");
        Debug.Log("5. COMPONENT INTEGRITY CHECK");
        Debug.Log("───────────────────────────────────────");

        var managerScript = Resources.FindObjectsOfTypeAll<MonoScript>()
            .FirstOrDefault(ms => ms.GetClass() == typeof(OutfitManager));

        if (managerScript != null)
        {
            diagnosticLog.Add("  ✓ OutfitManager script found");
            Debug.Log("  ✓ OutfitManager script found");
        }
        else
        {
            diagnosticLog.Add("  ✗ OutfitManager script NOT FOUND");
            Debug.LogError("  ✗ OutfitManager script NOT FOUND");
        }

        var dataScript = Resources.FindObjectsOfTypeAll<MonoScript>()
            .FirstOrDefault(ms => ms.GetClass() == typeof(OutfitData));

        if (dataScript != null)
        {
            diagnosticLog.Add("  ✓ OutfitData scriptable object found");
            Debug.Log("  ✓ OutfitData scriptable object found");
        }
        else
        {
            diagnosticLog.Add("  ✗ OutfitData script NOT FOUND");
            Debug.LogError("  ✗ OutfitData script NOT FOUND");
        }

        var uiManagerScript = Resources.FindObjectsOfTypeAll<MonoScript>()
            .FirstOrDefault(ms => ms.GetClass() == typeof(OutfitUIManager));

        if (uiManagerScript != null)
        {
            diagnosticLog.Add("  ✓ OutfitUIManager script found");
            Debug.Log("  ✓ OutfitUIManager script found");
        }
        else
        {
            diagnosticLog.Add("  ✗ OutfitUIManager script NOT FOUND");
            Debug.LogError("  ✗ OutfitUIManager script NOT FOUND");
        }
    }

    private void GeneratePerformanceSummary()
    {
        diagnosticLog.Add("6. PERFORMANCE SUMMARY");
        diagnosticLog.Add("───────────────────────────────────────");
        Debug.Log("6. PERFORMANCE SUMMARY");
        Debug.Log("───────────────────────────────────────");

        int totalIssues = 0;
        int totalWarnings = 0;

        foreach (string log in diagnosticLog)
        {
            if (log.StartsWith("ERROR:")) totalIssues++;
            if (log.StartsWith("WARNING:")) totalWarnings++;
        }

        diagnosticLog.Add($"  Total Issues Found: {totalIssues}");
        diagnosticLog.Add($"  Total Warnings: {totalWarnings}");
        Debug.Log($"  Total Issues Found: {totalIssues}");
        Debug.Log($"  Total Warnings: {totalWarnings}");

        if (totalIssues == 0 && totalWarnings == 0)
        {
            diagnosticLog.Add("  SUCCESS: System is fully operational!");
            Debug.Log("  ✅ SUCCESS: System is fully operational!");
        }
        else if (totalIssues == 0)
        {
            diagnosticLog.Add("  → System is operational with minor warnings");
            Debug.Log("  ⚠️ System is operational with minor warnings");
        }
        else
        {
            diagnosticLog.Add("  → Action required to fix critical issues");
            Debug.LogWarning("  → Action required to fix critical issues");
        }
    }

    private void ExportDiagnosticLog()
    {
        string path = EditorUtility.SaveFilePanel("Export Diagnostic Log", "", $"ROS_Scan_{System.DateTime.Now:yyyyMMdd_HHmmss}", "txt");

        if (!string.IsNullOrEmpty(path))
        {
            try
            {
                System.IO.File.WriteAllLines(path, diagnosticLog);
                EditorUtility.DisplayDialog("Export Complete", $"Diagnostic log saved to:\n{path}", "OK");
                Debug.Log($"✅ ROS: Diagnostic log exported to {path}");
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Export Failed", $"Could not save file:\n{e.Message}", "OK");
                Debug.LogError($"❌ ROS Error: {e.Message}");
            }
        }
    }

    #endregion

    #region Helper Methods

    private void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/RAZ Outfit Studio"))
            AssetDatabase.CreateFolder("Assets", "RAZ Outfit Studio");
        if (!AssetDatabase.IsValidFolder("Assets/RAZ Outfit Studio/Characters"))
            AssetDatabase.CreateFolder("Assets/RAZ Outfit Studio", "Characters");
        if (!AssetDatabase.IsValidFolder("Assets/RAZ Outfit Studio/Outfits"))
            AssetDatabase.CreateFolder("Assets/RAZ Outfit Studio", "Outfits");
        if (!AssetDatabase.IsValidFolder("Assets/RAZ Outfit Studio/OutfitData"))
            AssetDatabase.CreateFolder("Assets/RAZ Outfit Studio", "OutfitData");
        if (!AssetDatabase.IsValidFolder("Assets/RAZ Outfit Studio/UI"))
            AssetDatabase.CreateFolder("Assets/RAZ Outfit Studio", "UI");
        if (!AssetDatabase.IsValidFolder("Assets/RAZ Outfit Studio/Sprites"))
            AssetDatabase.CreateFolder("Assets/RAZ Outfit Studio", "Sprites");
        if (!AssetDatabase.IsValidFolder("Assets/RAZ Outfit Studio/Exports"))
            AssetDatabase.CreateFolder("Assets/RAZ Outfit Studio", "Exports");

        AssetDatabase.Refresh();
    }

    #endregion
}
