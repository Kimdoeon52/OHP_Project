using System.Collections;
using UnityEngine;

// 런 종료 결산 — 기록 상세 팝업을 그대로 씀
// 항상 켜져 있는 오브젝트에 부착 (팝업은 꺼져 있어도 이벤트를 받아야 함)
public class RunResultPanel : MonoBehaviour
{
    [Tooltip("결산에 쓸 기록 상세 팝업")]
    [SerializeField] private ScoreDetailPopup detailPopup;

    [Tooltip("점수 확정 후 표시까지 지연 (사망 연출용)")]
    [SerializeField, Min(0f)] private float showDelay;

    [Tooltip("클리어 시 표시까지 지연")]
    [SerializeField, Min(0f)] private float clearShowDelay = 0.5f;

    // 씬에 결산 패널이 있는지 — 없으면 클리어 즉시 엔딩
    public static bool IsActive { get; private set; }

    private Coroutine _showRoutine;
    private bool _waitingClose;

    private void OnEnable()
    {
        IsActive = true;
        GameEvents.OnScoreFinalized += OnScoreFinalized;
    }

    private void OnDisable()
    {
        IsActive = false;
        GameEvents.OnScoreFinalized -= OnScoreFinalized;
        StopWaitingClose();
    }

    public void Close()
    {
        if (_showRoutine != null)
        {
            StopCoroutine(_showRoutine);
            _showRoutine = null;
        }

        if (detailPopup != null)
            detailPopup.Close();
    }

    // 점수 확정 — 기록 1줄을 만들어 지연 후 팝업으로 표시
    private void OnScoreFinalized(ScoreResult result)
    {
        ScoreEntry entry = Scoreboard.BuildEntry(result, SaveManager.BuildEquipped(), SaveManager.BuildChallenges());

        if (_showRoutine != null)
            StopCoroutine(_showRoutine);

        _showRoutine = StartCoroutine(ShowAfterDelay(entry, result.cleared));
    }

    private IEnumerator ShowAfterDelay(ScoreEntry entry, bool cleared)
    {
        float delay = cleared ? clearShowDelay : showDelay;
        if (delay > 0f)
            yield return new WaitForSecondsRealtime(delay);

        _showRoutine = null;

        if (detailPopup == null)
        {
            if (cleared)
                GameEvents.OnRunResultClosed?.Invoke();
            yield break;
        }

        detailPopup.Open(entry, PlayerProfile.DisplayName);

        // 클리어 결산은 닫으면 엔딩으로
        if (cleared && !_waitingClose)
        {
            _waitingClose = true;
            detailPopup.Closed += OnClearResultClosed;
        }
    }

    private void OnClearResultClosed()
    {
        StopWaitingClose();
        GameEvents.OnRunResultClosed?.Invoke();
    }

    private void StopWaitingClose()
    {
        if (!_waitingClose)
            return;

        _waitingClose = false;
        if (detailPopup != null)
            detailPopup.Closed -= OnClearResultClosed;
    }
}
