using System.Collections.Generic;
using UnityEngine;

public class ItemInstance
{
	public ItemBase data;           // SO 원본

	// 스택 규칙은 NYH 인벤토리에서 판단합니다.
	// YJW 쪽은 포션/스크롤 수량을 담을 최소 데이터만 제공합니다.
	public int count = 1;           // 포션/스크롤처럼 인벤토리에서 합쳐지는 아이템의 보유 수량

    public virtual string GetToolTipStats() => data.GetToolTipStats();
}
