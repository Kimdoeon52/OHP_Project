using UnityEngine;

// 에디터에서 꺼둔 오브젝트를 실행 시 켬 — 화면을 가리는 페이드·연출용
// 항상 켜져 있는 부모에 붙일 것
public class ActivateOnAwake : MonoBehaviour
{
    [Tooltip("켤 대상")]
    [SerializeField] private GameObject[] targets;

    private void Awake()
    {
        if (targets == null)
            return;

        foreach (GameObject target in targets)
        {
            if (target != null)
                target.SetActive(true);
        }
    }
}
