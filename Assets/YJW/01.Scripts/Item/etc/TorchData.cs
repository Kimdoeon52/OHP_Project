using UnityEngine;

[CreateAssetMenu(fileName = "Torch", menuName = "Item/Etc/Torch")]
public class TorchData : ItemBase
{
    [Tooltip("시야 반경 증가량")]
    [SerializeField, Min(1)] private int visionBonus = 2;

    [Tooltip("지속 시간(초)")]
    [SerializeField, Min(1f)] private float duration = 30f;

    public override void Use()
    {
        if (VisionSystem.Instance == null)
        {
            Debug.LogWarning("TorchData: VisionSystem이 씬에 없어 횃불 효과를 적용하지 못했습니다.");
            return;
        }
        VisionSystem.Instance.ApplyTorchEffect(visionBonus, duration);
    }

    public override ItemInstance CreateInstance()
    {
        return new TorchInstance { data = this, count = 1 };
    }

    public override string GetToolTipStats()
    {
        return $"[{Name}]\n" +
               $"--------------------\n" +
               $"{Description}\n" +
               $"시야 +{visionBonus} / {duration}초";
    }
}
