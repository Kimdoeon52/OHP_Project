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

    [Header("특수 방 등장 확률 (GDD 6.2 — 원본은 GDD)")]
    [Tooltip("Special 방(열쇠로 여는 보상 방)이 한 층에 등장할 확률. 0~1.")]
    [Range(0f, 1f)] public float specialRoomChance = 0.5f;
    [Tooltip("Secret 방(숨겨진 보상 방)이 한 층에 등장할 확률. 0~1.")]
    [Range(0f, 1f)] public float secretRoomChance = 0.3f;
}
