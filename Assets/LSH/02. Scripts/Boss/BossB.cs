using Cysharp.Threading.Tasks;
using UnityEngine;

public class BossB : BossBase
{
    protected override void Start()
    {
        base.Start();
        maxHp = 100;
        currentHp = maxHp;
        AddActionCase(BossAction.Pattern1, 20);
        AddActionCase(BossAction.Pattern2, 10);
    }
    protected override async UniTask Phase1Pattern1()
    {
        Debug.Log("보스B 패턴1 실행");
        await UniTask.Delay(1000);
        isBossAttacking = false; //공격이 끝났음을 나타냄
    }
    protected override async UniTask Phase1Pattern2()
    {
        Debug.Log("보스B 패턴2 실행");
        await UniTask.Delay(1500);
        isBossAttacking = false; //공격이 끝났음을 나타냄
    }
}
