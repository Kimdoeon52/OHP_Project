using System.Collections.Generic;
using UnityEngine;

public enum RoomType { Safe, Normal, Special, Secret, Exit, Boss, Tunnel }

public enum SizeCategory { Normal, Large, Giant }

// 스페셜 방 하위 종류 (GDD 6.2.1) — type==Special 일 때만 유효. 순서·개수는 GDD 고정.
public enum SpecialRoomKind { RandomChest, Monster, RingChest, GoldChest, Scroll }

// 시크릿 방 하위 종류 (GDD 6.2.2) — type==Secret 일 때만 유효.
public enum SecretRoomKind { Potion, EquipChest, Gold, Enchant }

public class RoomData
{
    public RoomType type;
    public SizeCategory sizeCategory;

    // 특수 방 하위 종류 — MapGenerator 가 방 생성 시 1회 결정(중앙 단일 진실).
    // type==Special 이면 specialKind, type==Secret 이면 secretKind 가 유효.
    // ChestSpawner(상자류)·EnemySpawner(몬스터 방 골렘) 등이 이 값을 읽어 분담.
    public SpecialRoomKind specialKind;
    public SecretRoomKind secretKind;

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
