using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Tilemaps;
using DG.Tweening;

public enum BossAction
{
    Pattern1, 
    Pattern2,
    Pattern3
}

[System.Serializable]
public class ActionCases
{
    public BossAction action;
    [Range(0, 100)] public int patternChance;
}
public class BossBase : EnemyNormalBASE
{
    // 피격 이펙트 관련 필드
    protected SpriteRenderer[] _spriteRenderers;
    protected Dictionary<SpriteRenderer, Color> _originalColors = new Dictionary<SpriteRenderer, Color>();
    protected List<Tween> _activeTweens = new List<Tween>();

    //[Header("보스 데이터")]
    //[SerializeField] protected int maxHp;
    //[SerializeField] protected int currentHp;

    [Header("보스 레벨")] //레벨에 따라서 보스 패턴이나 보스의 능력치가 달라질 수 있음
    [SerializeField] protected int bossLevel;

    [Header("보스 체력 데이터")]
    [SerializeField] protected BossStatData statData; //기본/강화 체력 SO

    // 강화 시 보스 데미지 배율 (고정값).
    private const float EnhancedDamageMultiplier = 1.5f;

    // isEnhanced: 보스 강화 챌린지면 강화.
    // TODO(다회차): 전역 회차 카운터/세이브 도입 시 OR 조건 추가 — 주입 지점.
    public static bool IsBossEnhanced => ChallengeState.Has(ChallengeType.BossBuff);

    // 보스 고정 데미지에 강화 배율 적용. 패턴/소환물 등 모든 데미지 출처가 공유.
    public static int ScaleBossDamage(int damage) => IsBossEnhanced ? Mathf.RoundToInt(damage * EnhancedDamageMultiplier) : damage;

    [Header("보스 패턴")]
    [SerializeField] protected List<ActionCases> actionCases = new List<ActionCases>();

    [Header("보스 패턴 딜레이 (초)")]
    [SerializeField] protected int patternDelay; //보스 패턴 행동 사이의 딜레이 시간

    [Header("페이즈 전환 설정")]
    [SerializeField, Range(0.05f, 1f)] protected float phase2DelayMultiplier = 0.3f; //Phase 2 진입 시 patternDelay에 곱하는 배율

    [Header("보스전 시간")]
    [SerializeField] protected float bossFightTimeLimit = 120f; //보스전 진입 시 제한 시간(초)으로 고정

    [Header("경고 타일맵 (시각 표시 전용 — 판정 아님)")]
    [SerializeField] protected Tilemap warningTilemap; //공격 예고를 칠할 전용 타일맵
    [SerializeField] protected TileBase warningTile;   //예고 칸에 칠할 타일
    [SerializeField] protected Sprite warningSprite;    //(경고 스프라이트 생략용 등)

    [Header("사망 연출 공통 설정")]
    [SerializeField] protected GameObject deathParticlePrefab; // 사망 시 재생할 파티클 프리팹

    [Header("보스 전투 스탯")]
    [SerializeField] protected int evasion = 15;
    [SerializeField] protected int accuracy = 20;

    public int Evasion => evasion;
    public int Accuracy => accuracy;

    protected bool isBossAttacking; //보스가 공격 중인지 여부를 나타내는 변수
    protected Animator bossAnimation; //보스 애니메이션을 제어하기 위한
    protected CancellationTokenSource cancelToken;
    protected GridPlayer player; //플레이어 참조


    public Tilemap WarningTilemap => warningTilemap;
    public TileBase WarningTile => warningTile;
    public GridPlayer Player => player;
    public virtual Vector2Int BossSize => new Vector2Int(1, 1);
    public virtual Vector2Int GridPosition => Vector2Int.zero;

    [SerializeField] protected BossHpBar hpBar;
    public BossHpBar HpBar { get => hpBar; set => hpBar = value; }

    [Header("AttackFloor 타일맵 (이동/공격 제한)")]
    [SerializeField] protected Tilemap attackFloorTilemap;

    public Tilemap AttackFloorTilemap => attackFloorTilemap;

    protected RectInt mapBounds;
    protected bool hasMapBounds;

    public RectInt MapBounds => mapBounds;
    public bool HasMapBounds => hasMapBounds;

    private bool _patternDelayConverted; // patternDelay 초→ms 변환이 한 번만 일어나도록 가드

