using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 시야 안개 렌더러
/// </summary>
public class FogRenderer : MonoBehaviour
{
    [SerializeField] private Tilemap fogTilemap;
    [SerializeField] private TileBase fogTile;
    [SerializeField] private TileBase exploredTile; // 반투명

    private bool _initialized;

    private void OnEnable()
    {
        GameEvents.OnVisionUpdated += OnVisionUpdated;
    }

    private void OnDisable()
    {
        GameEvents.OnVisionUpdated -= OnVisionUpdated;
    }

    private void Start()
    {
        FillInitialFog();
    }

    public void RebuildFogForCurrentMap()
    {
        FillInitialFog();
    }

    private void ClearFog()
    {
        if (fogTilemap == null) return;

        fogTilemap.ClearAllTiles();
        _initialized = false;
    }

    private void FillInitialFog()
    {
        if (fogTilemap == null || fogTile == null || GridSystem.Instance == null) return;

        fogTilemap.ClearAllTiles();

        foreach (var cell in GridSystem.Instance.GetAllCells())
        {
            fogTilemap.SetTile(new Vector3Int(cell.x, cell.y, 0), fogTile);
        }

        _initialized = true;
    }

    private void OnVisionUpdated(IReadOnlyCollection<Vector2Int> visible, IReadOnlyCollection<Vector2Int> explored)
    {
        if ((visible == null || visible.Count == 0) && (explored == null || explored.Count == 0))
        {
            ClearFog();
            return;
        }

        if (!_initialized) FillInitialFog();
        if (fogTilemap == null) return;

        foreach (var cell in explored)
        {
            var pos = new Vector3Int(cell.x, cell.y, 0);
            fogTilemap.SetTile(pos, VisionSystem.Instance.IsVisible(cell) ? null : exploredTile);
        }
    }
}
