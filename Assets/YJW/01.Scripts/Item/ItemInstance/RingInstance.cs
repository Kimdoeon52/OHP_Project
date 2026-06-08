using UnityEngine;

public class RingInstance : EquipmentInstance
{
	public RingData Weapon => data as RingData;

	public override string GetToolTipStats() =>
		$"{Weapon.Name}\n" +
		$"----------\n" +
		$"{Weapon.Description}\n" +
		$"착용 힘: {Weapon.CanUseStrength}\n" +
		$"효과: {Weapon.EffectDescription} {(Weapon.Value >= 0 ? "+" : "")}{Weapon.Value}";

}
