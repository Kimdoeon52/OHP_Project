using System.Collections.Generic;
using UnityEngine;

public enum RoomType { Safe, Normal, Special, Secret, Exit, Boss, Tunnel }

public enum SizeCategory { Normal, Large, Giant }

// 스페셜 방 종류
public enum SpecialRoomKind { RandomChest, Monster, RingChest, GoldChest, Scroll }

// 시크릿 방 종류
public enum SecretRoomKind { Potion, EquipChest, Gold, Enchant }

public class RoomData
{
    public RoomType type;
    public SizeCategory sizeCategory;

    public SpecialRoomKind specialKind;
    public SecretRoomKind secretKind;

    public RectInt bounds;
    public List<Vector2Int> floorCells = new List<Vector2Int>();
    public List<Vector2Int> doorCells = new List<Vector2Int>();
    public List<RoomData> connections = new List<RoomData>();

    public List<Vector2Int> gatewayCells = new List<Vector2Int>();

    public int distanceFromSafe = -1;
}
