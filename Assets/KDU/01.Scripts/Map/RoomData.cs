using System.Collections.Generic;
using UnityEngine;

public enum RoomType { Safe, Normal, Special, Secret, Exit, Boss, Tunnel }

public enum SizeCategory { Normal, Large, Giant }

public class RoomData
{
    public RoomType type;
    public SizeCategory sizeCategory;

    // Build 단계 출력
    public RectInt bounds;
    public List<Vector2Int> floorCells = new List<Vector2Int>();
    public List<Vector2Int> doorCells = new List<Vector2Int>();
    public List<RoomData> connections = new List<RoomData>();

    // Safe Room으로부터의 그래프 거리
    public int distanceFromSafe = -1;
}
