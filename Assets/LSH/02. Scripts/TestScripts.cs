using UnityEngine;

// 임시 보스 소환 트리거. 실제 게임에선 BossRoomLoader가 방 진입 시 호출하게 될 자리.
// 지금은 P 키로 수동 소환만 함. 같은 보스가 두 번 생성되지 않도록 spawnedBoss를 들고 있다 살아있으면 무시.
public class TestScripts : MonoBehaviour
{
    [Header("소환할 보스")]
    [SerializeField] private GameObject bossPrefab;           // BossB 프리팹 (인스펙터에서 드래그)
    [SerializeField] private Transform spawnPoint;            // 소환 위치 (비워두면 이 오브젝트 위치 사용)

    [Header("키 설정")]
    [SerializeField] private KeyCode spawnKey = KeyCode.P;    // 임시 소환 키

    private GameObject spawnedBoss;

    void Update()
    {
        if (!Input.GetKeyDown(spawnKey)) return;
        if (spawnedBoss != null) return; // 이미 살아있으면 중복 소환 금지

        SpawnBoss();
    }

    // 외부(보스방 진입 트리거 등)에서도 호출할 수 있도록 public.
    public void SpawnBoss()
    {
        if (bossPrefab == null)
        {
            Debug.LogWarning("TestScripts: bossPrefab이 비어 있습니다.");
            return;
        }

        Vector3 pos = spawnPoint != null ? spawnPoint.position : transform.position;
        spawnedBoss = Instantiate(bossPrefab, pos, Quaternion.identity);
        Debug.Log($"<color=cyan>[테스트]</color> 보스 소환 @ {pos}");
    }
}
