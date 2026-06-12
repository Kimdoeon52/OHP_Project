using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using DG.Tweening;
using System.Threading;

/// <summary>
/// 최종 보스 하데스(LastBoss)의 행동 패턴 및 비석 기믹을 관리하는 클래스입니다.
/// </summary>
public class LastBoss : BossBase
{
    public enum HadesPattern
    {
        LeftHandSweep,   // 패턴1: 좌측 -> 우측 휩쓸기 (가로 4줄)
        RightHandSweep,  // 패턴2: 우측 -> 좌측 휩쓸기 (가로 4줄)
        BottomHandSweep, // 패턴3: 하단 -> 상단 휩쓸기 (세로 4줄)
        FistSlam         // 패턴4: 주먹 내리찍기 (3x3)
    }

    [System.Serializable]
    public class HadesPatternWeight
    {
        public HadesPattern pattern;
        [Range(0, 100)] public int weight;
    }

    [Header("하데스 전용 설정 (프리팹)")]
    [SerializeField] private GameObject leftHandPrefab;
    [SerializeField] private GameObject rightHandPrefab;
    [SerializeField] private GameObject bottomHandPrefab;
    [SerializeField] private GameObject fistHandPrefab;
    [SerializeField] private GameObject pillarPrefab;
    [UnityEngine.Serialization.FormerlySerializedAs("lavaTilePrefab")]
    [SerializeField] private GameObject lavaPrefab;

    [Header("하데스 패턴 데미지")]
    [SerializeField] private int handSweepDamage = 15;
    [SerializeField] private int fistSlamDamage = 25;
    [SerializeField] private int lavaDamage = 5;

    [Header("하데스 넉백 설정")]
    [SerializeField] private int knockbackDistance = 2;

    [Header("하데스 패턴 가중치")]
    [SerializeField] private List<HadesPatternWeight> patternWeights = new List<HadesPatternWeight>()
    {
        new HadesPatternWeight { pattern = HadesPattern.LeftHandSweep, weight = 25 },
        new HadesPatternWeight { pattern = HadesPattern.RightHandSweep, weight = 25 },
        new HadesPatternWeight { pattern = HadesPattern.BottomHandSweep, weight = 25 },
        new HadesPatternWeight { pattern = HadesPattern.FistSlam, weight = 25 }
    };

    private bool _isPillarDamageIncoming = false;
    private RedPillar _currentPillar;

    protected override void OnEnable()
    {
        base.OnEnable();

        // 맵 범위 캐싱
        CacheMapBounds();

        // 첫 번째 비석 스폰
        SpawnPillar();
    }

    //==================== 데미지 처리 및 비석 관리 ====================
   
    // 비석이 파괴되었을 때
    public void OnPillarDestroyed()
    {
        _currentPillar = null;
        
        // 보스에게 10% 데미지 입힘
        _isPillarDamageIncoming = true;
        BossTakeDamage(Mathf.CeilToInt(maxHp * 0.1f));
        _isPillarDamageIncoming = false;

        if (currentHp > 0 && !isDying)
        {
            // 5초 뒤 랜덤 위치에 리젠
            RespawnPillarAfterDelay().Forget();
        }
    }

    public override void BossTakeDamage(int amount)
    {
        // 붉은 비석 파괴 데미지이거나 K키 디버그 데미지일 때만 받음
        bool isDebug = Input.GetKeyDown(KeyCode.K);

        if (_isPillarDamageIncoming || isDebug)
        {
            base.BossTakeDamage(amount);
        }
        else
        {
            Debug.Log("<color=gray>[하데스]</color> 직접 공격은 받지 않습니다! 붉은 비석을 파괴하세요.");
        }
    }

    private async UniTaskVoid RespawnPillarAfterDelay() //리젠임
    {
        await UniTask.Delay(5000, cancellationToken: cancelToken.Token);
        SpawnPillar();
    }