    protected virtual void OnEnable()
    {
        // 상태이상 틴트(PlayerStatusVisual)보다 먼저 진짜 색을 저장 — 피격 플래시 복구 기준색
        CacheOriginalColors();

        // 스폰 시점(피격 전)에 상태 비주얼을 미리 부착 — normalColor가 플래시 빨강이 아닌 진짜 색으로 캐싱되게
        PlayerStatusVisual.EnsureFor(gameObject);

        if (attackFloorTilemap == null)
        {
            if (MapGenerator.Instance != null && MapGenerator.Instance.Tilemaps != null && MapGenerator.Instance.Tilemaps.attackFloor != null)
            {
                attackFloorTilemap = MapGenerator.Instance.Tilemaps.attackFloor;
            }

            if (attackFloorTilemap == null)
            {
                var tilemaps = GetComponentsInChildren<Tilemap>();
                foreach (var tm in tilemaps)
                {
                    if (tm.name.ToLower().Contains("attack") || tm.name.ToLower().Contains("floor"))
                    {
                        attackFloorTilemap = tm;
                        break;
                    }
                }
                if (attackFloorTilemap == null && tilemaps.Length > 0)
                {
                    attackFloorTilemap = tilemaps[0];
                }
            }
        }

        // 보스 체력 적용 — isEnhanced면 강화 체력.
        ApplyStats();

        currentHp = maxHp;

        // 1) 핵심 시스템 초기화 — 이 블록은 절대 예외로 끊기면 안 됨 (이후 모든 패턴이 의존).
        cancelToken = new CancellationTokenSource();
        actionCases.Clear();
        bossLevel = 1;
        bossAnimation = GetComponent<Animator>();

        // patternDelay는 인스펙터 값(초)을 ms로 한 번만 변환. OnEnable이 재호출돼도 누적 안 됨.
        if (!_patternDelayConverted)
        {
            patternDelay *= 1000;
            _patternDelayConverted = true;
        }

        player = FindFirstObjectByType<GridPlayer>();

        // 2) HP 바 연결. 프리팹으로 소환된 경우 인스펙터 참조가 끊겨 있을 수 있어,
        //    null이면 씬에서 자동 검색하고, 그래도 없으면 경고만 띄우고 계속 진행.
        if (hpBar == null) hpBar = FindFirstObjectByType<BossHpBar>(FindObjectsInactive.Include);
        if (hpBar != null) hpBar.Init(this);
        else Debug.LogWarning("BossBase.OnEnable: BossHpBar를 찾지 못함. HP바 표시가 동작하지 않습니다.");

        // 3) 인스펙터 미스매치 감지: warningTilemap이 GridSystem의 wall 타일맵과 같으면 경고.
        //    (자주 일어나는 실수 — 워닝 타일이 WallTilemap에 칠해지는 원인)
        if (warningTilemap != null && GridSystem.Instance != null
            && GridSystem.Instance.Tilemaps != null
            && warningTilemap == GridSystem.Instance.Tilemaps.wall)
        {
            Debug.LogError($"BossBase: warningTilemap이 GridSystem의 WallTilemap과 동일합니다. " +
                           $"인스펙터에서 별도의 WarningTilemap을 지정해 주세요. (현재 충돌 중)");
        }

        // 4) 보스전 진입 시 제한 시간을 bossFightTimeLimit(초)로 고정하고 타이머 시작.
        //    TimeSystem에 SetTime이 없어 AddTime(목표 - 현재)로 보정 (실험용 — 추후 SetTime 도입 검토).
        if (TimeSystem.Instance != null)
        {
            int currentDepth = 5;
            if (RunState.Instance != null)
            {
                currentDepth = RunState.Instance.CurrentDepth;
            }
            else if (MapGenerator.Instance != null)
            {
                currentDepth = MapGenerator.Instance.CurrentDepth;
            }

            if (currentDepth != 15)
            {
                TimeSystem.Instance.AddTime(bossFightTimeLimit - TimeSystem.Instance.TimeRemaining);
                TimeSystem.Instance.StartTimer();
                Debug.Log($"<color=cyan>[보스전]</color> 제한 시간 {bossFightTimeLimit}초로 고정, 타이머 시작.");
            }
            else
            {
                Debug.Log($"<color=cyan>[보스전]</color> 15층 보스전 진입: 타이머 재설정을 건너뛰고 무한 상태를 유지합니다.");
            }
        }
 
        WaitBossCoolTime().Forget();
    }

