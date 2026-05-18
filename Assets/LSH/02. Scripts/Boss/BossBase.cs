using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

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
public class BossBase : MonoBehaviour
{
    [Header("보스 데이터")]
    [SerializeField] protected int maxHp;
    [SerializeField] protected int currentHp;
    

    [Header("보스 레벨")] //레벨에 따라서 보스 패턴이나 보스의 능력치가 달라질 수 있음
    [SerializeField] protected int bossLevel;

    [Header("보스 패턴")]
    [SerializeField] protected List<ActionCases> actionCases = new List<ActionCases>();

    [Header("보스 패턴 딜레이 (초)")]
    [SerializeField] protected int patternDelay; //보스 패턴 행동 사이의 딜레이 시간

    protected bool isBossAttacking; //보스가 공격 중인지 여부를 나타내는 변수
    protected Animator bossAnimation; //보스 애니메이션을 제어하기 위한
    protected CancellationTokenSource cancelToken;
    protected GridPlayer player; //플레이어 참조
    protected virtual void Start()
    {
        //base.Start(); // EnemyBase: Player 찾기, 격자 스냅, TurnManager 등록

        cancelToken = new CancellationTokenSource();
        actionCases.Clear();
        bossLevel = 1; //보스 레벨 초기화
        bossAnimation = GetComponent<Animator>(); //보스 애니메이션 컴포넌트 가져오기
        patternDelay *= 1000; //초로 변환.
        player = FindFirstObjectByType<GridPlayer>(); //씬에서 GridPlayer 참조 가져오기
        //각 보스 체력이나 레벨에 따른 패턴 확률 설정은 각 보스마다 다르게 구현.
        WaitBossCoolTime().Forget(); //보스 패턴 행동 사이의 딜레이를 기다림.
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
    }

    protected virtual void PatternStop()
    {
        //보스 패턴을 중단
        if (cancelToken != null)
        {
            cancelToken.Cancel();
            cancelToken.Dispose();
        }
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
        await UniTask.Delay(patternDelay); //패턴 선택 후 딜레이
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
        await UniTask.Delay(patternDelay); //패턴 선택 후 행동 시작까지의 딜레이
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
    protected virtual async UniTask PhaseState() //첫 페이즈는 회피구간 두번째 페이즈는 공격구간
    {
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
}
