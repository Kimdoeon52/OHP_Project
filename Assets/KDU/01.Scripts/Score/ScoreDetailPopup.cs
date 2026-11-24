using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 랭킹 행 클릭 시 뜨는 상세 팝업 — 기록 정보·처치 수·챌린지·장착 장비 표시
// 이 스크립트가 붙은 오브젝트 자체를 켜고 끔. 씬에 비활성 상태로 배치.
public class ScoreDetailPopup : MonoBehaviour
{
    [Header("텍스트")]
    [Tooltip("기록한 유저 이름 (선택)")]
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text infoText;
    [SerializeField] private TMP_Text killText;
    [SerializeField] private TMP_Text challengeText;

    [Header("장비")]
    [SerializeField] private ChallengePool challengePool;
    [Tooltip("순서: 무기, 방어구, 반지0, 반지1, (확장) 아티팩트, 룬")]
    [SerializeField] private ScoreItemCell[] equipCells;
    [SerializeField] private GameObject emptyLabel;

    [Header("버튼")]
    [SerializeField] private Button closeButton;

    // 닫힘 알림
    public event System.Action Closed;

    private ScoreEntry _current;
    private string _playerName;

    private void Awake()
    {
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
    }

    private void OnEnable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += Refresh;
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= Refresh;
    }

    // 선택된 기록으로 갱신 후 열기 (이름 없으면 이름 칸 숨김)
    public void Open(ScoreEntry entry, string playerName = null)
    {
        if (entry == null)
            return;

        _current = entry;
        _playerName = playerName;
        gameObject.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        gameObject.SetActive(false);
        Closed?.Invoke();
    }

    // 현재 기록으로 전체 다시 그림 (언어 변경 시에도 호출)
    private void Refresh()
    {
        if (_current == null)
            return;

        if (nameText != null)
        {
            nameText.text = _playerName;
            nameText.gameObject.SetActive(!string.IsNullOrEmpty(_playerName));
        }

        if (infoText != null)
            infoText.text = $"{_current.date}\nSeed {_current.seed}\n{_current.reachedDepth}F · {_current.finalScore:N0}";

        if (killText != null)
            killText.text = $"{_current.killCount}";

        if (challengeText != null)
            challengeText.text = BuildChallengeNames();

        RebuildEquipGrid();
    }

    // 활성 챌린지 이름 나열. 없으면 "없음".
    private string BuildChallengeNames()
    {
        var names = new List<string>();

        if (challengePool != null && _current.challenges != null)
        {
            foreach (int t in _current.challenges)
            {
                ChallengeData data = challengePool.Get((ChallengeType)t);
                if (data != null)
                    names.Add(data.LocalizedDisplayName);
            }
        }

        return names.Count > 0 ? string.Join(", ", names) : LocalizationManager.GetOrDefault("msg_없음", "None");
    }

    // 슬롯 위치에 맞는 셀에 장비 표시. 빈 슬롯은 아이콘 숨김, 유효 0개면 "기록 없음".
    private void RebuildEquipGrid()
    {
        if (equipCells == null || equipCells.Length == 0)
            return;

        foreach (ScoreItemCell cell in equipCells)
        {
            if (cell != null)
                cell.Clear();
        }

        int filled = 0;
        if (_current.equipped != null)
        {
            foreach (EquipDTO equip in _current.equipped)
            {
                if (equip == null || equip.item == null)
                    continue;

                int index = CellIndexFor(equip.slot, equip.slotIndex);
                if (index < 0 || index >= equipCells.Length || equipCells[index] == null)
                    continue;

                if (equipCells[index].Set(equip.item))
                    filled++;
                else
                    equipCells[index].Clear();
            }
        }

        if (emptyLabel != null)
            emptyLabel.SetActive(filled == 0);
    }

    // 장비 슬롯 → equipCells 인덱스
    private static int CellIndexFor(int slot, int slotIndex)
    {
        switch ((EquipmentSlot)slot)
        {
            case EquipmentSlot.Weapon: return 0;
            case EquipmentSlot.Armor: return 1;
            case EquipmentSlot.Ring: return 2 + slotIndex;
            case EquipmentSlot.Artifact: return 4;
            case EquipmentSlot.Rune: return 5;
            default: return -1;
        }
    }
}
