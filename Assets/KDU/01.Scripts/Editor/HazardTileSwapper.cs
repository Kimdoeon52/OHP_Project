using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

// 고정맵(보스방 등)에 이미 칠해진 타일을 다른 타일로 일괄 교체
public class HazardTileSwapper : EditorWindow
{
    private Tilemap target;
    private TileBase fromTile;
    private TileBase toTile;

    [MenuItem("Tools/KDU/타일 일괄 교체")]
    private static void Open()
    {
        GetWindow<HazardTileSwapper>("타일 일괄 교체");
    }

    private void OnGUI()
    {
        target = (Tilemap)EditorGUILayout.ObjectField("대상 Tilemap", target, typeof(Tilemap), true);
        fromTile = (TileBase)EditorGUILayout.ObjectField("교체 전 타일", fromTile, typeof(TileBase), false);
        toTile = (TileBase)EditorGUILayout.ObjectField("교체 후 타일", toTile, typeof(TileBase), false);

        EditorGUI.BeginDisabledGroup(target == null || fromTile == null || toTile == null);
        if (GUILayout.Button("교체"))
            Swap();

        EditorGUI.EndDisabledGroup();
    }

    private void Swap()
    {
        Undo.RecordObject(target, "Swap Tiles");

        int count = 0;
        foreach (var pos in target.cellBounds.allPositionsWithin)
        {
            if (target.GetTile(pos) != fromTile)
                continue;

            target.SetTile(pos, toTile);
            count++;
        }

        EditorUtility.SetDirty(target);
        Debug.Log($"[HazardTileSwapper] {count}칸 교체");
    }
}
