using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Dibuja una barra flotante encima de la cabeza del guardia que se va llenando a medida que
/// sospecha de ti (de amarillo a rojo). Siempre mira a la cámara y se esconde cuando el guardia
/// está tranquilo. Crea toda la barra por código, así que no hay que montar nada en la escena:
/// HumanAI se la añade solo a cada guardia.
/// </summary>
[RequireComponent(typeof(HumanVision))]
public class DetectionBar : MonoBehaviour
{
    [Tooltip("Posición de la barra respecto al centro del guardia.")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.5f, 0f);
    [Tooltip("Ancho de la barra en metros.")]
    [SerializeField] private float worldWidth = 1.2f;
    [SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.65f);
    [SerializeField] private Color lowColor = new Color(1f, 0.9f, 0.2f, 1f);
    [SerializeField] private Color highColor = new Color(1f, 0.15f, 0.1f, 1f);

    private const float CANVAS_WIDTH = 100f;
    private const float CANVAS_HEIGHT = 12f;

    private HumanVision vision;
    private GameObject barObject;
    private Transform barTransform;
    private RectTransform fillRect;
    private Image fillImage;
    private Camera cam;

    private void Awake()
    {
        vision = GetComponent<HumanVision>();
        BuildBar();
    }

    private void BuildBar()
    {
        // Se crea ya con RectTransform y Canvas, para que la referencia al transform no se invalide.
        barObject = new GameObject("DetectionBar", typeof(RectTransform), typeof(Canvas));
        barTransform = barObject.transform;
        barTransform.SetParent(transform, false);
        barTransform.localPosition = offset;

        Canvas canvas = barObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        RectTransform rootRect = barObject.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(CANVAS_WIDTH, CANVAS_HEIGHT);
        float scale = worldWidth / CANVAS_WIDTH;
        rootRect.localScale = new Vector3(scale, scale, scale);

        CreateImage("Background", backgroundColor);
        fillImage = CreateImage("Fill", lowColor);
        fillRect = fillImage.rectTransform;

        barObject.SetActive(false);
    }

    private Image CreateImage(string objectName, Color color)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(barTransform, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private void LateUpdate()
    {
        float suspicion = vision.Suspicion;
        bool show = suspicion > 0.01f;

        if (barObject.activeSelf != show)
        {
            barObject.SetActive(show);
        }
        if (!show) return;

        // La barra se llena de izquierda a derecha según la sospecha.
        fillRect.anchorMax = new Vector2(Mathf.Clamp01(suspicion), 1f);
        fillImage.color = Color.Lerp(lowColor, highColor, suspicion);

        // Siempre de cara a la cámara del jugador.
        if (cam == null && Camera.main != null)
        {
            cam = Camera.main;
        }
        if (cam != null)
        {
            Vector3 away = barTransform.position - cam.transform.position;
            if (away.sqrMagnitude > 0.0001f)
            {
                barTransform.rotation = Quaternion.LookRotation(away);
            }
        }
    }
}
