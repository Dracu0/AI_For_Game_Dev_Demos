using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Profile;
#endif

/// <summary>
/// Fills a TMP dropdown with scene names from the active build profile, in build order.
/// </summary>
public class DropDownHandler : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI TextBox;
    [Tooltip("First build index to include. 1 skips index 0, 2 skips indexes 0 and 1.")]
    [SerializeField] int minBuildIndex;

    void Start()
    {
        TMP_Dropdown dropdown = GetComponent<TMP_Dropdown>();
        if (dropdown == null)
            return;

        dropdown.options.Clear();
        foreach (string name in GetBuildSceneNames())
            dropdown.options.Add(new TMP_Dropdown.OptionData(name));

        dropdown.RefreshShownValue();
        dropdown.onValueChanged.AddListener(_ => ShowSelected(dropdown));
        ShowSelected(dropdown);
    }

    IEnumerable<string> GetBuildSceneNames()
    {
#if UNITY_EDITOR
        BuildProfile profile = BuildProfile.GetActiveBuildProfile();
        EditorBuildSettingsScene[] scenes = profile != null
            ? profile.GetScenesForBuild()
            : EditorBuildSettings.scenes;

        for (int i = 0; i < scenes.Length; i++)
        {
            if (i < minBuildIndex)
                continue;

            EditorBuildSettingsScene scene = scenes[i];
            if (!scene.enabled || string.IsNullOrEmpty(scene.path))
                continue;

            yield return FormatSceneName(Path.GetFileNameWithoutExtension(scene.path));
        }
#else
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            if (i < minBuildIndex)
                continue;

            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (string.IsNullOrEmpty(path))
                continue;

            yield return FormatSceneName(Path.GetFileNameWithoutExtension(path));
        }
#endif
    }

    static string FormatSceneName(string sceneName) =>
        sceneName.Replace('_', ' ');

    void ShowSelected(TMP_Dropdown dropdown)
    {
        if (TextBox == null || dropdown.options.Count == 0)
            return;

        DemoInput.SetStatus(TextBox, dropdown.options[dropdown.value].text);
    }
}
