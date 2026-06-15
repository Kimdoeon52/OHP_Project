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
    [Header("하데스 좌/우 손 (인스펙터 할당 필수)")]
    [SerializeField] private LastBossHand leftHand;
    [SerializeField] private LastBossHand rightHand;

    [Header("각 손의 최대 체력")]
    [SerializeField] private int handMaxHp = 100;

    [Header("공유 손 HP 바 UI (하나로 표시)")]
    [SerializeField] private HandHpBar sharedHandHpBar;

    [Header("머리 페이즈 설정")]
    [SerializeField] private GameObject headPrefab;
    [SerializeField] private int headMaxHp = 200;
    [SerializeField] private int headSpawnYOffset = 1; // 머리 소환 Y 오프셋 (기본 1: 기존 대비 1칸 위로 소환)
    
    private LastBossHead spawnedHead;
    private bool isHeadPhaseStarted = false;


    protected override void OnEnable()
    {
        base.OnEnable();
    }

    private void Start()
    {
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

        // 안전장치: 좌/우 손 참조 유실 시 자동 복구
        if (leftHand == null || rightHand == null)
        {
            LastBossHand[] hands = GetComponentsInChildren<LastBossHand>(true);
            foreach (var hand in hands)
            {
                string handName = hand.name.ToLower();
                if (handName.Contains("left"))
                {
                    leftHand = hand;
                }
                else if (handName.Contains("right"))
                {
                    rightHand = hand;
                }
            }
            if (hands.Length == 2 && (leftHand == null || rightHand == null))
            {
                leftHand = hands[0];
                rightHand = hands[1];
            }
        }

        if (leftHand == null)
        {
            Debug.LogError("<color=red>[하데스]</color> leftHand가 인스펙터에 할당되지 않았으며 자동 감지에도 실패했습니다!");
        }
        if (rightHand == null)
        {
            Debug.LogError("<color=red>[하데스]</color> rightHand가 인스펙터에 할당되지 않았으며 자동 감지에도 실패했습니다!");
        }

        // 안전장치: 공유 손 HP 바 참조 유실 시 자동 복구 (비활성화된 오브젝트 포함)
        if (sharedHandHpBar == null)
        {
            sharedHandHpBar = FindFirstObjectByType<HandHpBar>(FindObjectsInactive.Include);
        }

        // Left Hand 초기화 (플레이어 기준 좌측에 있으므로 하데스 기준 오른손)
        int leftHp = handMaxHp;
        if (leftHand != null)
        {
            leftHand.gameObject.SetActive(true);
            leftHand.enabled = true; // 스크립트 활성화 보장
            if (leftHand.maxHp > 0) leftHp = leftHand.maxHp;
            if (sharedHandHpBar != null) leftHand.HandHpBar = sharedHandHpBar;
            leftHand.InitHand(this, LastBossHand.HandSide.Right, leftHp, perimeter, 0, false);
        }

        // Right Hand 초기화 (플레이어 기준 우측에 있으므로 하데스 기준 왼손)
        int rightHp = handMaxHp;
        if (rightHand != null)
        {
            rightHand.gameObject.SetActive(true);
            rightHand.enabled = true; // 스크립트 활성화 보장
            if (rightHand.maxHp > 0) rightHp = rightHand.maxHp;
            if (sharedHandHpBar != null) rightHand.HandHpBar = sharedHandHpBar;
            rightHand.InitHand(this, LastBossHand.HandSide.Left, rightHp, perimeter, perimeter.Count / 2, true);
        }

        // Combined HP 설정 (양 손의 합산 체력)
        maxHp = leftHp + rightHp;
        currentHp = maxHp;
        if (hpBar != null)
        {
            hpBar.UpdateBar();
            if (hpBar.allHpBar != null)
            {
                hpBar.allHpBar.SetActive(false); // 손 페이즈 동안 보스 메인 HP 바 UI는 감춤
            }
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
        // 손 페이즈 동안에는 보스 본체 체력바를 갱신하지 않고 비주얼을 감춘 상태로 둡니다.
    }

    public void OnHandDefeated(LastBossHand hand)
    {
        if (hand == leftHand) leftHand = null;
        if (hand == rightHand) rightHand = null;

        if (leftHand == null && rightHand == null)
        {
            StartHeadPhase().Forget();
        }
        else
        {
            // 한쪽 손이 제거됨 -> 아직 살아남은 다른 쪽 손으로 HP 바 타겟 전환
            LastBossHand remainingHand = (leftHand != null) ? leftHand : rightHand;
            if (sharedHandHpBar != null && remainingHand != null)
            {
                sharedHandHpBar.Init(remainingHand);
                sharedHandHpBar.UpdateBar();
            }
        }
    }

    private async UniTaskVoid StartHeadPhase()
    {
        if (isHeadPhaseStarted || isDying) return;
        isHeadPhaseStarted = true;

        Debug.Log("<color=red>[하데스]</color> 양 손 파괴 완료. 본체 페이즈로 전환합니다. 연출 시작!");

        // 1. DOTween으로 아래로 2만큼 내려가는 연출 (부드럽게 이동)
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

        int centerX = minX + (maxX - minX) / 2;
        int topY = maxY + headSpawnYOffset;
        Vector2Int headGridPos = new Vector2Int(centerX - 2, topY);

        if (headPrefab != null)
        {
            GameObject headObj = Instantiate(headPrefab);
            spawnedHead = headObj.GetComponent<LastBossHead>();
            if (spawnedHead != null)
            {
                spawnedHead.Init(this, headGridPos, headMaxHp);
            }
            else
            {
                Debug.LogError("<color=red>[하데스]</color> headPrefab에 LastBossHead 컴포넌트가 존재하지 않습니다!");
            }
        }
        else
        {
            Debug.LogError("<color=red>[하데스]</color> headPrefab이 할당되지 않았습니다!");
        }

        // 3. 체력 및 HP바 설정
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
        if (leftHand != null) Destroy(leftHand.gameObject);
        if (rightHand != null) Destroy(rightHand.gameObject);

        await base.OnDeath();
        Destroy(gameObject);
    }
}


