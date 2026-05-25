using UnityEngine;

// 임시 보스 활성화 트리거.
// 플레이어가 triggerObject의 Y값을 넘어서면 bossObject.SetActive(true).
// 보스는 씬에 미리 비활성 상태로 배치돼 있어야 함 (인스펙터 SerializeField 참조를 살리기 위해서).
// 실제 게임에선 BossRoomLoader가 방 진입 시 처리할 자리.
public class TestScripts : MonoBehaviour
{
    [Header("활성화할 보스")]
    [SerializeField] private GameObject bossObject;          // 씬에 미리 배치한 비활성 보스 GameObject

    [Header("트리거 오브젝트 (이 Y값을 플레이어가 넘으면 발동)")]
    [SerializeField] private Transform triggerObject;        // 임시 라인 오브젝트의 Transform

    [Header("플레이어 (비우면 GridPlayer를 씬에서 자동 검색)")]
    [SerializeField] private Transform player;

    private bool triggered;                                  // 한 번만 발동하도록

    void Start()
    {
        if (player == null)
        {
            var p = FindFirstObjectByType<GridPlayer>();
            if (p != null) player = p.transform;
        }
    }

    void Update()
    {
        if (triggered) return;
        if (bossObject == null || triggerObject == null || player == null) return;

        if (Input.GetKeyDown(KeyCode.F9))
        {
            ActivateBoss();
        }
    }

    public void ActivateBoss()
    {
        if (triggered) return;
        triggered = true;
        bossObject.SetActive(true);
        Debug.Log($"<color=cyan>[테스트]</color> 플레이어가 라인을 넘음 → 보스 활성화");
    }
}
