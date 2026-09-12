using System.Collections.Generic;
using UnityEngine;

using TMPro;

public class DropDownHandler : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI TextBox;

    void Start()
    {
        var dropdown = GetComponent<TMP_Dropdown>();

        dropdown.options.Clear();

        List<string> items = new List<string>();

        items.Add("Djikstra VS A*");
        items.Add("Seek And Flee");

        foreach(var item in items)
        {
            dropdown.options.Add(new TMP_Dropdown.OptionData() { text = item});
        }

        DropdownItemSelected(dropdown);
        dropdown.onValueChanged.AddListener(delegate { DropdownItemSelected(dropdown); });
    }

    void DropdownItemSelected(TMP_Dropdown dropdown)
    {
        int index = dropdown.value;

        TextBox.text = dropdown.options[index].text; 
    }
}
