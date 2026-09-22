using Steamworks;
using UnityEngine;

// Steam API 초기화·콜백 펌프. 첫 씬 로드 전에 자동 생성
public class SteamBootstrap : PersistentSingleton<SteamBootstrap>
{
    // 출시 앱 ID로 교체 (480 = Spacewar 테스트용)
    private const uint AppId = 480;

    public static bool IsReady => Instance != null && Instance._initialized;

    private bool _initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null)
            return;

        var go = new GameObject(nameof(SteamBootstrap));
        go.AddComponent<SteamBootstrap>();
        go.AddComponent<SteamLeaderboard>();
        go.AddComponent<SteamAchievements>();
    }

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
            return;

        if (!Packsize.Test())
        {
            Debug.LogError("[Steam] Steamworks 바이너리 불일치");
            return;
        }

#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
        // Steam 밖에서 실행되면 Steam 경유로 재실행 (릴리스 빌드만)
        if (SteamAPI.RestartAppIfNecessary(new AppId_t(AppId)))
        {
            Application.Quit();
            return;
        }
#endif

        _initialized = SteamAPI.InitEx(out string error) == ESteamAPIInitResult.k_ESteamAPIInitResult_OK;
        if (!_initialized)
            Debug.LogWarning($"[Steam] 초기화 실패: {error}");
    }

    private void Update()
    {
        if (_initialized)
            SteamAPI.RunCallbacks();
    }

    private void OnApplicationQuit()
    {
        if (!_initialized)
            return;

        SteamAPI.Shutdown();
        _initialized = false;
    }
}
