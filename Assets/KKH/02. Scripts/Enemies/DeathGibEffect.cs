using System.Collections;
using UnityEngine;

public class DeathGibEffect : MonoBehaviour
{
    [Header("스프라이트 설정")]
    [Tooltip("수동으로 슬라이스한 고깃덩이 및 뼈 스프라이트들을 등록할 수 있습니다. 비워둘 경우 빨강/하얀색 픽셀로 자동 생성됩니다.")]
    [SerializeField] private Sprite[] gibSprites;

    [Header("이펙트 수치 설정")]
    [SerializeField] private int minGibCount = 6;
    [SerializeField] private int maxGibCount = 10;
    [SerializeField] private float minForce = 2.5f;
    [SerializeField] private float maxForce = 5.5f;
    [SerializeField] private float gravity = 9.8f;
    [SerializeField] private float lifetime = 0.6f;

    private void Start()
    {
        // Resources/Gibs 폴더가 존재할 경우 스프라이트 리스트를 자동으로 채웁니다.
        if (gibSprites == null || gibSprites.Length == 0)
        {
            gibSprites = Resources.LoadAll<Sprite>("Gibs");
        }

        int count = Random.Range(minGibCount, maxGibCount + 1);
        for (int i = 0; i < count; i++)
        {
            SpawnGibParticle();
        }

        // 효과 메인 오브젝트는 파티클들의 최대 수명 이후 삭제
        Destroy(gameObject, lifetime + 0.1f);
    }

    private void SpawnGibParticle()
    {
        GameObject gib = new GameObject("GibParticle");
        gib.transform.position = transform.position;
        gib.transform.parent = transform;

        SpriteRenderer sr = gib.AddComponent<SpriteRenderer>();
        sr.sortingOrder = 15; // 적 캐릭터보다 약간 위에 출력되도록 설정

        // 스프라이트 선택 또는 절차적 생성
        if (gibSprites != null && gibSprites.Length > 0)
        {
            sr.sprite = gibSprites[Random.Range(0, gibSprites.Length)];
        }
        else
        {
            // 폴백: 판타지 몬스터 테마 컬러 랜덤 생성 (보라 슬라임, 회색 돌, 주황 불씨, 어두운 가시)
            float rand = Random.value;
            Color color;
            int size;

            if (rand < 0.25f)
            {
                color = new Color(0.55f, 0.15f, 0.75f, 1f); // 보라 슬라임
                size = 3;
            }
            else if (rand < 0.5f)
            {
                color = new Color(0.45f, 0.45f, 0.45f, 1f); // 회색 돌 파편
                size = 4;
            }
            else if (rand < 0.75f)
            {
                color = new Color(1f, 0.45f, 0.05f, 1f);   // 주황 불꽃 잔해
                size = 3;
            }
            else
            {
                color = new Color(0.15f, 0.15f, 0.18f, 1f); // 어두운 가시/뿔 조각
                size = 4;
            }

            sr.sprite = CreateFallbackSprite(color, size);
        }

        // 360도 전방향으로 튕겨나갈 힘과 각도 설정
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float force = Random.Range(minForce, maxForce);
        Vector3 velocity = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * force;
        
        // 포물선 궤적을 예쁘게 만들기 위해 Y축 상방 힘 추가 보정
        velocity.y += Random.Range(1.5f, 3.5f);

        float rotationSpeed = Random.Range(-400f, 400f);

        StartCoroutine(SimulateGib(gib, velocity, rotationSpeed));
    }

    private IEnumerator SimulateGib(GameObject gib, Vector3 velocity, float rotSpeed)
    {
        float elapsed = 0f;
        SpriteRenderer sr = gib.GetComponent<SpriteRenderer>();
        Color startColor = sr.color;
        Vector3 startScale = gib.transform.localScale;

        while (elapsed < lifetime && gib != null)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lifetime;

            // 중력 가속도 연산
            velocity.y -= gravity * Time.deltaTime;

            // 좌표 갱신
            gib.transform.position += velocity * Time.deltaTime;

            // 자전 효과
            gib.transform.Rotate(0f, 0f, rotSpeed * Time.deltaTime);

            // 절반 이상 도달하면 서서히 크기가 작아지며 투명해짐
            if (t > 0.5f)
            {
                float fadeT = (t - 0.5f) / 0.5f;
                if (sr != null)
                {
                    sr.color = Color.Lerp(startColor, new Color(startColor.r, startColor.g, startColor.b, 0f), fadeT);
                }
                gib.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, fadeT);
            }

            yield return null;
        }

        if (gib != null)
        {
            Destroy(gib);
        }
    }

    private Sprite CreateFallbackSprite(Color color, int size)
    {
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Point;
        
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = color;
        }
        
        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 16f);
    }
}
