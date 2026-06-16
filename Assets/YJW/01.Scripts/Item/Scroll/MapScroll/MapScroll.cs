using UnityEngine;

[CreateAssetMenu(fileName = "MapScroll", menuName = "Item/Scroll/MapScroll")]
public class MapScroll : ItemBase
{
	public override void Use()
	{
		if (GridSystem.Instance == null || VisionSystem.Instance == null)
			return;

		// 지형/hazard 공개 — 안개만 explored 처리(상자/아이템은 탐험 게이팅이라 그대로 숨김).
		if (FogRenderer.Instance != null)
			FogRenderer.Instance.SetTileExplored();

		// 출구 위치만 따로 공개 (상자/아이템은 공개하지 않음).
		var exit = Object.FindFirstObjectByType<GridExit>();
		if (exit != null)
			exit.Reveal();

		// 플레이어 주변은 현재 시야로 다시 밝힘.
		Vector2Int playerPos = GridSystem.Instance.WorldToGrid(GameManager.Instance.player.transform.position);
		VisionSystem.Instance.UpdateVision(playerPos);

		GameEvents.OnAudioEventRequested?.Invoke(AudioEventId.ScrollMapReveal);
		GameEvents.OnAudioEventRequested?.Invoke(AudioEventId.ScrollUsed);
	}
}
