using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Tilemaps;

public enum LazerAction
{
    Pattern1,
    Pattern2,
    Pattern3
}

[System.Serializable]
public class LazerActionCases
{
    public LazerAction action;
    [Range(0, 100)] public int patternChance;
}

/*
 * LazerPattern — 격자 레이저 패턴을 독립적으로 실행하는 모듈.
 * 보스의 행동 사이클(이동/배회/사라짐)과는 완전히 분리. 자체 타이머로 패턴을 계속 발사.
 * 현재 페이즈는 boss.currentHp를 읽어 판정.
 *
 * 페이즈별 패턴:
 *   HP > phase1HpMin       Phase1: P1 가로 랜덤 2~3 / P2 세로 랜덤 2~3
 *   HP > phase2HpMin       Phase2: P1 가로+세로 랜덤 2~3씩 / P2 가로 스윕 / P3 세로 스윕
 *   HP ≤ phase2HpMin       Phase3: 회전 붉은 레이저(닿으면 phase3LaserDamage) + Phase1 패턴 병행
 *
 * 경고 표시: warningTilemap에 타일(참조 카운트로 중복 방지).
 */
public class LazerPattern : MonoBehaviour
{
    [Header("보스/플레이어 참조 (null이면 자동 검색)")]
    [SerializeField] private BossBase boss;
    [SerializeField] private GridPlayer player;

    [Header("레이저 패턴 가중치 설정")]
    [SerializeField] private List<LazerActionCases> actionCases = new List<LazerActionCases>();

    [Header("레이저 프리팹")]
    [SerializeField] private GameObject lazerAttackPrefab;
    [SerializeField] private GameObject lazerWarningPrefab;   // 레이저 경고용 프리팹 (붉은 레이저)
    [SerializeField] private GameObject eyeHorizontalPrefab; //가로 레이저용 눈: 왼쪽 끝에 소환
    [SerializeField] private GameObject eyeVerticalPrefab;   //세로 레이저용 눈: 위쪽 끝에 소환
    [SerializeField] private GameObject specialEyePrefab;     //Phase 3용 특별한 눈 프리팹 (중앙 소환)

    [Header("AttackFloor 타일맵 (공격·레이저 범위 제한)")]
    [SerializeField] private Tilemap attackFloorTilemap;



    [Header("레이저 타이밍")]
    [SerializeField] private int laserTelegraphMs = 800; // 타일 경고 노출 시간 (회피 창)
    [SerializeField] private int laserActiveMs = 400;    // 실제 레이저 발사 + 판정 시간
    [SerializeField] private int laserDamage = 1;
    [SerializeField] private float laserVisualScale = 1.5f; // 레이저 이미지의 시각적 두께 배율 (히트 판정은 1칸 고정)

    [Header("패턴 사이클")]
    [SerializeField] private int patternDelayMs = 1000; // 패턴 사이 딜레이 (스펙: 1초)

    [Header("Phase 2 스윕")]
    [SerializeField] private int sweepStepMs = 2000;    // 스윕 한 줄당 시간 (스펙: 2초)
    [SerializeField] private int pattern2Repeats = 3;    // 홀수/짝수 반복 횟수
    [SerializeField] private int pattern3StepDelayMs = 600; // 조여드는 사각형 스텝 간격(ms)

    [Header("HP 페이즈 경계 (보스 currentHp 기준)")]
    [SerializeField] private int phase1HpMin = 70;
    [SerializeField] private int phase2HpMin = 20;

    [Header("Phase 3 회전 레이저")]
    [SerializeField] private float phase3LaserRotateSpeed = 60f;
    [SerializeField] private float phase3LaserLength = 30f;
    [SerializeField] private float phase3LaserHalfWidth = 0.5f;
    [SerializeField] private Color phase3LaserColor = Color.red;
    [SerializeField] private int phase3LaserDamage = 50;

    // ─── 내부 상태 ───────────────────────────────────────────────────
    private RectInt _mapBounds;
    private bool _hasMapBounds;
    private CancellationTokenSource _cts;
    private bool _phase3LaserStarted;

