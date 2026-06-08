using UnityEngine;

// 보스가 소환하는 크리스탈. 같은 prefab을 색/크기/지속시간 파라미터로 변형해 사용.
// 일반 Crystal은 생성 후 설정된 lifetimeSec(기본 2초 등) 뒤에 파괴되는 평범한 외형/수명용 크리스탈입니다.
public class Crystal : MonoBehaviour
{
    [SerializeField] private float defaultLifetimeSec = 2f;   // 기본 수명 2초

    private SpriteRenderer sr;
    private float lifetimeSec;

    private void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        lifetimeSec = defaultLifetimeSec;
    }

    public void Setup(Color tint, float scaleMultiplier, float lifetime)
    {
        if (sr == null) sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) sr.color = tint;
        if (scaleMultiplier > 0f) transform.localScale *= scaleMultiplier;
        if (lifetime > 0f) lifetimeSec = lifetime;
    }

    private void Start()
    {
        // Setup() 호출로 수명(lifetimeSec)이 지정된 후 Destroy 예약
        if (lifetimeSec > 0f) Destroy(gameObject, lifetimeSec);
    }
}
