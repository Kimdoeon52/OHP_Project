using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 상세 팝업 장비 1칸 — 아이콘만 표시, hover 시 기존 툴팁으로 설명
public class ScoreItemCell : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;

    private ItemInstance _instance;
    private bool _hovering;

    // DTO로 세팅. DB에 없는 id면 false (호출측이 Clear)
    public bool Set(ItemDTO dto)
    {
        _instance = SaveMapper.FromDTO(dto, ItemDatabase.Instance);
        if (_instance == null || _instance.data == null)
            return false;

        if (iconImage != null)
        {
            iconImage.sprite = _instance.data.Icon;
            iconImage.enabled = true;
            iconImage.preserveAspect = true;
        }

        return true;
    }

    // 빈 칸 — 아이콘 숨김, 툴팁 비활성
    public void Clear()
    {
        _instance = null;

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_instance == null || ToolTipController.Instance == null)
            return;

        _hovering = true;
        ToolTipController.Instance.Show(_instance.GetToolTipStats());
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovering = false;
        if (ToolTipController.Instance != null)
            ToolTipController.Instance.Hide();
    }

    // hover 중 팝업이 닫히면 툴팁이 남지 않게
    private void OnDisable()
    {
        if (_hovering && ToolTipController.Instance != null)
            ToolTipController.Instance.Hide();
        _hovering = false;
    }
}
