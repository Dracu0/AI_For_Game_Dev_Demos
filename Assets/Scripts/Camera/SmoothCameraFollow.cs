using UnityEngine;

/// <summary>
/// Smooth 2D camera follow.
/// Attach to Main Camera and assign a target, or leave empty to find "Player".
/// </summary>
public class SmoothCameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] Transform target;

    [Header("Follow")]
    [SerializeField] float smoothTime = 0.2f;
    [SerializeField] Vector3 offset = new Vector3(0f, 0f, -10f);

    Vector3 _smoothVelocity;

    void Awake()
    {
        if (target != null)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
            player = GameObject.Find("Player");

        if (player != null)
            target = player.transform;
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref _smoothVelocity,
            smoothTime);
    }
}
