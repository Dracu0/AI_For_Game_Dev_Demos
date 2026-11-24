using UnityEngine;

// Colours this sprite by whether the agent currently has a fact: a lit campfire, a fed (or hungry) worker.
[RequireComponent(typeof(SpriteRenderer))]
public class FactTint : MonoBehaviour
{
    public GoapAgent agent;
    public Fact fact;
    public Color whenTrue = Color.white;
    public Color whenFalse = Color.gray;

    private SpriteRenderer _sprite;

    private void Awake() => _sprite = GetComponent<SpriteRenderer>();

    private void Update() => _sprite.color = (agent.state & fact) != 0 ? whenTrue : whenFalse;
}
