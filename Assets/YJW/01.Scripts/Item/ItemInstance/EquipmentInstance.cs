using UnityEngine;

public class EquipmentInstance : ItemInstance
{
    public int enhanceLevel;        // 강화 단계
    public CurseData curse;         // 저주 정보 (null이면 저주 없음)
    public MagicData magic;
    public bool isAnalyzed;         // 감정 여부

    public bool IsBound => curse != null; // 저주 있으면 귀속 (해제 불가)

    public void Purify()
    {
        curse = null; // 정화 스크롤 사용 시 호출
    }
}
