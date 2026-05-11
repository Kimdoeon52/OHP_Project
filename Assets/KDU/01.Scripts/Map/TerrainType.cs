/// <summary>
/// 한 셀의 지형 종류. 포션/AI/시야/이동 등 게임 로직이 이 값으로 분기한다.
/// 비주얼은 TerrainTilemaps 의 각 Tilemap 에 분리 보관 — 동일 셀의 지형 = 어느 Tilemap 에 타일이 있는지로 결정.
/// </summary>
public enum TerrainType
{
    None,    // 어떤 타일도 없는 셀 (맵 바깥)
    Floor,   // 일반 floor — 기본 통행 가능, 효과 없음
    Water,   // 이동 슬로우, 일부 포션과 상호작용
    Grass,   // 시야 차단, 이슬 드롭
    Chasm    // 통과 불가, 추락
}
