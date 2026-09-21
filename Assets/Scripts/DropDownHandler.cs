using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build.Profile;
#endif

/// <summary>
/// Main-menu scene picker. Not part of the AI lessons — students can skip this file.
/// </summary>
public class DropDownHandler : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI TextBox;
    [SerializeField] Button loadButton;
    [Tooltip("First build index to include. 1 skips index 0, 2 skips indexes 0 and 1.")]
    [SerializeField] int minBuildIndex;

    readonly List<string> scenePaths = new List<string>();

    void Start()
    {
        TMP_Dropdown dropdown = GetComponent<TMP_Dropdown>();
        if (dropdown == null)
            return;

        dropdown.options.Clear();
        scenePaths.Clear();

        foreach (string path in GetBuildScenePaths())
        {
            scenePaths.Add(path);
            dropdown.options.Add(new TMP_Dropdown.OptionData(
                FormatSceneName(Path.GetFileNameWithoutExtension(path))));
        }

        dropdown.RefreshShownValue();
        dropdown.onValueChanged.AddListener(_ => ShowSelected(dropdown));
        ShowSelected(dropdown);

        if (loadButton != null)
            loadButton.onClick.AddListener(LoadSelectedScene);
    }

    void OnDestroy()
    {
        if (loadButton != null)
            loadButton.onClick.RemoveListener(LoadSelectedScene);
    }

    IEnumerable<string> GetBuildScenePaths()
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

            yield return scene.path;
        }
#else
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            if (i < minBuildIndex)
                continue;

            string path = SceneUtility.GetScenePathByBuildIndex(i);
            if (string.IsNullOrEmpty(path))
                continue;

            yield return path;
        }
#endif
    }

    void LoadSelectedScene()
    {
        TMP_Dropdown dropdown = GetComponent<TMP_Dropdown>();
        if (dropdown == null || scenePaths.Count == 0)
            return;

        int index = dropdown.value;
        if (index < 0 || index >= scenePaths.Count)
            return;

        SceneManager.LoadScene(Path.GetFileNameWithoutExtension(scenePaths[index]));
    }

    static string FormatSceneName(string sceneName) =>
        sceneName.Replace('_', ' ');

    void ShowSelected(TMP_Dropdown dropdown)
    {
        if (TextBox == null || dropdown.options.Count == 0)
            return;
    }
}