    // 스폰 시 1회. SO 체력을 isEnhanced에 따라 maxHp에 반영. SO 미연결이면 인스펙터 값 유지.
    protected virtual void ApplyStats()
    {
        if (statData != null)
            maxHp = statData.ResolveHp(IsBossEnhanced);
    }

    protected virtual void OnDisable()
    {
        if (cancelToken != null)
        {
            cancelToken.Cancel();
            cancelToken.Dispose();
            cancelToken = null;
        }
        ClearAllWarnings();

        if (_activeTweens != null)
        {
            foreach (var t in _activeTweens)
            {
                if (t != null && t.IsActive()) t.Kill();
            }
            _activeTweens.Clear();
        }
    }
    protected async UniTask WaitBossCoolTime()
    {
        isBossAttacking = true; //일단 공격 대기
        await UniTask.Delay(patternDelay, cancellationToken: cancelToken.Token); //보스 패턴 행동 사이의 딜레이를 기다림.
        isBossAttacking = false; //딜레이가 끝나면 보스가 공격할 수 있도록 설정
    }
    //public virtual void OnTurnUpdate()
    //{
    ////    보스는 Update() +UniTask 기반 패턴으로 독립 동작
    //}

    protected virtual void Update()
    {
        if (Input.GetKeyDown(KeyCode.K)) //디버그용 데미지 (P는 TestScripts 소환 키와 충돌)
        {
            Debug.Log($"<color=yellow>[디버그]  아야!!!!!!!!</color>");
            BossTakeDamage(10);
        }

        if (isBossAttacking)
            return;
        PhaseState().Forget();
    }

    protected virtual void OnDestroy()
    {
        if (cancelToken != null)
        {
            cancelToken.Cancel(); //보스가 파괴될 때 취소 토큰을 취소하여 모든 비동기 작업을 중단
            cancelToken.Dispose(); //취소 토큰 소멸
        }
        ClearAllWarnings(); //취소된 패턴이 남긴 워닝 타일 정리
    }



    //==================== 데미지 / 페이즈 전환 / 사망 ====================
    protected bool isDying; // 죽음 패턴 진행 중 추가 데미지 무시 + 재진입 방지

    // 보스가 데미지를 받을 때 호출. 페이즈 전환은 시간 기반으로 이관됨(PhaseState 참고) — 여기선 HP/사망만 처리.
    // HP가 0 이하가 되면 isDying=true로 잠그고 OnDeath() 호출 (기본은 즉시 Destroy, 서브클래스가 죽음 패턴 오버라이드 가능).
    public virtual void BossTakeDamage(int amount)
    {
        if (amount <= 0 || isDying) return;

        int beforeHp = currentHp;
        currentHp -= amount;
        int finalDamage = Mathf.Max(0, beforeHp - currentHp);
        PublishBossCombatText(finalDamage);
        if (finalDamage > 0)
        {
            GameEvents.OnAudioEventRequested?.Invoke(AudioEventId.BossHit);
            PlayHitEffect();
        }
        Debug.Log($"<color=red>[보스 데미지]</color> 보스가 {amount}만큼 데미지를 받음. 현재 HP: {currentHp}");
        if (hpBar != null) hpBar.UpdateBar();

        if (currentHp <= 0)
        {
            isDying = true;
            OnDeath().Forget();
        }
    }

