using System.Collections.Generic;
using UnityEngine;

public enum RoomType { Safe, Normal, Special, Secret, Exit, Tunnel }

public enum SizeCategory { Normal, Large, Giant }

/// <summary>
/// 절차적 맵 생성기의 기본 단위. Initialize 단계에서 type/sizeCategory 만 채워서 만들고,
/// Build 단계에서 bounds/floorCells/doorCells/connections/distanceFromSafe 가 결정된다.
/// </summary>
public class RoomData
{
    public RoomType type;
    public SizeCategory sizeCategory;

    // Build 단계 출력
    public RectInt bounds;
    public List<Vector2Int> floorCells = new List<Vector2Int>();
    public List<Vector2Int> doorCells = new List<Vector2Int>();
    public List<RoomData> connections = new List<RoomData>();

    // Safe Room 으로부터의 그래프 거리. 미계산 = -1.
    public int distanceFromSafe = -1;
}
