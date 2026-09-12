using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Fills a TMP dropdown with demo names and copies the selected name to a label.
/// </summary>
public class DropDownHandler : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI TextBox;

    static readonly string[] DemoNames =
    {
        "Dijkstra VS A*",
        "Seek And Flee"
    };

    void Start()
    {
        TMP_Dropdown dropdown = GetComponent<TMP_Dropdown>();
        if (dropdown == null)
            return;

        dropdown.options.Clear();
        foreach (string name in DemoNames)
            dropdown.options.Add(new TMP_Dropdown.OptionData(name));

        dropdown.onValueChanged.AddListener(_ => ShowSelected(dropdown));
        ShowSelected(dropdown);
    }

    void ShowSelected(TMP_Dropdown dropdown)
    {
        if (TextBox == null || dropdown.options.Count == 0)
            return;

        TextBox.text = dropdown.options[dropdown.value].text;
    }
}
