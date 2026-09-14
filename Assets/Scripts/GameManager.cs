using UnityEngine;

/// <summary>
/// Global game settings applied at startup.
/// </summary>
public class GameManager : MonoBehaviour
{
    static GameManager instance;

    [SerializeField] int maxFps = 60;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        Application.targetFrameRate = maxFps;
        DontDestroyOnLoad(gameObject);
    }
}
