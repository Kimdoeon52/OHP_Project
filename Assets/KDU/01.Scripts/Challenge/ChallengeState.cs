using System.Collections.Generic;

// 현재 런의 활성 챌린지. 1층 시작 시 Configure+Lock, 이후 고정. 점수/효과가 조회.
public class ChallengeState : Singleton<ChallengeState>
{
    private readonly HashSet<ChallengeType> _active = new HashSet<ChallengeType>();

    public bool IsLocked { get; private set; }
    public int ActiveCount => _active.Count;
    public IReadOnlyCollection<ChallengeType> Active => _active;

    public bool IsActive(ChallengeType type)
    {
        return _active.Contains(type);
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

    // 1층 진입 시 호출 — 이후 구성 변경 차단.
    public void Lock()
    {
        IsLocked = true;
    }

    // 새 게임 시작 시 초기화.
    public void ResetForNewRun()
    {
        _active.Clear();
        IsLocked = false;
    }
}
