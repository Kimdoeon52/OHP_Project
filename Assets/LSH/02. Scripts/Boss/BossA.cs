using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

/*
 * BossA 예상 컨셉
 * phase1: 30초 버티기. 패턴1: 화면가리기. 패턴2: 가로 레이저 공격. 패턴3: 세로 레이져 공격
 *
 * phase2: 보스 등장.
 * 체력 100%- 패턴1: 마구 점프(기본적으로 닿으면 데미지). 패턴2: 8방향 회전 레이저 공격.
 * 체력 30%- 빨갛게 변하며 긴박함 음악+8방향 회전 레이저 공격 + 화면 가리기 공격. 안끝남.
 */

public class BossA : BossBase
{

    [Header("보스 화면 가리기 패턴")]
    [SerializeField] private Image image;
    [SerializeField] private Image image2;

    [Header("보스 총알")]
    [SerializeField] private GameObject bossABullet;

    [Header("레이저 설정")]
    [SerializeField] private LineRenderer dangerLinePrefab;
    [SerializeField] private GameObject lazerAttackPrefab;

    private RectInt _bossRoomBounds;
    private bool _hasBossRoomBounds;

    protected override void Start()
    {
        base.Start();
        currentHp = maxHp; // 보스 체력 설정
        AddActionCase(BossAction.Pattern1, 30);
        AddActionCase(BossAction.Pattern2, 30);
        AddActionCase(BossAction.Pattern3, 40);
        image.gameObject.SetActive(false);
        image2.gameObject.SetActive(false);
        CacheBossRoom();
    }

    private void CacheBossRoom()
    {
        if (MapGenerator.Instance?.CurrentRooms == null) return;
        var bossRoom = MapGenerator.Instance.CurrentRooms.Find(r => r.type == RoomType.Boss);
        if (bossRoom == null) return;
        _bossRoomBounds = bossRoom.bounds;
        _hasBossRoomBounds = true;
    }

