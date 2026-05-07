using Cysharp.Threading.Tasks;
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
    //===================================== 패턴2 ==================================================    
    protected override async UniTask Phase1Pattern2()
    {
        Debug.Log($"<color=red>[행동패턴2]</color> ");
        await UniTask.Delay(1000); //패턴1 행동 딜레이
        isBossAttacking = false; //보스가 공격을 마쳤음을 나타냄
    }

    //===================================== 패턴3 ==================================================
    protected override async UniTask Phase1Pattern3()
    {
        Debug.Log($"<color=red>[행동패턴3]</color> ");
        await UniTask.Delay(1000); //패턴1 행동 딜레이
        isBossAttacking = false; //보스가 공격을 마쳤음을 나타냄
    }

    //<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<<< Phase2 >>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>

    //=====================================패턴1==================================================
    protected override async UniTask Phase2Pattern1()
    {
        await UniTask.Delay(1000); //패턴1 행동 딜레이
        Debug.Log($"<color=red>[Phase2 행동패턴1]</color> ");
    }
    //=====================================패턴2==================================================
    protected override async UniTask Phase2Pattern2()
    {
        await UniTask.Delay(1000); //패턴1 행동 딜레이
        Debug.Log($"<color=red>[Phase2 행동패턴2]</color> ");
    }
}
