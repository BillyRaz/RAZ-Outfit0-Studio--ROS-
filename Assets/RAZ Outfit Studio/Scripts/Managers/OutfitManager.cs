using UnityEngine;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Animator))]
public class OutfitManager : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer baseMesh;
    [SerializeField] private Transform outfitRoot;
    [SerializeField] private List<OutfitData> availableOutfits = new List<OutfitData>();
    [SerializeField] private OutfitData defaultOutfit;
    [SerializeField] private GameObject sourceRootPrefab;

    private OutfitData currentOutfit;
    private GameObject currentOutfitInstance;

    public SkinnedMeshRenderer BaseMesh => baseMesh;
    public Transform OutfitRoot => outfitRoot;
    public OutfitData CurrentOutfit => currentOutfit;
    public OutfitData DefaultOutfit => defaultOutfit;
    public GameObject SourceRootPrefab => sourceRootPrefab;

    private void Awake()
    {
        if (outfitRoot == null)
        {
            Debug.LogWarning("OutfitRoot not assigned, creating automatically");
            CreateOutfitRoot();
        }
    }

    private void Start()
    {
        SyncDefaultOutfitFromSourcePrefab();

        if (defaultOutfit != null && currentOutfit == null)
        {
            EquipOutfit(defaultOutfit, false);
        }
    }

    private void CreateOutfitRoot()
    {
        GameObject root = new GameObject("OutfitRoot");
        root.transform.SetParent(transform);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        outfitRoot = root.transform;
    }

    public void EquipOutfit(OutfitData outfit, bool persistSelection = true)
    {
        if (outfit == null)
        {
            ClearCurrentOutfit();
            return;
        }

        if (currentOutfit == outfit && currentOutfitInstance != null)
        {
            return;
        }

        ClearCurrentOutfit();

        currentOutfit = outfit;
        defaultOutfit = outfit;

        if (!availableOutfits.Contains(outfit))
        {
            availableOutfits.Add(outfit);
        }

        if (outfit.outfitPrefab != null && outfitRoot != null)
        {
            if (Application.isPlaying)
                currentOutfitInstance = Instantiate(outfit.outfitPrefab, outfitRoot);
            else
                currentOutfitInstance = Instantiate(outfit.outfitPrefab, outfitRoot);

            currentOutfitInstance.name = outfit.outfitName + "_Instance";
            currentOutfitInstance.transform.localPosition = Vector3.zero;
            currentOutfitInstance.transform.localRotation = Quaternion.identity;
            currentOutfitInstance.transform.localScale = Vector3.one;

            BindOutfitToCharacterSkeleton(currentOutfitInstance);

            OnOutfitChanged(outfit);
            if (persistSelection)
            {
                PersistDefaultOutfitToPrefab(outfit);
            }
        }
        else
        {
            Debug.LogError($"Cannot equip outfit {outfit.outfitName}: Missing prefab or outfit root");
        }
    }

    public void AddOutfitToLibrary(OutfitData outfit)
    {
        if (!availableOutfits.Contains(outfit))
        {
            availableOutfits.Add(outfit);
        }
    }

    public void SetSourceRootPrefab(GameObject prefabAsset)
    {
        sourceRootPrefab = prefabAsset;
    }

    public void ConfigureRuntimeSetup(SkinnedMeshRenderer meshRenderer, Transform runtimeOutfitRoot, OutfitData startingOutfit)
    {
        baseMesh = meshRenderer;
        outfitRoot = runtimeOutfitRoot;
        defaultOutfit = startingOutfit;
        currentOutfit = startingOutfit;

        if (startingOutfit != null && !availableOutfits.Contains(startingOutfit))
        {
            availableOutfits.Add(startingOutfit);
        }
    }

    public void RemoveOutfitFromLibrary(OutfitData outfit)
    {
        if (availableOutfits.Contains(outfit))
        {
            availableOutfits.Remove(outfit);

            if (currentOutfit == outfit)
            {
                EquipOutfit(null);
            }
        }
    }

    private void OnOutfitChanged(OutfitData newOutfit)
    {
        int rendererCount = currentOutfitInstance != null
            ? currentOutfitInstance.GetComponentsInChildren<Renderer>(true).Length
            : 0;
        Debug.Log($"Outfit changed to: {newOutfit.outfitName} (renderers: {rendererCount})");
    }

    private void ClearCurrentOutfit()
    {
        if (currentOutfitInstance != null)
        {
            if (Application.isPlaying)
                Destroy(currentOutfitInstance);
            else
                DestroyImmediate(currentOutfitInstance);
        }

        currentOutfitInstance = null;
        currentOutfit = null;
    }

    private void SyncDefaultOutfitFromSourcePrefab()
    {
        OutfitManager sourceManager = null;

        if (sourceRootPrefab != null)
        {
            sourceManager = sourceRootPrefab.GetComponent<OutfitManager>();
        }

#if UNITY_EDITOR
        if (sourceManager == null)
        {
            GameObject sourcePrefab = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
            if (sourcePrefab != null)
            {
                sourceManager = sourcePrefab.GetComponent<OutfitManager>();
                if (sourceRootPrefab == null)
                {
                    sourceRootPrefab = sourcePrefab;
                }
            }
        }
#endif

        if (sourceManager == null || sourceManager == this)
        {
            return;
        }

        defaultOutfit = sourceManager.defaultOutfit;

        if (sourceManager.availableOutfits != null)
        {
            foreach (OutfitData outfit in sourceManager.availableOutfits)
            {
                if (outfit != null && !availableOutfits.Contains(outfit))
                {
                    availableOutfits.Add(outfit);
                }
            }
        }
    }

    private void PersistDefaultOutfitToPrefab(OutfitData outfit)
    {
#if UNITY_EDITOR
        string sourcePrefabPath = string.Empty;

        if (sourceRootPrefab != null)
        {
            sourcePrefabPath = AssetDatabase.GetAssetPath(sourceRootPrefab);
        }

        if (string.IsNullOrEmpty(sourcePrefabPath))
        {
            GameObject sourcePrefabAsset = PrefabUtility.GetCorrespondingObjectFromSource(gameObject);
            if (sourcePrefabAsset != null)
            {
                sourcePrefabPath = AssetDatabase.GetAssetPath(sourcePrefabAsset);
            }
        }

        if (string.IsNullOrEmpty(sourcePrefabPath))
        {
            Debug.LogWarning($"OutfitManager: '{gameObject.name}' is not linked to a root prefab asset, so default outfit was not saved.");
            return;
        }

        GameObject prefabContentsRoot = null;

        try
        {
            prefabContentsRoot = PrefabUtility.LoadPrefabContents(sourcePrefabPath);
            OutfitManager sourceManager = prefabContentsRoot != null ? prefabContentsRoot.GetComponent<OutfitManager>() : null;

            if (sourceManager == null)
            {
                Debug.LogWarning($"OutfitManager: Could not find OutfitManager on prefab asset at '{sourcePrefabPath}'.");
                return;
            }

            sourceManager.defaultOutfit = outfit;
            sourceManager.sourceRootPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePrefabPath);

            if (outfit != null && !sourceManager.availableOutfits.Contains(outfit))
            {
                sourceManager.availableOutfits.Add(outfit);
            }

            sourceManager.SyncPrefabDefaultOutfitVisual(outfit);

            EditorUtility.SetDirty(sourceManager);
            PrefabUtility.SaveAsPrefabAsset(prefabContentsRoot, sourcePrefabPath);
            AssetDatabase.SaveAssets();

            string outfitName = outfit != null ? outfit.outfitName : "None";
            Debug.Log($"OutfitManager: Saved default outfit '{outfitName}' to prefab asset '{sourcePrefabPath}'");
        }
        finally
        {
            if (prefabContentsRoot != null)
            {
                PrefabUtility.UnloadPrefabContents(prefabContentsRoot);
            }
        }
