using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Color normalColor = Color.white;
    public Color hoverColor = Color.gray;

    private Image buttonImage;
    private Button button;

    private void Start()
    {
        buttonImage = GetComponent<Image>();
        button = GetComponent<Button>();

        if (buttonImage != null)
        {
            normalColor = buttonImage.color;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button != null && button.interactable && buttonImage != null)
        {
            buttonImage.color = hoverColor;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (buttonImage != null)
        {
            buttonImage.color = normalColor;
        }
    }
}
