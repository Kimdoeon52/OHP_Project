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
        { "msg_게임종료", new Dictionary<Language, string> {
            { Language.Korean, "게임을 종료 \r\n하시겠습니까?" },
            { Language.English, "Are you sure you\r\nwant to quit?" }
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
        { "bt_메인화면", new Dictionary<Language, string> {
            { Language.Korean, "메인화면" },
            { Language.English, "Main Menu" }
        }},
        { "msg_메인화면", new Dictionary<Language, string> {
            { Language.Korean, "메인 화면으로 돌아 \r\n가시겠습니까?" },
            { Language.English, "Are you sure you want to\nreturn to the main menu?" }
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
        //------------------------------------------ 세팅창 키 설정
        { "msg_자동줍기", new Dictionary<Language, string> {
            { Language.Korean, "자동 줍기" },
            { Language.English, "Auto Pick Up" }
        }},
        { "msg_상단이동", new Dictionary<Language, string> {
            { Language.Korean, "상단 이동" },
            { Language.English, "Move Up" }
        }},
        { "msg_하단이동", new Dictionary<Language, string> {
            { Language.Korean, "하단 이동" },
            { Language.English, "Move Down" }
        }},
        { "msg_좌측이동", new Dictionary<Language, string> {
            { Language.Korean, "좌측 이동" },
            { Language.English, "Move Left" }
        }},
        { "msg_우측이동", new Dictionary<Language, string> {
            { Language.Korean, "우측 이동" },
            { Language.English, "Move Right" }
        }},
        { "msg_인벤토리", new Dictionary<Language, string> {
            { Language.Korean, "인벤토리" },
            { Language.English, "Inventory" }
        }},
        { "msg_조사", new Dictionary<Language, string> {
            { Language.Korean, "조사" },
            { Language.English, "Search" }
        }},
        { "msg_줍기", new Dictionary<Language, string> {
            { Language.Korean, "줍기" },
            { Language.English, "Pick Up" }
        }},
        { "msg_구매", new Dictionary<Language, string> {
            { Language.Korean, "구매" },
            { Language.English, "Perchase" }
        }},
        { "msg_퀵슬롯", new Dictionary<Language, string> {
            { Language.Korean, "퀵슬롯" },
            { Language.English, "Quick Slot" }
        }},
        //------------------------------------------ 장비칸 & 스탯
        { "msg_장비칸스탯", new Dictionary<Language, string> {
            { Language.Korean, "장비칸 & 스탯" },
            { Language.English, "Equipment & Stats" }
        }},
        { "msg_레벨", new Dictionary<Language, string> {
            { Language.Korean, "레벨" },
            { Language.English, "LV" }
        }},
        { "msg_경험치", new Dictionary<Language, string> {
            { Language.Korean, "경험치" },
            { Language.English, "Experience" }
        }},
        { "msg_체력", new Dictionary<Language, string> {
            { Language.Korean, "체력" },
            { Language.English, "HP" }
        }},
        { "msg_공격력", new Dictionary<Language, string> {
            { Language.Korean, "공격력" },
            { Language.English, "Damage" }
        }},
        { "msg_공격속도", new Dictionary<Language, string> {
            { Language.Korean, "공격 속도" },
            { Language.English, "Attack Speed" }
        }},
        { "msg_방어력", new Dictionary<Language, string> {
            { Language.Korean, "방어력" },
            { Language.English, "Defense" }
        }},
        { "msg_명중률", new Dictionary<Language, string> {
            { Language.Korean, "명중률" },
            { Language.English, "Accuracy" }
        }},
        { "msg_이동속도", new Dictionary<Language, string> {
            { Language.Korean, "이동 속도" },
            { Language.English, "Move Speed" }
        }},
        { "msg_회피", new Dictionary<Language, string> {
            { Language.Korean, "회피" },
            { Language.English, "Evasion" }
        }},
        { "msg_힘", new Dictionary<Language, string> {
            { Language.Korean, "힘" },
            { Language.English, "STR" }
        }},
        { "msg_허기", new Dictionary<Language, string> {
            { Language.Korean, "허기" },
            { Language.English, "Hunger" }
        }},
        //------------------------------------------ 플레이어 특성
        { "msg_감정", new Dictionary<Language, string> {
            { Language.Korean, "감정\n장착 무기 감정 속도 2배,\n갑옷 1.5배, 무기 즉시 감정,\n갑옷 1.5배" },
            { Language.English, "Appraisal\nEquipped weapon identify speed ×2,\nArmor ×1.5, Weapon instant identify,\nArmor ×1.5" }
        }},
        { "msg_건강한몸", new Dictionary<Language, string> {
            { Language.Korean, "건강한 몸\n배고픔/보통 상태 체력 회복 1.25배, 배고픔/보통 상태 체력 회복 1.5배" },
            { Language.English, "Healthy Body\nHP regen ×1.25 while Hungry/Normal, HP regen ×1.5 while Hungry/Normal" }
        }},
        { "msg_비상식량", new Dictionary<Language, string> {
            { Language.Korean, "비상 식량\n다음 층부터 보스층 제외 2개 층에서\n소형 비상식량 등장, 추가 1개 층에서 더 등장" },
            { Language.English, "Survivalist\nSmall emergency rations spawn on 2 non-boss floors starting next floor,\n+1 additional floor" }
        }},
        { "msg_운동", new Dictionary<Language, string> {
            { Language.Korean, "운동\n힘 +1, 힘 +2" },
            { Language.English, "Physical\nSTR +1, STR +2" }
        }},
        { "msg_탐욕", new Dictionary<Language, string> {
            { Language.Korean, "탐욕\n골드 획득량 1.25배, 골드 획득량 1.5배" },
            { Language.English, "Greed\nGold gain ×1.25, Gold gain ×1.5" }
        }},
        { "msg_조심스러운발걸음", new Dictionary<Language, string> {
            { Language.Korean, "조심스러운 발걸음\n주변 탐색 확률 1.5배, 주변 탐색 확률 2배" },
            { Language.English, "Careful Steps\nSearch chance ×1.5, Search chance ×2" }
        }},
        { "msg_액세서리중독", new Dictionary<Language, string> {
            { Language.Korean, "액세서리 중독\n반지 효율 1.25배, 반지 효율 1.5배" },
            { Language.English, "Ring Addiction\nRing efficiency ×1.25, Ring efficiency ×1.5" }
        }},
        { "msg_강타", new Dictionary<Language, string> {
            { Language.Korean, "강타\n기본 공격 데미지 +1, 기본 공격 데미지 +1~2" },
            { Language.English, "Smash\nBase attack damage +1, Base attack damage +1~2" }
        }},
        { "msg_넓은시야", new Dictionary<Language, string> {
            { Language.Korean, "넓은 시야\n시야 +1, 시야 +2" },
            { Language.English, "Wide Vision\nVision +1, Vision +2" }
        }},
        //------------------------------------------ 인게임 버튼
        { "bt_튜토리얼스킵", new Dictionary<Language, string> {
            { Language.Korean, "튜토리얼 스킵" },
            { Language.English, "Skip Tutorial" }
        }},
        { "bt_취소", new Dictionary<Language, string> {
            { Language.Korean, "취소" },
            { Language.English, "Cancel" }
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
            { Language.Korean, "등 뒤를 서늘하게 만들던 시선이,\r\n마침내 코앞까지 다가왔다." },
            { Language.English, "The gaze that once chilled\r\nyour spine has finally\r\ncrept within reach." }
        }},
        { "msg_보스1대사2", new Dictionary<Language, string> {
            { Language.Korean, "그 깊은 어둠이,\r\n단 하나의 눈동자가 되어 모습을 드러낸다." },
            { Language.English, "From the depths of that darkness,\r\na single eye emerges." }
        }},
        { "msg_보스1대사3", new Dictionary<Language, string> {
            { Language.Korean, "그것의 이름은……" },
            { Language.English, "Its name is……" }
        }},
        //------------------------------------------ 보스 2 대사
        { "msg_보스2대사1", new Dictionary<Language, string> {
            { Language.Korean, "뼈가 부딪히는 소리가\r\n던전에 울려퍼진다." },
            { Language.English, "The clatter of bones\r\nechoes through the dungeon." }
        }},
        { "msg_보스2대사2", new Dictionary<Language, string> {
            { Language.Korean, "그를 마주한 이들은\r\n모두 이곳에서 끝을 맞이한다." },
            { Language.English, "All who have faced him\r\nmeet their end here." }
        }},
        { "msg_보스2대사3", new Dictionary<Language, string> {
            { Language.Korean, "수많은 생명을 앗아간\r\n대검의 칼끝은.." },
            { Language.English, "The tip of a greatsword\r\nthat has claimed countless lives.." }
        }},
        { "msg_보스2대사4", new Dictionary<Language, string> {
            { Language.Korean, "지금 당신을 향해 있다." },
            { Language.English, "is now pointed at you." }
        }},
        //------------------------------------------ 보스 3 대사
        { "msg_보스3대사1", new Dictionary<Language, string> {
            { Language.Korean, "저주의 근원이 모습을 드러낸다." },
            { Language.English, "The source of the curse\r\nreveals itself." }
        }},
        { "msg_보스3대사2", new Dictionary<Language, string> {
            { Language.Korean, "죽음의 종착역에 드디어 도착했다." },
            { Language.English, "You have finally reached the end of the line." }
        }},
        { "msg_보스3대사3", new Dictionary<Language, string> {
            { Language.Korean, "드디어 당신의 여정의 끝이 보인다." },
            { Language.English, "At last, the end of your journey\r\ncomes into sight." }
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
