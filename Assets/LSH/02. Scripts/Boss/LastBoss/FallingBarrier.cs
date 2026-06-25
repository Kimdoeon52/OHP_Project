using System.Collections;
using UnityEngine;

// 플레이어가 트리거 셀을 넘어가면 오브젝트가 떨어져 길을 막는 일방통행 장벽
public class FallingBarrier : MonoBehaviour
{
    [Header("Trigger")]
    [Tooltip("이 위치의 Y 칸이 기준선 — 플레이어 Y가 더 커지면 발동")]
    [SerializeField] private Transform triggerLine;

    [Tooltip("이 층에서만 발동")]
    [SerializeField] private int activeFloor = 15;

    [Tooltip("RunState/MapGenerator 없을 때(테스트 씬) 층 무시하고 발동")]
    [SerializeField] private bool ignoreFloorIfNoRun = true;

    [Header("Barrier")]
    [Tooltip("떨어져서 길을 막을 오브젝트 (배치된 최종 위치)")]
    [SerializeField] private Transform[] fallingObjects;

    [Tooltip("오브젝트 하나가 차단할 셀 크기 (가로x세로)")]
    [SerializeField] private Vector2Int barrierSize = new Vector2Int(4, 2);

    [Tooltip("떨어지기 시작하는 높이")]
    [SerializeField] private float fallHeight = 6f;

    [Tooltip("낙하 시간(초)")]
    [SerializeField] private float fallDuration = 0.25f;

    [Tooltip("오브젝트 간 낙하 간격(초)")]
    [SerializeField] private float fallStagger = 0.05f;

    private GridPlayer _player;
    private int _triggerY;
    private bool _triggered;

    private void Start()
    {
        _player = FindFirstObjectByType<GridPlayer>();

        // 기준 Y 칸 캐시
        _triggerY = GridSystem.Instance.WorldToGrid(triggerLine.position).y;

        // 재진입 대비 — 이전 발동에서 막힌 셀 초기화 (GridSystem은 싱글톤이라 잔류)
        foreach (var obj in fallingObjects)
        {
            if (obj != null)
                ForEachBarrierCell(obj, GridSystem.Instance.UnblockCell);
        }

        // 발동 전엔 장벽 숨김
        foreach (var obj in fallingObjects)
        {
            if (obj != null)
                obj.gameObject.SetActive(false);
        }
    }

    // 오브젝트 기준 셀부터 barrierSize 사각 영역에 동작 적용
    private void ForEachBarrierCell(Transform obj, System.Action<Vector2Int> action)
    {
        Vector2Int origin = GridSystem.Instance.WorldToGrid(obj.position);
        for (int dx = 0; dx < barrierSize.x; dx++)
        {
            for (int dy = 0; dy < barrierSize.y; dy++)
                action(origin + new Vector2Int(dx, dy));
        }
    }

    private void Update()
    {
        if (_triggered || _player == null || _player.IsDead)
            return;

        if (!IsActiveFloor())
            return;

        if (_player.GridPosition.y > _triggerY)
        {
            _triggered = true;
            StartCoroutine(DropBarrier());
        }
    }

    // 현재 층이 발동 층인지 — RunState 우선, 없으면 MapGenerator
    private bool IsActiveFloor()
    {
        if (RunState.Instance != null)
            return RunState.Instance.CurrentDepth == activeFloor;

        if (MapGenerator.Instance != null)
            return MapGenerator.Instance.CurrentDepth == activeFloor;

        return ignoreFloorIfNoRun;
    }

    private IEnumerator DropBarrier()
    {
        foreach (var obj in fallingObjects)
        {
            if (obj == null)
                continue;

            // 셀 즉시 차단 — 떨어지는 도중에도 되돌아가지 못함
            ForEachBarrierCell(obj, GridSystem.Instance.BlockCell);

            StartCoroutine(DropOne(obj));

            if (fallStagger > 0f)
                yield return new WaitForSeconds(fallStagger);
        }
    }

    private IEnumerator DropOne(Transform obj)
    {
        Vector3 target = obj.position;
        Vector3 start = target + Vector3.up * fallHeight;

        obj.position = start;
        obj.gameObject.SetActive(true);

        float elapsed = 0f;
        while (elapsed < fallDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fallDuration);
            // 가속 낙하 (ease in)
            obj.position = Vector3.Lerp(start, target, t * t);
            yield return null;
        }

        obj.position = target;
    }
}
