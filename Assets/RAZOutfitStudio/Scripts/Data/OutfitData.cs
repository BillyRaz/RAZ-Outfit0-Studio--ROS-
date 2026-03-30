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
    }

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(outfitName) && !string.IsNullOrEmpty(name))
        {
            outfitName = name;
        }
    }
}