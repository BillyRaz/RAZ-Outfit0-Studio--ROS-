using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class RAZOutfitStudioWindow : EditorWindow
{
    private int selectedTab = 0;
    private readonly string[] tabNames = { "Root Setup", "Outfit", "UI", "Studio", "Diagnosed" };

    // Root Setup Tab Variables
    private GameObject meshForRoot;
    private string characterName = "PlayerCharacter";

    // Diagnostic Tab Variables
    private Vector2 scrollPosition;
    private List<string> diagnosticLog = new List<string>();
    private bool isDeepScanning = false;

    [MenuItem("Tools/RAZ Outfit Studio")]
    public static void ShowWindow()
    {
        RAZOutfitStudioWindow window = GetWindow<RAZOutfitStudioWindow>("RAZ Outfit Studio");
        window.minSize = new Vector2(400, 500);
        window.Show();
    }

    private void OnGUI()
    {
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
                DrawOutfitTab(); // Placeholder for Phase 2
                break;
            case 2:
                DrawUITab(); // Placeholder for Phase 2
                break;
            case 3:
                DrawStudioTab(); // Placeholder for Phase 2
                break;
            case 4:
                DrawDiagnosedTab();
                break;
        }
    }

    #region Root Setup Tab

    private void DrawRootSetupTab()
    {
        EditorGUILayout.LabelField("Player Character Setup", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Select a mesh with SkinnedMeshRenderer to generate the player root prefab", MessageType.Info);
        EditorGUILayout.Space(10);

        // Mesh Selection
        meshForRoot = (GameObject)EditorGUILayout.ObjectField("Character Mesh/Prefab", meshForRoot, typeof(GameObject), false);
        characterName = EditorGUILayout.TextField("Character Name", characterName);

        EditorGUILayout.Space(20);

        // Generation Button
        GUI.enabled = meshForRoot != null && !string.IsNullOrEmpty(characterName);
        if (GUILayout.Button("Generate Root Prefab", GUILayout.Height(30)))
        {
            GenerateRootPrefab();
        }
        GUI.enabled = true;

        EditorGUILayout.Space(10);

        // Instructions
        EditorGUILayout.HelpBox(
            "This will create:\n" +
            "• A player prefab with OutfitManager component\n" +
            "• OutfitRoot transform for attaching outfits\n" +
            "• Automatic linking of SkinnedMeshRenderer\n" +
            "• Proper folder structure in Assets/RAZ Outfit Studio/",
            MessageType.None);

        EditorGUILayout.Space(10);

        // Quick Status
        EditorGUILayout.LabelField("Status:", EditorStyles.boldLabel);
        if (meshForRoot != null)
        {
            SkinnedMeshRenderer skinned = meshForRoot.GetComponent<SkinnedMeshRenderer>();
            if (skinned != null)
            {
                EditorGUILayout.HelpBox("✓ Valid mesh selected with SkinnedMeshRenderer", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("✗ Selected object has no SkinnedMeshRenderer component", MessageType.Error);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("⚠ No mesh selected", MessageType.Warning);
        }
    }

    private void GenerateRootPrefab()
    {
        try
        {
            // Ensure folders exist
            EnsureFolders();

            // Create temporary root GameObject
            GameObject tempRoot = new GameObject(characterName + "_Root");
            Undo.RegisterCreatedObjectUndo(tempRoot, "Create Player Root");

            // Add required components
            Animator animator = tempRoot.AddComponent<Animator>();
            OutfitManager outfitManager = tempRoot.AddComponent<OutfitManager>();

            // Instantiate the selected mesh as child
            GameObject meshInstance = Instantiate(meshForRoot, tempRoot.transform);
            meshInstance.name = characterName + "_Mesh";

            // Get SkinnedMeshRenderer
            SkinnedMeshRenderer skinnedRenderer = meshInstance.GetComponent<SkinnedMeshRenderer>();
            if (skinnedRenderer == null)
            {
                EditorUtility.DisplayDialog("Error", "Selected object has no SkinnedMeshRenderer component!", "OK");
                DestroyImmediate(tempRoot);
                return;
            }

            // Create outfit root transform
            GameObject outfitRoot = new GameObject("OutfitRoot");
            outfitRoot.transform.SetParent(tempRoot.transform);
            outfitRoot.transform.localPosition = Vector3.zero;
            outfitRoot.transform.localRotation = Quaternion.identity;

            // Setup OutfitManager references via SerializedObject
            SerializedObject so = new SerializedObject(outfitManager);
            so.FindProperty("baseMesh").objectReferenceValue = skinnedRenderer;
            so.FindProperty("outfitRoot").objectReferenceValue = outfitRoot.transform;
            so.ApplyModifiedProperties();

            // Save as prefab
            string prefabPath = $"Assets/RAZ Outfit Studio/Characters/{characterName}_Root.prefab";
            PrefabUtility.SaveAsPrefabAsset(tempRoot, prefabPath);

            // Clean up
            DestroyImmediate(tempRoot);

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Success!",
                $"Root prefab successfully created at:\n{prefabPath}\n\nYou can now use this prefab in your scenes.",
                "OK");

            // Optionally select the prefab in Project view
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed to generate root prefab:\n{e.Message}", "OK");
            Debug.LogError($"ROS Error: {e.Message}");
        }
    }

    #endregion

    #region Diagnostic Tab

    private void DrawDiagnosedTab()
    {
        EditorGUILayout.LabelField("System Diagnostics", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Deep Scan will check all ROS components and report any issues", MessageType.Info);
        EditorGUILayout.Space(10);

        // Scan Button
        GUI.enabled = !isDeepScanning;
        if (GUILayout.Button(isDeepScanning ? "Scanning..." : "Run Deep Scan", GUILayout.Height(30)))
        {
            RunDeepScan();
        }
        GUI.enabled = true;

        EditorGUILayout.Space(10);

        // Quick Status
        EditorGUILayout.LabelField("Quick Status:", EditorStyles.boldLabel);
        DisplayQuickStatus();

        EditorGUILayout.Space(10);

        // Detailed Log
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
                else
                    EditorGUILayout.LabelField(logEntry);
            }
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(10);

        // Export Log Button
        if (diagnosticLog.Count > 0 && GUILayout.Button("Export Scan Log"))
        {
            ExportDiagnosticLog();
        }
    }

    private void DisplayQuickStatus()
    {
        // Check folders
        bool hasRootFolder = AssetDatabase.IsValidFolder("Assets/RAZ Outfit Studio");
        bool hasCharactersFolder = AssetDatabase.IsValidFolder("Assets/RAZ Outfit Studio/Characters");

        // Check for root prefab
        string rootPrefabPath = "Assets/RAZ Outfit Studio/Characters/";
        string[] rootPrefabs = AssetDatabase.FindAssets("t:Prefab", new[] { rootPrefabPath });

        EditorGUILayout.BeginVertical("box");

        if (!hasRootFolder)
        {
            EditorGUILayout.HelpBox("ROS folders not created yet", MessageType.Warning);
        }
        else if (rootPrefabs.Length == 0)
        {
            EditorGUILayout.HelpBox("No root prefab found. Use Root Setup tab to create one.", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.HelpBox($"✓ Found {rootPrefabs.Length} root prefab(s)", MessageType.Info);
        }

        EditorGUILayout.EndVertical();
    }

    private void RunDeepScan()
    {
        isDeepScanning = true;
        diagnosticLog.Clear();

        try
        {
            diagnosticLog.Add("═══════════════════════════════════════");
            diagnosticLog.Add("      RAZ Outfit Studio Deep Scan");
            diagnosticLog.Add("═══════════════════════════════════════");
            diagnosticLog.Add($"Scan Time: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            diagnosticLog.Add("");

            // 1. Check Folder Structure
            diagnosticLog.Add("1. FOLDER STRUCTURE CHECK");
            diagnosticLog.Add("───────────────────────────────────────");
            CheckFolderStructure();
            diagnosticLog.Add("");

            // 2. Check Root Prefabs
            diagnosticLog.Add("2. ROOT PREFABS CHECK");
            diagnosticLog.Add("───────────────────────────────────────");
            CheckRootPrefabs();
            diagnosticLog.Add("");

            // 3. Check Outfit Data
            diagnosticLog.Add("3. OUTFIT DATA CHECK");
            diagnosticLog.Add("───────────────────────────────────────");
            CheckOutfitData();
            diagnosticLog.Add("");

            // 4. Check UI System
            diagnosticLog.Add("4. UI SYSTEM CHECK");
            diagnosticLog.Add("───────────────────────────────────────");
            CheckUISystem();
            diagnosticLog.Add("");

            // 5. Check Components
            diagnosticLog.Add("5. COMPONENT INTEGRITY CHECK");
            diagnosticLog.Add("───────────────────────────────────────");
            CheckComponentIntegrity();
            diagnosticLog.Add("");

            // 6. Performance Summary
            diagnosticLog.Add("6. PERFORMANCE SUMMARY");
            diagnosticLog.Add("───────────────────────────────────────");
            GeneratePerformanceSummary();

            diagnosticLog.Add("");
            diagnosticLog.Add("═══════════════════════════════════════");
            diagnosticLog.Add("            SCAN COMPLETE");
            diagnosticLog.Add("═══════════════════════════════════════");
        }
        catch (System.Exception e)
        {
            diagnosticLog.Add($"ERROR: Scan failed - {e.Message}");
            Debug.LogError($"ROS Deep Scan Error: {e.Message}");
        }
        finally
        {
            isDeepScanning = false;
            Repaint();
        }
    }

    private void CheckFolderStructure()
    {
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
                diagnosticLog.Add($"  ✓ {folder} exists");
            }
            else
            {
                diagnosticLog.Add($"  ✗ {folder} MISSING");
            }
        }
    }

    private void CheckRootPrefabs()
    {
        string[] rootPrefabGuids = AssetDatabase.FindAssets("t:Prefab _Root", new[] { "Assets/RAZ Outfit Studio/Characters" });

        if (rootPrefabGuids.Length == 0)
        {
            diagnosticLog.Add("  WARNING: No root prefabs found");
            diagnosticLog.Add("  → Use Root Setup tab to generate a player root prefab");
            return;
        }

        diagnosticLog.Add($"  Found {rootPrefabGuids.Length} root prefab(s):");

        foreach (string guid in rootPrefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null)
            {
                diagnosticLog.Add($"    • {System.IO.Path.GetFileName(path)}");

                // Check if prefab has required components
                OutfitManager manager = prefab.GetComponent<OutfitManager>();
                if (manager == null)
                {
                    diagnosticLog.Add($"      → WARNING: Missing OutfitManager component");
                }
                else
                {
                    // Check serialized fields
                    SerializedObject so = new SerializedObject(manager);
                    var baseMesh = so.FindProperty("baseMesh").objectReferenceValue;
                    var outfitRoot = so.FindProperty("outfitRoot").objectReferenceValue;

                    if (baseMesh == null)
                        diagnosticLog.Add($"      → WARNING: BaseMesh not assigned in OutfitManager");
                    if (outfitRoot == null)
                        diagnosticLog.Add($"      → WARNING: OutfitRoot not assigned in OutfitManager");
                    if (baseMesh != null && outfitRoot != null)
                        diagnosticLog.Add($"      → SUCCESS: OutfitManager properly configured");
                }

                Animator animator = prefab.GetComponent<Animator>();
                if (animator == null)
                {
                    diagnosticLog.Add($"      → WARNING: Missing Animator component");
                }
            }
        }
    }

    private void CheckOutfitData()
    {
        string[] outfitDataGuids = AssetDatabase.FindAssets("t:OutfitData", new[] { "Assets/RAZ Outfit Studio/OutfitData" });

        if (outfitDataGuids.Length == 0)
        {
            diagnosticLog.Add("  INFO: No outfit data found (this is normal for first setup)");
            diagnosticLog.Add("  → Use Outfit tab to create outfits");
            return;
        }

        diagnosticLog.Add($"  Found {outfitDataGuids.Length} outfit data asset(s):");

        foreach (string guid in outfitDataGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            OutfitData data = AssetDatabase.LoadAssetAtPath<OutfitData>(path);

            if (data != null)
            {
                string status = "OK";
                if (data.outfitPrefab == null) status = "MISSING PREFAB";
                if (data.previewSprite == null) status = "NO PREVIEW";
                if (data.outfitName == null || data.outfitName == "") status = "NO NAME";

                diagnosticLog.Add($"    • {data.outfitName ?? "Unnamed"} - {status}");

                if (data.outfitPrefab == null)
                {
                    diagnosticLog.Add($"      → ERROR: Outfit prefab reference missing");
                }
            }
            else
            {
                diagnosticLog.Add($"    • {System.IO.Path.GetFileName(path)} - FAILED TO LOAD");
            }
        }
    }

    private void CheckUISystem()
    {
        string[] uiPrefabGuids = AssetDatabase.FindAssets("t:Prefab OutfitUI", new[] { "Assets/RAZ Outfit Studio/UI" });

        if (uiPrefabGuids.Length == 0)
        {
            diagnosticLog.Add("  INFO: No UI prefab found");
            diagnosticLog.Add("  → Use UI tab to generate the outfit UI system");
            return;
        }

        foreach (string guid in uiPrefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (uiPrefab != null)
            {
                diagnosticLog.Add($"  ✓ UI prefab found: {System.IO.Path.GetFileName(path)}");

                OutfitUIManager uiManager = uiPrefab.GetComponent<OutfitUIManager>();
                if (uiManager == null)
                {
                    diagnosticLog.Add($"    → WARNING: OutfitUIManager component missing");
                }
                else
                {
                    diagnosticLog.Add($"    → SUCCESS: OutfitUIManager present");
                }
            }
        }
    }

    private void CheckComponentIntegrity()
    {
        // Check if OutfitManager script exists
        var managerScript = Resources.FindObjectsOfTypeAll<MonoScript>()
            .FirstOrDefault(ms => ms.GetClass() == typeof(OutfitManager));

        if (managerScript != null)
        {
            diagnosticLog.Add("  ✓ OutfitManager script found");
        }
        else
        {
            diagnosticLog.Add("  ✗ OutfitManager script NOT FOUND");
        }

        // Check if OutfitData scriptable object exists
        var dataScript = Resources.FindObjectsOfTypeAll<MonoScript>()
            .FirstOrDefault(ms => ms.GetClass() == typeof(OutfitData));

        if (dataScript != null)
        {
            diagnosticLog.Add("  ✓ OutfitData scriptable object found");
        }
        else
        {
            diagnosticLog.Add("  ✗ OutfitData script NOT FOUND");
        }

        // Check if OutfitUIManager script exists
        var uiManagerScript = Resources.FindObjectsOfTypeAll<MonoScript>()
            .FirstOrDefault(ms => ms.GetClass() == typeof(OutfitUIManager));

        if (uiManagerScript != null)
        {
            diagnosticLog.Add("  ✓ OutfitUIManager script found");
        }
        else
        {
            diagnosticLog.Add("  ✗ OutfitUIManager script NOT FOUND");
        }
    }

    private void GeneratePerformanceSummary()
    {
        int totalIssues = 0;
        int totalWarnings = 0;

        foreach (string log in diagnosticLog)
        {
            if (log.StartsWith("ERROR:")) totalIssues++;
            if (log.StartsWith("WARNING:")) totalWarnings++;
        }

        diagnosticLog.Add($"  Total Issues Found: {totalIssues}");
        diagnosticLog.Add($"  Total Warnings: {totalWarnings}");

        if (totalIssues == 0 && totalWarnings == 0)
        {
            diagnosticLog.Add("  SUCCESS: System is fully operational!");
        }
        else if (totalIssues == 0)
        {
            diagnosticLog.Add("  → System is operational with minor warnings");
        }
        else
        {
            diagnosticLog.Add("  → Action required to fix critical issues");
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
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Export Failed", $"Could not save file:\n{e.Message}", "OK");
            }
        }
    }

    #endregion

    #region Placeholder Tabs (Phase 2)

    private void DrawOutfitTab()
    {
        EditorGUILayout.HelpBox("Outfit Tab - Coming in Phase 2", MessageType.Info);
        EditorGUILayout.LabelField("This tab will handle outfit creation and management", EditorStyles.centeredGreyMiniLabel);
    }

    private void DrawUITab()
    {
        EditorGUILayout.HelpBox("UI Tab - Coming in Phase 2", MessageType.Info);
        EditorGUILayout.LabelField("This tab will generate the runtime UI system", EditorStyles.centeredGreyMiniLabel);
    }

    private void DrawStudioTab()
    {
        EditorGUILayout.HelpBox("Studio Tab - Coming in Phase 2", MessageType.Info);
        EditorGUILayout.LabelField("This tab will handle preview rendering and sprite capture", EditorStyles.centeredGreyMiniLabel);
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

        AssetDatabase.Refresh();
    }

    #endregion
}