using UnityEngine;

[CreateAssetMenu(fileName = "MapScroll", menuName = "Item/Scroll/MapScroll")]
public class MapScroll : ItemBase
{
	public override void Use(ItemInstance targetItem)
	{
		Debug.LogError("맵스크롤 사용");
		FogRenderer.Instance.SetTileExplored();
	}
}
