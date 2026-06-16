using UnityEngine;

/// <summary>
/// 층 번호와 보스 여부를 BGM 클립으로 변환하는 테이블입니다.
/// 곡 선택 규칙을 코드에 흩뿌리지 않고 데이터로 모아두기 위해 ScriptableObject로 분리합니다.
/// </summary>
[CreateAssetMenu(fileName = "MusicTable", menuName = "60s Dungeon/Audio/Music Table")]
public class MusicTable : ScriptableObject
{
    [Header("공통 BGM")]
    [Tooltip("메인 메뉴에서 사용할 BGM입니다.")]
    [SerializeField] private AudioClip mainMenuBgm;

    [Tooltip("Safe Room에서 별도 분위기를 줄 때 사용할 BGM입니다. 비워두면 현재 층 BGM을 유지합니다.")]
    [SerializeField] private AudioClip safeRoomBgm;

    [Tooltip("상점/휴식 공간에서 사용할 BGM입니다. 비워두면 현재 층 BGM을 유지합니다.")]
    [SerializeField] private AudioClip shopOrRestBgm;

    [Tooltip("플레이어 사망 화면에서 사용할 BGM입니다.")]
    [SerializeField] private AudioClip playerDeathBgm;

    [Tooltip("게임 클리어 화면에서 사용할 BGM입니다.")]
    [SerializeField] private AudioClip gameClearBgm;

    [Header("일반 층 BGM")]
    [Tooltip("1~5층 일반층에서 사용할 BGM입니다. 단, 5층이 보스층이면 bossDepth5Bgm이 우선됩니다.")]
    [SerializeField] private AudioClip area1Bgm;

    [Tooltip("6~10층 일반층에서 사용할 BGM입니다. 단, 10층이 보스층이면 bossDepth10Bgm이 우선됩니다.")]
    [SerializeField] private AudioClip area2Bgm;

    [Tooltip("11~15층 일반층에서 사용할 BGM입니다. 단, 15층이 보스층이면 bossDepth15Bgm이 우선됩니다.")]
    [SerializeField] private AudioClip area3Bgm;

    [Header("보스 층 BGM")]
    [Tooltip("5층 보스 전용 BGM입니다. 비워두면 Area1 BGM을 대신 사용합니다.")]
    [SerializeField] private AudioClip bossDepth5Bgm;

    [Tooltip("10층 보스 전용 BGM입니다. 비워두면 Area2 BGM을 대신 사용합니다.")]
    [SerializeField] private AudioClip bossDepth10Bgm;

    [Tooltip("15층 보스 전용 BGM입니다. 비워두면 Area3 BGM을 대신 사용합니다.")]
    [SerializeField] private AudioClip bossDepth15Bgm;

    public AudioClip MainMenuBgm => mainMenuBgm;
    public AudioClip SafeRoomBgm => safeRoomBgm;
    public AudioClip ShopOrRestBgm => shopOrRestBgm;
    public AudioClip PlayerDeathBgm => playerDeathBgm;
    public AudioClip GameClearBgm => gameClearBgm;

    public AudioClip GetBgmForDepth(int depth, bool isBoss)
    {
        if (isBoss)
        {
            AudioClip bossClip = GetBossBgm(depth);
            if (bossClip != null)
                return bossClip;
        }

        return GetAreaBgm(depth);
    }

    private AudioClip GetAreaBgm(int depth)
    {
        if (depth <= 5)
            return area1Bgm;

        if (depth <= 10)
            return area2Bgm;

        return area3Bgm;
    }

    private AudioClip GetBossBgm(int depth)
    {
        if (depth == 5)
            return bossDepth5Bgm;

        if (depth == 10)
            return bossDepth10Bgm;

        if (depth == 15)
            return bossDepth15Bgm;

        return null;
    }
}
