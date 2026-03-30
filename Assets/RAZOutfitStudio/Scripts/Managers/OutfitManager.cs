using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Animator))]
public class OutfitManager : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer baseMesh;
    [SerializeField] private Transform outfitRoot;
    [SerializeField] private List<OutfitData> availableOutfits = new List<OutfitData>();

    private OutfitData currentOutfit;
    private GameObject currentOutfitInstance;

    public SkinnedMeshRenderer BaseMesh => baseMesh;
    public Transform OutfitRoot => outfitRoot;
    public OutfitData CurrentOutfit => currentOutfit;

    private void Awake()
    {
        if (outfitRoot == null)
        {
            Debug.LogWarning("OutfitRoot not assigned, creating automatically");
            CreateOutfitRoot();
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

    public void EquipOutfit(OutfitData outfit)
    {
        if (outfit == null)
        {
            Debug.LogWarning("Attempted to equip null outfit");
            return;
        }

        if (currentOutfitInstance != null)
        {
            if (Application.isPlaying)
                Destroy(currentOutfitInstance);
            else
                DestroyImmediate(currentOutfitInstance);
        }

        currentOutfit = outfit;

        if (outfit.outfitPrefab != null && outfitRoot != null)
        {
            if (Application.isPlaying)
                currentOutfitInstance = Instantiate(outfit.outfitPrefab, outfitRoot);
            else
                currentOutfitInstance = Instantiate(outfit.outfitPrefab, outfitRoot);

            currentOutfitInstance.name = outfit.outfitName + "_Instance";

            // Optional: Trigger outfit changed event
            OnOutfitChanged(outfit);
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
        // This can be extended for custom events, animations, etc.
        Debug.Log($"Outfit changed to: {newOutfit.outfitName}");
    }
}