    private void SpawnPillar()
    {
        if (isDying) return;

        List<Vector2Int> validCells = new List<Vector2Int>();
        foreach (var cell in GridSystem.Instance.GetAllWalkableCells())
        {
            // 플레이어 위치 제외
            if (player != null && cell == player.GridPosition) continue;
            // 이미 다른 오브젝트가 차지한 격자 제외
            if (GridSystem.Instance.IsCellOccupied(cell)) continue;

            validCells.Add(cell);
        }

        if (validCells.Count > 0)
        {
            int randIdx = Random.Range(0, validCells.Count);
            Vector2Int spawnPos = validCells[randIdx];

            if (pillarPrefab != null)
            {
                Vector3 worldPos = GridSystem.Instance.GridToWorld(spawnPos);
                GameObject pObj = Instantiate(pillarPrefab, worldPos, Quaternion.identity);
                _currentPillar = pObj.GetComponent<RedPillar>();
                if (_currentPillar != null)
                {
                    _currentPillar.InitPillar(this, spawnPos);
                }
            }
            else
            {
                Debug.LogError("LastBoss: pillarPrefab이 등록되지 않았습니다.");
            }
        }
        else
        {
            // 스폰할 빈 타일이 없는 경우 0.5초 대기 후 재시도
            RetrySpawnPillar().Forget();
        }
    }

    private async UniTaskVoid RetrySpawnPillar() //비석 리젠
    {
        await UniTask.Delay(500, cancellationToken: cancelToken.Token);
        SpawnPillar();
    }

    //==================== 패턴 선택 및 페이즈 관리 ====================

    private HadesPattern ChooseHadesPattern()
    {
        int totalWeight = 0;
        foreach (var pw in patternWeights) totalWeight += pw.weight;
        if (totalWeight == 0) return HadesPattern.LeftHandSweep;

        int randVal = Random.Range(0, totalWeight);
        int curWeight = 0;
        foreach (var pw in patternWeights)
        {
            curWeight += pw.weight;
            if (randVal < curWeight) return pw.pattern;
        }
        return HadesPattern.LeftHandSweep;
    }

    protected override async UniTask Phase1BossPattern() //Phase 1은 한 개
    {
        isBossAttacking = true;
        HadesPattern pattern = ChooseHadesPattern();

        // 쿨타임(패턴 딜레이) 대기
        await UniTask.Delay(patternDelay, cancellationToken: cancelToken.Token);

        if (!isDying)
        {
            await ExecutePattern(pattern);
        }

        isBossAttacking = false;
    }

    protected override async UniTask Phase2BossPattern() //Phase 2는 2개 동시임
    {
        isBossAttacking = true;

        // 서로 다른 2개 패턴 무작위 선택
        HadesPattern p1 = ChooseHadesPattern();
        HadesPattern p2;
        int maxRetries = 10;
        do
        {
            p2 = ChooseHadesPattern();
            maxRetries--;
        } while (p1 == p2 && maxRetries > 0);

        // 쿨타임 대기 (Phase 2 전환 시 patternDelay는 EnterPhase2에 의해 감소되어 있음)
        await UniTask.Delay(patternDelay, cancellationToken: cancelToken.Token);

        if (!isDying)
        {
            // 2개 패턴 동시 실행
            await UniTask.WhenAll(
                ExecutePattern(p1),
                ExecutePattern(p2)
            );
        }

        isBossAttacking = false;
    }

    private async UniTask ExecutePattern(HadesPattern pattern)
    {
        switch (pattern)
        {
            case HadesPattern.LeftHandSweep:
                await ExecuteLeftHandSweep();
                break;
            case HadesPattern.RightHandSweep:
                await ExecuteRightHandSweep();
                break;
            case HadesPattern.BottomHandSweep:
                await ExecuteBottomHandSweep();
                break;
            case HadesPattern.FistSlam:
                await ExecuteFistSlam();
                break;
        }
    }

    //==================== 패턴 ====================

