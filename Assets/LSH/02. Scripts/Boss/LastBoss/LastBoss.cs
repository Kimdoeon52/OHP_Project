using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using DG.Tweening;
using System.Threading;

/// <summary>
/// 최종 보스 하데스(LastBoss)의 메인 컨트롤러 클래스입니다.
/// 좌측 손과 우측 손의 상태 및 체력을 통합 관리합니다.
/// </summary>
public class LastBoss : BossBase
{
    [Header("하데스 손 리스트 (인스펙터 할당 필수, 총 4개)")]
    [SerializeField] private List<LastBossHand> hands = new List<LastBossHand>();

    [Header("각 손의 최대 체력")]
    [SerializeField] private int handMaxHp = 100;

    [Header("공유 손 HP 바 UI (하나로 표시)")]
    [SerializeField] private HandHpBar sharedHandHpBar;

    [Header("머리 페이즈 설정")]
    [SerializeField] private LastBossHead spawnedHead; // 씬에 사전 배치된 머리 오브젝트 참조
    [SerializeField] private int headMaxHp = 200;
    [SerializeField] private int headSpawnYOffset = 1; // 머리 소환 Y 오프셋 (기본 1: 기존 대비 1칸 위로 소환)
    
    private bool isHeadPhaseStarted = false;
    private bool isHeadDead = false;


    protected override void OnEnable()
    {
        base.OnEnable();
    }

    private void Start()
    {
        if (spawnedHead != null)
        {
            spawnedHead.gameObject.SetActive(false);
        }
        CacheMapBounds();
        SpawnAndInitializeHands();
    }

    private void SpawnAndInitializeHands()
    {
        List<Vector2Int> perimeter = GeneratePerimeterPath();
        if (perimeter == null || perimeter.Count == 0)
        {
            Debug.LogError("LastBoss: Perimeter path generation failed.");
            return;
        }

        // 안전장치: 손 참조 유실 시 자동 복구
        if (hands == null || hands.Count == 0)
        {
            hands = new List<LastBossHand>(GetComponentsInChildren<LastBossHand>(true));
        }

        if (hands.Count == 0)
        {
            Debug.LogError("<color=red>[하데스]</color> hands 리스트가 인스펙터에 할당되지 않았으며 자동 감지에도 실패했습니다!");
        }

        // 안전장치: 공유 손 HP 바 참조 유실 시 자동 복구 (비활성화된 오브젝트 포함)
        if (sharedHandHpBar == null)
        {
            sharedHandHpBar = FindFirstObjectByType<HandHpBar>(FindObjectsInactive.Include);
        }

        int leftHp = handMaxHp;
        int combinedMaxHp = 0;

        // mapBounds를 설정하고, 맵 중앙 가로축 계산
        if (!hasMapBounds) CacheMapBounds();
        int centerX = mapBounds.xMin + mapBounds.width / 2;

        int numHands = hands.Count;
        for (int i = 0; i < numHands; i++)
        {
            LastBossHand hand = hands[i];
            if (hand == null) continue;

            hand.gameObject.SetActive(true);
            hand.enabled = true; // 스크립트 활성화 보장

            // 외곽 경로에 균등 배치
            int startIndex = (perimeter.Count * i) / numHands;
            Vector2Int startPos = perimeter[startIndex];

            // 시작 좌표 기준으로 왼쪽에 있으면 Right(플레이어 기준 왼쪽), 오른쪽에 있으면 Left(플레이어 기준 오른쪽)
            LastBossHand.HandSide handSide = (startPos.x < centerX) ? LastBossHand.HandSide.Right : LastBossHand.HandSide.Left;

            // Player 기준 오른쪽(Left)은 시계방향, Player 기준 왼쪽(Right)은 반시계방향
            bool clockwise = (handSide == LastBossHand.HandSide.Left);

            int hp = hand.maxHp > 0 ? hand.maxHp : leftHp;
            combinedMaxHp += hp;

            if (sharedHandHpBar != null)
            {
                hand.HandHpBar = sharedHandHpBar;
            }

            hand.InitHand(this, handSide, hp, perimeter, startIndex, clockwise);
        }

        // Combined HP 설정 (모든 손의 합산 체력)
        maxHp = combinedMaxHp;
        currentHp = maxHp;

        if (hpBar != null)
        {
            if (hpBar.allHpBar != null)
            {
                hpBar.allHpBar.SetActive(true); // 손 페이즈 시작할 때 공통 HP 바 UI 노출
            }
            if (hands.Count > 0 && hands[0] != null)
            {
                hpBar.Init(hands[0]); // 첫 번째 활성 손을 초기 타겟으로 설정
            }
            hpBar.UpdateBar();
        }
    }

