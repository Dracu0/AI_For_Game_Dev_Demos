using UnityEngine;
using UnityEngine.InputSystem;

/// Put this on the enemy sprite. Click a node to go there.
public class WaypointFollow : MonoBehaviour
{
    [SerializeField] WaypointPath path;
    [SerializeField] float speed = 2f;
    [SerializeField] float clickRadius = 0.5f;
    [SerializeField] bool loop = true;

    int index;
    bool done;

    void Update()
    {
        if (path == null || path.Count == 0) return;

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) PickNode();
        if (done) return;

        transform.position = Vector3.MoveTowards(transform.position, path.Get(index), speed * Time.deltaTime);
        if (transform.position != path.Get(index)) return;

        if (index + 1 < path.Count) index++;
        else if (loop) index = 0;
        else done = true;
    }

    void PickNode()
    {
        Vector2 click = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        for (int i = 0; i < path.Count; i++)
        {
            if (Vector2.Distance(click, path.Get(i)) > clickRadius) continue;
            index = i;
            done = false;
            return;
        }
    }
}
