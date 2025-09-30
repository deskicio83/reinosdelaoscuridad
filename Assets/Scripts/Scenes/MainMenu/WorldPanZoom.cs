/*
============================================================
WorldPanZoom.cs — Pan/zoom de cámara en menú principal
------------------------------------------------------------
PROPÓSITO
- Arrastre y zoom sobre mundo ortográfico.

MÉTODOS (COMPLETA AQUÍ)
- Update(): input → posición/ortographicSize.
- ClampToBounds(): límites.
============================================================
*/

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-50)]
[RequireComponent(typeof(Camera))]
public class WorldPanZoom : MonoBehaviour
{
    [Header("Refs")]
    public Camera worldCamera;               // Si lo dejas vacío, toma este mismo Camera
    public SpriteRenderer backgroundSprite;  // SpriteRenderer del fondo grande

    [Header("Zoom")]
    [Range(0.5f, 10f)] public float minOrthoSize = 4f;
    [Range(2f, 20f)] public float maxOrthoSize = 12f;
    [Range(0.1f, 10f)] public float wheelZoomSensitivity = 2f;   // rueda ratón
    [Range(0.05f, 1.5f)] public float pinchZoomSensitivity = 0.6f; // pinch móvil

    [Header("Pan")]
    [Range(0.2f, 5f)] public float dragSpeed = 1.0f;

    private Bounds worldBounds;
    private bool boundsReady;

    private bool isDragging;
    private Vector2 lastPointerScreenPos;

    private bool hadTwoTouchesLastFrame;
    private Vector2 lastPinchCenter;
    private float lastPinchDistance;

    private void Awake()
    {
        if (!worldCamera) worldCamera = GetComponent<Camera>();
        worldCamera.orthographic = true;
    }

    private void Start()
    {
        RecalculateLimitsImmediate();
        ClampCameraInside();
    }

    public void RecalculateLimitsImmediate()
    {
        boundsReady = (backgroundSprite != null && backgroundSprite.sprite != null);
        if (boundsReady)
            worldBounds = backgroundSprite.bounds;
    }

    private void Update()
    {
        if (!boundsReady || !worldCamera) return;

        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            HandleTouch();
        }
        else
        {
            HandleMouse();
        }

        ClampCameraInside();
    }

    private void HandleMouse()
    {
        // Zoom rueda
        if (Mouse.current != null)
        {
            float wheel = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(wheel) > 0.01f)
            {
                float size = worldCamera.orthographicSize - wheel * wheelZoomSensitivity * Time.deltaTime;
                worldCamera.orthographicSize = Mathf.Clamp(size, minOrthoSize, maxOrthoSize);
            }

            // Pan con botón izq (si no es UI)
            if (Mouse.current.leftButton.wasPressedThisFrame && !IsPointerOverUI())
            {
                isDragging = true;
                lastPointerScreenPos = Mouse.current.position.ReadValue();
            }
            else if (Mouse.current.leftButton.isPressed && isDragging)
            {
                Vector2 curr = Mouse.current.position.ReadValue();
                PanFromScreenDelta(curr - lastPointerScreenPos);
                lastPointerScreenPos = curr;
            }
            else if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                isDragging = false;
            }
        }
    }

    private void HandleTouch()
    {
        var ts = Touchscreen.current;
        int active = 0;
        Vector2 p0 = default, p1 = default;

        foreach (var t in ts.touches)
        {
            if (!t.press.isPressed) continue;
            if (active == 0) p0 = t.position.ReadValue();
            else if (active == 1) p1 = t.position.ReadValue();
            active++;
            if (active >= 2) break;
        }

        if (active == 1)
        {
            if (!IsPointerOverUI())
            {
                Vector2 curr = p0;
                if (!isDragging)
                {
                    isDragging = true;
                    lastPointerScreenPos = curr;
                }
                else
                {
                    PanFromScreenDelta(curr - lastPointerScreenPos);
                    lastPointerScreenPos = curr;
                }
            }
            hadTwoTouchesLastFrame = false;
        }
        else if (active >= 2)
        {
            Vector2 center = (p0 + p1) * 0.5f;
            float dist = Vector2.Distance(p0, p1);

            if (!hadTwoTouchesLastFrame)
            {
                hadTwoTouchesLastFrame = true;
                lastPinchCenter = center;
                lastPinchDistance = dist;
            }
            else
            {
                float delta = dist - lastPinchDistance;

                float before = worldCamera.orthographicSize;
                float after = Mathf.Clamp(before - delta * pinchZoomSensitivity * Time.deltaTime, minOrthoSize, maxOrthoSize);

                Vector3 beforeWorld = worldCamera.ScreenToWorldPoint(new Vector3(center.x, center.y, 0));
                worldCamera.orthographicSize = after;
                Vector3 afterWorld = worldCamera.ScreenToWorldPoint(new Vector3(center.x, center.y, 0));
                Vector3 camShift = beforeWorld - afterWorld;
                worldCamera.transform.position += camShift;

                lastPinchCenter = center;
                lastPinchDistance = dist;
            }
            isDragging = false;
        }
        else
        {
            isDragging = false;
            hadTwoTouchesLastFrame = false;
        }
    }

    private void PanFromScreenDelta(Vector2 screenDelta)
    {
        Vector3 a = worldCamera.ScreenToWorldPoint(Vector3.zero);
        Vector3 b = worldCamera.ScreenToWorldPoint(new Vector3(screenDelta.x, screenDelta.y, 0));
        Vector3 worldDelta = (a - b) * dragSpeed;
        worldCamera.transform.position += new Vector3(worldDelta.x, worldDelta.y, 0);
    }

    private void ClampCameraInside()
    {
        if (!boundsReady || worldCamera == null) return;

        float halfH = worldCamera.orthographicSize;
        float halfW = halfH * worldCamera.aspect;

        float minX = worldBounds.min.x + halfW;
        float maxX = worldBounds.max.x - halfW;
        float minY = worldBounds.min.y + halfH;
        float maxY = worldBounds.max.y - halfH;

        Vector3 p = worldCamera.transform.position;

        // Si la vista es más grande que el fondo en un eje, NO recentres ese eje.
        if (minX <= maxX) p.x = Mathf.Clamp(p.x, minX, maxX);
        if (minY <= maxY) p.y = Mathf.Clamp(p.y, minY, maxY);

        worldCamera.transform.position = new Vector3(p.x, p.y, worldCamera.transform.position.z);
    }

    private static bool IsPointerOverUI()
    {
        if (EventSystem.current == null) return false;
#if UNITY_EDITOR || UNITY_STANDALONE
        return EventSystem.current.IsPointerOverGameObject();
#else
        return EventSystem.current.IsPointerOverGameObject();
#endif
    }
}
