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

    [Header("다이얼로그 매니저")]
    [SerializeField] private DialogManager dialogManager;

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
        if (dialogManager == null)
        {
            dialogManager = FindFirstObjectByType<DialogManager>();
        }
    }

    void Update()
    {
        if (triggered) return;
        if (bossObject == null || triggerObject == null || player == null) return;

        // F9 디버그용 즉시 소환
        if (Input.GetKeyDown(KeyCode.F9))
        {
            ActivateBoss();
            return;
        }

        // Y값을 플레이어가 넘어서면 대화 시퀀스 트리거
        if (player.position.y > triggerObject.position.y)
        {
            TriggerDialogSequence();
        }
    }

    private void TriggerDialogSequence()
    {
        triggered = true;

        if (dialogManager != null)
        {
            dialogManager.OnDialogComplete = () =>
            {
                bossObject.SetActive(true);
                Debug.Log($"<color=cyan>[테스트]</color> 대화 종료 → 보스 활성화 완료");
            };
            dialogManager.gameObject.SetActive(true);
            Debug.Log($"<color=cyan>[테스트]</color> 플레이어가 라인을 넘음 → 다이얼로그 개시");
        }
        else
        {
            ActivateBoss();
            Debug.LogWarning("TestScripts: DialogManager가 없어 보스를 즉시 소환했습니다.");
        }
    }

    public void ActivateBoss()
    {
        if (triggered) return;
        triggered = true;
        bossObject.SetActive(true);
        Debug.Log($"<color=cyan>[테스트]</color> 보스 즉시 활성화");
    }
}
