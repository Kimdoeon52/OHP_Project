using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "60s Dungeon/Game Config")]
public class GameConfig : ScriptableObject
{
    [Header("타이머")]
    public float startTime = 60f;
    public float lowTimeWarning = 10f;

    [Header("행동별 시간 비용")]
    public float moveTimeDelay = 0.5f;
    public float potionTimeDealy = 1.0f;
    public float meleeTimeDealy = 1.0f;

    [Header("시야")]
    public int visionRadius = 5;

    [Header("스페셜 방 등장 확률")]
    [Range(0f, 1f)] public float specialRoomChance = 0.5f;
    [Tooltip("시크릿 방 등장 확률")]
    [Range(0f, 1f)] public float secretRoomChance = 0.3f;
}
