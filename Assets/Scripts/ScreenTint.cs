using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen tint overlay (e.g. blue during rewind, brief flash on jump).
/// Add to a GameObject; it will create a Canvas + Image at runtime if not assigned.
/// Assign "Tint Image" in the Inspector to use an existing full-screen Image.
/// </summary>
public class ScreenTint : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Optional. If null, a Canvas and Image are created at runtime.")]
    [SerializeField] private Image tintImage;

    [Header("Defaults")]
    [SerializeField] private Color rewindTintColor = new Color(0.4f, 0.5f, 1f, 0.12f);
    [SerializeField] private float rewindTintDuration = 0.25f;
    [Tooltip("Set alpha to 0 to disable jump flash (avoids disorienting players).")]
    [SerializeField] private Color jumpTintColor = new Color(1f, 1f, 1f, 0f);
    [SerializeField] private float jumpTintDuration = 0.08f;

    private float _tintTimeLeft;
    private float _tintDuration;
    private Color _tintColor;

    private void Awake()
    {
        if (tintImage == null)
            CreateTintOverlay();
        if (tintImage != null)
        {
            tintImage.color = new Color(0f, 0f, 0f, 0f);
            tintImage.raycastTarget = false;
        }
    }

    private void CreateTintOverlay()
    {
        var canvasGo = new GameObject("JuiceTintCanvas");
        canvasGo.transform.SetParent(transform);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.AddComponent<GraphicRaycaster>();

        var imageGo = new GameObject("TintImage");
        imageGo.transform.SetParent(canvasGo.transform, false);
        tintImage = imageGo.AddComponent<Image>();
        tintImage.color = new Color(0f, 0f, 0f, 0f);
        tintImage.raycastTarget = false;

        var rt = tintImage.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void Update()
    {
        if (tintImage == null || _tintTimeLeft <= 0f) return;
        _tintTimeLeft -= Time.unscaledDeltaTime;
        float progress = _tintDuration > 0f ? 1f - (_tintTimeLeft / _tintDuration) : 1f;
        float alpha = _tintColor.a * Mathf.Clamp01(progress < 0.2f ? progress / 0.2f : progress > 0.6f ? (1f - progress) / 0.4f : 1f);
        tintImage.color = new Color(_tintColor.r, _tintColor.g, _tintColor.b, alpha);
        if (_tintTimeLeft <= 0f)
            tintImage.color = new Color(0f, 0f, 0f, 0f);
    }

    /// <summary>Brief tint (e.g. rewind = blue, jump = subtle white).</summary>
    public void Tint(Color color, float duration)
    {
        if (tintImage == null) return;
        _tintColor = color;
        _tintDuration = duration;
        _tintTimeLeft = duration;
    }

    public void TintRewind() => Tint(rewindTintColor, rewindTintDuration);
    public void TintJump() => Tint(jumpTintColor, jumpTintDuration);
}
