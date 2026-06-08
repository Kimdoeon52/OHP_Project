using UnityEngine;

[CreateAssetMenu(fileName = "MapScroll", menuName = "Item/Scroll/MapScroll")]
public class MapScroll : ItemBase
{
	public override void Use()
	{
		Debug.LogError("맵스크롤 사용");
		FogRenderer.Instance.SetTileExplored();
		Vector2Int playerPos = GridSystem.Instance.WorldToGrid(GameManager.Instance.player.transform.position);
		VisionSystem.Instance.UpdateVision(playerPos);
	}
}
