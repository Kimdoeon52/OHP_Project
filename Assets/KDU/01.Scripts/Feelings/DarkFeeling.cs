using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DarkFeeling", menuName = "60s Dungeon/Feelings/Dark")]
public class DarkFeeling : FloorFeeling
{
    public override string Id => "Dark"; // 아이디 기본값

    [Tooltip("제한 할 시야 반경")]
    [SerializeField, Min(0)] private int visionRadiusReduction = 2;

    public override void ApplyToInit(List<RoomData> rooms)
    {
        if (VisionSystem.Instance == null)
        {
            Debug.LogWarning("[DarkFeeling] VisionSystem.Instance 가 null");
            return;
        }
        VisionSystem.Instance.VisionRadiusDelta = -visionRadiusReduction;
    }
}