    // 패턴 1: 좌측 -> 우측으로 한쪽 라인(가로 4줄) 휩쓸기
    private async UniTask ExecuteLeftHandSweep()
    {
        if (!hasMapBounds) CacheMapBounds();

        // 플레이어의 현재 y좌표 기준 가로 4줄 범위 선정
        int playerY = player != null ? player.GridPosition.y : (mapBounds.yMin + mapBounds.height / 2);
        int startY = Mathf.Clamp(playerY - 1, mapBounds.yMin, mapBounds.yMax - 3);

        List<Vector2Int> targetCells = new List<Vector2Int>();
        for (int x = mapBounds.xMin; x <= mapBounds.xMax; x++)
        {
            for (int y = startY; y < startY + 4; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (IsInAttackFloor(cell))
                {
                    targetCells.Add(cell);
                }
            }
        }

        // 1.5초 워닝 표시
        ShowWarning(targetCells);
        await UniTask.Delay(1500, cancellationToken: cancelToken.Token);
        ClearWarning(targetCells);

        if (isDying) return;

        // 시작/종료 위치 월드 좌표 설정
        // y 좌표는 휩쓸기 범위의 중앙 부근으로 설정
        float centerGridY = startY + 1.5f;
        Vector3 startWorld = new Vector3(
            GridSystem.Instance.GridToWorld(new Vector2Int(mapBounds.xMin, 0)).x - 2.5f,
            GridSystem.Instance.GridToWorld(new Vector2Int(0, Mathf.FloorToInt(centerGridY))).y + (centerGridY - Mathf.FloorToInt(centerGridY)) * 1.0f,
            0
        );
        Vector3 endWorld = new Vector3(
            GridSystem.Instance.GridToWorld(new Vector2Int(mapBounds.xMax, 0)).x + 2.5f,
            startWorld.y,
            0
        );

        if (leftHandPrefab != null)
        {
            GameObject hand = Instantiate(leftHandPrefab, startWorld, Quaternion.identity);
            
            // 손 휩쓸기 속도 (0.5초)
            float duration = 0.5f;

            // 지나가면서 플레이어가 있는 x 좌표에 도달했을 때 판정 적용
            float hitDelay = 0.25f;
            if (player != null && targetCells.Contains(player.GridPosition))
            {
                float progress = (float)(player.GridPosition.x - mapBounds.xMin) / (mapBounds.xMax - mapBounds.xMin + 1);
                hitDelay = Mathf.Clamp(progress * duration, 0f, duration);
            }

            // DOTween 이동 시작
            hand.transform.DOMove(endWorld, duration).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                Destroy(hand);
            });

            // 플레이어 히트 타이밍 대기
            await UniTask.Delay((int)(hitDelay * 1000), cancellationToken: cancelToken.Token);

            if (!isDying)
            {
                DamageAndKnockbackPlayer(targetCells, Vector2Int.right, handSweepDamage);
            }

