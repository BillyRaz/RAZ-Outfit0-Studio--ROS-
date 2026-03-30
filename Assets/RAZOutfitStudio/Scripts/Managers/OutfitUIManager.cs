using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class OutfitUIManager : MonoBehaviour
{
    [Header("References")]
    public OutfitManager playerOutfitManager;
    public Transform outfitButtonContainer;
    public GameObject outfitButtonPrefab;

    [Header("Outfit Library")]
    public List<OutfitData> outfitLibrary = new List<OutfitData>();

    [Header("UI Settings")]
    public bool autoRefreshOnStart = true;
    public bool showOutfitNames = true;

    private List<Button> currentButtons = new List<Button>();

    private void Start()
    {
        if (autoRefreshOnStart)
        {
            RefreshOutfitLibrary();
        }

        if (playerOutfitManager == null)
        {
            playerOutfitManager = FindObjectOfType<OutfitManager>();
            if (playerOutfitManager == null)
            {
                Debug.LogError("OutfitUIManager: No OutfitManager found in scene!");
            }
        }
    }

    public void RefreshOutfitLibrary()
    {
        // Clear existing buttons
        foreach (Button btn in currentButtons)
        {
            if (btn != null && btn.gameObject != null)
                Destroy(btn.gameObject);
        }
        currentButtons.Clear();

        // Create new buttons
        foreach (OutfitData outfit in outfitLibrary)
        {
            if (outfit != null && outfit.outfitPrefab != null)
            {
                CreateOutfitButton(outfit);
            }
        }
    }

    private void CreateOutfitButton(OutfitData outfit)
    {
        if (outfitButtonPrefab == null || outfitButtonContainer == null)
        {
            Debug.LogError("OutfitUIManager: Button prefab or container not assigned!");
            return;
        }

        GameObject buttonObj = Instantiate(outfitButtonPrefab, outfitButtonContainer);
        Button button = buttonObj.GetComponent<Button>();

        if (button == null)
        {
            button = buttonObj.AddComponent<Button>();
        }

        // Setup button visuals
        Image image = buttonObj.GetComponent<Image>();
        if (image != null && outfit.previewSprite != null)
        {
            image.sprite = outfit.previewSprite;
        }

        // Setup button text if exists
        Text buttonText = buttonObj.GetComponentInChildren<Text>();
        if (buttonText != null && showOutfitNames)
        {
            buttonText.text = outfit.outfitName;
        }

        // Add click listener
        button.onClick.AddListener(() => OnOutfitSelected(outfit));

        currentButtons.Add(button);
    }

    private void OnOutfitSelected(OutfitData outfit)
    {
        if (playerOutfitManager != null)
        {
            playerOutfitManager.EquipOutfit(outfit);
            Debug.Log($"OutfitUIManager: Equipped {outfit.outfitName}");
        }
        else
        {
            Debug.LogError("OutfitUIManager: Cannot equip outfit - No OutfitManager assigned!");
        }
    }

    public void AddOutfitToLibrary(OutfitData outfit)
    {
        if (!outfitLibrary.Contains(outfit))
        {
            outfitLibrary.Add(outfit);
            RefreshOutfitLibrary();
        }
    }

    public void RemoveOutfitFromLibrary(OutfitData outfit)
    {
        if (outfitLibrary.Contains(outfit))
        {
            outfitLibrary.Remove(outfit);
            RefreshOutfitLibrary();
        }
    }
}