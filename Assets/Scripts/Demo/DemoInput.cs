using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>
/// Shared mouse and UI helpers used by demo scenes.
/// </summary>
public static class DemoInput
{
    public static bool IsPointerOverUI() =>
        EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

    public static bool TryGetMouseWorldPosition(Camera camera, out Vector3 worldPosition)
    {
        worldPosition = default;

        if (camera == null || Mouse.current == null)
            return false;

        Vector2 screen = Mouse.current.position.ReadValue();
        float depth = Mathf.Abs(camera.transform.position.z);
        worldPosition = camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
        return true;
    }

    public static void SetStatus(TMP_Text statusText, string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}
