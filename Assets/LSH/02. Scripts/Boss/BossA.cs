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

//다음에 할꺼 가로 세로 레이져 공격 만들기.
public class BossA : BossBase
{
    [Header("보스 화면 가리기 패턴")]
    [SerializeField] private Image image;
    [SerializeField] private Image image2;

    [Header("보스 총알")]
    [SerializeField] private GameObject bossABullet;

    [Header("레이저 설정")]
    [SerializeField] private LineRenderer dangerLinePrefab; // LineRenderer가 달린 프리팹
    [SerializeField] private GameObject lazerAttackPrefab; // 실제 데미지 판정이 있는 프리팹

    protected override void Start()
    {
        base.Start();
        maxHp = 100;
        currentHp = maxHp;
        AddActionCase(BossAction.Pattern1, 30);
        AddActionCase(BossAction.Pattern2, 30);
        AddActionCase(BossAction.Pattern3, 40);
        image.gameObject.SetActive(false);
        image2.gameObject.SetActive(false);
    }
    protected override void Update()
    {
        base.Update();
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            bossLevel = 2; //다음 페이즈로 넘어감.
            bossAnimation.SetBool("Phase2", true);
            patternDelay = patternDelay / 2; //패턴 행동 딜레이 절반으로 줄임.
        }
    }
    //=====================================보스 행동 패턴 구현=====================================

    //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<< Phase 1 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

    //===================================== 패턴1 ==================================================
    protected override async UniTask Phase1Pattern1()
    {
        Debug.Log($"<color=red>[행동패턴1]</color> ");
        ImageAttack().Forget();
        await UniTask.Delay(patternDelay); //패턴1 행동 딜레이
        isBossAttacking = false; //보스가 공격을 마쳤음을 나타냄
    }

    private async UniTask ImageAttack() //화면 가리기 공격
    {
        Debug.Log($"<color = red > [화면 가리기 공격!] </color >");
        int randomValue = Random.Range(0, 2);
        switch (randomValue)
        {
            case 0:
                image.gameObject.SetActive(true);
                break;
            case 1:
                image2.gameObject.SetActive(true);
                break;
        }
        await UniTask.Delay(3000); //공격 딜레이
        ImageOff();
    }

    void ImageOff()
    {
        image.gameObject.SetActive(false);
        image2.gameObject.SetActive(false);
    }
    //===================================== 가로레이져2 ==================================================    
    protected override async UniTask Phase1Pattern2()
    {
        Debug.Log($"<color=red>[행동패턴2]</color> ");

        // 화면상의 랜덤한 높이(Y축) 결정
        int ranY = Random.Range(-4, 4);
        Vector3 pos = new Vector3(0, ranY, 0);

        // 공격 실행
        await LazerAttack(pos, Quaternion.identity, cancelToken.Token);

        await UniTask.Delay(patternDelay, cancellationToken: cancelToken.Token);
        isBossAttacking = false;
    }

    //===================================== 패턴3 ==================================================
    protected override async UniTask Phase1Pattern3()
    {
        Debug.Log($"<color=red>[행동패턴3]</color> ");
        // 화면상의 랜덤한 가로 위치(X축) 결정
        int ranX = Random.Range(-8, 8);
        Vector3 pos = new Vector3(ranX, 0, 0);

        // 90도 회전시켜 세로로 만듦
        Quaternion quaternion = Quaternion.Euler(0, 0, 90f);

        await LazerAttack(pos, quaternion, cancelToken.Token);

        await UniTask.Delay(patternDelay, cancellationToken: cancelToken.Token);
        isBossAttacking = false;
    }
    //===================================== 레이저 공격 구현 =======================================
    private async UniTask LazerAttack(Vector3 pos, Quaternion q, CancellationToken token)
    {
        // 1. 예고선 생성
        LineRenderer dangerLine = Instantiate(dangerLinePrefab, pos, q);

        dangerLine.useWorldSpace = false;
        dangerLine.positionCount = 2;

        // 로컬 좌표 기준이므로 (0,0,0)을 중심으로 좌우(혹은 상하)로 길게 뻗게 합니다.
        // 2D 게임이므로 충분히 긴 값(예: 50f)을 줍니다.
        dangerLine.SetPosition(0, new Vector3(-25f, 0, 0));
        dangerLine.SetPosition(1, new Vector3(25f, 0, 0));
        try
        {
            await UniTask.Delay(patternDelay, cancellationToken: token);
            if (dangerLine != null)
            {
                Destroy(dangerLine.gameObject);
            }
            GameObject lazer = Instantiate(lazerAttackPrefab, pos, q);

            await UniTask.Delay(patternDelay, cancellationToken: token);
            if (lazer != null)
            {
                Destroy(lazer);
            }
        }
        catch (System.OperationCanceledException)
        {
            // 공격이 취소된 경우 예고선을 즉시 제거
            Destroy(dangerLine.gameObject);
            return;
        }
    }
    //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<< Phase2 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

    //=====================================패턴1==================================================
    protected override async UniTask Phase2Pattern1()
    {
        Debug.Log($"<color=red>[Phase2 행동패턴1]</color> ");
        for (int i = 0; i < 5; i++)
        {
            // 화면상의 랜덤한 가로 위치(X축) 결정
            int ranX = Random.Range(-8, 8);
            Vector3 pos = new Vector3(ranX, 0, 0);

            // 90도 회전시켜 세로로 만듦
            Quaternion quaternion = Quaternion.Euler(0, 0, 90f);

            await LazerAttack(pos, quaternion, cancelToken.Token);
        }
        isBossAttacking = false;
    }
    //=====================================패턴2==================================================
    protected override async UniTask Phase2Pattern2()
    {
        Debug.Log($"<color=red>[Phase2 행동패턴2]</color> ");

        for(int i = 0; i < 8; i++)
        {
            // 45도씩 회전하여 8방향으로 레이저 공격
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

            // 공격 실행
            await LazerAttack(pos, Quaternion.identity, cancelToken.Token);
        }
        isBossAttacking = false;
    }
}


