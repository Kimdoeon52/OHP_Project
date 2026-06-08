using UnityEngine;

[CreateAssetMenu(fileName = "MapScroll", menuName = "Item/Scroll/MapScroll")]
public class MapScroll : ItemBase
{
	public override void Use(ItemInstance targetItem)
	{
		FogRenderer.Instance.SetTileExplored();
	}
}
