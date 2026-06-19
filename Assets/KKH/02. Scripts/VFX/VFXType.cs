public enum VFXType
{
    None = 0,

    // 전투 관련 (Combat)
    PlayerHit,           // 플레이어 피격 (붉은 피 등)
    PlayerDeath,         // 플레이어 사망 연출
    PlayerMeleeSlash,    // 플레이어 근접 공격 슬래시 궤적
    PlayerRangedDagger,  // 플레이어 단검 투척 투사체 궤적 및 충돌 파편
    MonsterHit,          // 몬스터 피격 (초록 피 등)
    MonsterDeath,        // 몬스터 사망 (뼛조각, 먼지, 파편 등)
    MonsterTelegraph,    // 몬스터 공격 예고 범위 바닥 장판
    BossSkillA,          // 보스 전용 광역 공격 기술 이펙트

    // 던전 환경 및 함정 (Environment & Traps)
    TrapSpike,           // 가시 함정 솟구침 및 먼지 파편
    TrapGas,             // 가스 함정 독가스 살포
    TrapAlarm,           // 경보 함정 발동 시각 파동
    TrapTriggered,       // 함정 발동 공용 이펙트
    FloorExit,           // 출구 포탈 및 다음 층 이동 포탈
    SafeRoomEnter,       // 세이프룸 진입 시 시간 정지 시각 효과

    // 아이템 및 상태이상 (Items & Status)
    ItemHeal,            // 체력 포션 사용 및 치유
    ItemCollectGold,     // 골드 획득 연출
    ScrollScan,          // 지도 스크롤 사용 시 안개 걷힘 맵 스캔 광파
    ScrollPurify,        // 저주 정화 스크롤 사용 시 신성한 정화 빛줄기
    StatusBurn,          // 화상 상태 디버프 지속 화염 효과
    StatusPoison,        // 중독 상태 디버프 지속 가스 효과
    StatusFreeze         // 빙결 상태 디버프 지속 얼음 효과
}