using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dark Feeling — 기본 시야 반경 감소 (GDD 6.3: -2).
/// 횃불·심안 포션·지도 스크롤의 가치를 높이는 변형.
/// ApplyToInit 시점에 VisionSystem.VisionRadiusDelta 를 음수로 세팅,
/// 다음 층 Generate 호출이 0 으로 리셋 (MapGenerator 책임).
/// </summary>
[CreateAssetMenu(fileName = "DarkFeeling", menuName = "60s Dungeon/Feelings/Dark")]
public class DarkFeeling : FloorFeeling
{
    public override string Id => "Dark";

    [Tooltip("기본 시야 반경에서 빼는 칸 수 (GDD 6.3: 2)")]
    [SerializeField, Min(0)] private int visionRadiusReduction = 2;

    public override void ApplyToInit(List<RoomData> rooms)
    {
        if (VisionSystem.Instance == null)
        {
            Debug.LogWarning("[DarkFeeling] VisionSystem.Instance 가 null — Edit 모드 또는 씬에 VisionSystem 없음");
            return;
        }
        VisionSystem.Instance.VisionRadiusDelta = -visionRadiusReduction;
        Debug.Log($"[DarkFeeling] VisionRadiusDelta = -{visionRadiusReduction} 적용");
    }
}
