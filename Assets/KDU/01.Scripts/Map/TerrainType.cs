// 한 셀의 지형 종류. 
public enum TerrainType
{
    None,    // 어떤 타일도 없는 셀
    Floor,   // 일반 floor — 기본 통행 가능, 효과 없음
    Hazard,  // Floor Feeling 의 Hazard 바닥 (오염수/함정바닥/용암) — 이동 시간 2배 + 데미지, 통과 가능
}
