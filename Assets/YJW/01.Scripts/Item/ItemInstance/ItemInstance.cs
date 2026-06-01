using System.Collections.Generic;
using UnityEngine;

public class ItemInstance
{
	public ItemBase data;           // SO 원본

    public virtual string GetToolTipStats() => data.GetToolTipStats();
}
