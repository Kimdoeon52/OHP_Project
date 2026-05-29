using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

/*
 * BossA — 시간 기반 생존형 보스 (눈알 보스).
 * 제한 시간 120초 동안 살아남기. 페이즈는 HP가 아니라 TimeSystem.TimeRemaining으로 전환.
 *   120~80초 : 일반 패턴 (플레이어 방향 직선 레이저 / 굵은 직선 레이저)
 *   80 ~20초 : 분노 패턴 (가로5 / 세로5 / 가로3+세로3 랜덤 레이저)
 *   20 ~ 0초 : 죽음 패턴 (중앙으로 이동 + 10초간 4방향 회전 붉은 레이저, 닿으면 즉사)
 * 패턴은 patternDelay(=2초) 간격으로 실행.
 */
public class BossA : BossBase
{
    [Header("레이저 프리팹")]
    [SerializeField] private GameObject lazerAttackPrefab; //레이저 본체 (경고는 BossBase의 warningTilemap 타일로 표시)

    [Header("레이저 타이밍 (patternDelay와 분리)")]
    [SerializeField] private int laserTelegraphMs = 800; // 경고선 노출 시간 (회피 창)
    [SerializeField] private int laserActiveMs = 400;    // 실제 레이저 발사 + 판정 노출 시간

    [Header("레이저 판정")]
    [SerializeField] private int laserDamage = 1;         // 일반 레이저 피해
    [SerializeField] private float thickLaserScaleY = 3f; // 굵은 레이저 시각 Y배수
    [SerializeField] private int thickLaserWidth = 1;     // 굵은 레이저 폭 (중심선 양옆 칸 수)

    [Header("페이즈 시간 경계 (초)")]
    [SerializeField] private float phase1Until = 80f; // 이 시간보다 많으면 Phase1(일반)
    [SerializeField] private float phase2Until = 20f; // 이 시간보다 많으면 Phase2(분노), 이하면 죽음

    [Header("죽음 패턴 (회전 레이저)")]
    [SerializeField] private float deathLaserDurationSec = 10f; // 회전 지속 시간
    [SerializeField] private float deathLaserRotateSpeed = 60f; // 초당 회전 각도(도)
    [SerializeField] private float deathLaserLength = 30f;      // 레이저 길이(월드)
    [SerializeField] private float deathLaserHalfWidth = 0.5f;  // 판정 두께(월드, 중심선에서 거리)
    [SerializeField] private Color deathLaserColor = Color.red;
    [SerializeField] private int deathInstantKillDamage = 99999;

    private RectInt _bossRoomBounds;
    private bool _hasBossRoomBounds;
    private RectInt _mapBounds;        // 전체 맵(통행 가능 셀) 경계 — 가로/세로 레이저 기준
    private bool _hasMapBounds;
    private bool _deathPhaseStarted;

    protected override void OnEnable()
    {
        base.OnEnable(); // patternDelay 변환 + 보스전 120초 타이머 시작
        currentHp = maxHp;
        CacheBossRoom();
        CacheMapBounds();
    }

    // BossA는 시간 기반 페이즈라 HP 기반 Phase2 전환을 막는다 (patternDelay가 멋대로 줄지 않도록).
    protected override void EnterPhase2() { /* no-op: 페이즈는 시간으로만 전환 */ }

    private void CacheBossRoom()
    {
        if (MapGenerator.Instance?.CurrentRooms == null) return;
        var bossRoom = MapGenerator.Instance.CurrentRooms.Find(r => r.type == RoomType.Boss);
        if (bossRoom == null) return;
        _bossRoomBounds = bossRoom.bounds;
        _hasBossRoomBounds = true;
    }

