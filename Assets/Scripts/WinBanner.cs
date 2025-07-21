using TMPro;
using UnityEngine;

public class WinBanner : MonoBehaviour
{
    [Tooltip("Drag the TextMeshProUGUI component here")]
    public TMP_Text label;

    void Awake() => Hide();

    public void Show(string text, Color color)
    {
        if (!label) label = GetComponent<TMP_Text>();
        label.text  = text;
        label.color = color;
        gameObject.SetActive(true);
    }
    public void Hide() => gameObject.SetActive(false);
}
