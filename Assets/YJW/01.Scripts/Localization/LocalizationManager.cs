using System.Collections.Generic;
using UnityEngine;

public class LocalizationManager : PersistentSingleton<LocalizationManager>
{
    public enum Language { Korean, English }
    public Language currentLanguage = Language.Korean;

    // 언어 저장 키 (드롭다운과 공유)
    public const string LanguagePrefsKey = "Localization.Language";

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
        { "bt_순위표", new Dictionary<Language, string> {
            { Language.Korean, "순위표" },
            { Language.English, "Ranking" }
        }},
        { "msg_일반순위표", new Dictionary<Language, string> {
            { Language.Korean, "일반 순위표" },
            { Language.English, "Normal Ranking" }
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
        { "bt_비디오설정", new Dictionary<Language, string> {
            { Language.Korean, "비디오 설정" },
            { Language.English, "Video Settings" }
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
        { "msg_상태_굶주림", new Dictionary<Language, string> {
            { Language.Korean, "굶주림" },
            { Language.English, "Starving" }
        }},
        { "msg_상태_배고픔", new Dictionary<Language, string> {
            { Language.Korean, "배고픔" },
            { Language.English, "Hungry" }
        }},
        { "msg_상태_보통", new Dictionary<Language, string> {
            { Language.Korean, "보통" },
            { Language.English, "Normal" }
        }},
        { "msg_상태_배부름", new Dictionary<Language, string> {
            { Language.Korean, "배부름" },
            { Language.English, "Full" }
        }},
        //------------------------------------------ 비디오 세팅
        { "msg_전체화면", new Dictionary<Language, string> {
            { Language.Korean, "전체화면" },
            { Language.English, "Fullscreen" }
        }},
        { "msg_테두리없는창", new Dictionary<Language, string> {
            { Language.Korean, "테두리 없는 창" },
            { Language.English, "Borderless Window" }
        }},
        { "msg_창모드", new Dictionary<Language, string> {
            { Language.Korean, "창모드" },
            { Language.English, "Windowed" }
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
        { "msg_특성", new Dictionary<Language, string> {
            { Language.Korean, "특성" },
            { Language.English, "Trait" }
        }},
        { "msg_특성설명", new Dictionary<Language, string> {
            { Language.Korean, "특성설명" },
            { Language.English, "Trait Description" }
        }},
        { "msg_스킬포인트", new Dictionary<Language, string> {
            { Language.Korean, "스킬포인트" },
            { Language.English, "Skill Points" }
        }},
        //------------------------------------------ 인게임
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
        //------------------------------------------ 툴팁
        { "msg_착용힘", new Dictionary<Language, string> {
            { Language.Korean, "착용 힘" },
            { Language.English, "STR Required" }
        }},
        { "msg_T공격력", new Dictionary<Language, string> {
            { Language.Korean, "공격력" },
            { Language.English, "Damage" }
        }},
        { "msg_피해감소", new Dictionary<Language, string> {
            { Language.Korean, "피해 감소" },
            { Language.English, "Damage Reduction" }
        }},
        { "msg_T공격속도", new Dictionary<Language, string> {
            { Language.Korean, "공격속도" },
            { Language.English, "Attack Speed" }
        }},
        { "msg_효과", new Dictionary<Language, string> {
            { Language.Korean, "효과" },
            { Language.English, "Effect" }
        }},
        { "msg_미감정", new Dictionary<Language, string> {
            { Language.Korean, "미감정" },
            { Language.English, "Unidentified" }
        }},
        { "msg_받은피해감소", new Dictionary<Language, string> {
            { Language.Korean, "받은 피해 감소" },
            { Language.English, "Block Reduction" }
        }},
        { "msg_내구도", new Dictionary<Language, string> {
            { Language.Korean, "내구도" },
            { Language.English, "Durability" }
        }},
        { "msg_시야", new Dictionary<Language, string> {
            { Language.Korean, "시야" },
            { Language.English, "Vision" }
        }},
        { "msg_초", new Dictionary<Language, string> {
            { Language.Korean, "초" },
            { Language.English, "s" }
        }},
        { "msg_생명파편효과", new Dictionary<Language, string> {
            { Language.Korean, "효과: 최대 체력의 1/20 회복" },
            { Language.English, "Effect: Heals 1/20 of Max HP" }
        }},
        { "msg_음식효과", new Dictionary<Language, string> {
            { Language.Korean, "효과: 허기 +{0}" },
            { Language.English, "Effect: Hunger +{0}" }
        }},
        //------------------------------------------ 아이템 - 포션
        { "item_name_2001", new Dictionary<Language, string> {
            { Language.Korean, "회복 포션" },
            { Language.English, "Healing Potion" }
        }},
        { "item_desc_2001", new Dictionary<Language, string> {
            { Language.Korean, "생명이 돌아온다. 죽기 전에 마셔라." },
            { Language.English, "Life flows back into you. Drink it before you die." }
        }},
        { "item_name_2002", new Dictionary<Language, string> {
            { Language.Korean, "화염 포션" },
            { Language.English, "Fire Potion" }
        }},
        { "item_desc_2002", new Dictionary<Language, string> {
            { Language.Korean, "던지는 순간, 불길이 사방으로 번진다. 적도 나도 예외는 없다." },
            { Language.English, "The moment it's thrown, flames spread in every direction. Neither friend nor foe is spared." }
        }},
        { "item_name_2003", new Dictionary<Language, string> {
            { Language.Korean, "힘 포션" },
            { Language.English, "Strength Potion" }
        }},
        { "item_desc_2003", new Dictionary<Language, string> {
            { Language.Korean, "근육이 팽창하고 한계가 밀려난다. 더 무거운 것도 들어 올릴 수 있다." },
            { Language.English, "Your muscles swell and your limits push outward. You can lift even heavier things." }
        }},
        { "item_name_2004", new Dictionary<Language, string> {
            { Language.Korean, "서리 포션" },
            { Language.English, "Frost Potion" }
        }},
        { "item_desc_2004", new Dictionary<Language, string> {
            { Language.Korean, "냉기가 주변을 감싼다. 얼어붙은 자는 도망치지 못한다." },
            { Language.English, "Cold wraps around the area. Those who freeze cannot flee." }
        }},
        { "item_name_2006", new Dictionary<Language, string> {
            { Language.Korean, "신속 포션" },
            { Language.English, "Haste Potion" }
        }},
        { "item_desc_2006", new Dictionary<Language, string> {
            { Language.Korean, "발이 땅에 닿기도 전에 다음 발이 나간다. 10초간 바람처럼 달려라." },
            { Language.English, "Your next step lands before the last one touches the ground. Run like the wind for 10 seconds." }
        }},
        { "item_name_2007", new Dictionary<Language, string> {
            { Language.Korean, "부유 포션" },
            { Language.English, "Levitation Potion" }
        }},
        { "item_desc_2007", new Dictionary<Language, string> {
            { Language.Korean, "잠시 땅을 잊어라. 위험 지형도 발끝 아래 스쳐 지나간다." },
            { Language.English, "Forget the ground for a while. Even hazardous terrain slips harmlessly beneath your feet." }
        }},
        { "item_name_2008", new Dictionary<Language, string> {
            { Language.Korean, "정화 포션" },
            { Language.English, "Purify Potion" }
        }},
        { "item_desc_2008", new Dictionary<Language, string> {
            { Language.Korean, "독과 둔화가 몸 밖으로 빠져나간다. 10초간, 다시는 걸리지 않는다." },
            { Language.English, "Poison and slow are flushed from your body. For 10 seconds, you won't be afflicted again." }
        }},
        //------------------------------------------ 아이템 - 포션 효과
        { "item_potionEffect_HP", new Dictionary<Language, string> {
            { Language.Korean, "체력 +{0}" },
            { Language.English, "HP +{0}" }
        }},
        { "item_potionEffect_strength", new Dictionary<Language, string> {
            { Language.Korean, "힘 +{0}" },
            { Language.English, "STR +{0}" }
        }},
        { "item_potionEffect_sight", new Dictionary<Language, string> {
            { Language.Korean, "시야 전체 공개" },
            { Language.English, "Reveals the entire map" }
        }},
        { "item_potionEffect_experience", new Dictionary<Language, string> {
            { Language.Korean, "경험치 +{0}" },
            { Language.English, "EXP +{0}" }
        }},
        { "item_potionEffect_fire", new Dictionary<Language, string> {
            { Language.Korean, "착탄 범위 화염 피해" },
            { Language.English, "Fire damage in blast radius" }
        }},
        { "item_potionEffect_ice", new Dictionary<Language, string> {
            { Language.Korean, "착탄 범위 빙결 피해" },
            { Language.English, "Frost damage in blast radius" }
        }},
        { "item_potionEffect_moveSpeed", new Dictionary<Language, string> {
            { Language.Korean, "이동속도 +{0}" },
            { Language.English, "Move Speed +{0}" }
        }},
        { "item_potionEffect_levitation", new Dictionary<Language, string> {
            { Language.Korean, "부유 {0}초 / Hazard 지형 무시" },
            { Language.English, "Levitate {0}s / Ignores Hazard terrain" }
        }},
        { "item_potionEffect_purify", new Dictionary<Language, string> {
            { Language.Korean, "독·둔화 즉시 해제 / {0}초간 면역" },
            { Language.English, "Cures Poison & Slow instantly / {0}s Immunity" }
        }},
        //------------------------------------------ 아이템 - 스크롤
        { "item_name_1001", new Dictionary<Language, string> {
            { Language.Korean, "지도 스크롤" },
            { Language.English, "Map Scroll" }
        }},
        { "item_desc_1001", new Dictionary<Language, string> {
            { Language.Korean, "어둠 속 지형이 눈앞에 펼쳐진다. 함정도 피할 수 없다." },
            { Language.English, "The terrain hidden in darkness unfolds before your eyes. Not even traps can hide." }
        }},
        { "item_name_1002", new Dictionary<Language, string> {
            { Language.Korean, "강화 스크롤" },
            { Language.English, "Reinforce Scroll" }
        }},
        { "item_desc_1002", new Dictionary<Language, string> {
            { Language.Korean, "어둠의 기운이 씻겨 나간다. 저주 받은 장비를 다시 내 것으로." },
            { Language.English, "The dark energy is washed away. Reclaim your cursed gear as your own." }
        }},
        { "item_name_1003", new Dictionary<Language, string> {
            { Language.Korean, "감정 스크롤" },
            { Language.English, "Appraise Scroll" }
        }},
        { "item_desc_1003", new Dictionary<Language, string> {
            { Language.Korean, "봉인된 비밀을 드러낸다. 이 장비가 축복인지, 저주인지 알게 될 것이다." },
            { Language.English, "It reveals a sealed secret. You'll learn whether this gear is a blessing or a curse." }
        }},
        { "item_name_1004", new Dictionary<Language, string> {
            { Language.Korean, "정화 스크롤" },
            { Language.English, "Purify Scroll" }
        }},
        { "item_desc_1004", new Dictionary<Language, string> {
            { Language.Korean, "고대의 문자가 무기에 깃든다. 날은 더 예리해지고, 갑옷은 더 단단해진다." },
            { Language.English, "Ancient script settles into the gear. The blade grows sharper, the armor grows tougher." }
        }},
        //------------------------------------------ 아이템 - 근접무기
        { "item_name_5001", new Dictionary<Language, string> {
            { Language.Korean, "소검" },
            { Language.English, "Short Sword" }
        }},
        { "item_desc_5001", new Dictionary<Language, string> {
            { Language.Korean, "모든 전사의 첫 번째 동반자. 가볍고 믿을 수 있다." },
            { Language.English, "Every warrior's first companion. Light and dependable." }
        }},
        { "item_name_5002", new Dictionary<Language, string> {
            { Language.Korean, "방패" },
            { Language.English, "Shield" }
        }},
        { "item_desc_5002", new Dictionary<Language, string> {
            { Language.Korean, "막는 것이 곧 공격이다. 강화될수록 차감되는 피해가 늘어난다." },
            { Language.English, "To block is to strike back. The more it's enhanced, the more damage it cancels out." }
        }},
        { "item_name_5003", new Dictionary<Language, string> {
            { Language.Korean, "검" },
            { Language.English, "Sword" }
        }},
        { "item_desc_5003", new Dictionary<Language, string> {
            { Language.Korean, "균형 잡힌 날. 힘과 속도, 어느 쪽도 포기하지 않았다." },
            { Language.English, "A well-balanced blade. It sacrifices neither power nor speed." }
        }},
        { "item_name_5004", new Dictionary<Language, string> {
            { Language.Korean, "쌍검" },
            { Language.English, "Twin Swords" }
        }},
        { "item_desc_5004", new Dictionary<Language, string> {
            { Language.Korean, "두 손으로 두 배를 베어낸다. 빠른 공격이 적의 반격을 허용하지 않는다." },
            { Language.English, "Two blades cut twice as deep. Fast strikes leave no room for retaliation." }
        }},
        { "item_name_5005", new Dictionary<Language, string> {
            { Language.Korean, "장검" },
            { Language.English, "Longsword" }
        }},
        { "item_desc_5005", new Dictionary<Language, string> {
            { Language.Korean, "긴 날이 주는 위압감. 강한 힘을 가진 자만이 다룰 수 있다." },
            { Language.English, "The long blade commands respect. Only the truly strong can wield it." }
        }},
        { "item_name_5006", new Dictionary<Language, string> {
            { Language.Korean, "특대 방패" },
            { Language.English, "Tower Shield" }
        }},
        { "item_desc_5006", new Dictionary<Language, string> {
            { Language.Korean, "거대한 강철 벽. 강화할수록 막을 수 없는 공격이 없어진다." },
            { Language.English, "A massive wall of steel. The more it's enhanced, the fewer attacks can get through." }
        }},
        { "item_name_5007", new Dictionary<Language, string> {
            { Language.Korean, "특대검" },
            { Language.English, "Greatsword" }
        }},
        { "item_desc_5007", new Dictionary<Language, string> {
            { Language.Korean, "한 번의 일격으로 승부를 낸다. 손에 쥔 순간, 적이 작아 보인다." },
            { Language.English, "One strike decides everything. The moment you grip it, your enemies look small." }
        }},
        { "item_name_5008", new Dictionary<Language, string> {
            { Language.Korean, "시미터" },
            { Language.English, "Scimitar" }
        }},
        { "item_desc_5008", new Dictionary<Language, string> {
            { Language.Korean, "굽은 날이 살을 훑고 지나간다. 날렵함과 파괴력을 동시에 갖췄다." },
            { Language.English, "The curved blade sweeps through flesh. Agile and devastating in equal measure." }
        }},
        //------------------------------------------ 아이템 - 투척무기
        { "item_name_3001", new Dictionary<Language, string> {
            { Language.Korean, "투척용 칼" },
            { Language.English, "Throwing Knife" }
        }},
        { "item_desc_3001", new Dictionary<Language, string> {
            { Language.Korean, "가볍고 빠르다. 거리를 두고 싸우는 가장 기본적인 선택." },
            { Language.English, "Light and quick. The most basic choice for fighting at range." }
        }},
        { "item_name_3002", new Dictionary<Language, string> {
            { Language.Korean, "수리검" },
            { Language.English, "Shuriken" }
        }},
        { "item_desc_3002", new Dictionary<Language, string> {
            { Language.Korean, "바람을 가르듯 날아간다. 누구보다 빠르게 손을 떠난다." },
            { Language.English, "It cuts through the wind. Nothing leaves your hand faster." }
        }},
        { "item_name_3003", new Dictionary<Language, string> {
            { Language.Korean, "작살" },
            { Language.English, "Harpoon" }
        }},
        { "item_desc_3003", new Dictionary<Language, string> {
            { Language.Korean, "묵직하게 꽂힌다. 한 번의 투척으로 깊은 상처를 남긴다." },
            { Language.English, "It lands with brutal weight, leaving a deep wound with a single throw." }
        }},
        { "item_name_3004", new Dictionary<Language, string> {
            { Language.Korean, "올가미" },
            { Language.English, "Snare" }
        }},
        { "item_desc_3004", new Dictionary<Language, string> {
            { Language.Korean, "발목을 잡아라. 2초간 적의 속도를 반으로 끊어낸다." },
            { Language.English, "Catch them by the ankle. Halves the enemy's speed for 2 seconds." }
        }},
        { "item_name_3005", new Dictionary<Language, string> {
            { Language.Korean, "쿠나이" },
            { Language.English, "Kunai" }
        }},
        { "item_desc_3005", new Dictionary<Language, string> {
            { Language.Korean, "눈치채기 전에 꽂힌다. 인식하지 못한 적에게 치명적이다." },
            { Language.English, "It lands before they notice. Lethal against an unaware enemy." }
        }},
        { "item_name_3006", new Dictionary<Language, string> {
            { Language.Korean, "투척용 도끼" },
            { Language.English, "Throwing Axe" }
        }},
        { "item_desc_3006", new Dictionary<Language, string> {
            { Language.Korean, "날이 살을 파고든다. 3초간 흘러내리는 피가 멈추지 않는다." },
            { Language.English, "The blade digs into flesh. The bleeding won't stop for 3 seconds." }
        }},
        { "item_name_3007", new Dictionary<Language, string> {
            { Language.Korean, "투창" },
            { Language.English, "Javelin" }
        }},
        { "item_desc_3007", new Dictionary<Language, string> {
            { Language.Korean, "긴 사정거리와 강한 관통력. 믿음직한 마지막 수단." },
            { Language.English, "Long range and brutal penetration. A reliable last resort." }
        }},
        //------------------------------------------ 아이템 - 반지
        { "item_name_7001", new Dictionary<Language, string> {
            { Language.Korean, "신속의 반지" },
            { Language.English, "Ring of Haste" }
        }},
        { "item_desc_7001", new Dictionary<Language, string> {
            { Language.Korean, "손가락에 바람이 깃든다. 한 걸음이 더 빨라진다." },
            { Language.English, "Wind gathers around your finger. Every step comes faster." }
        }},
        { "item_effect_7001", new Dictionary<Language, string> {
            { Language.Korean, "이동 속도" },
            { Language.English, "Move Speed" }
        }},
        { "item_name_7002", new Dictionary<Language, string> {
            { Language.Korean, "타격의 반지" },
            { Language.English, "Ring of Striking" }
        }},
        { "item_desc_7002", new Dictionary<Language, string> {
            { Language.Korean, "주먹이 가벼워진다. 공격이 더 빠르게 연속된다." },
            { Language.English, "Your fists grow lighter. Attacks chain together faster." }
        }},
        { "item_effect_7002", new Dictionary<Language, string> {
            { Language.Korean, "공격 속도" },
            { Language.English, "Attack Speed" }
        }},
        { "item_name_7003", new Dictionary<Language, string> {
            { Language.Korean, "힘의 반지" },
            { Language.English, "Ring of Strength" }
        }},
        { "item_desc_7003", new Dictionary<Language, string> {
            { Language.Korean, "낀 순간부터 더 무거운 것을 들 수 있다." },
            { Language.English, "From the moment you wear it, you can lift heavier things." }
        }},
        { "item_effect_7003", new Dictionary<Language, string> {
            { Language.Korean, "힘" },
            { Language.English, "STR" }
        }},
        { "item_name_7004", new Dictionary<Language, string> {
            { Language.Korean, "저격의 반지" },
            { Language.English, "Ring of Sniping" }
        }},
        { "item_desc_7004", new Dictionary<Language, string> {
            { Language.Korean, "투척 무기가 더 날카롭게, 더 깊이 꽂힌다." },
            { Language.English, "Throwing weapons land sharper, and dig in deeper." }
        }},
        { "item_effect_7004", new Dictionary<Language, string> {
            { Language.Korean, "투척 무기 강화" },
            { Language.English, "Throwable Enhance" }
        }},
        { "item_name_7005", new Dictionary<Language, string> {
            { Language.Korean, "회피의 반지" },
            { Language.English, "Ring of Evasion" }
        }},
        { "item_desc_7005", new Dictionary<Language, string> {
            { Language.Korean, "투척 무기가 더 날카롭게, 더 깊이 꽂힌다." },
            { Language.English, "Throwing weapons land sharper, and dig in deeper." }
        }},
        { "item_effect_7005", new Dictionary<Language, string> {
            { Language.Korean, "회피" },
            { Language.English, "Evasion" }
        }},
        { "item_name_7006", new Dictionary<Language, string> {
            { Language.Korean, "정확성의 반지" },
            { Language.English, "Ring of Accuracy" }
        }},
        { "item_desc_7006", new Dictionary<Language, string> {
            { Language.Korean, "투척 무기가 더 날카롭게, 더 깊이 꽂힌다." },
            { Language.English, "Throwing weapons land sharper, and dig in deeper." }
        }},
        { "item_effect_7006", new Dictionary<Language, string> {
            { Language.Korean, "명중률" },
            { Language.English, "Accuracy" }
        }},
        //------------------------------------------ 아이템 - 갑옷
        { "item_name_4001", new Dictionary<Language, string> {
            { Language.Korean, "천 갑옷" },
            { Language.English, "Cloth Armor" }
        }},
        { "item_desc_4001", new Dictionary<Language, string> {
            { Language.Korean, "촘촘히 엮인 쇠사슬이 날카로운 것들을 막아낸다." },
            { Language.English, "Tightly woven chain links turn aside anything sharp." }
        }},
        { "item_name_4002", new Dictionary<Language, string> {
            { Language.Korean, "가죽 갑옷" },
            { Language.English, "Leather Armor" }
        }},
        { "item_desc_4002", new Dictionary<Language, string> {
            { Language.Korean, "유연하고 가볍다. 방어와 기동성 사이의 첫 번째 타협." },
            { Language.English, "Flexible and light — the first compromise between defense and mobility." }
        }},
        { "item_name_4003", new Dictionary<Language, string> {
            { Language.Korean, "사슬 갑옷" },
            { Language.English, "Chainmail" }
        }},
        { "item_desc_4003", new Dictionary<Language, string> {
            { Language.Korean, "보잘것없어 보여도 없는 것보다는 낫다. 모험의 시작." },
            { Language.English, "Looks pitiful, but better than nothing.\nThe start of an adventure." }
        }},
        { "item_name_4004", new Dictionary<Language, string> {
            { Language.Korean, "황금 갑옷" },
            { Language.English, "Gold Armor" }
        }},
        { "item_desc_4004", new Dictionary<Language, string> {
            { Language.Korean, "빛나는 방어구. 두꺼운 금속이 어지간한 공격을 튕겨낸다." },
            { Language.English, "Gleaming armor. Thick metal deflects most attacks." }
        }},
        //------------------------------------------ 아이템 - 저주
        { "item_name_curse_Slow", new Dictionary<Language, string> {
            { Language.Korean, "둔화저주" },
            { Language.English, "Slow Curse" }
        }},
        { "item_desc_curse_Slow", new Dictionary<Language, string> {
            { Language.Korean, "발이 무거워진다. 한 걸음마다 어둠이 잡아끄는 것 같다." },
            { Language.English, "Your feet grow heavy. Every step feels like the dark is dragging you back." }
        }},
        { "item_name_curse_Bleed", new Dictionary<Language, string> {
            { Language.Korean, "출혈저주" },
            { Language.English, "Bleed Curse" }
        }},
        { "item_desc_curse_Bleed", new Dictionary<Language, string> {
            { Language.Korean, "맞을 때마다 피가 흐를 수 있다. 상처는 쉽게 아물지 않는다." },
            { Language.English, "Every hit may draw blood. The wounds don't close easily." }
        }},
        { "item_name_curse_Displace", new Dictionary<Language, string> {
            { Language.Korean, "변위저주" },
            { Language.English, "Displace Curse" }
        }},
        { "item_desc_curse_Displace", new Dictionary<Language, string> {
            { Language.Korean, "공격 받는 순간 어디로 튕길지 알 수 없다. 최악의 순간에 배신한다." },
            { Language.English, "The moment you're struck, you can't tell where you'll be flung. It betrays you at the worst possible moment." }
        }},
        { "item_name_curse_Alert", new Dictionary<Language, string> {
            { Language.Korean, "경보저주" },
            { Language.English, "Alert Curse" }
        }},
        { "item_desc_curse_Alert", new Dictionary<Language, string> {
            { Language.Korean, "칼을 휘두를 때마다 적들이 고개를 돌릴 수 있다." },
            { Language.English, "Every swing of your blade may turn enemies' heads your way." }
        }},
        { "item_name_curse_Drain", new Dictionary<Language, string> {
            { Language.Korean, "착취저주" },
            { Language.English, "Drain Curse" }
        }},
        { "item_desc_curse_Drain", new Dictionary<Language, string> {
            { Language.Korean, "칼날이 주인의 피도 탐낸다." },
            { Language.English, "The blade hungers for its wielder's blood too." }
        }},
        { "item_name_curse_Polarize", new Dictionary<Language, string> {
            { Language.Korean, "양극화저주" },
            { Language.English, "Polarize Curse" }
        }},
        { "item_desc_curse_Polarize", new Dictionary<Language, string> {
            { Language.Korean, "완벽하게 베거나, 아무것도 하지 못하거나. 운에 모든 것을 맡겨라." },
            { Language.English, "Either it cuts perfectly, or it does nothing at all. Leave everything to chance." }
        }},
        //------------------------------------------ 아이템 - 마법
        { "item_name_magic_EmptyStep", new Dictionary<Language, string> {
            { Language.Korean, "공허 발걸음" },
            { Language.English, "Empty Step" }
        }},
        { "item_desc_magic_EmptyStep", new Dictionary<Language, string> {
            { Language.Korean, "위험 지형이 길을 막지 못한다. 허공을 걷듯 지나쳐라." },
            { Language.English, "Hazardous terrain can no longer bar your path. Walk through it as if it weren't there." }
        }},
        { "item_name_magic_LastSprint", new Dictionary<Language, string> {
            { Language.Korean, "마지막 질주" },
            { Language.English, "Last Sprint" }
        }},
        { "item_desc_magic_LastSprint", new Dictionary<Language, string> {
            { Language.Korean, "10초가 남았을 때, 공포가 아닌 속도로 응답한다." },
            { Language.English, "When 10 seconds remain, you answer with speed, not fear." }
        }},
        { "item_name_magic_SecondOfGreed", new Dictionary<Language, string> {
            { Language.Korean, "탐욕의 초침" },
            { Language.English, "Second of Greed" }
        }},
        { "item_desc_magic_SecondOfGreed", new Dictionary<Language, string> {
            { Language.Korean, "손이 빨라진다. 상자도, 아이템도 순식간에 집어 든다." },
            { Language.English, "Your hands quicken. Chests and items alike are snatched up in an instant." }
        }},
        { "item_name_magic_BloodClock", new Dictionary<Language, string> {
            { Language.Korean, "혈시계" },
            { Language.English, "Blood Clock" }
        }},
        { "item_desc_magic_BloodClock", new Dictionary<Language, string> {
            { Language.Korean, "피해를 입을 때마다 간혹 시간이 되돌아온다." },
            { Language.English, "Each time you take damage, time occasionally rewinds." }
        }},
        { "item_name_magic_DrainLife", new Dictionary<Language, string> {
            { Language.Korean, "흡혈" },
            { Language.English, "Drain Life" }
        }},
        { "item_desc_magic_DrainLife", new Dictionary<Language, string> {
            { Language.Korean, "적을 베면 피가 내게로 돌아온다." },
            { Language.English, "Cutting down an enemy returns their blood to you." }
        }},
        { "item_name_magic_Execution", new Dictionary<Language, string> {
            { Language.Korean, "처형" },
            { Language.English, "Execution" }
        }},
        { "item_desc_magic_Execution", new Dictionary<Language, string> {
            { Language.Korean, "막바지에 몰린 적은 살려두지 않는다. 한 번에 끝낸다." },
            { Language.English, "An enemy on the brink is shown no mercy. It ends in a single blow." }
        }},
        { "item_name_magic_Focus", new Dictionary<Language, string> {
            { Language.Korean, "집중" },
            { Language.English, "Focus" }
        }},
        { "item_desc_magic_Focus", new Dictionary<Language, string> {
            { Language.Korean, "빗나간 공격은 다음 번의 확실한 명중을 약속한다." },
            { Language.English, "A missed attack guarantees the next strike will land true." }
        }},
        { "item_name_magic_Chase", new Dictionary<Language, string> {
            { Language.Korean, "추격" },
            { Language.English, "Chase" }
        }},
        { "item_desc_magic_Chase", new Dictionary<Language, string> {
            { Language.Korean, "적이 쓰러진 자리에서 다음 목표를 향해 달려라. 3초간 빨라진다." },
            { Language.English, "From where an enemy falls, dash toward your next target. You move faster for 3 seconds." }
        }},
        //------------------------------------------ 아이템 - 기타
        { "item_name_6001", new Dictionary<Language, string> {
            { Language.Korean, "횃불" },
            { Language.English, "Torch" }
        }},
        { "item_desc_6001", new Dictionary<Language, string> {
            { Language.Korean, "어둠을 밀어낸다. 30초 동안 더 먼 곳까지 볼 수 있다." },
            { Language.English, "It pushes back the dark. For 30 seconds, you can see much farther." }
        }},
        { "item_name_6002", new Dictionary<Language, string> {
            { Language.Korean, "생명 파편" },
            { Language.English, "Fragment of Life" }
        }},
        { "item_desc_6002", new Dictionary<Language, string> {
            { Language.Korean, "하수인들이 품고 있던 시간 조각. 상처를 조금씩 회복시킨다." },
            { Language.English, "A shard of time the minions once carried. It slowly mends your wounds." }
        }},
        { "item_name_8001", new Dictionary<Language, string> {
            { Language.Korean, "소형 비상 식량" },
            { Language.English, "Small Emergency Rations" }
        }},
        { "item_desc_8001", new Dictionary<Language, string> {
            { Language.Korean, "급할 때 한 입. 배고픔을 조금 달랜다." },
            { Language.English, "A quick bite in a pinch. Takes the edge off your hunger." }
        }},
        { "item_name_8002", new Dictionary<Language, string> {
            { Language.Korean, "비상 식량" },
            { Language.English, "Emergency Rations" }
        }},
        { "item_desc_8002", new Dictionary<Language, string> {
            { Language.Korean, "허기를 채운다. 살아남는 자는 먹는 것도 잊지 않는다." },
            { Language.English, "Fills your stomach. Survivors never forget to eat." }
        }},
        { "item_name_8003", new Dictionary<Language, string> {
            { Language.Korean, "빵" },
            { Language.English, "Bread" }
        }},
        { "item_desc_8003", new Dictionary<Language, string> {
            { Language.Korean, "따뜻한 빵 한 덩이. 이 지하에서 이런 걸 찾다니 운이 좋다." },
            { Language.English, "A loaf of warm bread. Lucky to find something like this down here." }
        }},
        //------------------------------------------ 열쇠 이름
        { "msg_반지상자열쇠", new Dictionary<Language, string> {
            { Language.Korean, "반지 상자 열쇠" },
            { Language.English, "Ring Chest Key" }
        }},
        { "msg_스페셜방열쇠", new Dictionary<Language, string> {
            { Language.Korean, "스페셜 방 열쇠" },
            { Language.English, "Special Room Key" }
        }},
        //------------------------------------------ 인챈트
        { "msg_마법부여", new Dictionary<Language, string> {
            { Language.Korean, "마법 부여" },
            { Language.English, "Enchant" }
        }},
        { "msg_무기마법부여", new Dictionary<Language, string> {
            { Language.Korean, "무기\r\n마법부여" },
            { Language.English, "Weapon\r\nEnchant" }
        }},
        { "msg_갑옷마법부여", new Dictionary<Language, string> {
            { Language.Korean, "갑옷\r\n마법부여" },
            { Language.English, "Armor\r\nEnchant" }
        }},
        //------------------------------------------- 챌린지
        { "msg_챌린지설명", new Dictionary<Language, string> {
            { Language.Korean, "챌린지 설명" },
            { Language.English, "Challenge Description" }
        }},
        { "msg_시드입력", new Dictionary<Language, string> {
            { Language.Korean, "시드 입력" },
            { Language.English, "Enter Seed" }
        }},
        { "msg_챌린지선택", new Dictionary<Language, string> {
            { Language.Korean, "챌린지 선택" },
            { Language.English, "Select Challenge" }
        }},
        { "msg_시간압박", new Dictionary<Language, string> {
            { Language.Korean, "시간 압박" },
            { Language.English, "Time Pressure" }
        }},
        { "msg_보스강화", new Dictionary<Language, string> {
            { Language.Korean, "보스 강화" },
            { Language.English, "Boss Buff" }
        }},
        { "msg_빈약한강화", new Dictionary<Language, string> {
            { Language.Korean, "빈약한 강화" },
            { Language.English, "Weak Upgrade" }
        }},
        { "msg_회복봉인", new Dictionary<Language, string> {
            { Language.Korean, "회복 봉인" },
            { Language.English, "Sealed Recovery" }
        }},
        { "msg_파편봉인", new Dictionary<Language, string> {
            { Language.Korean, "파편 봉인" },
            { Language.English, "Sealed Shards" }
        }},
        { "msg_시야제한", new Dictionary<Language, string> {
            { Language.Korean, "시야 제한" },
            { Language.English, "Limited Vision" }
        }},
        //------------------------------------------- 챌린지 설명 (호버 패널)
        { "challenge_desc_TimePressure", new Dictionary<Language, string> {
            { Language.Korean, "모래시계가 더 빠르게 떨어진다. \n각 층의 제한시간이 60초에서 45초로 줄어든다." },
            { Language.English, "The hourglass falls faster.\nEach floor's time limit drops from 60 to 45 seconds." }
        }},
        { "challenge_desc_BossBuff", new Dictionary<Language, string> {
            { Language.Korean, "저승의 수호자들이 본래의 힘을 되찾았다. \n모든 보스의 능력치가 상승했다." },
            { Language.English, "The guardians of the underworld have reclaimed their true power.\nAll boss stats are increased." }
        }},
        { "challenge_desc_ScarceUpgrade", new Dictionary<Language, string> {
            { Language.Korean, "대장간의 불씨가 꺼져간다. \n영역당 등장하는 강화 주문서가 3개에서 2개로 줄어든다." },
            { Language.English, "The forge's embers are dying out.\nReinforce Scrolls per area drop from 3 to 2." }
        }},
        { "challenge_desc_HealSeal", new Dictionary<Language, string> {
            { Language.Korean, "이 땅의 약은 더 이상 듣지 않는다. \n회복 포션을 마셔도 체력이 회복되지 않는다." },
            { Language.English, "Medicine no longer works in this land.\nDrinking a Healing Potion no longer restores HP." }
        }},
        { "challenge_desc_FragmentSeal", new Dictionary<Language, string> {
            { Language.Korean, "생명 파편을 더 이상 획득할 수 없다." },
            { Language.English, "You can no longer obtain Fragments of Life." }
        }},
        { "challenge_desc_VisionLimit", new Dictionary<Language, string> {
            { Language.Korean, "저승의 어둠이 시야를 좁힌다. \n기본 시야가 2칸 감소한다." },
            { Language.English, "The darkness of the underworld narrows your sight.\nBase vision is reduced by 2 tiles." }
        }},
        //------------------------------------------- 상점
        { "msg_골드", new Dictionary<Language, string> {
            { Language.Korean, "골드" },
            { Language.English, "Gold" }
        }},
        { "msg_지갑없음", new Dictionary<Language, string> {
            { Language.Korean, "지갑 없음" },
            { Language.English, "No Wallet" }
        }},
        { "msg_골드부족", new Dictionary<Language, string> {
            { Language.Korean, "골드 부족" },
            { Language.English, "Not Enough Gold" }
        }},
        { "msg_인벤토리없음", new Dictionary<Language, string> {
            { Language.Korean, "인벤토리 없음" },
            { Language.English, "No Inventory" }
        }},
        { "msg_인벤토리가득", new Dictionary<Language, string> {
            { Language.Korean, "인벤토리 가득" },
            { Language.English, "Inventory Full" }
        }},
        //------------------------------------------- 오프닝
        { "msg_오프닝1", new Dictionary<Language, string> {
            { Language.Korean, "평범한 탐험가의 운명을 송두리째 삼켜버린 '하데스의 모래시계'\r\n거스를 수 없는 시간의 저주가 그를 덮쳐온다\r\n영원히 갇혀버린 시간의 감옥 속에서,\r\n저주를 풀기 위해 탐험가는 어둠 속으로 발을 내딛는다" },
            { Language.English, "The Hourglass of Hades' swallowed the fate of an ordinary explorer whole.\r\nThe unstoppable curse of time crashes down upon him.\r\nTrapped forever within a prison of frozen time,\r\nthe explorer steps into the darkness to break the curse." }
        }},
        { "msg_오프닝2", new Dictionary<Language, string> {
            { Language.Korean, "한정된 시간속의 탐험가 이야기" },
            { Language.English, "A tale of an explorer against the clock." }
        }},
        //------------------------------------------- 엔딩
        { "msg_엔딩", new Dictionary<Language, string> {
            { Language.Korean, "하데스를 쓰러뜨리는데 성공했으나\r\n저주는 끝나지 않았다\r\n저주를 풀기 위한 그의 여정은\r\n앞으로도 계속 될 것이다" },
            { Language.English, "The explorer succeeded in defeating Hades,\r\nbut the curse was not lifted.\r\nHis journey to break the curse\r\nwill continue on." }
        }},
        { "msg_엔딩계속", new Dictionary<Language, string> {
            { Language.Korean, "TO BE CONTINUED" },
            { Language.English, "TO BE CONTINUED" }
        }},
        //------------------------------------------- 상점 주인 대사
        { "msg_상점대사1", new Dictionary<Language, string> {
            { Language.Korean, "어서오시게" },
            { Language.English, "Welcome" }
        }},
        { "msg_상점대사2", new Dictionary<Language, string> {
            { Language.Korean, "원하는 물건을 구입하게" },
            { Language.English, "Just buy something" }
        }},
        { "msg_상점대사3", new Dictionary<Language, string> {
            { Language.Korean, "조심하게" },
            { Language.English, "Be careful" }
        }},
        { "msg_상점대사4", new Dictionary<Language, string> {
            { Language.Korean, "저주의 기운이 느껴지는군" },
            { Language.English, "I feel a cursed presence" }
        }},
        { "msg_상점대사5", new Dictionary<Language, string> {
            { Language.Korean, "도라지즙은 팔고있지 않다네" },
            { Language.English, "We do not sell juice here" }
        }},
        { "msg_상점대사6", new Dictionary<Language, string> {
            { Language.Korean, "배고픈가?" },
            { Language.English, "Hungry?" }
        }},
        { "msg_상점대사7", new Dictionary<Language, string> {
            { Language.Korean, "여기가 어디지?" },
            { Language.English, "Where am I?" }
        }},
    };

    // 게임 시작 시 저장된 언어를 즉시 불러온다
    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
            return;
        currentLanguage = (Language)PlayerPrefs.GetInt(LanguagePrefsKey, (int)currentLanguage);
    }

    public static string GetOrDefault(string key, string fallback)
    {
        return Instance != null ? Instance.Get(key) : fallback;
    }

    public string Get(string key)
    {
        if (texts.ContainsKey(key))
            return texts[key][currentLanguage];
        return key; // fallback
    }

    public void SetLanguage(Language lang)
    {
        currentLanguage = lang;
        // 변경 즉시 저장
        PlayerPrefs.SetInt(LanguagePrefsKey, (int)lang);
        PlayerPrefs.Save();
        // 언어 변경 이벤트 발생
        OnLanguageChanged?.Invoke();
    }

    public event System.Action OnLanguageChanged;
}