    protected override void Update()
    {
        base.Update();
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            bossLevel = 2;
            bossAnimation.SetBool("Phase2", true);
            patternDelay = patternDelay / 2;
        }
    }

    //=====================================보스 행동 패턴 구현=====================================

    //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<< Phase 1 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

    //===================================== 패턴1 ==================================================
    protected override async UniTask Phase1Pattern1()
    {
        Debug.Log($"<color=red>[행동패턴1]</color> ");
        ImageAttack().Forget();
        await UniTask.Delay(patternDelay);
        isBossAttacking = false;
    }

    private async UniTask ImageAttack()
    {
        Debug.Log($"<color=red>[화면 가리기 공격!]</color>");
        int randomValue = Random.Range(0, 2);
        switch (randomValue)
        {
            case 0: image.gameObject.SetActive(true);  break;
            case 1: image2.gameObject.SetActive(true); break;
        }
        await UniTask.Delay(3000);
        ImageOff();
    }

    void ImageOff()
    {
        image.gameObject.SetActive(false);
        image2.gameObject.SetActive(false);
    }

    //===================================== 가로 레이저 ==================================================
    protected override async UniTask Phase1Pattern2()
    {
        Debug.Log($"<color=red>[행동패턴2]</color> ");
        int ranY = Random.Range(-4, 4);
        Vector3 pos = new Vector3(0, ranY, 0);
        await LazerAttack(pos, Quaternion.identity, cancelToken.Token);
        await UniTask.Delay(patternDelay, cancellationToken: cancelToken.Token);
        isBossAttacking = false;
    }

    //===================================== 세로 레이저 ==================================================
    protected override async UniTask Phase1Pattern3()
    {
        Debug.Log($"<color=red>[행동패턴3]</color> ");
        int ranX = Random.Range(-8, 8);
        Vector3 pos = new Vector3(ranX, 0, 0);
        Quaternion quaternion = Quaternion.Euler(0, 0, 90f);
        await LazerAttack(pos, quaternion, cancelToken.Token);
        await UniTask.Delay(patternDelay, cancellationToken: cancelToken.Token);
        isBossAttacking = false;
    }

    //===================================== 레이저 공격 구현 =======================================
    private async UniTask LazerAttack(Vector3 pos, Quaternion q, CancellationToken token)
    {
        var (startPt, endPt) = FindLaserEndpoints(pos, q);

        LineRenderer dangerLine = Instantiate(dangerLinePrefab, pos, q);
        dangerLine.useWorldSpace = true; // 월드 좌표로 방 경계까지 직접 지정
        dangerLine.positionCount = 2;
        dangerLine.SetPosition(0, startPt);
        dangerLine.SetPosition(1, endPt);

        try
        {
            await UniTask.Delay(patternDelay, cancellationToken: token);
            if (dangerLine != null)
                Destroy(dangerLine.gameObject);

            // 레이저 길이에 맞춰 프리팹 크기 조정 (프리팹 기본 localScale.x = 1 → 1 world unit 가정)
            float length = Vector3.Distance(startPt, endPt);
            Vector3 center = (startPt + endPt) * 0.5f;
            GameObject lazer = Instantiate(lazerAttackPrefab, center, q);
            var s = lazer.transform.localScale;
            lazer.transform.localScale = new Vector3(length, s.y, s.z);

            await UniTask.Delay(patternDelay, cancellationToken: token);
            if (lazer != null)
                Destroy(lazer);
        }
        catch (System.OperationCanceledException)
        {
            if (dangerLine != null)
                Destroy(dangerLine.gameObject);
        }
    }

    // ─── 방 경계 탐색 헬퍼 ────────────────────────────────────────────────────

    private (Vector3 start, Vector3 end) FindLaserEndpoints(Vector3 worldPos, Quaternion q)
    {
        Vector3 fallbackStart = worldPos + q * new Vector3(-25f, 0, 0);
        Vector3 fallbackEnd   = worldPos + q * new Vector3( 25f, 0, 0);

        if (!_hasBossRoomBounds || GridSystem.Instance == null)
            return (fallbackStart, fallbackEnd);

        var dir = WorldDirToGridDir(q * Vector3.right);
        if (dir == Vector2Int.zero)
            return (fallbackStart, fallbackEnd);

        Vector2Int startCell, endCell;

        if (dir.y == 0) // 가로 레이저: bounds에서 랜덤 행 선택
        {
            int gridY = Random.Range(_bossRoomBounds.yMin + 1, _bossRoomBounds.yMax - 1);
            startCell = new Vector2Int(_bossRoomBounds.xMin, gridY);
            endCell   = new Vector2Int(_bossRoomBounds.xMax - 1, gridY);
        }
        else if (dir.x == 0) // 세로 레이저: bounds에서 랜덤 열 선택
        {
            int gridX = Random.Range(_bossRoomBounds.xMin + 1, _bossRoomBounds.xMax - 1);
            startCell = new Vector2Int(gridX, _bossRoomBounds.yMin);
            endCell   = new Vector2Int(gridX, _bossRoomBounds.yMax - 1);
        }
        else // 대각선(Phase2 8방향): 보스 위치에서 IsWalkable로 벽 탐색
        {
            var gridPos = GridSystem.Instance.WorldToGrid(transform.position);
            endCell   = WalkToWall(gridPos,  dir);
            startCell = WalkToWall(gridPos, -dir);
        }

        return (GridSystem.Instance.GridToWorld(startCell),
                GridSystem.Instance.GridToWorld(endCell));
    }

    // 대각선 레이저 전용: 벽(IsWalkable 실패)에 닿을 때까지 이동
    private Vector2Int WalkToWall(Vector2Int origin, Vector2Int dir)
    {
        var current = origin;
        int guard = 100;
        while (guard-- > 0 && GridSystem.Instance.IsWalkable(current + dir))
            current += dir;
        return current;
    }

    private static Vector2Int WorldDirToGridDir(Vector3 worldDir)
    {
        int x = Mathf.Clamp(Mathf.RoundToInt(worldDir.x), -1, 1);
        int y = Mathf.Clamp(Mathf.RoundToInt(worldDir.y), -1, 1);
        return new Vector2Int(x, y);
    }

    //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<< Phase2 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

    //=====================================패턴1==================================================
    protected override async UniTask Phase2Pattern1()
    {
        Debug.Log($"<color=red>[Phase2 행동패턴1]</color> ");
        for (int i = 0; i < 5; i++)
        {
            int ranX = Random.Range(-8, 8);
            Vector3 pos = new Vector3(ranX, 0, 0);
            Quaternion quaternion = Quaternion.Euler(0, 0, 90f);
            await LazerAttack(pos, quaternion, cancelToken.Token);
        }
        isBossAttacking = false;
    }

    //=====================================패턴2==================================================
    protected override async UniTask Phase2Pattern2()
    {
        Debug.Log($"<color=red>[Phase2 행동패턴2]</color> ");
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Quaternion q = Quaternion.Euler(0, 0, angle);
            await LazerAttack(Vector3.zero, q, cancelToken.Token);
        }
        isBossAttacking = false;
    }

    //=====================================패턴3==================================================
    protected override async UniTask Phase2Pattern3()
    {
        Debug.Log($"<color=red>[Phase2 행동패턴3]</color> ");
        for (int i = 0; i < 5; i++)
        {
            int ranY = Random.Range(-4, 4);
            Vector3 pos = new Vector3(0, ranY, 0);
            await LazerAttack(pos, Quaternion.identity, cancelToken.Token);
        }
        isBossAttacking = false;
    }
}