    private List<Vector2Int> GeneratePerimeterPath()
    {
        int minX = int.MaxValue;
        int maxX = int.MinValue;
        int minY = int.MaxValue;
        int maxY = int.MinValue;

        bool found = false;
        if (GridSystem.Instance != null)
        {
            foreach (var cell in GridSystem.Instance.GetAllWalkableCells())
            {
                if (IsInAttackFloor(cell))
                {
                    found = true;
                    if (cell.x < minX) minX = cell.x;
                    if (cell.x > maxX) maxX = cell.x;
                    if (cell.y < minY) minY = cell.y;
                    if (cell.y > maxY) maxY = cell.y;
                }
            }
        }

        if (!found)
        {
            minX = mapBounds.xMin;
            maxX = mapBounds.xMax - 1;
            minY = mapBounds.yMin;
            maxY = mapBounds.yMax - 1;
        }

        // 외곽 1셀 확장 좌표 생성
        int extMinX = minX - 1;
        int extMaxX = maxX + 1;
        int extMinY = minY - 1;
        int extMaxY = maxY + 1;

        List<Vector2Int> path = new List<Vector2Int>();

        // Left edge (bottom to top)
        for (int y = extMinY; y <= extMaxY; y++)
            path.Add(new Vector2Int(extMinX, y));

        // Top edge (left to right)
        for (int x = extMinX + 1; x <= extMaxX; x++)
            path.Add(new Vector2Int(x, extMaxY));

        // Right edge (top to bottom)
        for (int y = extMaxY - 1; y >= extMinY; y--)
            path.Add(new Vector2Int(extMaxX, y));

        // Bottom edge (right to left)
        for (int x = extMaxX - 1; x > extMinX; x--)
            path.Add(new Vector2Int(x, extMinY));

        return path;
    }

    public void OnHandDamaged(int amount)
    {
        // 데미지 알림 — 현재 HP 기반 페이즈 전환 조건 없음
    }

    public void OnHandDefeated(LastBossHand hand)
    {
        // 어떤 손이든 하나 제거되면 머리에 운석 낙하 신호 보냄
        if (spawnedHead != null && spawnedHead.gameObject.activeSelf)
        {
            spawnedHead.ActivateMeteorRain();
        }

        if (!isHeadPhaseStarted)
        {
            // 손이 하나라도 제거되면 즉시 머리 페이즈 시작
            LastBossHand remainingHand = null;
            foreach (var h in hands)
            {
                if (h != null && h != hand && !h.IsDead)
                {
                    remainingHand = h;
                    break;
                }
            }

            // HP 바 타겟을 남은 손 중 하나로 전환 (있으면)
            if (hpBar != null && remainingHand != null)
            {
                hpBar.Init(remainingHand);
                hpBar.UpdateBar();
            }

            StartHeadPhase().Forget();
        }
        else
        {
            // 머리 페이즈 진행 중일 때는 최종 클리어 조건 체크
            CheckFinalClearCondition();
        }
    }

    public void OnHeadDefeated()
    {
        isHeadDead = true;
        CheckFinalClearCondition();
    }

    private void CheckFinalClearCondition()
    {
        if (!isHeadDead) return;

        bool allHandsDead = true;
        foreach (var hand in hands)
        {
            if (hand != null && !hand.IsDead)
            {
                allHandsDead = false;
                break;
            }
        }

        if (allHandsDead && !isDying)
        {
            isDying = true;
            TriggerFinalClear().Forget();
        }
    }

    private async UniTaskVoid TriggerFinalClear()
    {
        Debug.Log("<color=red>[하데스]</color> 머리와 모든 손이 제거되었습니다! 보스전 최종 클리어!");

        // 전역 승리 이벤트 호출 (MapGenerator에서 출구를 생성하도록 유도)
        GameEvents.OnBossDefeated?.Invoke();

        // 씬 상의 남은 손들 확실히 삭제
        foreach (var hand in hands)
        {
            if (hand != null)
            {
                Destroy(hand.gameObject);
            }
        }

        // 자신 제거
        Destroy(gameObject);
        await UniTask.CompletedTask;
    }

