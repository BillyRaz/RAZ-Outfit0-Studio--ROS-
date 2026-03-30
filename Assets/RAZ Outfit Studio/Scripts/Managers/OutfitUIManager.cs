using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class OutfitUIManager : MonoBehaviour
{
    [Header("References")]
    public OutfitManager playerOutfitManager;
    public GameObject playerRootPrefabAsset;
    public Transform outfitButtonContainer;
    public GameObject outfitButtonPrefab;

    [Header("Current Outfit Display")]
    public Text currentOutfitNameText;
    public Image currentOutfitPreviewImage;
    public Image playerPreviewImage;
    public GameObject currentOutfitPanel;

    [Header("Outfit Library")]
    public List<OutfitData> outfitLibrary = new List<OutfitData>();

    [Header("UI Settings")]
    public bool autoRefreshOnStart = true;
    public bool showOutfitNames = true;
    public bool showOutfitPreviews = true;
    public bool autoLinkPlayer = true;
    public bool spawnPlayerIfMissing = true;
    public bool keepSearchingForPlayer = true;
    public float refreshInterval = 0f;
    public Sprite defaultOutfitSprite;
    public Sprite defaultPlayerSprite;

    [Header("Visual Settings")]
    public Color normalButtonColor = new Color(0.25f, 0.25f, 0.25f);
    public Color selectedButtonColor = new Color(0.4f, 0.5f, 0.6f);
    public Color hoverButtonColor = new Color(0.35f, 0.35f, 0.35f);

    private readonly List<Button> currentButtons = new List<Button>();
    private readonly List<OutfitData> buttonOutfitData = new List<OutfitData>();
    private OutfitData currentlySelectedOutfit;
    private readonly Dictionary<Texture2D, Sprite> runtimeIconSprites = new Dictionary<Texture2D, Sprite>();
    private float lastRefreshTime = 0f;
    private Canvas parentCanvas;
    private float nextPlayerLookupTime = 0f;

    private void Awake()
    {
        parentCanvas = GetComponent<Canvas>() ?? GetComponentInParent<Canvas>(true);
        InitializeUI();
        EnsureDefaultSprites();
    }

    private void Start()
    {
        if (autoLinkPlayer && playerOutfitManager == null)
        {
            AutoLinkPlayer();
        }

        if (autoRefreshOnStart)
        {
            ManualRefresh();
        }
        else if (playerOutfitManager != null)
        {
            currentlySelectedOutfit = playerOutfitManager.CurrentOutfit;
            UpdateCurrentOutfitDisplay(currentlySelectedOutfit);
            UpdateSelectedButtonFromOutfit(currentlySelectedOutfit);
        }

        LoadPlayerPreview();
    }

    private void Update()
    {
        if (refreshInterval > 0f && Time.time - lastRefreshTime >= refreshInterval)
        {
            lastRefreshTime = Time.time;
            RefreshOutfitLibrary();
        }

        if (playerOutfitManager == null)
        {
            if (autoLinkPlayer && keepSearchingForPlayer && Time.time >= nextPlayerLookupTime)
            {
                nextPlayerLookupTime = Time.time + 1f;
                AutoLinkPlayer();
            }
            return;
        }

        if (playerOutfitManager.CurrentOutfit != currentlySelectedOutfit)
        {
            currentlySelectedOutfit = playerOutfitManager.CurrentOutfit;
            UpdateCurrentOutfitDisplay(currentlySelectedOutfit);
            UpdateSelectedButtonFromOutfit(currentlySelectedOutfit);
        }
    }

    private void InitializeUI()
    {
        if (currentOutfitPanel == null)
        {
            Transform leftPanel = transform.Find("MainPanel/LeftPanel") ?? transform.Find("LeftPanel");
            if (leftPanel != null)
            {
                currentOutfitPanel = leftPanel.gameObject;
            }
        }

        if (currentOutfitNameText == null && currentOutfitPanel != null)
        {
            foreach (Text text in currentOutfitPanel.GetComponentsInChildren<Text>(true))
            {
                if (text.gameObject.name == "OutfitName" || text.gameObject.name.Contains("Name"))
                {
                    currentOutfitNameText = text;
                    break;
                }
            }
        }

        if (currentOutfitPreviewImage == null && currentOutfitPanel != null)
        {
            foreach (Image image in currentOutfitPanel.GetComponentsInChildren<Image>(true))
            {
                if (image.gameObject.name == "OutfitPreview")
                {
                    currentOutfitPreviewImage = image;
                    break;
                }
            }
        }

        if (playerPreviewImage == null && currentOutfitPanel != null)
        {
            foreach (Image image in currentOutfitPanel.GetComponentsInChildren<Image>(true))
            {
                if (image.gameObject.name == "PlayerPreview")
                {
                    playerPreviewImage = image;
                    break;
                }
            }
        }

        if (outfitButtonContainer == null)
        {
            outfitButtonContainer = transform.Find("MainPanel/RightPanel/ScrollView/Viewport/Content")
                ?? transform.Find("RightPanel/ScrollView/Viewport/Content");
        }

        Debug.Log("OutfitUIManager: UI initialized");
    }

    private void AutoLinkPlayer()
    {
        playerOutfitManager = Object.FindFirstObjectByType<OutfitManager>();

        if (playerOutfitManager == null && spawnPlayerIfMissing && playerRootPrefabAsset != null)
        {
            GameObject spawnedRoot = Instantiate(playerRootPrefabAsset);
            spawnedRoot.name = playerRootPrefabAsset.name;
            playerOutfitManager = spawnedRoot.GetComponent<OutfitManager>();
            Debug.Log($"OutfitUIManager: Spawned fallback player root from prefab: {playerRootPrefabAsset.name}");
        }

        if (playerOutfitManager == null && spawnPlayerIfMissing)
        {
            playerOutfitManager = CreateFallbackPlayerFromDefaultOutfit();
        }

        if (playerOutfitManager != null)
        {
            Debug.Log($"OutfitUIManager: Auto-linked to player: {playerOutfitManager.gameObject.name}");
            currentlySelectedOutfit = playerOutfitManager.CurrentOutfit;
            UpdateCurrentOutfitDisplay(currentlySelectedOutfit);
        }
        else
        {
            Debug.LogWarning("OutfitUIManager: No OutfitManager found in scene.");
        }
    }

    private OutfitManager CreateFallbackPlayerFromDefaultOutfit()
    {
        OutfitData fallbackOutfit = outfitLibrary.Find(outfit =>
            outfit != null &&
            outfit.outfitPrefab != null &&
            (outfit.outfitName.Contains("Default") || outfit.outfitName.Contains("Base")));

        if (fallbackOutfit == null)
        {
            fallbackOutfit = outfitLibrary.Find(outfit => outfit != null && outfit.outfitPrefab != null);
        }

        if (fallbackOutfit == null)
        {
            return null;
        }

        GameObject runtimeRoot = new GameObject("RuntimePlayerRoot");
        GameObject meshInstance = Instantiate(fallbackOutfit.outfitPrefab, runtimeRoot.transform);
        meshInstance.name = fallbackOutfit.outfitName + "_Base";

        SkinnedMeshRenderer meshRenderer = meshInstance.GetComponentInChildren<SkinnedMeshRenderer>(true);
        if (meshRenderer == null)
        {
            Destroy(runtimeRoot);
            Debug.LogWarning($"OutfitUIManager: Could not build fallback player because '{fallbackOutfit.outfitName}' has no SkinnedMeshRenderer.");
            return null;
        }

        OutfitManager runtimeManager = runtimeRoot.GetComponent<OutfitManager>();
        if (runtimeManager == null)
        {
            runtimeManager = runtimeRoot.AddComponent<OutfitManager>();
        }

        GameObject runtimeOutfitRoot = new GameObject("OutfitRoot");
        runtimeOutfitRoot.transform.SetParent(runtimeRoot.transform, false);

        runtimeManager.ConfigureRuntimeSetup(meshRenderer, runtimeOutfitRoot.transform, fallbackOutfit);
        Debug.Log($"OutfitUIManager: Created runtime fallback player from default outfit '{fallbackOutfit.outfitName}'.");
        return runtimeManager;
    }

    public void ManualRefresh()
    {
        if (autoLinkPlayer && playerOutfitManager == null)
        {
            AutoLinkPlayer();
        }

        RefreshOutfitLibrary();
        lastRefreshTime = Time.time;

        if (playerOutfitManager != null)
        {
            currentlySelectedOutfit = playerOutfitManager.CurrentOutfit;
            UpdateCurrentOutfitDisplay(currentlySelectedOutfit);
            UpdateSelectedButtonFromOutfit(currentlySelectedOutfit);
        }

        LoadPlayerPreview();
        Debug.Log($"OutfitUIManager: Manual refresh completed - {outfitLibrary.Count} outfits in library");
    }

    public void RefreshOutfitLibrary()
    {
        Debug.Log($"OutfitUIManager: Refreshing outfit library. List count = {outfitLibrary.Count}");

        if (outfitButtonContainer == null)
        {
            outfitButtonContainer = transform.Find("MainPanel/RightPanel/ScrollView/Viewport/Content")
                ?? transform.Find("RightPanel/ScrollView/Viewport/Content");
        }

        if (outfitButtonContainer != null)
        {
            for (int i = outfitButtonContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = outfitButtonContainer.GetChild(i);
                if (Application.isPlaying)
                    Destroy(child.gameObject);
                else
                    DestroyImmediate(child.gameObject);
            }
        }

        foreach (Button button in currentButtons)
        {
            if (button != null && button.gameObject != null)
            {
                if (Application.isPlaying)
                    Destroy(button.gameObject);
                else
                    DestroyImmediate(button.gameObject);
            }
        }

        currentButtons.Clear();
        buttonOutfitData.Clear();

        if (outfitButtonContainer == null)
        {
            Debug.LogError("OutfitUIManager: Outfit button container is missing, so no cards can be created.");
            return;
        }

        if (outfitButtonPrefab == null)
        {
            Debug.LogError("OutfitUIManager: Outfit button prefab is missing, so no cards can be created.");
            return;
        }

        foreach (OutfitData outfit in outfitLibrary)
        {
            if (outfit != null)
            {
                CreateOutfitButton(outfit);
            }
        }

        RectTransform containerRect = outfitButtonContainer as RectTransform;
        if (containerRect != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
            containerRect.anchoredPosition = Vector2.zero;
        }

        GridLayoutGroup grid = outfitButtonContainer.GetComponent<GridLayoutGroup>();
        if (grid != null && containerRect != null)
        {
            int columnCount = Mathf.Max(1, Mathf.FloorToInt((containerRect.rect.width - grid.padding.left - grid.padding.right + grid.spacing.x) / (grid.cellSize.x + grid.spacing.x)));
            int rowCount = Mathf.CeilToInt(currentButtons.Count / (float)columnCount);
            float height = grid.padding.top + grid.padding.bottom + rowCount * grid.cellSize.y + Mathf.Max(0, rowCount - 1) * grid.spacing.y;
            containerRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, Mathf.Max(height, grid.cellSize.y + grid.padding.top + grid.padding.bottom));
        }

        Canvas.ForceUpdateCanvases();
        ScrollRect scrollRect = outfitButtonContainer.GetComponentInParent<ScrollRect>();
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
        UpdateSelectedButtonFromOutfit(currentlySelectedOutfit);
        Debug.Log($"OutfitUIManager: Refreshed library with {currentButtons.Count} cards. Container children = {outfitButtonContainer.childCount}");
    }

    private void CreateOutfitButton(OutfitData outfit)
    {
        if (outfitButtonPrefab == null || outfitButtonContainer == null)
        {
            Debug.LogError("OutfitUIManager: Button prefab or container not assigned.");
            return;
        }

        GameObject buttonObject = Instantiate(outfitButtonPrefab, outfitButtonContainer);
        buttonObject.name = $"Btn_{outfit.outfitName}";
        buttonObject.transform.localScale = Vector3.one;
        buttonObject.transform.localPosition = Vector3.zero;
        buttonObject.SetActive(true);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(120f, 140f);
            rect.localScale = Vector3.one;
        }

        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
        {
            button = buttonObject.AddComponent<Button>();
        }

        SetupButtonVisuals(buttonObject, button, outfit);

        buttonOutfitData.Add(outfit);
        int outfitIndex = buttonOutfitData.Count - 1;
        button.onClick.AddListener(() => OnOutfitSelected(outfitIndex));

        currentButtons.Add(button);
        Debug.Log($"OutfitUIManager: Created card '{buttonObject.name}'. Total cards = {currentButtons.Count}");
    }

    private void SetupButtonVisuals(GameObject buttonObject, Button button, OutfitData outfit)
    {
        ColorBlock colors = button.colors;
        colors.normalColor = normalButtonColor;
        colors.highlightedColor = hoverButtonColor;
        colors.pressedColor = selectedButtonColor;
        colors.selectedColor = selectedButtonColor;
        button.colors = colors;

        Image backgroundImage = buttonObject.GetComponent<Image>();
        if (backgroundImage != null)
        {
            backgroundImage.color = normalButtonColor;
        }

        Image previewImage = buttonObject.transform.Find("PreviewImage")?.GetComponent<Image>()
            ?? buttonObject.transform.Find("Preview")?.GetComponent<Image>();

        if (previewImage == null)
        {
            foreach (Image image in buttonObject.GetComponentsInChildren<Image>(true))
            {
                if (image != backgroundImage)
                {
                    previewImage = image;
                    break;
                }
            }
        }

        Sprite previewSprite = GetPreviewSprite(outfit) ?? defaultOutfitSprite;
        if (previewImage != null)
        {
            previewImage.sprite = previewSprite;
            previewImage.preserveAspect = true;
            previewImage.color = previewSprite != null ? Color.white : new Color(0.5f, 0.5f, 0.5f);
            previewImage.raycastTarget = false;
        }

        Text buttonText = buttonObject.GetComponentInChildren<Text>(true);
        if (buttonText == null)
        {
            GameObject textObject = new GameObject("ButtonText");
            textObject.transform.SetParent(buttonObject.transform, false);
            RectTransform textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 0.4f);
            textRect.offsetMin = new Vector2(5f, 5f);
            textRect.offsetMax = new Vector2(-5f, -5f);

            buttonText = textObject.AddComponent<Text>();
            buttonText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            buttonText.fontSize = 12;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.color = Color.white;
        }

        if (buttonText != null)
        {
            buttonText.text = showOutfitNames ? outfit.outfitName : string.Empty;
            buttonText.raycastTarget = false;
        }
    }

    private void OnOutfitSelected(int outfitIndex)
    {
        if (outfitIndex < 0 || outfitIndex >= buttonOutfitData.Count)
        {
            return;
        }

        OutfitData selectedOutfit = buttonOutfitData[outfitIndex];
        if (playerOutfitManager == null)
        {
            Debug.LogError("OutfitUIManager: Cannot equip outfit because no OutfitManager is assigned.");
            return;
        }

        playerOutfitManager.EquipOutfit(selectedOutfit);
        currentlySelectedOutfit = selectedOutfit;
        UpdateButtonVisuals(outfitIndex);
        UpdateCurrentOutfitDisplay(selectedOutfit);

        Debug.Log($"OutfitUIManager: Equipped {selectedOutfit.outfitName}");
    }

    private void UpdateButtonVisuals(int selectedIndex)
    {
        for (int i = 0; i < currentButtons.Count; i++)
        {
            Button button = currentButtons[i];
            if (button == null)
            {
                continue;
            }

            Color buttonColor = i == selectedIndex ? selectedButtonColor : normalButtonColor;
            ColorBlock colors = button.colors;
            colors.normalColor = buttonColor;
            button.colors = colors;

            Image image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = buttonColor;
            }
        }
    }

    private void UpdateSelectedButtonFromOutfit(OutfitData outfit)
    {
        int index = buttonOutfitData.IndexOf(outfit);
        UpdateButtonVisuals(index);
    }

    private void UpdateCurrentOutfitDisplay(OutfitData outfit)
    {
        Sprite previewSprite = GetPreviewSprite(outfit);

        if (currentOutfitNameText != null)
        {
            if (outfit != null && !string.IsNullOrEmpty(outfit.outfitName))
            {
                currentOutfitNameText.text = $"Current Outfit: {outfit.outfitName}";
                currentOutfitNameText.color = Color.green;
            }
            else
            {
                currentOutfitNameText.text = "Current Outfit: None";
                currentOutfitNameText.color = Color.yellow;
            }
        }

        if (currentOutfitPreviewImage != null)
        {
            Sprite displaySprite = previewSprite ?? defaultOutfitSprite;
            currentOutfitPreviewImage.sprite = displaySprite;
            currentOutfitPreviewImage.color = displaySprite != null ? Color.white : new Color(0.3f, 0.3f, 0.3f);
            currentOutfitPreviewImage.preserveAspect = true;
            currentOutfitPreviewImage.gameObject.SetActive(true);
        }

        if (playerPreviewImage != null)
        {
            bool showPlayerPreview = outfit == null && playerPreviewImage.sprite != null;
            playerPreviewImage.gameObject.SetActive(showPlayerPreview);
            if (showPlayerPreview && currentOutfitPreviewImage != null)
            {
                currentOutfitPreviewImage.gameObject.SetActive(false);
            }
        }

        Debug.Log($"OutfitUIManager: Updated display to {(outfit != null ? outfit.outfitName : "None")}");
    }

    private void LoadPlayerPreview()
    {
        if (playerPreviewImage == null)
        {
            return;
        }

        EnsureDefaultSprites();
        playerPreviewImage.sprite = defaultPlayerSprite;
        playerPreviewImage.color = Color.white;
        playerPreviewImage.preserveAspect = true;
    }

    private void EnsureDefaultSprites()
    {
        if (defaultOutfitSprite == null)
        {
            defaultOutfitSprite = CreateRuntimePlaceholderSprite(
                new Color(0.35f, 0.35f, 0.35f, 1f),
                new Color(0.6f, 0.6f, 0.6f, 1f));
        }

        if (defaultPlayerSprite == null)
        {
            defaultPlayerSprite = CreateRuntimePlaceholderSprite(
                new Color(0.2f, 0.28f, 0.36f, 1f),
                new Color(0.55f, 0.72f, 0.9f, 1f));
        }
    }

    private Sprite CreateRuntimePlaceholderSprite(Color background, Color accent)
    {
        const int size = 96;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "ROS_RuntimePlaceholder";

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color pixel = background;

                if (x > 18 && x < size - 18 && y > 18 && y < size - 18)
                {
                    pixel = accent;
                }

                if ((x - size / 2) * (x - size / 2) + (y - size / 2) * (y - size / 2) < 18 * 18)
                {
                    pixel = Color.white;
                }

                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite GetPreviewSprite(OutfitData outfit)
    {
        if (outfit == null)
        {
            return null;
        }

        if (outfit.previewSprite != null)
        {
            return outfit.previewSprite;
        }

        if (outfit.icon != null)
        {
            if (!runtimeIconSprites.TryGetValue(outfit.icon, out Sprite sprite) || sprite == null)
            {
                Rect rect = new Rect(0f, 0f, outfit.icon.width, outfit.icon.height);
                sprite = Sprite.Create(outfit.icon, rect, new Vector2(0.5f, 0.5f), 100f);
                runtimeIconSprites[outfit.icon] = sprite;
            }

            return sprite;
        }

        return null;
    }

    public void AddOutfitToLibrary(OutfitData outfit)
    {
        if (outfit != null && !outfitLibrary.Contains(outfit))
        {
            outfitLibrary.Add(outfit);
            RefreshOutfitLibrary();
            Debug.Log($"OutfitUIManager: Added {outfit.outfitName} to library");
        }
    }

    public void RemoveOutfitFromLibrary(OutfitData outfit)
    {
        if (outfit == null || !outfitLibrary.Contains(outfit))
        {
            return;
        }

        outfitLibrary.Remove(outfit);
        RefreshOutfitLibrary();

        if (currentlySelectedOutfit == outfit)
        {
            currentlySelectedOutfit = null;
            UpdateCurrentOutfitDisplay(null);
        }

        Debug.Log($"OutfitUIManager: Removed {outfit.outfitName} from library");
    }

    public OutfitData GetCurrentOutfit()
    {
        return currentlySelectedOutfit;
    }

    public void ForceRefresh()
    {
        ManualRefresh();
    }

    public void ToggleUI()
    {
        if (parentCanvas != null)
        {
            parentCanvas.enabled = !parentCanvas.enabled;
            return;
        }

        gameObject.SetActive(!gameObject.activeSelf);
    }

    public void CloseUI()
    {
        if (parentCanvas != null)
        {
            parentCanvas.enabled = false;
            return;
        }

        gameObject.SetActive(false);
    }

    public void ShowUI()
    {
        if (parentCanvas != null)
        {
            parentCanvas.enabled = true;
            return;
        }

        gameObject.SetActive(true);
    }
}
