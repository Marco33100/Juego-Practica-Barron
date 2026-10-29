using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Brillo visible en Windows y persistente al cargar el nivel.</summary>
public class BrilloGlobal : MonoBehaviour
{
    private static BrilloGlobal instance;
    private Image overlay;
    public float Value { get; private set; }
    public float Alpha => overlay == null ? 0 : overlay.color.a;

    public static void Apply(float value)
    {
        if (instance == null)
        {
            var root = new GameObject("Brillo global", typeof(RectTransform), typeof(Canvas));
            DontDestroyOnLoad(root);
            instance = root.AddComponent<BrilloGlobal>();
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 32700;
            var image = new GameObject("Filtro de brillo", typeof(RectTransform), typeof(Image));
            image.transform.SetParent(root.transform, false);
            var rect = image.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero;
            instance.overlay = image.GetComponent<Image>();
            instance.overlay.raycastTarget = false;
        }
        instance.Value = Mathf.Clamp01(value);
        instance.overlay.color = new Color(0, 0, 0, (1 - instance.Value) * .75f);
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name != "Menu" && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            SceneManager.LoadScene("Menu");
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }
}
