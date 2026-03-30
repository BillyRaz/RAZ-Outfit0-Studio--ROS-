using UnityEngine;

[CreateAssetMenu(fileName = "NewOutfitData", menuName = "RAZ Outfit Studio/Outfit Data")]
public class OutfitData : ScriptableObject
{
    [Header("Basic Information")]
    public string outfitName;
    public string description;

    [Header("Assets")]
    public GameObject outfitPrefab;
    public Sprite previewSprite;
    public Texture2D icon;

    [Header("Metadata")]
    public string author;
    public System.DateTime creationDate;
    public string version = "1.0";

    [Header("Preview Settings")]
    public bool hasCustomPreview = false;
    public Vector2 previewOffset;
    public float previewScale = 1f;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(outfitName))
        {
            outfitName = name;
        }

        if (creationDate == default)
        {
            creationDate = System.DateTime.Now;
        }

        if (string.IsNullOrEmpty(author))
        {
            author = System.Environment.UserName;
        }
    }

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(outfitName) && !string.IsNullOrEmpty(name))
        {
            outfitName = name;
        }

        if (previewSprite != null || icon != null)
        {
            hasCustomPreview = true;
        }
    }
}