    //=============================== 초기화 / 정리 ==========================================
    private void OnEnable()
    {
        if (boss == null) boss = GetComponent<BossBase>();
        if (boss == null) boss = FindFirstObjectByType<BossBase>(); // 별도 오브젝트에 있을 때 폴백
        if (boss == null) Debug.LogError("[LazerPattern] BossBase를 찾지 못했습니다. 페이즈가 항상 1로 고정됩니다.");
        if (player == null) player = FindFirstObjectByType<GridPlayer>();

        if (actionCases == null || actionCases.Count == 0)
        {
            actionCases = new List<LazerActionCases>
            {
                new LazerActionCases { action = LazerAction.Pattern1, patternChance = 40 },
                new LazerActionCases { action = LazerAction.Pattern2, patternChance = 30 },
                new LazerActionCases { action = LazerAction.Pattern3, patternChance = 30 }
            };
        }

        _cts = new CancellationTokenSource();
        _phase3LaserStarted = false;
        CacheMapBounds();
        MainLoop().Forget();
    }

    private void OnDestroy()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }

    private void CacheMapBounds()
    {
        if (attackFloorTilemap != null)
        {
            attackFloorTilemap.CompressBounds();
            var b = attackFloorTilemap.cellBounds;
            _mapBounds = new RectInt(b.xMin, b.yMin, b.size.x, b.size.y);
            _hasMapBounds = true;
            return;
        }

        // attackFloorTilemap 미설정 시 기존 방식(전체 walkable) 폴백
        if (GridSystem.Instance == null) return;
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        bool any = false;
        foreach (var c in GridSystem.Instance.GetAllWalkableCells())
        {
            any = true;
            if (c.x < minX) minX = c.x;
            if (c.y < minY) minY = c.y;
            if (c.x > maxX) maxX = c.x;
            if (c.y > maxY) maxY = c.y;
        }
        if (!any) return;
        _mapBounds = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        _hasMapBounds = true;
    }

    //=============================== 메인 루프 ==========================================
    private bool IsBossAlive()
    {
        // Unity의 destroyed-object special equality: null 비교로 잡힘.
        if (boss == null) return false;
        return boss.currentHp > 0;
    }

    private int CurrentHpPhase()
    {
        if (boss == null) return 1;
        int hp = boss.currentHp;
        if (hp > phase1HpMin) return 1;
        if (hp > phase2HpMin) return 2;
        return 3;
    }

    private async UniTask MainLoop()
    {
        try
        {
            // 보스 컴포넌트의 OnEnable 초기화(currentHp 설정 등)가 완료되도록 첫 프레임을 대기합니다.
            await UniTask.Yield();

            while (!_cts.IsCancellationRequested && IsBossAlive())
            {
                if (!_hasMapBounds) CacheMapBounds();

                int hpPhase = CurrentHpPhase();

                // Phase 3 진입 1회: 회전 레이저 백그라운드 시작
                if (hpPhase == 3 && !_phase3LaserStarted)
                {
                    _phase3LaserStarted = true;
                    Phase3RotatingLaserAsync().Forget();
                    patternDelayMs = 200; 
                }

                // Phase 3에서는 Phase 1 패턴을 계속 발사 (회전 레이저는 백그라운드).
                int patternPhase = (hpPhase == 3) ? 1 : hpPhase;
                await DoOneAttack(patternPhase);
                await UniTask.Delay(patternDelayMs, cancellationToken: _cts.Token);
            }
        }
        catch (System.OperationCanceledException) { }
    }

    private LazerAction RollLazerAction()
    {
        int totalWeight = 0;
        foreach (var actionCase in actionCases)
        {
            totalWeight += actionCase.patternChance;
        }
        if (totalWeight == 0) return LazerAction.Pattern1;
        int randomValue = Random.Range(0, totalWeight);
        int currentWeight = 0;

        foreach (var actionCase in actionCases)
        {
            currentWeight += actionCase.patternChance;
            if (randomValue < currentWeight)
            {
                return actionCase.action;
            }
        }
        return LazerAction.Pattern1;
    }

    private static AudioEventId GetLazerPatternAudioEventId(LazerAction action)
    {
        return action switch
        {
            LazerAction.Pattern1 => AudioEventId.BossA_Pattern1,
            LazerAction.Pattern2 => AudioEventId.BossA_Pattern2,
            LazerAction.Pattern3 => AudioEventId.BossA_Pattern3,
            _ => AudioEventId.None
        };
    }

    private static void RequestLazerPatternAudio(AudioEventId eventId)
    {
        if (eventId == AudioEventId.None)
            return;

        GameEvents.OnAudioEventRequested?.Invoke(eventId);
    }

    private async UniTask DoOneAttack(int hpPhase)
    {
        LazerAction action = RollLazerAction();
        AudioEventId audioEventId = GetLazerPatternAudioEventId(action);

        switch (hpPhase)
        {
            case 1:
                switch (action)
                {
                    case LazerAction.Pattern1:
                        await FireRandomAxisLasers(Vector2Int.right, Random.Range(2, 4), audioEventId);
                        break;
                    case LazerAction.Pattern2:
                        await FireRandomAxisLasers(Vector2Int.up, Random.Range(2, 4), audioEventId);
                        break;
                    default:
                        // Pattern3이 뽑혔을 때도 가로/세로 중 무작위 분기
                        var axis = (Random.Range(0, 2) == 0) ? Vector2Int.right : Vector2Int.up;
                        await FireRandomAxisLasers(axis, Random.Range(2, 4), audioEventId);
                        break;
                }
                break;

            case 2:
                switch (action)
                {
                    case LazerAction.Pattern1:
                        await SweepCrossLasers(audioEventId);
                        break;
                    case LazerAction.Pattern2:
                        await FireOddEvenLasersPattern(audioEventId);
                        break;
                    case LazerAction.Pattern3:
                        await ShrinkingSquarePattern(audioEventId);
                        break;
                }
                break;
        }
    }

    private async UniTask SweepCrossLasers(AudioEventId audioEventId)
    {
        if (!_hasMapBounds) return;

        int xLength = _mapBounds.xMax - _mapBounds.xMin;
        int yLength = _mapBounds.yMax - _mapBounds.yMin;
        int maxLength = Mathf.Max(xLength, yLength);

        for (int i = 0; i < maxLength; i++)
        {
            if (_cts.IsCancellationRequested || !IsBossAlive()) break;

            if (i < xLength)
            {
                int xCoord = _mapBounds.xMin + i;
                FireAxisLaserAt(Vector2Int.up, xCoord, audioEventId).Forget();
            }
            if (i < yLength)
            {
                int yCoord = _mapBounds.yMin + i;
                FireAxisLaserAt(Vector2Int.right, yCoord, audioEventId).Forget();
            }

            await UniTask.Delay(sweepStepMs, cancellationToken: _cts.Token);
        }
    }

    private async UniTask FireOddEvenLasersPattern(AudioEventId audioEventId)
    {
        if (!_hasMapBounds) return;

        List<int> oddCoords = new List<int>();
        List<int> evenCoords = new List<int>();

        for (int x = _mapBounds.xMin; x < _mapBounds.xMax; x++)
        {
            if (x % 2 != 0)
                oddCoords.Add(x);
            else
                evenCoords.Add(x);
        }

        for (int i = 0; i < pattern2Repeats; i++)
        {
            if (_cts.IsCancellationRequested || !IsBossAlive()) break;

            // 1. Odd columns
            await FireAxisLasersAtCoords(Vector2Int.up, oddCoords, audioEventId);

            if (_cts.IsCancellationRequested || !IsBossAlive()) break;

            // 2. Even columns
            await FireAxisLasersAtCoords(Vector2Int.up, evenCoords, audioEventId);
        }
    }

    private async UniTask FireAxisLasersAtCoords(Vector2Int axis, List<int> coords, AudioEventId audioEventId)
    {
        List<UniTask> tasks = new List<UniTask>();
        foreach (int coord in coords)
        {
            tasks.Add(FireAxisLaserAt(axis, coord, audioEventId));
        }
        if (tasks.Count > 0)
        {
            await UniTask.WhenAll(tasks);
        }
    }

    private async UniTask ShrinkingSquarePattern(AudioEventId audioEventId)
    {
        if (!_hasMapBounds) return;

        List<UniTask> allLaserTasks = new List<UniTask>();
        int d = 0;
        while (true)
        {
            if (_cts.IsCancellationRequested || !IsBossAlive()) break;

            int left = _mapBounds.xMin + d;
            int right = _mapBounds.xMax - 1 - d;
            int bottom = _mapBounds.yMin + d;
            int top = _mapBounds.yMax - 1 - d;

            if (left > right && bottom > top) break;

            List<int> xCoords = new List<int>();
            List<int> yCoords = new List<int>();

            if (left <= right)
            {
                xCoords.Add(left);
                if (left != right) xCoords.Add(right);
            }
            if (bottom <= top)
            {
                yCoords.Add(bottom);
                if (bottom != top) yCoords.Add(top);
            }

            if (xCoords.Count == 0 && yCoords.Count == 0) break;

            foreach (int x in xCoords)
            {
                allLaserTasks.Add(FireAxisLaserAt(Vector2Int.up, x, audioEventId));
            }
            foreach (int y in yCoords)
            {
                allLaserTasks.Add(FireAxisLaserAt(Vector2Int.right, y, audioEventId));
            }

            d++;
            await UniTask.Delay(pattern3StepDelayMs, cancellationToken: _cts.Token);
        }

        if (allLaserTasks.Count > 0)
        {
            await UniTask.WhenAll(allLaserTasks);
        }
    }

    private async UniTask FireRandomAxisLasers(Vector2Int axis, int count, AudioEventId audioEventId)
    {
        var tasks = new List<UniTask>();
        for (int i = 0; i < count; i++) tasks.Add(FireRandomAxisLaser(axis, audioEventId));
        await UniTask.WhenAll(tasks);
    }

    //=============================== Phase 3 회전 레이저 ==========================================
    private async UniTaskVoid Phase3RotatingLaserAsync()
    {
        var token = _cts.Token;
        Vector3 center = GetMapCenterWorld();

        GameObject eyeObj = null;
        if (specialEyePrefab != null)
        {
            eyeObj = Instantiate(specialEyePrefab, center, Quaternion.identity);
        }

        var warningBeams = new GameObject[4];
        var beams = new GameObject[4];
        float baseAngle = 0f;

        try
        {
            while (!token.IsCancellationRequested && IsBossAlive())
            {
                // 1) 2초간 얇고 붉은 경고 레이저 스폰 및 대기 (현재 baseAngle에 맞춰 배치)
                GameObject prefabToUse = lazerWarningPrefab != null ? lazerWarningPrefab : lazerAttackPrefab;
                if (prefabToUse != null)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        float deg = baseAngle + i * 90f;
                        float a = deg * Mathf.Deg2Rad;
                        Vector3 dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);

                        warningBeams[i] = Instantiate(prefabToUse, center + dir * (phase3LaserLength * 0.5f), Quaternion.Euler(0, 0, deg));
                        
                        float scaleYMultiplier = (lazerWarningPrefab != null) ? 1.0f : 0.2f;
                        var s = warningBeams[i].transform.localScale;
                        warningBeams[i].transform.localScale = new Vector3(phase3LaserLength, s.y * scaleYMultiplier, s.z);

                        if (lazerWarningPrefab == null)
                        {
                            var srs = warningBeams[i].GetComponentsInChildren<SpriteRenderer>();
                            foreach (var sr in srs)
                            {
                                sr.color = new Color(1f, 0f, 0f, 0.5f); // 경고용 붉은 반투명 강제 설정
                            }
                        }
                    }
                }

                await UniTask.Delay(2000, cancellationToken: token);

                // 경고 레이저 제거
                for (int i = 0; i < 4; i++)
                {
                    if (warningBeams[i] != null) Destroy(warningBeams[i]);
                }

                if (token.IsCancellationRequested || !IsBossAlive()) break;

                // 2) 진짜 빔 스폰
                for (int i = 0; i < 4; i++)
                {
                    beams[i] = Instantiate(lazerAttackPrefab, center, Quaternion.identity);
                    var s = beams[i].transform.localScale;
                    beams[i].transform.localScale = new Vector3(phase3LaserLength, s.y, s.z);
                    var sr = beams[i].GetComponentInChildren<SpriteRenderer>();
                    if (sr != null) sr.color = phase3LaserColor;
                }

                // 3) 5초간 진짜 레이저 활성화 및 회전 처리
                float activeTimer = 0f;
                while (activeTimer < 5f && !token.IsCancellationRequested && IsBossAlive())
                {
                    float dt = Time.deltaTime;
                    baseAngle += phase3LaserRotateSpeed * dt;
                    activeTimer += dt;

                    bool hit = false;
                    Vector3 playerPos = (player != null && !player.IsDead)
                        ? GridSystem.Instance.GridToWorld(player.GridPosition)
                        : Vector3.positiveInfinity;

                    for (int i = 0; i < 4; i++)
                    {
                        if (beams[i] == null) continue;

                        float deg = baseAngle + i * 90f;
                        float a = deg * Mathf.Deg2Rad;
                        Vector3 dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);

                        beams[i].transform.SetPositionAndRotation(
                            center + dir * (phase3LaserLength * 0.5f),
                            Quaternion.Euler(0, 0, deg));

                        if (!hit && IsOnRay(playerPos, center, dir, phase3LaserLength, phase3LaserHalfWidth))
                            hit = true;
                    }

                    if (hit && player != null && !player.IsDead)
                        player.TakeDamage(phase3LaserDamage);

                    await UniTask.Yield(token);
                }

                // 4) 진짜 빔 파괴 (대기 상태 진입 전)
                for (int i = 0; i < 4; i++)
                {
                    if (beams[i] != null) Destroy(beams[i]);
                }
            }
        }
        catch (System.OperationCanceledException) { }
        finally
        {
            for (int i = 0; i < 4; i++)
            {
                if (warningBeams[i] != null) Destroy(warningBeams[i]);
                if (beams[i] != null) Destroy(beams[i]);
            }
            if (eyeObj != null) Destroy(eyeObj);
        }
    }

    //=============================== 레이저 발사 (시각 + 타일 경고 + 격자 데미지) ==========================================

    private UniTask FireRandomAxisLaser(Vector2Int axis, AudioEventId audioEventId)
    {
        if (!_hasMapBounds) return UniTask.CompletedTask;
        int coord = (axis == Vector2Int.right)
            ? Random.Range(_mapBounds.yMin, _mapBounds.yMax)
            : Random.Range(_mapBounds.xMin, _mapBounds.xMax);
        return FireAxisLaserAt(axis, coord, audioEventId);
    }

    private UniTask FireAxisLaserAt(Vector2Int axis, int coord, AudioEventId audioEventId)
    {
        if (!_hasMapBounds) return UniTask.CompletedTask;
        Vector2Int startCell, endCell;
        if (axis == Vector2Int.right)
        {
            startCell = new Vector2Int(_mapBounds.xMin, coord);
            endCell   = new Vector2Int(_mapBounds.xMax - 1, coord);
        }
        else
        {
            startCell = new Vector2Int(coord, _mapBounds.yMin);
            endCell   = new Vector2Int(coord, _mapBounds.yMax - 1);
        }
        return FireLaserBetween(startCell, endCell, axis, 0, laserVisualScale, laserDamage, _cts.Token, audioEventId);
    }

    // 두 격자 셀 사이 레이저 공용 구현: 경고 레이저 + 눈 prefab → 레이저 시각 + 격자 데미지 → 정리.
    private async UniTask FireLaserBetween(Vector2Int startCell, Vector2Int endCell, Vector2Int axis,
                                           int width, float visualScaleY, int damage, CancellationToken token, AudioEventId audioEventId)
    {
        var cells = CellsAlongLine(startCell, endCell, axis, width);

        GameObject eye = SpawnEyeForAxis(axis, startCell, endCell);
        GameObject warningLaser = null;
        GameObject lazer = null;

        try
        {
            // 경고 레이저 스폰
            GameObject prefabToUse = lazerWarningPrefab != null ? lazerWarningPrefab : lazerAttackPrefab;
            if (prefabToUse != null)
            {
                Vector3 startPt = GridSystem.Instance.GridToWorld(startCell);
                Vector3 endPt   = GridSystem.Instance.GridToWorld(endCell);
                Quaternion q = Quaternion.Euler(0, 0, Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg);
                float length = Vector3.Distance(startPt, endPt);
                Vector3 center = (startPt + endPt) * 0.5f;
                warningLaser = Instantiate(prefabToUse, center, q);
                
                float scaleYMultiplier = (lazerWarningPrefab != null) ? 1.0f : 0.2f;
                var s = warningLaser.transform.localScale;
                warningLaser.transform.localScale = new Vector3(length, s.y * visualScaleY * scaleYMultiplier, s.z);

                if (lazerWarningPrefab == null)
                {
                    var srs = warningLaser.GetComponentsInChildren<SpriteRenderer>();
                    foreach (var sr in srs)
                    {
                        sr.color = new Color(1f, 0f, 0f, 0.5f); // 붉은 반투명 강제 설정
                    }
                }
            }

            await UniTask.Delay(laserTelegraphMs, cancellationToken: token);

            if (warningLaser != null) { Destroy(warningLaser); warningLaser = null; }
            if (eye != null) { Destroy(eye); eye = null; }

            // 레이저 시각 spawn
            // 패턴 시작음이 아니라 실제 레이저가 발사되는 순간마다 소리를 낸다.
            RequestLazerPatternAudio(audioEventId);

            Vector3 startPtReal = GridSystem.Instance.GridToWorld(startCell);
            Vector3 endPtReal   = GridSystem.Instance.GridToWorld(endCell);
            Quaternion qReal = Quaternion.Euler(0, 0, Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg);
            float lengthReal = Vector3.Distance(startPtReal, endPtReal);
            Vector3 centerReal = (startPtReal + endPtReal) * 0.5f;
            lazer = Instantiate(lazerAttackPrefab, centerReal, qReal);
            var sReal = lazer.transform.localScale;
            lazer.transform.localScale = new Vector3(lengthReal, sReal.y * visualScaleY, sReal.z);

            DamagePlayerIfInCells(cells, damage);

            await UniTask.Delay(laserActiveMs, cancellationToken: token);
            if (lazer != null) { Destroy(lazer); lazer = null; }
        }
        catch (System.OperationCanceledException) { }
        finally
        {
            if (warningLaser != null) Destroy(warningLaser);
            if (eye != null) Destroy(eye);
            if (lazer != null) Destroy(lazer);
        }
    }

    private GameObject SpawnEyeForAxis(Vector2Int axis, Vector2Int startCell, Vector2Int endCell)
    {
        GameObject prefab = (axis == Vector2Int.right) ? eyeHorizontalPrefab : eyeVerticalPrefab;
        if (prefab == null) return null;
        Vector2Int spawnCell = (axis == Vector2Int.right) ? startCell : endCell;
        return Instantiate(prefab, GridSystem.Instance.GridToWorld(spawnCell), Quaternion.identity);
    }

    private static List<Vector2Int> CellsAlongLine(Vector2Int startCell, Vector2Int endCell, Vector2Int axis, int width)
    {
        var cells = new List<Vector2Int>();
        var perp = new Vector2Int(-axis.y, axis.x);
        var c = startCell;
        int guard = 500;
        while (guard-- > 0)
        {
            for (int w = -width; w <= width; w++) cells.Add(c + perp * w);
            if (c == endCell) break;
            c += axis;
        }
        return cells;
    }

    //=============================== 데미지 판정 (LazerPattern 자체 구현) ==========================================
    private void DamagePlayerIfInCells(IEnumerable<Vector2Int> cells, int damage)
    {
        if (player == null || player.IsDead) return;
        foreach (var c in cells)
        {
            if (player.GridPosition == c)
            {
                player.TakeDamage(damage);
                return;
            }
        }
    }

    //=============================== 헬퍼 ==========================================
    private bool IsOnRay(Vector3 point, Vector3 origin, Vector3 dir, float length, float halfWidth)
    {
        Vector3 n = dir.normalized;
        Vector3 toPoint = point - origin;
        float along = Vector3.Dot(toPoint, n);
        if (along < 0f || along > length) return false;
        Vector3 proj = origin + n * along;
        return Vector3.Distance(point, proj) <= halfWidth;
    }

    private Vector3 GetMapCenterWorld()
    {
        if (_hasMapBounds && GridSystem.Instance != null)
        {
            int cx = (_mapBounds.xMin + _mapBounds.xMax) / 2;
            int cy = (_mapBounds.yMin + _mapBounds.yMax) / 2;
            // 2x2 보스의 중심점에 맞춰 회전 레이저 중심을 정렬합니다.
            return GridSystem.Instance.GridToWorld(new Vector2Int(cx, cy)) + new Vector3(0.5f, 0.5f, 0f);
        }
        return transform.position;
    }
}
