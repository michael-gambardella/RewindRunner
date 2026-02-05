using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RespawnManager : MonoBehaviour
{
    public static RespawnManager Instance { get; private set; }

    [Header("Player")]
    [SerializeField] private GameObject player;
    [SerializeField] private Transform respawnPoint;

    [Header("Respawn")]
    [Tooltip("Time the screen stays fully black before the player reappears (not the total respawn time).")]
    [SerializeField] private float respawnDelay = 0.25f;
    [SerializeField] private bool resetSceneOnDeath = false;

    [Header("Screen Fade")]
    [Tooltip("Time to fade to black and time to fade back (0 = no fade, instant respawn). Separate from Respawn Delay.")]
    [SerializeField] private float fadeDuration = 0.15f;

    private Vector3 _defaultSpawnPos;
    private Canvas _fadeCanvas;
    private Image _fadeImage;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (player != null)
            _defaultSpawnPos = player.transform.position;
    }

    private void EnsureFadeOverlay()
    {
        if (_fadeImage != null) return;
        if (fadeDuration <= 0f) return;

        var go = new GameObject("RespawnFadeCanvas");
        go.transform.SetParent(transform);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();

        var imageGo = new GameObject("FadeOverlay");
        imageGo.transform.SetParent(go.transform, false);

        _fadeImage = imageGo.AddComponent<Image>();
        _fadeImage.color = new Color(0f, 0f, 0f, 0f);
        _fadeImage.raycastTarget = false;

        var rt = imageGo.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        _fadeCanvas = canvas;
        _fadeCanvas.enabled = false;
    }

    public void SetRespawnPoint(Transform t) => respawnPoint = t;

    public void KillPlayer()
    {
        if (resetSceneOnDeath)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        if (player == null) return;
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        // Create overlay on first use (in case fadeDuration was 0 when scene was saved)
        EnsureFadeOverlay();

        // Fade to black (player still visible briefly, then we hide)
        if (_fadeCanvas != null && _fadeImage != null && fadeDuration > 0f)
        {
            _fadeCanvas.enabled = true;
            _fadeImage.color = new Color(0f, 0f, 0f, 0f);
            yield return FadeTo(1f);
        }

        player.SetActive(false);

        // Wait while screen is black (respawn delay)
        yield return new WaitForSeconds(respawnDelay);

        Vector3 spawnPos = respawnPoint != null ? respawnPoint.position : _defaultSpawnPos;
        player.transform.position = spawnPos;

        var rb = player.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        player.SetActive(true);

        // Fade back from black
        if (_fadeCanvas != null && _fadeImage != null && fadeDuration > 0f)
        {
            yield return FadeTo(0f);
            _fadeCanvas.enabled = false;
        }
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        if (_fadeImage == null || fadeDuration <= 0f) yield break;
        float startAlpha = _fadeImage.color.a;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float a = Mathf.Lerp(startAlpha, targetAlpha, t);
            _fadeImage.color = new Color(0f, 0f, 0f, a);
            yield return null;
        }
        _fadeImage.color = new Color(0f, 0f, 0f, targetAlpha);
    }
}