    // 전체 맵의 통행 가능 셀들로 바운딩 박스를 계산. 가로/세로 레이저를 맵 전체 기준으로 뽑기 위함.
    private void CacheMapBounds()
    {
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

    //=============================== 시간 기반 페이즈 디스패치 ==========================================
    private int CurrentTimePhase()
    {
        float t = TimeSystem.Instance != null ? TimeSystem.Instance.TimeRemaining : 0f;
        if (t > phase1Until) return 1; // 120~80
        if (t > phase2Until) return 2; // 80~20
        return 3;                       // 20~0
    }

    protected override async UniTask PhaseState()
    {
        int phase = CurrentTimePhase();

        // 죽음 패턴은 단 한 번만 시작하고 자체 수명으로 동작.
        if (phase == 3)
        {
            if (!_deathPhaseStarted)
            {
                _deathPhaseStarted = true;
                isBossAttacking = true; // 죽음 패턴 동안 일반 디스패치 잠금
                DeathPhaseAsync().Forget();
            }
            return;
        }

        // 일반/분노: patternDelay(2초) 간격으로 패턴 1개 실행.
        isBossAttacking = true;
        await UniTask.Delay(patternDelay, cancellationToken: cancelToken.Token);

        if (phase == 1)
        {
            if (Random.Range(0, 2) == 0) Phase1Pattern1().Forget();
            else Phase1Pattern2().Forget();
        }
        else
        {
            int p = Random.Range(0, 3);
            if (p == 0) Phase2Pattern1().Forget();
            else if (p == 1) Phase2Pattern2().Forget();
            else Phase2Pattern3().Forget();
        }
    }

    //<<<<<<<<<<<<<<<<<<<<<<<< 일반 패턴 (120~80초) >>>>>>>>>>>>>>>>>>>>>>>>

    // 패턴1: 플레이어 방향(8방향 스냅) 전방 1직선 레이저
    protected override async UniTask Phase1Pattern1()
    {
        await FirePlayerDirectedLaser(width: 0, visualScaleY: 1f, damage: laserDamage);
        isBossAttacking = false;
    }

    // 패턴2: 플레이어 방향 굵은 직선 레이저
    protected override async UniTask Phase1Pattern2()
    {
        await FirePlayerDirectedLaser(width: thickLaserWidth, visualScaleY: thickLaserScaleY, damage: laserDamage);
        isBossAttacking = false;
    }

    //<<<<<<<<<<<<<<<<<<<<<<<< 분노 패턴 (80~20초) >>>>>>>>>>>>>>>>>>>>>>>>

    // 패턴1: 가로 랜덤 5줄 (동시)
    protected override async UniTask Phase2Pattern1()
    {
        var tasks = new List<UniTask>();
        for (int i = 0; i < 5; i++) tasks.Add(FireRandomAxisLaser(Vector2Int.right));
        await UniTask.WhenAll(tasks);
        isBossAttacking = false;
    }

    // 패턴2: 세로 랜덤 5줄 (동시)
    protected override async UniTask Phase2Pattern2()
    {
        var tasks = new List<UniTask>();
        for (int i = 0; i < 5; i++) tasks.Add(FireRandomAxisLaser(Vector2Int.up));
        await UniTask.WhenAll(tasks);
        isBossAttacking = false;
    }

    // 패턴3: 가로 3 + 세로 3 (동시)
    protected override async UniTask Phase2Pattern3()
    {
        var tasks = new List<UniTask>();
        for (int i = 0; i < 3; i++) tasks.Add(FireRandomAxisLaser(Vector2Int.right));
        for (int i = 0; i < 3; i++) tasks.Add(FireRandomAxisLaser(Vector2Int.up));
        await UniTask.WhenAll(tasks);
        isBossAttacking = false;
    }

    //=============================== 레이저 발사 (시각 + 격자 데미지) ==========================================

    // 보스에서 플레이어 방향(8방향 스냅)으로 벽까지 뻗는 레이저.
    private async UniTask FirePlayerDirectedLaser(int width, float visualScaleY, int damage)
    {
        var bossCell = GridSystem.Instance.WorldToGrid(transform.position);
        var dir = DirToPlayer8(bossCell);
        var endCell = WalkToWall(bossCell, dir);
        var startCell = WalkToWall(bossCell, -dir);
        await FireLaserBetween(startCell, endCell, dir, width, visualScaleY, damage, cancelToken.Token);
    }

    // 방 경계 안에서 랜덤한 행(가로) 또는 열(세로) 전체를 가로지르는 레이저.
    private async UniTask FireRandomAxisLaser(Vector2Int axis)
    {
        Vector2Int startCell, endCell;

        if (_hasBossRoomBounds)
        {
            if (axis == Vector2Int.right) // 가로: 랜덤 행
            {
                int y = Random.Range(_bossRoomBounds.yMin + 1, _bossRoomBounds.yMax - 1);
                startCell = new Vector2Int(_bossRoomBounds.xMin, y);
                endCell = new Vector2Int(_bossRoomBounds.xMax - 1, y);
            }
            else // 세로: 랜덤 열
            {
                int x = Random.Range(_bossRoomBounds.xMin + 1, _bossRoomBounds.xMax - 1);
                startCell = new Vector2Int(x, _bossRoomBounds.yMin);
                endCell = new Vector2Int(x, _bossRoomBounds.yMax - 1);
            }
        }
        else
        {
            // 방 경계 없으면 보스 위치 기준 벽 탐색으로 폴백
            var bossCell = GridSystem.Instance.WorldToGrid(transform.position);
            startCell = WalkToWall(bossCell, -axis);
            endCell = WalkToWall(bossCell, axis);
        }

        await FireLaserBetween(startCell, endCell, axis, 0, 1f, laserDamage, cancelToken.Token);
    }

    // 두 격자 셀 사이를 잇는 레이저 공용 구현: 타일 경고 → 레이저 시각 + 격자 데미지 판정 → 정리.
    private async UniTask FireLaserBetween(Vector2Int startCell, Vector2Int endCell, Vector2Int axis,
                                           int width, float visualScaleY, int damage, CancellationToken token)
    {
        // 레이저가 지나는 칸들 — 경고 표시와 데미지 판정에 동일하게 사용.
        var cells = CellsAlongLine(startCell, endCell, axis, width);

        ShowWarning(cells); // 타일맵 경고 (회피 창)
        try
        {
            await UniTask.Delay(laserTelegraphMs, cancellationToken: token);
            ClearWarning(cells); // 경고 종료 → 레이저 발사

            // 레이저 시각 spawn (길이에 맞춰 X스케일, 굵기 visualScaleY)
            Vector3 startPt = GridSystem.Instance.GridToWorld(startCell);
            Vector3 endPt = GridSystem.Instance.GridToWorld(endCell);
            Quaternion q = Quaternion.Euler(0, 0, Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg);
            float length = Vector3.Distance(startPt, endPt);
            Vector3 center = (startPt + endPt) * 0.5f;
            GameObject lazer = Instantiate(lazerAttackPrefab, center, q);
            var s = lazer.transform.localScale;
            lazer.transform.localScale = new Vector3(length, s.y * visualScaleY, s.z);

            // 격자 데미지 판정 (레이저가 지나는 칸에 플레이어가 있으면)
            DamagePlayerIfInCells(cells, damage);

            await UniTask.Delay(laserActiveMs, cancellationToken: token);
            if (lazer != null) Destroy(lazer);
        }
        catch (System.OperationCanceledException) { }
        finally
        {
            ClearWarning(cells); // 취소 시 잔존 방지 (정상 경로에선 이미 비워져 no-op)
        }
    }

    // startCell → endCell 사이 axis 방향 셀들 + 좌우 width칸 (굵은 레이저용).
    private List<Vector2Int> CellsAlongLine(Vector2Int startCell, Vector2Int endCell, Vector2Int axis, int width)
    {
        var cells = new List<Vector2Int>();
        var perp = new Vector2Int(-axis.y, axis.x);
        var c = startCell;
        int guard = 500; // 무한루프 방지
        while (guard-- > 0)
        {
            for (int w = -width; w <= width; w++)
                cells.Add(c + perp * w);
            if (c == endCell) break;
            c += axis;
        }
        return cells;
    }

    //=============================== 죽음 패턴 : 중앙 + 4방향 회전 레이저 ==========================================
    private async UniTask DeathPhaseAsync()
    {
        var token = this.GetCancellationTokenOnDestroy();

        // 1) 보스를 방 중앙으로 이동
        Vector3 center = GetRoomCenterWorld();
        transform.position = center;
        if (bossAnimation != null) bossAnimation.SetBool("Phase2", true); // 연출용 (애니 있으면)

        // 2) 4방향 회전 레이저용 lazerAttackPrefab 4개 생성 (+자 모양, 함께 회전). LineRenderer 미사용.
        var beams = new GameObject[4];
        for (int i = 0; i < 4; i++)
        {
            beams[i] = Instantiate(lazerAttackPrefab, center, Quaternion.identity);
            var s = beams[i].transform.localScale;
            beams[i].transform.localScale = new Vector3(deathLaserLength, s.y, s.z); // 길이만큼 X스케일
            var sr = beams[i].GetComponentInChildren<SpriteRenderer>();
            if (sr != null) sr.color = deathLaserColor; // 붉은 틴트 (SpriteRenderer 있을 때만)
        }

        float elapsed = 0f;
        float baseAngle = 0f;

        try
        {
            while (elapsed < deathLaserDurationSec)
            {
                baseAngle += deathLaserRotateSpeed * Time.deltaTime;

                bool hit = false;
                Vector3 playerPos = (player != null && !player.IsDead)
                    ? GridSystem.Instance.GridToWorld(player.GridPosition)
                    : Vector3.positiveInfinity;

                for (int i = 0; i < 4; i++)
                {
                    float deg = baseAngle + i * 90f;
                    float a = deg * Mathf.Deg2Rad;
                    Vector3 dir = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);

                    // 시각 갱신: 빔 중심을 center에서 dir로 길이/2만큼 밀고, 그 각도로 회전.
                    beams[i].transform.SetPositionAndRotation(
                        center + dir * (deathLaserLength * 0.5f),
                        Quaternion.Euler(0, 0, deg));

                    // 판정: 플레이어가 이 레이저 선 위에 있으면 즉사
                    if (!hit && IsOnRay(playerPos, center, dir, deathLaserLength, deathLaserHalfWidth))
                        hit = true;
                }

                if (hit && player != null && !player.IsDead)
                    player.TakeDamage(deathInstantKillDamage);

                elapsed += Time.deltaTime;
                await UniTask.Yield(token);
            }
        }
        catch (System.OperationCanceledException) { }
        finally
        {
            for (int i = 0; i < 4; i++)
                if (beams[i] != null) Destroy(beams[i]);
        }
        // 죽음 패턴 종료 후에는 isBossAttacking을 풀지 않음 → 보스 정지. 시간이 0이 되면 생존 성공 처리는 별도.
    }