    private async UniTaskVoid StartHeadPhase()
    {
        if (isHeadPhaseStarted || isDying) return;
        isHeadPhaseStarted = true;

        Debug.Log("<color=red>[하데스]</color> 본체 페이즈로 전환합니다. 연출 시작!");

        // SpriteRenderer 알파를 0으로 천천히 내리기 (1.5초)
        // 단, 손(LastBossHand)에 속한 SpriteRenderer는 투명해지지 않도록 제외합니다.
        var srs = GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in srs)
        {
            if (sr != null)
            {
                if (sr.GetComponentInParent<LastBossHand>() != null)
                    continue;

                sr.DOFade(0f, 1.5f).SetEase(Ease.InOutSine);
            }
        }

        // 손들이 본체 부모(transform)를 따라 아래로 내려가지 않도록 부모 관계 해제
        foreach (var hand in hands)
        {
            if (hand != null)
            {
                hand.transform.SetParent(null);
            }
        }

        // 1. DOTween으로 아래로 10만큼 내려가는 연출 (부드럽게 이동)
        await transform.DOMoveY(transform.position.y - 10f, 1.5f)
            .SetEase(Ease.InOutSine)
            .AsyncWaitForCompletion();

        if (isDying) return;

        // 2. 맵 최상단 가로 중앙 구역 계산 (실제 플레이어가 도달 가능한 구역 기준)
        int maxY = int.MinValue;
        int minX = int.MaxValue;
        int maxX = int.MinValue;
        bool foundArea = false;

        if (GridSystem.Instance != null)
        {
            foreach (var cell in GridSystem.Instance.GetAllWalkableCells())
            {
                if (IsInAttackFloor(cell))
                {
                    foundArea = true;
                    if (cell.y > maxY) maxY = cell.y;
                    if (cell.x < minX) minX = cell.x;
                    if (cell.x > maxX) maxX = cell.x;
                }
            }
        }

        if (!foundArea)
        {
            // 폴백 (예외 방지)
            minX = mapBounds.xMin;
            maxX = mapBounds.xMax - 1;
            maxY = mapBounds.yMax - 1;
        }

        // 플레이어가 최상단 바닥 줄(maxY)에 있으면, 머리 소환 및 격자 블록 시점 전에 안전하게 한 칸 아래(maxY - 1)로 이동시킵니다.
        if (player != null && !player.IsDead && player.GridPosition.y >= maxY)
        {
            Vector2Int newPlayerPos = new Vector2Int(player.GridPosition.x, maxY - 1);
            player.SetGridPosition(newPlayerPos);
            if (VisionSystem.Instance != null)
            {
                VisionSystem.Instance.UpdateVision(newPlayerPos);
            }
            Debug.Log($"<color=orange>[하데스]</color> 플레이어가 최상단 바닥 줄에 있어 {newPlayerPos}로 위치를 재조정했습니다.");
        }

        int centerX = minX + (maxX - minX) / 2;
        int topY = maxY + headSpawnYOffset;
        Vector2Int headGridPos = new Vector2Int(centerX - 2, topY);

        if (spawnedHead != null)
        {
            spawnedHead.gameObject.SetActive(true);
            spawnedHead.Init(this, headGridPos, headMaxHp, warningTilemap, warningTile);
        }
        else
        {
            Debug.LogError("<color=red>[하데스]</color> spawnedHead가 인스펙터에 할당되지 않았습니다!");
        }

        // 3. 체력 및 HP바 설정
        if (hpBar == null)
        {
            hpBar = FindFirstObjectByType<BossHpBar>(FindObjectsInactive.Include);
        }

        if (hpBar != null)
        {
            if (hpBar.allHpBar != null)
            {
                hpBar.allHpBar.SetActive(true); // 머리 페이즈 시작 시 메인 HP 바 UI 활성화!
            }
            hpBar.Init(spawnedHead); // 타겟을 LastBossHead 본체로 설정
            hpBar.UpdateBar();
        }
    }

    protected override UniTask PhaseState()
    {
        // 양 손이 독자적으로 동작하므로 메인 루프 바이패스
        return UniTask.CompletedTask;
    }

    protected override async UniTask OnDeath()
    {
        foreach (var hand in hands)
        {
            if (hand != null) Destroy(hand.gameObject);
        }

        await base.OnDeath();
        Destroy(gameObject);
    }
}