    // 자식 포함 모든 SpriteRenderer의 최초 색을 1회 저장. 상태이상 틴트 전에 호출돼야 함.
    private void CacheOriginalColors()
    {
        if (_spriteRenderers != null)
            return;

        _spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in _spriteRenderers)
        {
            if (sr != null && !_originalColors.ContainsKey(sr))
            {
                _originalColors[sr] = sr.color;
            }
        }
    }

    public void PlayHitEffect()
    {
        // OnEnable에서 미리 캐싱하지만, 혹시 비어 있으면 보강
        CacheOriginalColors();

        // 진행 중이던 기존 트윈 중단
        foreach (var t in _activeTweens)
        {
            if (t != null && t.IsActive()) t.Kill();
        }
        _activeTweens.Clear();

        // 빨간색으로 변경 후 0.1초 동안 원래 색상으로 복구
        foreach (var sr in _spriteRenderers)
        {
            if (sr == null) continue;
            if (_originalColors.TryGetValue(sr, out Color originColor))
            {
                sr.color = Color.red;
                Tween t = sr.DOColor(originColor, 0.1f).SetEase(Ease.OutQuad);
                _activeTweens.Add(t);
            }
        }
    }

    // 사망 시점 훅. 기본 동작은 즉시 Destroy. 서브클래스가 죽음 패턴(연출, 즉사 영역 등)을 위해 오버라이드.
    // 죽음 패턴 끝에 반드시 Destroy(gameObject)를 호출해야 함.
    private void PublishBossCombatText(int finalDamage)
    {
        if (finalDamage <= 0) return;

        // TODO(UI): 보스 피격 숫자 위치/폰트는 CombatFloatingTextSpawner 또는 전용 floatingTextPrefab에서 조정합니다.
        CombatTextType textType = IsBossCriticalDamage(finalDamage)
            ? CombatTextType.CriticalDamage
            : CombatTextType.Damage;

        GameEvents.OnCombatTextRequested?.Invoke(new CombatTextRequest(transform.position, finalDamage.ToString(), finalDamage, textType));
    }

    private bool IsBossCriticalDamage(int finalDamage)
    {
        int maxDamage = CombatTextCriticalContext.CurrentMaxDamage;
        if (finalDamage <= 0 || maxDamage <= 0)
            return false;

        return finalDamage >= Mathf.CeilToInt(maxDamage * 0.8f);
    }

    protected virtual UniTask OnDeath()
    {
        // 보스 처치 시점 — 구독자에게 알림. MapGenerator 가 보스 출구 마커를 이때 스폰한다.
        // (서브클래스는 죽음 연출 끝에서 base.OnDeath() 를 호출해 이 알림이 발행되게 한다.)
        GameEvents.OnBossDefeated?.Invoke();
        return UniTask.CompletedTask;
    }

    protected virtual void EnterPhase2()
    {
        bossLevel = 2;
        patternDelay = Mathf.Max(50, Mathf.RoundToInt(patternDelay * phase2DelayMultiplier));
        Debug.Log($"<color=orange>[보스 Phase 2 진입]</color> patternDelay={patternDelay}ms");
    }

    //====================================행동=====================================
    protected virtual void AddActionCase(BossAction bossAction, int chance) //보스별 행동 추가.
    {
        actionCases.Add(new ActionCases { action = bossAction, patternChance = chance });
    }
    protected virtual async UniTask Phase1BossPattern() //첫번째 페이즈
    {
        isBossAttacking = true; //보스가 공격 중임을 나타냄
        BossAction bossAction = BossPatternPer();
        await UniTask.Delay(patternDelay, cancellationToken: cancelToken.Token); //패턴 선택 후 딜레이
        RequestBossPatternSfx(bossAction);
        switch(bossAction)
        {
            case BossAction.Pattern1:
                Phase1Pattern1().Forget();
                //패턴1 행동 구현
                break;
            case BossAction.Pattern2:
                Phase1Pattern2().Forget();
                //패턴2 행동 구현
                break;
            case BossAction.Pattern3:
                Phase1Pattern3().Forget();
                //패턴3 행동 구현
                break;
        }
    }

    protected virtual async UniTask Phase2BossPattern() //두번째 페이즈 보스 패턴
    {
        isBossAttacking = true; //보스가 공격 중임을 나타냄
        BossAction bossAction = BossPatternPer();
        await UniTask.Delay(patternDelay, cancellationToken: cancelToken.Token); //패턴 선택 후 행동 시작까지의 딜레이
        RequestBossPatternSfx(bossAction);
        switch (bossAction)
        {
            case BossAction.Pattern1:
                Phase2Pattern1().Forget();
                //패턴1 행동 구현
                break;
            case BossAction.Pattern2:
                Phase2Pattern2().Forget();
                //패턴2 행동 구현
                break;
            case BossAction.Pattern3:
                Phase2Pattern3().Forget();
                //패턴3 행동 구현
                break;
        }
    }
    protected virtual void RequestBossPatternSfx(BossAction bossAction)
    {
    }

    protected virtual BossAction BossPatternPer() //보스 패턴 확률 계산
    {
        int totalWeight = 0;
        foreach (var actionCase in actionCases)
        {
            totalWeight += actionCase.patternChance; //각 패턴의 가중치 합산
        }
        if(totalWeight == 0) return 0; //가중치가 모두 0인 경우 예외 처리
        int randomValue = Random.Range(0, totalWeight); //0부터 총 가중치 사이의 랜덤 값 생성
        int currentWeight = 0;

        foreach(var actionCase in actionCases)
        {
            currentWeight += actionCase.patternChance; //현재 패턴의 가중치를 누적
            if (randomValue < currentWeight) //랜덤 값이 현재 누적 가중치보다 작은 경우 해당 패턴 선택
            {
                return actionCase.action; //선택된 패턴 반환
            }
        }
        return 0; //예외 처리, 모든 패턴이 선택되지 않은 경우
    }
    //==================================보스 Phase 구현======================================
    // 페이즈는 남은 시간으로 전환. 남은 시간이 phase2TimeThreshold 이하가 되면 1회 EnterPhase2.
    // (3페이즈 이상 보스는 PhaseState를 오버라이드해서 자체 시간 분기 — 예: BossA)
    protected virtual async UniTask PhaseState() //첫 페이즈는 회피구간 두번째 페이즈는 공격구간
    {
        if (bossLevel == 1 && currentHp <= maxHp * 0.5f)
        {
            Debug.Log($"<color=orange>[보스 Phase 2 진입 조건 충족]</color> 체력 {currentHp} ≤ {maxHp * 0.5f}");
            EnterPhase2();
        }

        switch(bossLevel)
        {
            case 1: //첫번째 페이즈
                await Phase1BossPattern();
                break;
            case 2: //두번째 페이즈
                await Phase2BossPattern();
                break;
        }
    }
    //====================================Phase1 패턴====================================
    protected virtual async UniTask Phase1Pattern1()
    {
        await UniTask.Delay(1000); //패턴1 행동 딜레이
         Debug.Log($"<color=red>[Phase1행동패턴1]</color> ");
    }
    protected virtual async UniTask Phase1Pattern2()
    {
        await UniTask.Delay(1000); //패턴1 행동 딜레이
        Debug.Log($"<color=red>[Phase1행동패턴2]</color> ");
    }
    protected virtual async UniTask Phase1Pattern3()
    {
        await UniTask.Delay(1000); //패턴1 행동 딜레이
        Debug.Log($"<color=red>[Phase1행동패턴3]</color> ");
    }

    //=====================================Phase2 패턴=================================================
    protected virtual async UniTask Phase2Pattern1()
    {
        await UniTask.Delay(1000); //패턴1 행동 딜레이
        Debug.Log($"<color=red>[Phase2행동패턴1]</color> ");
    }
    protected virtual async UniTask Phase2Pattern2()
    {
        await UniTask.Delay(1000); //패턴1 행동 딜레이
        Debug.Log($"<color=red>[Phase2행동패턴2]</color> ");
    }
    protected virtual async UniTask Phase2Pattern3()
    {
        await UniTask.Delay(1000); //패턴1 행동 딜레이
        Debug.Log($"<color=red>[Phase2행동패턴3]</color> ");
    }

    //==================== 경고 표시 (참조 카운트 방식) ====================
    // 같은 셀을 여러 패턴이 동시에 워닝할 수 있으므로 카운터로 관리.
    // ShowWarning이 +1, ClearWarning이 -1. 0이 될 때만 실제 타일 제거.
    // 이렇게 안 하면 한 쪽이 ClearWarning 호출 시 다른 쪽의 활성 워닝까지 지워버림.
    private readonly Dictionary<Vector2Int, int> _warningRefCount = new Dictionary<Vector2Int, int>();

    public void ShowWarning(IEnumerable<Vector2Int> cells)
    {
        if (warningTilemap == null || warningTile == null || cells == null) return;
        foreach (var c in cells) ShowWarningInternal(c);
    }

    public void ShowWarning(Vector2Int cell)
    {
        if (warningTilemap == null || warningTile == null) return;
        ShowWarningInternal(cell);
    }

    public void ClearWarning(IEnumerable<Vector2Int> cells)
    {
        if (warningTilemap == null || cells == null) return;
        foreach (var c in cells) ClearWarningInternal(c);
    }

    public void ClearWarning(Vector2Int cell)
    {
        if (warningTilemap == null) return;
        ClearWarningInternal(cell);
    }

    private void ShowWarningInternal(Vector2Int cell)
    {
        if (_warningRefCount.TryGetValue(cell, out int count))
        {
            _warningRefCount[cell] = count + 1;
        }
        else
        {
            _warningRefCount[cell] = 1;
            warningTilemap.SetTile(new Vector3Int(cell.x, cell.y, 0), warningTile);
        }
    }

    private void ClearWarningInternal(Vector2Int cell)
    {
        if (!_warningRefCount.TryGetValue(cell, out int count)) return;
        if (count <= 1)
        {
            _warningRefCount.Remove(cell);
            warningTilemap.SetTile(new Vector3Int(cell.x, cell.y, 0), null);
        }
        else
        {
            _warningRefCount[cell] = count - 1;
        }
    }

    // 보스 파괴/리셋 시 남은 워닝 강제 정리 (취소된 패턴이 ClearWarning을 못 부른 경우 대비).
    protected void ClearAllWarnings()
    {
        if (warningTilemap == null) return;
        foreach (var kv in _warningRefCount)
            warningTilemap.SetTile(new Vector3Int(kv.Key.x, kv.Key.y, 0), null);
        _warningRefCount.Clear();
    }

    // 플레이어 공격 시 회피 및 보스 정확도 비교 판정 후 데미지를 가하는 공통 함수
    public bool TryDamagePlayer(int rawDamage)
    {
        if (player == null || player.IsDead || rawDamage <= 0) return false;

        int finalDamage = ScaleBossDamage(rawDamage); // 강화 시 1.5배 적용

        NYHPlayerStats playerStats = player.GetComponent<NYHPlayerStats>();
        if (playerStats == null)
            playerStats = NYHPlayerStats.Instance;
        int targetEvasion = playerStats != null ? playerStats.Evasion : 0;

        int missChancePercent = Mathf.Clamp(targetEvasion - accuracy, 0, 50);
        if (Random.Range(0, 100) < missChancePercent)
        {
            GameEvents.OnCombatTextRequested?.Invoke(new CombatTextRequest(player.transform.position, "Miss", 0, CombatTextType.Miss));
            Debug.Log($"[보스 공격 빗나감] 보스 정확도 {accuracy}, 플레이어 회피 {targetEvasion}");
            return false;
        }

        Debug.Log($"<color=red>[보스 공격 히트]</color> 플레이어에게 데미지 가함. 데미지: {finalDamage}");
        player.TakeDamage(finalDamage);
        return true;
    }

    // 데이터 기반 히트 판정. 플레이어 격자 좌표가 공격 칸 집합에 들어 있으면 데미지.
    protected void DamagePlayerIfInCells(IEnumerable<Vector2Int> cells, int damage)
    {
        if (player == null || player.IsDead) return;
        foreach (var c in cells)
        {
            if (player.GridPosition == c)
            {
                TryDamagePlayer(damage);
                return;
            }
        }
    }




    protected void CacheMapBounds()
    {
        if (attackFloorTilemap != null)
        {
            attackFloorTilemap.CompressBounds();
            var b = attackFloorTilemap.cellBounds;
            mapBounds = new RectInt(b.xMin, b.yMin, b.size.x, b.size.y);
            hasMapBounds = true;
            return;
        }

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
        mapBounds = new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        hasMapBounds = true;
    }

    protected bool IsInAttackFloor(Vector2Int cell)
    {
        // 1) 벽 타일이나 크리스탈 낙하 등으로 인해 막힌 셀(IsWalkable이 false)로는 보스가 절대 이동할 수 없도록 제한
        if (GridSystem.Instance != null && !GridSystem.Instance.IsWalkable(cell))
            return false;

        // 2) 보스 공격 전용 구역(AttackFloor) 타일맵이 지정된 경우 해당 타일 위에 있는지도 검사
        if (attackFloorTilemap != null)
            return attackFloorTilemap.GetTile(new Vector3Int(cell.x, cell.y, 0)) != null;

        return true;
    }

    protected bool IsAreaInAttackFloor(Vector2Int bottomLeft, Vector2Int size)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                if (!IsInAttackFloor(bottomLeft + new Vector2Int(x, y)))
                    return false;
            }
        }
        return true;
    }

    protected void SpawnDeathParticle()
    {
        if (deathParticlePrefab != null)
        {
            GameObject go = Instantiate(deathParticlePrefab, transform.position, Quaternion.identity);
            // 파티클 자동 제거 예약 (예: 5초 후 자동 삭제)
            Destroy(go, 5f);
        }
    }
}