    private bool IsOnRay(Vector3 point, Vector3 origin, Vector3 dir, float length, float halfWidth)
    {
        Vector3 n = dir.normalized;
        Vector3 toPoint = point - origin;
        float along = Vector3.Dot(toPoint, n);
        if (along < 0f || along > length) return false; // 레이저 범위 밖
        Vector3 proj = origin + n * along;
        return Vector3.Distance(point, proj) <= halfWidth;
    }

    private Vector3 GetRoomCenterWorld()
    {
        if (_hasBossRoomBounds && GridSystem.Instance != null)
        {
            int cx = (_bossRoomBounds.xMin + _bossRoomBounds.xMax) / 2;
            int cy = (_bossRoomBounds.yMin + _bossRoomBounds.yMax) / 2;
            return GridSystem.Instance.GridToWorld(new Vector2Int(cx, cy));
        }
        return transform.position;
    }

    //=============================== 방향 헬퍼 ==========================================

    // 보스→플레이어 방향을 8방향 중 가장 가까운 격자 방향으로 스냅.
    private Vector2Int DirToPlayer8(Vector2Int from)
    {
        if (player == null) return Vector2Int.right;
        Vector2 v = player.GridPosition - from;
        if (v == Vector2.zero) return Vector2Int.right;

        float oct = Mathf.Round(Mathf.Atan2(v.y, v.x) / (Mathf.PI / 4f)) * (Mathf.PI / 4f);
        return new Vector2Int(Mathf.RoundToInt(Mathf.Cos(oct)), Mathf.RoundToInt(Mathf.Sin(oct)));
    }

    // 벽(IsWalkable 실패)에 닿기 직전까지 dir로 전진한 마지막 통행 가능 셀 반환.
    private Vector2Int WalkToWall(Vector2Int origin, Vector2Int dir)
    {
        var current = origin;
        int guard = 100;
        while (guard-- > 0 && GridSystem.Instance.IsWalkable(current + dir))
            current += dir;
        return current;
    }
}
