using System.Text;
using Steamworks;
using UnityEngine;

// 기록판에 표시할 플레이어 이름 — 한글·영문·숫자 8자, PlayerPrefs 영구 저장
public static class PlayerProfile
{
    public const int MaxLength = 8;

    private const string PrefsKey = "Player.Name";

    // 자동 생성용 단어 — 수식어+명사가 MaxLength 안에 들어가게 유지
    private static readonly string[] KoreanPrefixes = { "굶주린", "성급한", "겁없는", "무모한", "조용한", "교활한", "재빠른", "창백한" };
    private static readonly string[] KoreanNouns = { "도굴꾼", "방랑자", "망령", "탐험가", "도적", "순례자", "나그네", "유령" };
    private static readonly string[] EnglishPrefixes = { "Grim", "Pale", "Bold", "Sly", "Lost", "Cold", "Fast", "Old" };
    private static readonly string[] EnglishNouns = { "Crow", "Moth", "Wolf", "Fox", "Monk", "Rook", "Sage", "Owl" };

    // 저장된 이름 (없으면 빈 문자열)
    public static string Name => PlayerPrefs.GetString(PrefsKey, string.Empty);

    public static bool HasName => !string.IsNullOrEmpty(Name);

    // 기록에 쓸 이름 — 없으면 만들어서 저장
    public static string DisplayName => EnsureName();

    // 이름 확보 — 저장 이름 → Steam 닉네임 → 언어별 자동 생성. 만든 이름은 저장해 계속 씀
    public static string EnsureName()
    {
        string saved = Name;
        if (!string.IsNullOrEmpty(saved))
            return saved;

        string name = SteamName();
        if (string.IsNullOrEmpty(name))
            name = Generate();

        PlayerPrefs.SetString(PrefsKey, name);
        PlayerPrefs.Save();
        return name;
    }

    // 입력창 기본값
    public static string SuggestedName()
    {
        return EnsureName();
    }

    // 현재 언어에 맞는 이름 생성 — 수식어 + 명사
    public static string Generate()
    {
        bool korean = CurrentLanguage() == LocalizationManager.Language.Korean;
        string[] prefixes = korean ? KoreanPrefixes : EnglishPrefixes;
        string[] nouns = korean ? KoreanNouns : EnglishNouns;

        var rng = new DungeonRandom();
        rng.Init(System.Environment.TickCount);

        return Sanitize(rng.Pick(prefixes) + rng.Pick(nouns));
    }

    // 언어 설정 — 매니저가 아직 없으면 저장값
    private static LocalizationManager.Language CurrentLanguage()
    {
        if (LocalizationManager.Instance != null)
            return LocalizationManager.Instance.currentLanguage;

        return (LocalizationManager.Language)PlayerPrefs.GetInt(LocalizationManager.LanguagePrefsKey, (int)LocalizationManager.Language.Korean);
    }

    // 허용 문자만 남겨 저장. 빈 이름이면 저장 안 함
    public static bool Save(string raw)
    {
        string clean = Sanitize(raw);
        if (string.IsNullOrEmpty(clean))
            return false;

        PlayerPrefs.SetString(PrefsKey, clean);
        PlayerPrefs.Save();
        return true;
    }

    // 허용 문자만 남기고 MaxLength로 자름 — 한글·영문·숫자
    public static string Sanitize(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return string.Empty;

        var sb = new StringBuilder(MaxLength);
        foreach (char c in raw)
        {
            if (!IsAllowed(c))
                continue;

            sb.Append(c);
            if (sb.Length >= MaxLength)
                break;
        }

        return sb.ToString();
    }

    // 한글 완성형·자모, 영문, 숫자만 허용 (공백·기호 제외)
    private static bool IsAllowed(char c)
    {
        if (c >= '0' && c <= '9')
            return true;
        if (c >= 'A' && c <= 'Z')
            return true;
        if (c >= 'a' && c <= 'z')
            return true;
        if (c >= '가' && c <= '힣')
            return true;

        // 조합 중인 자모 (ㄱ~ㅣ)
        return c >= 'ㄱ' && c <= 'ㆎ';
    }

    // Steam 닉네임 (미연결이면 빈 문자열)
    private static string SteamName()
    {
        if (!SteamBootstrap.IsReady)
            return string.Empty;

        return Sanitize(SteamFriends.GetPersonaName());
    }
}
