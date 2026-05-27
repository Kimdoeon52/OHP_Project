using UnityEngine;

// 보스가 소환하는 크리스탈. 같은 prefab을 색/크기/지속시간 파라미터로 변형해 3가지 용도로 사용:
//  - 추격 크리스탈 (Phase1 Pattern3): 기본 색, 짧은 lifetime
//  - 블로킹 크리스탈 (Phase2 패시브): 붉은 색, 약 10초 lifetime
//  - 죽음 크리스탈 (사망 패턴):       2배 크기 + 붉은 색
// 데미지 판정과 워닝은 소환자(보스)가 처리. 이 스크립트는 외형/수명만 책임짐.
public class Crystal : MonoBehaviour
{
    [SerializeField] private float defaultLifetimeSec = 1f;   // Setup()을 호출하지 않을 때 기본 수명

    private SpriteRenderer sr;
    private float lifetimeSec;

    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        lifetimeSec = defaultLifetimeSec;
    }

    // 외부 호출 후 OnEnable에서 Destroy 예약하도록 lifetimeSec만 갱신.
    public void Setup(Color tint, float scaleMultiplier, float lifetime)
    {
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = tint;
        if (scaleMultiplier > 0f) transform.localScale *= scaleMultiplier;
        if (lifetime > 0f) lifetimeSec = lifetime;
    }

    private void OnEnable()
    {
        // Setup이 호출되지 않은 경우 defaultLifetimeSec 사용.
        if (lifetimeSec > 0f) Destroy(gameObject, lifetimeSec);
    }
}
