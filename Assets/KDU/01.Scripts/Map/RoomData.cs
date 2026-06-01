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

    // 이 방으로 들어가는 관문 셀 — 문/봉인이 놓일 자리.
    // Secret = 노말 방 쪽 봉인 2칸(발견 시 1칸 폭 문으로 개통), Special = 잠긴 문 1칸.
    public List<Vector2Int> gatewayCells = new List<Vector2Int>();

    // Safe Room으로부터의 그래프 거리
    public int distanceFromSafe = -1;
}
