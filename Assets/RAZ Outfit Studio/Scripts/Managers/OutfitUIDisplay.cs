using UnityEngine;
using UnityEngine.UI;

public class OutfitUIDisplay : MonoBehaviour
{
    [HideInInspector]
    public Text outfitNameText;

    [HideInInspector]
    public Image outfitPreviewImage;

    private OutfitUIManager uiManager;

    private void Start()
    {
        uiManager = GetComponentInParent<OutfitUIManager>();
        if (uiManager != null)
        {
            Debug.Log("OutfitUIDisplay initialized");
        }
    }

    public void UpdateDisplay(OutfitData outfit)
    {
        if (outfitNameText != null)
        {
            if (outfit != null)
            {
                outfitNameText.text = outfit.outfitName;
            }
            else
            {
                outfitNameText.text = "No Outfit Equipped";
            }
        }

        if (outfitPreviewImage != null && outfit != null && outfit.previewSprite != null)
        {
            outfitPreviewImage.sprite = outfit.previewSprite;
            outfitPreviewImage.color = Color.white;
        }
        else if (outfitPreviewImage != null)
        {
            outfitPreviewImage.sprite = null;
            outfitPreviewImage.color = new Color(0.3f, 0.3f, 0.3f);
        }
    }
}
