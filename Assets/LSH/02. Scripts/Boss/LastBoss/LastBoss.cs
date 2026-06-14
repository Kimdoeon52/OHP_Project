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

    [Header("각 손 전용 HP 바 UI")]
    [SerializeField] private BossHpBar leftHandHpBar;
    [SerializeField] private BossHpBar rightHandHpBar;

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

        if (leftHand == null)
        {
            Debug.LogError("<color=red>[하데스]</color> leftHand가 인스펙터에 할당되지 않았습니다!");
        }
        if (rightHand == null)
        {
            Debug.LogError("<color=red>[하데스]</color> rightHand가 인스펙터에 할당되지 않았습니다!");
        }

        // Left Hand 초기화
        if (leftHand != null)
        {
            leftHand.gameObject.SetActive(true);
            leftHand.enabled = true; // 스크립트 활성화 보장
            if (leftHandHpBar != null) leftHand.HpBar = leftHandHpBar;
            leftHand.InitHand(this, LastBossHand.HandSide.Left, handMaxHp, perimeter, 0, false);
        }

        // Right Hand 초기화
        if (rightHand != null)
        {
            rightHand.gameObject.SetActive(true);
            rightHand.enabled = true; // 스크립트 활성화 보장
            if (rightHandHpBar != null) rightHand.HpBar = rightHandHpBar;
            rightHand.InitHand(this, LastBossHand.HandSide.Right, handMaxHp, perimeter, perimeter.Count / 2, true);
        }

        // Combined HP 설정
        maxHp = handMaxHp * 2;
        currentHp = maxHp;
        if (hpBar != null) hpBar.UpdateBar();
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
        int beforeHp = currentHp;
        currentHp = Mathf.Max(0, currentHp - amount);

        if (hpBar != null) hpBar.UpdateBar();

        if (currentHp <= 0 && !isDying)
        {
            isDying = true;
            OnDeath().Forget();
        }
    }

    public void OnHandDefeated(LastBossHand hand)
    {
        if (hand == leftHand) leftHand = null;
        if (hand == rightHand) rightHand = null;

        if (leftHand == null && rightHand == null)
        {
            if (!isDying)
            {
                isDying = true;
                OnDeath().Forget();
            }
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
