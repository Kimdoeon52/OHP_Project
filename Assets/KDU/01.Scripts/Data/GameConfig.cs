using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "60s Dungeon/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("타이머")]
    public float startTime = 60f;
    public float lowTimeWarning = 10f;
    [Tooltip("보스전 제한시간 (5/10층)")]
    public float bossStartTime = 120f;

    [Header("챌린지 — 시간 압박")]
    [Tooltip("층 제한시간 배율")]
    [Range(0.1f, 1f)] public float timePressureMultiplier = 0.75f;

    [Header("행동별 시간 비용")]
    public float moveTimeDelay = 0.5f;
    public float potionTimeDealy = 1.0f;
    public float meleeTimeDealy = 1.0f;

    [Header("시야")]
    public int visionRadius = 5;

    [Header("챌린지 — 시야 제한")]
    [Tooltip("시야 반경 가감")]
    public int visionLimitDelta = -2;

    [Header("스페셜 방 등장 확률")]
    [Range(0f, 1f)] public float specialRoomChance = 0.5f;
    [Tooltip("시크릿 방 등장 확률")]
    [Range(0f, 1f)] public float secretRoomChance = 0.3f;
}
