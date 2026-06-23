using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 보유 열쇠 종류별 아이콘+개수 고정 표시 (우상단 HUD)
// 이름 라벨은 LocalizedText가 따로 처리 — 여기선 개수(숫자)만 갱신.
public class KeyInventoryHud : MonoBehaviour
{
    [System.Serializable]
    public class Slot
    {
        public KeyType type;
        [Tooltip("슬롯 루트")]
        public GameObject root;
        [Tooltip("아이콘")]
        public Image icon;
        [Tooltip("개수(숫자만)")]
        public TMP_Text countText;
    }

    [Tooltip("열쇠 종류별 슬롯")]
    [SerializeField] private Slot[] slots;
    [Tooltip("0개 슬롯 숨김 여부")]
    [SerializeField] private bool hideWhenEmpty = true;

    private void OnEnable()
    {
        GameEvents.OnKeyChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        GameEvents.OnKeyChanged -= Refresh;
    }

    // 전체 슬롯 갱신
    private void Refresh()
    {
        var inv = KeyInventory.Instance;
        foreach (var s in slots)
        {
            int n = inv != null ? inv.GetCount(s.type) : 0;
            if (s.countText != null)
                s.countText.text = n.ToString();
            if (hideWhenEmpty && s.root != null)
                s.root.SetActive(n > 0);
        }
    }
}
