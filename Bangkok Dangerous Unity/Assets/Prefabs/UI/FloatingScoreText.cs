using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Simple component for floating score text popups.
/// Attach to a UI Text GameObject to enable floating score display.
/// </summary>
public class FloatingScoreText : MonoBehaviour
{
    public Text textComponent;
    
    private void Start()
    {
        if (textComponent == null)
        {
            textComponent = GetComponent<Text>();
        }
    }
}