#endif
    }

#if UNITY_EDITOR
    private void SyncPrefabDefaultOutfitVisual(OutfitData outfit)
    {
        if (outfitRoot == null)
        {
            CreateOutfitRoot();
        }

        if (outfitRoot == null)
        {
            Debug.LogWarning("OutfitManager: Could not sync prefab default outfit visual because OutfitRoot is missing.");
            return;
        }

        for (int i = outfitRoot.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(outfitRoot.GetChild(i).gameObject);
        }

        if (outfit == null || outfit.outfitPrefab == null)
        {
            return;
        }

        GameObject outfitVisual = PrefabUtility.InstantiatePrefab(outfit.outfitPrefab, outfitRoot) as GameObject;
        if (outfitVisual == null)
        {
            outfitVisual = Instantiate(outfit.outfitPrefab, outfitRoot);
        }

        outfitVisual.name = outfit.outfitPrefab.name;
        outfitVisual.transform.localPosition = Vector3.zero;
        outfitVisual.transform.localRotation = Quaternion.identity;
        outfitVisual.transform.localScale = Vector3.one;

        BindOutfitToCharacterSkeleton(outfitVisual);
        Debug.Log($"OutfitManager: Synced prefab default visual to '{outfit.outfitName}'.");
    }
#endif

    private void BindOutfitToCharacterSkeleton(GameObject outfitInstance)
    {
        if (outfitInstance == null)
        {
            return;
        }

        Dictionary<string, Transform> skeletonMap = transform
            .GetComponentsInChildren<Transform>(true)
            .GroupBy(t => t.name)
            .ToDictionary(group => group.Key, group => group.First());

        foreach (Transform child in outfitInstance.GetComponentsInChildren<Transform>(true))
        {
            if (child == outfitInstance.transform)
            {
                continue;
            }

            child.localPosition = Vector3.zero;
            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;
        }

        SkinnedMeshRenderer[] outfitRenderers = outfitInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (SkinnedMeshRenderer outfitRenderer in outfitRenderers)
        {
            Transform mappedRootBone = ResolveMappedBone(outfitRenderer.rootBone, skeletonMap);
            if (mappedRootBone == null && baseMesh != null)
            {
                mappedRootBone = baseMesh.rootBone;
            }

            if (mappedRootBone != null)
            {
                outfitRenderer.rootBone = mappedRootBone;
            }

            Transform[] mappedBones = new Transform[outfitRenderer.bones.Length];
            for (int i = 0; i < outfitRenderer.bones.Length; i++)
            {
                mappedBones[i] = ResolveMappedBone(outfitRenderer.bones[i], skeletonMap);
            }

            if (mappedBones.Length > 0 && mappedBones.Any(bone => bone != null))
            {
                outfitRenderer.bones = mappedBones;
            }
            else if (baseMesh != null && baseMesh.bones != null && baseMesh.bones.Length > 0)
            {
                outfitRenderer.bones = baseMesh.bones;
            }

            outfitRenderer.updateWhenOffscreen = true;
        }

        MeshRenderer[] meshRenderers = outfitInstance.GetComponentsInChildren<MeshRenderer>(true);
        foreach (MeshRenderer meshRenderer in meshRenderers)
        {
            meshRenderer.enabled = true;
        }

        Debug.Log($"OutfitManager: Bound '{outfitInstance.name}' to character skeleton with {outfitRenderers.Length} skinned renderer(s).");
    }

    private Transform ResolveMappedBone(Transform sourceBone, Dictionary<string, Transform> skeletonMap)
    {
        if (sourceBone == null)
        {
            return null;
        }

        if (skeletonMap.TryGetValue(sourceBone.name, out Transform mappedBone))
        {
            return mappedBone;
        }

        return null;
    }
}
