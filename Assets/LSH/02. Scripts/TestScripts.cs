using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

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

    private int lastFloor = -1;

    [Header("디버그 단축키 설정 (인스펙터 토글 및 전체 주석 처리 가능)")]
    [SerializeField] private bool enableDebugFloorKeys = true;

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
    }

    private void InitializeForFloor(int floor)
    {
        lastFloor = floor;
        triggered = false; // 새로운 층으로 바뀔 때 트리거 리셋

        StageBossDialogMapping mapping = stageDialogs.Find(m => m.floor == floor);
        if (mapping.dialogData != null || mapping.bossObject != null)
        {
            activeBossObject = mapping.bossObject;
            activeDialogData = mapping.dialogData;
            Debug.Log($"<color=cyan>[테스트]</color> {floor}층 매핑 데이터 적용 (보스: {activeBossObject?.name}, 대화: {activeDialogData?.name})");
        }
        else
        {
            activeBossObject = bossObject;
            activeDialogData = dialogData;
            Debug.Log($"<color=cyan>[테스트]</color> {floor}층 매핑이 없어 폴백 데이터 사용 (보스: {activeBossObject?.name}, 대화: {activeDialogData?.name})");
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

        ControlBossBgm(floor);
    }

    void Update()
    {
        // ─── 디버그 단축키 블록 (주석 처리하여 비활성화 가능) ───
        if (enableDebugFloorKeys)
        {
            if (Input.GetKeyDown(KeyCode.F5))
            {
                Debug.Log("<color=yellow>[디버그]</color> F5 입력: 5층으로 즉시 이동합니다.");
                TriggerDebugFloorTransition(5);
            }
            if (Input.GetKeyDown(KeyCode.F6))
            {
                Debug.Log("<color=yellow>[디버그]</color> F6 입력: 10층으로 즉시 이동합니다.");
                TriggerDebugFloorTransition(10);
            }
            if(Input.GetKeyDown(KeyCode.F7))
            {
                Debug.Log("<color=yellow>[디버그]</color> F7 입력: 15층으로 즉시 이동합니다.");
                TriggerDebugFloorTransition(15);
            }
        }
        // ────────────────────────────────────────────────────────

        // 실시간으로 층 번호를 감시
        int currentFloor = 5; // 기본값
        if (RunState.Instance != null)
        {
            currentFloor = RunState.Instance.CurrentDepth;
        }
        else if (MapGenerator.Instance != null)
        {
            currentFloor = MapGenerator.Instance.CurrentDepth;
        }

        if (currentFloor != lastFloor)
        {
            InitializeForFloor(currentFloor);
        }

        if (triggered) return;

        // 플레이어가 활성화된 보스의 AttackFloor를 밟으면 트리거 작동
        if (player != null && activeBossObject != null)
        {
            Tilemap attackFloor = GetBossAttackFloor(activeBossObject);
            if (attackFloor != null)
            {
                Vector2Int pPos = player.GridPosition;
                if (attackFloor.HasTile(new Vector3Int(pPos.x, pPos.y, 0)))
                {
                    TriggerDialogSequence();
                }
            }
        }
    }

    private void TriggerDebugFloorTransition(int targetFloor)
    {
        if (RunState.Instance != null)
        {
            // FloorTransitionController가 TransitionCoroutine에서 AdvanceDepth()를 실행하므로
            // targetFloor - 1로 세팅해놓고 이벤트를 트리거합니다.
            RunState.Instance.ResetRun(RunState.Instance.RunSeed, targetFloor - 1);
            GameEvents.OnLevelComplete?.Invoke();
        }
        else
        {
            Debug.LogWarning("TestScripts: RunState.Instance가 없어 디버그 이동이 불가능합니다.");
        }
    }

    private Tilemap GetBossAttackFloor(GameObject bossObj)
    {
        if (bossObj == null) return null;

        BossBase bossComp = bossObj.GetComponent<BossBase>();
        if (bossComp != null && bossComp.AttackFloorTilemap != null)
        {
            return bossComp.AttackFloorTilemap;
        }

        // 보스가 비활성화 상태이면 OnEnable()이 실행되지 않아 AttackFloorTilemap 필드가 null일 수 있습니다.
        // 비활성화된 자식 오브젝트를 포함하여 타일맵을 직접 검색합니다.
        var tilemaps = bossObj.GetComponentsInChildren<Tilemap>(true);
        foreach (var tm in tilemaps)
        {
            if (tm.name.ToLower().Contains("attack") || tm.name.ToLower().Contains("floor"))
            {
                return tm;
            }
        }

        // 글로벌 맵 생성기의 타일맵 조회
        if (MapGenerator.Instance != null && MapGenerator.Instance.Tilemaps != null && MapGenerator.Instance.Tilemaps.attackFloor != null)
        {
            return MapGenerator.Instance.Tilemaps.attackFloor;
        }

        if (tilemaps.Length > 0) return tilemaps[0];
        return null;
    }

    private void TriggerDialogSequence()
    {
        triggered = true;

        if (dialogManager != null)
        {
            if (activeBossObject != null)
                activeBossObject.SetActive(false);

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

    // ────────────────────────────────────────────────────────
    // 보스 층 BGM 제어 관련 함수
    // ────────────────────────────────────────────────────────
    private void ControlBossBgm(int floor)
    {
        bool isBossFloor = stageDialogs.Exists(m => m.floor == floor && (m.bossObject != null || m.dialogData != null));
        
        // 보스 층이 아니라면 대화 BGM을 정지합니다.
        if (!isBossFloor)
        {
            if (dialogManager != null)
            {
                dialogManager.StopBgm();
            }
        }
    }
}
