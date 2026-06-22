using UnityEngine;

// 지정한 층에서만 오브젝트를 활성화 (배경 타일맵 등)
public class TilemapFloorGate : MonoBehaviour
{
    [Tooltip("이 층에서만 켤 대상 (비우면 자기 자신)")]
    [SerializeField] private GameObject target;

    [Tooltip("활성화할 층")]
    [SerializeField] private int activeFloor = 15;

    [Tooltip("RunState/MapGenerator 없을 때(테스트 씬) 무시하고 켬")]
    [SerializeField] private bool showIfNoRun = true;

    // Awake에서 구독 — target이 자기 자신이라 비활성돼도 OnDisable 언구독으로 죽지 않게.
    private void Awake()
    {
        if (target == null)
            target = gameObject;

        GameEvents.OnFloorReset += Apply;
    }

    private void OnDestroy()
    {
        GameEvents.OnFloorReset -= Apply;
    }

    private void Start()
    {
        Apply();
    }

    // 매 층 생성(OnFloorReset)마다 재평가 — Start 1회로는 15층 도달 시 못 켬
    private void Apply()
    {
        if (target != null)
            target.SetActive(IsActiveFloor());
    }

    // 현재 층이 발동 층인지 — RunState 우선, 없으면 MapGenerator
    private bool IsActiveFloor()
    {
        if (RunState.Instance != null)
            return RunState.Instance.CurrentDepth == activeFloor;

        if (MapGenerator.Instance != null)
            return MapGenerator.Instance.CurrentDepth == activeFloor;

        return showIfNoRun;
    }
}
