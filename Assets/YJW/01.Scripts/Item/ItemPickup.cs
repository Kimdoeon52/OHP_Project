using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemBase itemSO;
    public int enhanceLevel;
    public CurseData curse;   // SO라서 인스펙터에서 드래그 가능, 없으면 None

    public ItemInstance item;

	private void Start()
	{
		item = itemSO switch
		{
			CloseWeapon => new WeaponInstance { data = itemSO, enhanceLevel = enhanceLevel, curse = curse },
			ArmorData => new ArmorInstance { data = itemSO, enhanceLevel = enhanceLevel, curse = curse },
			PotionData => new PotionInstance { data = itemSO, count = 1},
			_ => new ItemInstance { data = itemSO }
		};
	}
}