            // 패턴 종료 대기
            float remaining = Mathf.Max(0f, duration - hitDelay);
            if (remaining > 0f)
            {
                await UniTask.Delay((int)(remaining * 1000), cancellationToken: cancelToken.Token);
            }
        }
    }

    // 패턴 2: 우측 -> 좌측으로 한쪽 라인(가로 4줄) 휩쓸기
    private async UniTask ExecuteRightHandSweep()
    {
        if (!hasMapBounds) CacheMapBounds();

        int playerY = player != null ? player.GridPosition.y : (mapBounds.yMin + mapBounds.height / 2);
        int startY = Mathf.Clamp(playerY - 1, mapBounds.yMin, mapBounds.yMax - 3);

        List<Vector2Int> targetCells = new List<Vector2Int>();
        for (int x = mapBounds.xMin; x <= mapBounds.xMax; x++)
        {
            for (int y = startY; y < startY + 4; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (IsInAttackFloor(cell))
                {
                    targetCells.Add(cell);
                }
            }
        }

        ShowWarning(targetCells);
        await UniTask.Delay(1500, cancellationToken: cancelToken.Token);
        ClearWarning(targetCells);

        if (isDying) return;

        float centerGridY = startY + 1.5f;
        Vector3 startWorld = new Vector3(
            GridSystem.Instance.GridToWorld(new Vector2Int(mapBounds.xMax, 0)).x + 2.5f,
            GridSystem.Instance.GridToWorld(new Vector2Int(0, Mathf.FloorToInt(centerGridY))).y + (centerGridY - Mathf.FloorToInt(centerGridY)) * 1.0f,
            0
        );
        Vector3 endWorld = new Vector3(
            GridSystem.Instance.GridToWorld(new Vector2Int(mapBounds.xMin, 0)).x - 2.5f,
            startWorld.y,
            0
        );

        if (rightHandPrefab != null)
        {
            GameObject hand = Instantiate(rightHandPrefab, startWorld, Quaternion.identity);
            float duration = 0.5f;

            float hitDelay = 0.25f;
            if (player != null && targetCells.Contains(player.GridPosition))
            {
                float progress = (float)(mapBounds.xMax - player.GridPosition.x) / (mapBounds.xMax - mapBounds.xMin + 1);
                hitDelay = Mathf.Clamp(progress * duration, 0f, duration);
            }

            hand.transform.DOMove(endWorld, duration).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                Destroy(hand);
            });

            await UniTask.Delay((int)(hitDelay * 1000), cancellationToken: cancelToken.Token);

            if (!isDying)
            {
                DamageAndKnockbackPlayer(targetCells, Vector2Int.left, handSweepDamage);
            }

            float remaining = Mathf.Max(0f, duration - hitDelay);
            if (remaining > 0f)
            {
                await UniTask.Delay((int)(remaining * 1000), cancellationToken: cancelToken.Token);
            }
        }
    }

    // 패턴 3: 하단 -> 상단으로 한쪽 라인(세로 4줄) 휩쓸기

    private async UniTask ExecuteBottomHandSweep()
    {
        if (!hasMapBounds) CacheMapBounds();

        int playerX = player != null ? player.GridPosition.x : (mapBounds.xMin + mapBounds.width / 2);
        int startX = Mathf.Clamp(playerX - 1, mapBounds.xMin, mapBounds.xMax - 3);

        List<Vector2Int> targetCells = new List<Vector2Int>();
        for (int x = startX; x < startX + 4; x++)
        {
            for (int y = mapBounds.yMin; y <= mapBounds.yMax; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (IsInAttackFloor(cell))
                {
                    targetCells.Add(cell);
                }
            }
        }

        ShowWarning(targetCells);
        await UniTask.Delay(1500, cancellationToken: cancelToken.Token);
        ClearWarning(targetCells);

        if (isDying) return;

        float centerGridX = startX + 1.5f;
        Vector3 startWorld = new Vector3(
            GridSystem.Instance.GridToWorld(new Vector2Int(Mathf.FloorToInt(centerGridX), 0)).x + (centerGridX - Mathf.FloorToInt(centerGridX)) * 1.0f,
            GridSystem.Instance.GridToWorld(new Vector2Int(0, mapBounds.yMin)).y - 2.5f,
            0
        );
        Vector3 endWorld = new Vector3(
            startWorld.x,
            GridSystem.Instance.GridToWorld(new Vector2Int(0, mapBounds.yMax)).y + 2.5f,
            0
        );

        if (bottomHandPrefab != null)
        {
            GameObject hand = Instantiate(bottomHandPrefab, startWorld, Quaternion.identity);
            float duration = 0.5f;

            float hitDelay = 0.25f;
            if (player != null && targetCells.Contains(player.GridPosition))
            {
                float progress = (float)(player.GridPosition.y - mapBounds.yMin) / (mapBounds.yMax - mapBounds.yMin + 1);
                hitDelay = Mathf.Clamp(progress * duration, 0f, duration);
            }

            hand.transform.DOMove(endWorld, duration).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                Destroy(hand);
            });

            await UniTask.Delay((int)(hitDelay * 1000), cancellationToken: cancelToken.Token);

            if (!isDying)
            {
                DamageAndKnockbackPlayer(targetCells, Vector2Int.up, handSweepDamage);
            }

            float remaining = Mathf.Max(0f, duration - hitDelay);
            if (remaining > 0f)
            {
                await UniTask.Delay((int)(remaining * 1000), cancellationToken: cancelToken.Token);
            }
        }
    }

    // 패턴 4: 주먹 내리찍기 (3x3 범위 공격 후 2초간 용암 바닥 생성)

    private async UniTask ExecuteFistSlam()
    {
        if (player == null) return;

        Vector2Int center = player.GridPosition;
        List<Vector2Int> targetCells = new List<Vector2Int>();
        for (int x = center.x - 1; x <= center.x + 1; x++)
        {
            for (int y = center.y - 1; y <= center.y + 1; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                if (IsInAttackFloor(cell))
                {
                    targetCells.Add(cell);
                }
            }
        }

        // 1초 워닝 표시
        ShowWarning(targetCells);
        await UniTask.Delay(1000, cancellationToken: cancelToken.Token);
        ClearWarning(targetCells);

        if (isDying) return;

        // 하늘 위에서 수직 하강하는 좌표 설정
        Vector3 hitWorld = GridSystem.Instance.GridToWorld(center);
        Vector3 startWorld = hitWorld + new Vector3(0, 8f, 0);

        if (fistHandPrefab != null)
        {
            GameObject fist = Instantiate(fistHandPrefab, startWorld, Quaternion.identity);

            // 0.25초 만에 강력하게 낙하
            fist.transform.DOMove(hitWorld, 0.25f).SetEase(Ease.InQuad);
            await UniTask.Delay(250, cancellationToken: cancelToken.Token);

            if (!isDying)
            {
                // 타격 판정
                DamagePlayerIfInCells(targetCells, fistSlamDamage);

                // 2초 동안 용암 바닥 생성
                SpawnLavaZone(targetCells);
            }
            
            // 0.2초간 머문 후 다시 상승하면서 소멸
            await UniTask.Delay(200, cancellationToken: cancelToken.Token);
            fist.transform.DOMove(startWorld, 0.25f).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                Destroy(fist);
            });
            
            await UniTask.Delay(250, cancellationToken: cancelToken.Token);
        }
    }

    private void SpawnLavaZone(List<Vector2Int> cells)
    {
        if (lavaPrefab == null) return;

        // 타일들의 중앙 좌표를 계산합니다.
        Vector3 spawnPos = Vector3.zero;
        if (cells.Count > 0 && GridSystem.Instance != null)
        {
            Vector2 sum = Vector2.zero;
            foreach (var cell in cells)
            {
                sum += cell;
            }
            Vector2 avg = sum / cells.Count;
            Vector2Int centerCell = new Vector2Int(Mathf.RoundToInt(avg.x), Mathf.RoundToInt(avg.y));
            spawnPos = GridSystem.Instance.GridToWorld(centerCell);
        }

        GameObject lavaObj = Instantiate(lavaPrefab, spawnPos, Quaternion.identity);
        Lava lava = lavaObj.GetComponent<Lava>();
        if (lava == null)
        {
            // 프리팹에 Lava 컴포넌트가 없을 경우 런타임에 동적으로 붙여 줍니다.
            lava = lavaObj.AddComponent<Lava>();
        }

        if (lava != null)
        {
            lava.Init(cells, lavaDamage, 2f);
        }
    }

    // 플레이어에게 피해를 입히고 넉백
    private void DamageAndKnockbackPlayer(List<Vector2Int> cells, Vector2Int knockbackDir, int damage)
    {
        if (player == null || player.IsDead) return;

        bool hit = false;
        foreach (var cell in cells)
        {
            if (player.GridPosition == cell)
            {
                hit = true;
                break;
            }
        }

        if (hit)
        {
            // 데미지
            player.TakeDamage(damage);

            // 넉백 위치 산정
            Vector2Int startPos = player.GridPosition;
            Vector2Int finalPos = startPos;

            for (int i = 1; i <= knockbackDistance; i++)
            {
                Vector2Int checkPos = startPos + knockbackDir * i;
                if (GridSystem.Instance.IsWalkable(checkPos) && !GridSystem.Instance.IsCellBlocked(checkPos))
                {
                    finalPos = checkPos;
                }
                else
                {
                    break;
                }
            }

            if (finalPos != startPos)
            {
                player.Movement.Knockback(finalPos, 0.2f);
            }
        }
    }

    protected override UniTask OnDeath() //사망 처리 함수임 여기서 다 하셈.
    {
        // 보스가 사망하면 현재 필드를 확실히 비워줌
        if (_currentPillar != null)
        {
            Destroy(_currentPillar.gameObject);
            _currentPillar = null;
        }

        return base.OnDeath();
    }
}
