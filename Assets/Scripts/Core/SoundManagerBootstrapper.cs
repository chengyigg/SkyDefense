using UnityEngine;

public static class SoundManagerBootstrapper
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        if (SoundManager.Instance != null) return;

        GameObject g = new GameObject("[SoundManager]");
        g.AddComponent<SoundManager>();
    }
}