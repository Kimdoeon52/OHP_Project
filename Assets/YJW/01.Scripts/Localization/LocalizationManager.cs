using System.Collections.Generic;
using UnityEngine;

public class LocalizationManager : PersistentSingleton<LocalizationManager>
{
    public enum Language { Korean, English }
    public Language currentLanguage = Language.Korean;

    private Dictionary<string, Dictionary<Language, string>> texts
        = new Dictionary<string, Dictionary<Language, string>>()
    {
        //------------------------------------------ 메인 메뉴
        { "bt_게임시작", new Dictionary<Language, string> {
            { Language.Korean, "게임 시작" },
            { Language.English, "Start" }
        }},
        { "bt_이어하기", new Dictionary<Language, string> {
            { Language.Korean, "이어하기" },
            { Language.English, "Continue" }
        }},
        { "bt_튜토리얼", new Dictionary<Language, string> {
            { Language.Korean, "튜토리얼" },
            { Language.English, "Tutorial" }
        }},
        { "bt_챌린지", new Dictionary<Language, string> {
            { Language.Korean, "챌린지" },
            { Language.English, "Challenge" }
        }},
        { "bt_게임설정", new Dictionary<Language, string> {
            { Language.Korean, "게임 설정" },
            { Language.English, "Setting" }
        }},
        { "bt_게임종료", new Dictionary<Language, string> {
            { Language.Korean, "게임 종료" },
            { Language.English, "Quit" }
        }},
        //------------------------------------------ 세팅창 왼쪽 탭
        { "msg_시스템", new Dictionary<Language, string> {
            { Language.Korean, "시스템" },
            { Language.English, "System" }
        }},
        { "bt_키설정", new Dictionary<Language, string> {
            { Language.Korean, "키 설정" },
            { Language.English, "Key Setting" }
        }},
        { "bt_오디오설정", new Dictionary<Language, string> {
            { Language.Korean, "오디오 설정" },
            { Language.English, "Audio Setting" }
        }},
        { "bt_저작권", new Dictionary<Language, string> {
            { Language.Korean, "저작권" },
            { Language.English, "License" }
        }},
        //------------------------------------------ 세팅창 오디오 설정
        { "msg_전체볼륨", new Dictionary<Language, string> {
            { Language.Korean, "전체 볼륨" },
            { Language.English, "Master Volume" }
        }},
        { "msg_배경음", new Dictionary<Language, string> {
            { Language.Korean, "배경음" },
            { Language.English, "BGM" }
        }},
        { "msg_효과음", new Dictionary<Language, string> {
            { Language.Korean, "효과음" },
            { Language.English, "SFX" }
        }},
        //------------------------------------------ 세팅창 저작권
        { "msg_저작권내용", new Dictionary<Language, string> {
            { Language.Korean, "라이선스 전문 파일은 게임 설치 폴더 (_Data/StreamingAssets/License/) 내에 동봉되어 있습니다." },
            { Language.English, "The full license file is included in the game installation folder (_Data/StreamingAssets/License/)." }
        }},
        //------------------------------------------ 인게임 버튼
        { "bt_튜토리얼스킵", new Dictionary<Language, string> {
            { Language.Korean, "튜토리얼 스킵" },
            { Language.English, "Skip Tutorial" }
        }},
        //------------------------------------------ 튜토리얼 대사
        { "msg_튜토리얼대사1", new Dictionary<Language, string> {
            { Language.Korean, "60초. 던전에서 네게 주어진 시간은 그게 전부다.\r\n…하지만 여기 세이프룸에선 시간이 멈춰 있다. 천천히 익혀라.\r\n[아무 키나 눌러 시작]" },
            { Language.English, "60 seconds. That's all the time you have in the dungeon.\r\n…But time stands still here in the safe room. Take your time and learn the ropes.\r\n[Press any key to begin]" }
        }},
        { "msg_튜토리얼대사2", new Dictionary<Language, string> {
            { Language.Korean, "방 안에 있는 지도 스크롤로 이동해\r\n스페이스 바를 눌러 획득해라" },
            { Language.English, "Scroll to navigate the map in the room.\r\nPress Space to pick it up." }
        }},
        { "msg_튜토리얼대사3", new Dictionary<Language, string> {
            { Language.Korean, "Tab 를 눌러 인벤토리를 열어라. 가진 아이템을 확인하고 쓸 수 있다." },
            { Language.English, "Press Tab to open your inventory. You can check and use the items you have." }
        }},
        { "msg_튜토리얼대사4", new Dictionary<Language, string> {
            { Language.Korean, "지도 스크롤을 펼치면 던전의 구조가 드러난다. 사용해 보자. \r\n[ 아이템 더블 클릭 ]" },
            { Language.English, "Unrolling the map scroll reveals the dungeon's layout. Give it a try.\r\n[ Double-click the item ]" }
        }},
        { "msg_튜토리얼대사5", new Dictionary<Language, string> {
            { Language.Korean, "세이프룸에선 시간이 흐르지 않는다\r\n휠로 확대/축소, 드래그로 맵을 살피며 동선을 미리 짜라.\r\n[스페이스바를 눌러 다음으로 진행]" },
            { Language.English, "Time does not pass in the safe room.\r\nZoom in/out with the scroll wheel and drag to survey the map — plan your route in advance.\r\n[Press Space to continue]" }
        }},
        { "msg_튜토리얼대사6", new Dictionary<Language, string> {
            { Language.Korean, "계획은 끝났다. 이제 실행이다 — 발을 들이는 순간 시간이 흐른다.\r\n오른쪽 몬스터 방으로 이동하라." },
            { Language.English, "The plan is set. Now it's time to act — the moment you step in, the clock starts ticking.\r\nHead to the monster room on the right." }
        }},
        { "msg_튜토리얼대사7", new Dictionary<Language, string> {
            { Language.Korean, "적을 처치하고 열쇠를 손에 넣어라." },
            { Language.English, "Take down the enemy and grab the key." }
        }},
        { "msg_튜토리얼대사8", new Dictionary<Language, string> {
            { Language.Korean, "이 벽… 어딘가 이상하다. 수상한 벽 앞에서 F 로 탐색해 숨겨진 문을 찾아라." },
            { Language.English, "This wall… something's off. Press F to search in front of the suspicious wall and find the hidden door." }
        }},
        { "msg_튜토리얼대사9", new Dictionary<Language, string> {
            { Language.Korean, "문이 드러났다. 안으로 들어가라." },
            { Language.English, "The door has been revealed. Go inside." }
        }},
        { "msg_튜토리얼대사10", new Dictionary<Language, string> {
            { Language.Korean, "열쇠로 상자를 열어라. 잠긴 상자엔 아이템이 들어 있다." },
            { Language.English, "Use the key to unlock the chest. Locked chests hold valuable items." }
        }},
        { "msg_튜토리얼대사11", new Dictionary<Language, string> {
            { Language.Korean, "출구 방으로 향하라. 포탈이 다음 층으로 이어진다." },
            { Language.English, "Head to the exit room. The portal leads to the next floor." }
        }},
        { "msg_튜토리얼대사12", new Dictionary<Language, string> {
            { Language.Korean, "포탈에 들어가 1층으로 내려가라. 단, 여기서 얻은 것은 가져갈 수 없다. 진짜 던전은 빈손에서 시작된다." },
            { Language.English, "Enter the portal and descend to Floor 1. However, nothing you've gained here can be taken with you. The real dungeon begins with empty hands." }
        }},
        //------------------------------------------ 보스 1 대사
        { "msg_보스1대사1", new Dictionary<Language, string> {
            { Language.Korean, "등 뒤를 서늘하게 만들던 시선이, 마침내 코앞까지 다가왔다." },
            { Language.English, "The gaze that once chilled your spine has finally crept within reach." }
        }},
        { "msg_보스1대사2", new Dictionary<Language, string> {
            { Language.Korean, "그 깊은 어둠이, 단 하나의 눈동자가 되어 모습을 드러낸다." },
            { Language.English, "From the depths of that darkness, a single eye emerges." }
        }},
        { "msg_보스1대사3", new Dictionary<Language, string> {
            { Language.Korean, "그것의 이름은……" },
            { Language.English, "Its name is……" }
        }},
        //------------------------------------------ 보스 2 대사
        { "msg_보스2대사1", new Dictionary<Language, string> {
            { Language.Korean, "뼈가 부딪히는 소리가 던전에 울려퍼진다." },
            { Language.English, "The clatter of bones echoes through the dungeon." }
        }},
        { "msg_보스2대사2", new Dictionary<Language, string> {
            { Language.Korean, "그를 마주한 이들은 모두 이곳에서 끝을 맞이한다." },
            { Language.English, "All who have faced him meet their end here." }
        }},
        { "msg_보스2대사3", new Dictionary<Language, string> {
            { Language.Korean, "수많은 생명을 앗아간 대검의 칼끝은.." },
            { Language.English, "The tip of a greatsword that has claimed countless lives.." }
        }},
        { "msg_보스2대사4", new Dictionary<Language, string> {
            { Language.Korean, "지금 당신을 향해 있다." },
            { Language.English, "is now pointed at you." }
        }},
        //------------------------------------------ 보스 3 대사
        { "msg_보스3대사1", new Dictionary<Language, string> {
            { Language.Korean, "저주의 근원이 모습을 드러낸다." },
            { Language.English, "The source of the curse reveals itself." }
        }},
        { "msg_보스3대사2", new Dictionary<Language, string> {
            { Language.Korean, "죽음의 종착역에 드디어 도착했다." },
            { Language.English, "You have finally reached the end of the line." }
        }},
        { "msg_보스3대사3", new Dictionary<Language, string> {
            { Language.Korean, "드디어 당신의 여정의 끝이 보인다." },
            { Language.English, "At last, the end of your journey comes into sight." }
        }},
    };

    public string Get(string key)
    {
        if (texts.ContainsKey(key))
            return texts[key][currentLanguage];
        return key; // fallback
    }

    public void SetLanguage(Language lang)
    {
        currentLanguage = lang;
        // 언어 변경 이벤트 발생
        OnLanguageChanged?.Invoke();
    }

    public event System.Action OnLanguageChanged;
}
