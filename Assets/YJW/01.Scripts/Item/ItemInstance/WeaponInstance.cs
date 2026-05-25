using UnityEngine;

public class WeaponInstance : ItemInstance
{
	public CloseWeapon Weapon => data as CloseWeapon;

	public int GetDamage()
	{
		int dmg = Weapon.GetBaseDamage();   // SO에서 기본값
		dmg += enhanceLevel * 2;            // 강화 보정
											// 저주 모디파이어 적용
		
		//dmg += curse.damageMod;

		return dmg;
	}
}
