using System.Collections.Generic;
using UnityEngine;

public class ItemInstance
{
	public ItemBase data;           // SO 원본
	public int enhanceLevel;        // 강화 단계
	public CurseData curse;		// 저주 정보
	public bool isAnalyzed;         // 감정 여부
}
