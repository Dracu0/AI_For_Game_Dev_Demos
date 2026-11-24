using System;
using UnityEngine;

// Everything a class needs to see: the goal, the world facts (click one to flip it) and the plan with its progress.
public class GoapHud : MonoBehaviour
{
    public GoapAgent agent;

    private GUIStyle _text;
    private GUIStyle _button;
    private GUIStyle _tag;

    private void OnGUI()
    {
        if (agent == null) return;

        _text ??= new GUIStyle(GUI.skin.label) { fontSize = 16, richText = true };
        _button ??= new GUIStyle(GUI.skin.button) { fontSize = 16, alignment = TextAnchor.MiddleLeft };
        _tag ??= new GUIStyle(_text) { alignment = TextAnchor.MiddleCenter };

        DrawPanel();
        DrawWorldLabels();
    }

    private void DrawPanel()
    {
        GUILayout.BeginArea(new Rect(10f, 10f, 300f, Screen.height - 20f));
        GUILayout.BeginVertical(GUI.skin.box);

        GUILayout.Label($"<b>Goal:</b> {agent.goal}", _text);

        GUILayout.Label("<b>World</b>  (click a fact to flip it)", _text);
        foreach (Fact fact in Enum.GetValues(typeof(Fact)))
        {
            if (fact == Fact.None) continue;

            bool on = (agent.state & fact) != 0;
            float left = agent.TimeLeft(fact);
            string label = $"{(on ? "[x]" : "[   ]")} {fact}{(left >= 0f ? $"   {left:0}s" : "")}";
            if (GUILayout.Button(label, _button)) agent.Toggle(fact);
        }

        GUILayout.Label(PlanTitle(), _text);
        for (int i = 0; i < agent.Plan.Count; i++) GUILayout.Label(PlanLine(i), _text);

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

    private string PlanTitle()
    {
        if (agent.GoalMet) return "<b>Plan:</b> goal reached, idle";
        if (agent.Plan.Count == 0) return "<b>Plan:</b> none";

        float cost = 0f;
        foreach (GoapAction action in agent.Plan) cost += action.cost;
        return $"<b>Plan</b>  (total cost {cost:0.#})";
    }

    private string PlanLine(int index)
    {
        string name = agent.Plan[index].name;
        if (index < agent.Step) return $"<color=#808080>x  {name}</color>";
        if (index == agent.Step) return $"<color=#ffd24a>>  {name}</color>";
        return $"-  {name}";
    }

    // Names above the places the actions happen, and what the agent is doing above the agent.
    private void DrawWorldLabels()
    {
        foreach (GoapAction action in agent.actions)
            if (action.target != null)
                DrawWorldLabel(action.target.position + Vector3.down * 1f, action.target.name);

        string doing = agent.Current != null ? agent.Current.name : agent.GoalMet ? "Idle" : "Planning...";
        DrawWorldLabel(agent.transform.position + Vector3.up * 0.8f, doing);
    }

    private void DrawWorldLabel(Vector3 worldPosition, string text)
    {
        if (Camera.main == null) return;

        Vector3 screen = Camera.main.WorldToScreenPoint(worldPosition);
        var rect = new Rect(screen.x - 80f, Screen.height - screen.y - 12f, 160f, 24f);

        _tag.normal.textColor = Color.black;
        GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), text, _tag);
        _tag.normal.textColor = Color.white;
        GUI.Label(rect, text, _tag);
    }
}
