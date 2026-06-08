using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct StageBossDialogMapping
{
    [Tooltip("층 번호 (예: 5, 10, 15)")]
    public int floor;
    [Tooltip("해당 층의 대화 데이터")]
    public DialogData dialogData;
    [Tooltip("해당 층의 보스 GameObject")]
    public GameObject bossObject;
}

// 임시 보스 활성화 트리거.
// 플레이어가 보스의 AttackFloor를 밟으면 bossObject.SetActive(true) 및 대화 실행.
// 보스는 씬에 미리 비활성 상태로 배치돼 있어야 함 (인스펙터 SerializeField 참조를 살리기 위해서).
// 실제 게임에선 BossRoomLoader가 방 진입 시 처리할 자리.
public class TestScripts : MonoBehaviour
{
    [Header("기본 폴백 설정")]
    [SerializeField] private GameObject bossObject;          // 씬에 미리 배치한 비활성 보스 GameObject (폴백)
    [SerializeField] private DialogData dialogData;          // 스크립터블 오브젝트 대화 데이터 (폴백)

    [Header("층별 보스 및 대화 데이터 매핑")]
    [SerializeField] private List<StageBossDialogMapping> stageDialogs = new List<StageBossDialogMapping>();

    [Header("다이얼로그 매니저")]
    [SerializeField] private DialogManager dialogManager;

    [Header("플레이어 (비우면 GridPlayer를 씬에서 자동 검색)")]
    [SerializeField] private GridPlayer player;

    private GameObject activeBossObject;
    private DialogData activeDialogData;
    private bool triggered = false;                     

    void Start()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<GridPlayer>();
        }

        if (dialogManager == null)
        {
            // 비활성화(SetActive(false)) 상태의 오브젝트도 감지함
            dialogManager = FindFirstObjectByType<DialogManager>(FindObjectsInactive.Include);
        }

        // 현재 층(Depth) 조회 및 연관 데이터 세팅
        int currentFloor = 5; // 기본값
        if (MapGenerator.Instance != null)
        {
            currentFloor = MapGenerator.Instance.CurrentDepth;
        }

        StageBossDialogMapping mapping = stageDialogs.Find(m => m.floor == currentFloor);
        if (mapping.dialogData != null || mapping.bossObject != null)
        {
            activeBossObject = mapping.bossObject;
            activeDialogData = mapping.dialogData;
            Debug.Log($"<color=cyan>[테스트]</color> {currentFloor}층 매핑 데이터 적용 (보스: {activeBossObject?.name}, 대화: {activeDialogData?.name})");
        }
        else
        {
            activeBossObject = bossObject;
            activeDialogData = dialogData;
            Debug.Log($"<color=cyan>[테스트]</color> {currentFloor}층 매핑이 없어 폴백 데이터 사용 (보스: {activeBossObject?.name}, 대화: {activeDialogData?.name})");
        }

        // 모든 보스 오브젝트 비활성화 보장
        if (activeBossObject != null)
        {
            activeBossObject.SetActive(false);
        }

        foreach (var m in stageDialogs)
        {
            if (m.bossObject != null && m.bossObject != activeBossObject)
            {
                m.bossObject.SetActive(false);
            }
        }
        if (bossObject != null && bossObject != activeBossObject)
        {
            bossObject.SetActive(false);
        }
    }

    void Update()
    {
        if (triggered) return;

        // 플레이어가 활성화된 보스의 AttackFloor를 밟으면 트리거 작동
        if (player != null && activeBossObject != null)
        {
            BossBase bossComp = activeBossObject.GetComponent<BossBase>();
            if (bossComp != null && bossComp.AttackFloorTilemap != null)
            {
                Vector2Int pPos = player.GridPosition;
                if (bossComp.AttackFloorTilemap.HasTile(new Vector3Int(pPos.x, pPos.y, 0)))
                {
                    TriggerDialogSequence();
                }
            }
        }
    }

    private void TriggerDialogSequence()
    {
        triggered = true;

        if (dialogManager != null)
        {
            dialogManager.OnDialogComplete = () =>
            {
                if (activeBossObject != null)
                {
                    activeBossObject.SetActive(true);
                    Debug.Log($"<color=cyan>[테스트]</color> 대화 종료 → 보스 활성화 완료 ({activeBossObject.name})");
                }
            };
            dialogManager.StartDialog(activeDialogData); // 다이얼로그 매니저에 데이터 전달 및 시작   
            Debug.Log($"<color=cyan>[테스트]</color> 플레이어가 AttackFloor에 진입 → 다이얼로그 개시");
        }
        else
        {
            if (activeBossObject != null)
            {
                activeBossObject.SetActive(true);
                Debug.LogWarning($"TestScripts: DialogManager가 없어 보스({activeBossObject.name})를 즉시 소환했습니다.");
            }
        }
    }
}
