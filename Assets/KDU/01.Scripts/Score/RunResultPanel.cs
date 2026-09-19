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

    private Coroutine _showRoutine;

    private void OnEnable()
    {
        GameEvents.OnScoreFinalized += OnScoreFinalized;
    }

    private void OnDisable()
    {
        GameEvents.OnScoreFinalized -= OnScoreFinalized;
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

        _showRoutine = StartCoroutine(ShowAfterDelay(entry));
    }

    private IEnumerator ShowAfterDelay(ScoreEntry entry)
    {
        if (showDelay > 0f)
            yield return new WaitForSecondsRealtime(showDelay);

        if (detailPopup != null)
            detailPopup.Open(entry, PlayerProfile.DisplayName);

        _showRoutine = null;
    }
}
