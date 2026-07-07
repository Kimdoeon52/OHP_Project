using System;
using System.Collections.Generic;
using UnityEngine;

// 플레이어에게 걸린 모든 상태이상을 아이콘 바로 나열해 표시하는 HUD 뷰.
// 상태 로직/남은 시간은 StatusController가 담당하고, 이 스크립트는 활성 상태를 읽어 아이콘 칸에 뿌린다.
public class StatusIconBarView : MonoBehaviour
{
    [Header("데이터 참조")]
    [Tooltip("상태 목록과 남은 시간을 제공하는 컨트롤러. 비우면 플레이어에서 자동으로 찾는다.")]
    [SerializeField] private StatusController statusController;

    [Header("아이콘 표시")]
    [Tooltip("아이콘 1칸으로 복제할 StatusIconSlot 프리팹")]
    [SerializeField] private StatusIconSlot iconPrefab;

    [Tooltip("아이콘을 담을 부모. 보통 HorizontalLayoutGroup이 붙은 오브젝트. 비우면 자기 자신.")]
    [SerializeField] private Transform iconContainer;

    [Tooltip("상태별 아이콘 매핑. 여기에 등록한 순서대로 왼쪽부터 표시된다. 표시할 상태를 전부 등록.")]
    [SerializeField] private StatusIconEntry[] icons;

    private readonly List<StatusIconSlot> _spawned = new();
    private readonly List<StatusController.StatusSnapshot> _active = new();

    private void Awake()
    {
        if (iconContainer == null)
            iconContainer = transform;

        ResolveController();
    }

    private void Start()
    {
        // 플레이어가 HUD보다 늦게 준비되는 씬을 대비해 한 번 더 참조를 잡는다.
        ResolveController();
    }

    // 회색 오버레이가 남은 시간에 따라 계속 차오르고 깜빡여야 하므로 매 프레임 갱신한다.
    private void Update()
    {
        Refresh();
    }

    // 인스펙터 연결이 없으면 플레이어 오브젝트의 StatusController를 잡는다. 몬스터 것을 잡지 않도록 플레이어를 앵커로 쓴다.
    private void ResolveController()
    {
        if (statusController != null)
            return;

        if (GridPlayerHealth.Instance != null)
            statusController = GridPlayerHealth.Instance.GetComponent<StatusController>();
    }

    // 현재 활성 상태를 읽어 아이콘 바를 다시 그린다. 매핑 배열 순서대로 활성 상태만 왼쪽부터 채운다.
    private void Refresh()
    {
        if (statusController == null)
        {
            ResolveController();
            if (statusController == null)
            {
                HideFrom(0);
                return;
            }
        }

        statusController.GetActiveStatuses(_active);

        int shown = 0;
        for (int i = 0; i < icons.Length; i++)
        {
            if (icons[i].Icon == null)
                continue;

            if (!TryGetSnapshot(icons[i].Type, out StatusController.StatusSnapshot snapshot))
                continue;

            StatusIconSlot slot = GetOrCreateSlot(shown);
            if (slot == null)
                continue;

            slot.Apply(icons[i].Icon, snapshot.Remaining, snapshot.Total, !icons[i].persistent);
            slot.gameObject.SetActive(true);
            shown++;
        }

        // 이번에 안 쓴 칸은 꺼서 이전 아이콘이 남지 않게 한다.
        HideFrom(shown);
    }

    private bool TryGetSnapshot(StatusType type, out StatusController.StatusSnapshot snapshot)
    {
        for (int i = 0; i < _active.Count; i++)
        {
            if (_active[i].Type != type)
                continue;

            snapshot = _active[i];
            return true;
        }

        snapshot = default;
        return false;
    }

    private StatusIconSlot GetOrCreateSlot(int index)
    {
        if (index < _spawned.Count)
            return _spawned[index];

        // 프리팹이 없으면 표시할 방법이 없으므로 조용히 실패한다.
        if (iconPrefab == null)
            return null;

        StatusIconSlot slot = Instantiate(iconPrefab, iconContainer);
        _spawned.Add(slot);
        return slot;
    }

    private void HideFrom(int startIndex)
    {
        for (int i = startIndex; i < _spawned.Count; i++)
        {
            if (_spawned[i] != null)
                _spawned[i].gameObject.SetActive(false);
        }
    }

    [Serializable]
    private struct StatusIconEntry
    {
        public StatusType Type;
        public Sprite Icon;

        [Tooltip("켜면 쿨타임 오버레이/깜빡임 없이 상태가 사라질 때까지 아이콘만 표시. 배고픔처럼 지속 상태에 사용.")]
        public bool persistent;
    }
}
