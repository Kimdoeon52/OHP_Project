using UnityEngine;

// 런 사이로 넘기는 골드. 사망=절반, 최종 클리어=전액. run.json과 분리해 보관해야
// 새로 시작 시 덮어쓰이는 저장본이 아니라 직전 런 종료 시점의 골드를 이어받는다.
public static class GoldCarryOver
{
    private const string Key = "carry_gold";

    // 새 런이 이어받을 골드
    public static int Pending => PlayerPrefs.GetInt(Key, 0);

    // 사망/클리어 시점에 기록
    public static void Set(int amount)
    {
        PlayerPrefs.SetInt(Key, Mathf.Max(0, amount));
        PlayerPrefs.Save();
    }

    // 새 런이 소비하면 비움
    public static void Clear()
    {
        PlayerPrefs.DeleteKey(Key);
        PlayerPrefs.Save();
    }
}
