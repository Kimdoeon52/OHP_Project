using System.Collections.Generic;
using UnityEngine;

// 현재 런의 활성 챌린지. 1층 시작 시 Configure+Lock, 이후 고정. 점수/효과가 조회.
public class ChallengeState : Singleton<ChallengeState>
{
    // 씬에 인스턴스가 없으면 만들어 보장 — 런 시작부에서 호출.
    public static ChallengeState Ensure()
    {
        if (Instance == null)
            new GameObject(nameof(ChallengeState)).AddComponent<ChallengeState>();

        return Instance;
    }

    private readonly HashSet<ChallengeType> _active = new HashSet<ChallengeType>();

    public bool IsLocked { get; private set; }

    // 일일 챌린지 날짜 키 (일반 런이면 null)
    public string DailyKey { get; private set; }
    public bool IsDaily => !string.IsNullOrEmpty(DailyKey);

    // 시드 직접 입력 런 (로컬·전체 랭킹 제외)
    public bool HasCustomSeed { get; private set; }
    public int ActiveCount => _active.Count;
    public IReadOnlyCollection<ChallengeType> Active => _active;

    public bool IsActive(ChallengeType type)
    {
        return _active.Contains(type);
    }

    // 효과 분기용 정적 단축 — 인스턴스 없으면 false.
    public static bool Has(ChallengeType type)
    {
        return Instance != null && Instance._active.Contains(type);
    }

    // 런 시작 시 1번. 잠긴 후엔 무시.
    public void Configure(IEnumerable<ChallengeType> types)
    {
        if (IsLocked)
            return;

        _active.Clear();
        if (types == null)
            return;

        foreach (var t in types)
            _active.Add(t);
    }

    // 일일 챌린지 런 지정. 잠긴 후엔 무시.
    public void SetDaily(string key)
    {
        if (IsLocked)
            return;

        DailyKey = string.IsNullOrEmpty(key) ? null : key;
    }

    // 시드 직접 입력 런 지정. 잠긴 후엔 무시.
    public void SetCustomSeed(bool custom)
    {
        if (IsLocked)
            return;

        HasCustomSeed = custom;
    }

    // 1층 진입 시 호출 — 이후 구성 변경 차단.
    public void Lock()
    {
        IsLocked = true;
    }

    // 새 게임 시작 시 초기화.
    public void ResetForNewRun()
    {
        _active.Clear();
        DailyKey = null;
        HasCustomSeed = false;
        IsLocked = false;
    }
}
