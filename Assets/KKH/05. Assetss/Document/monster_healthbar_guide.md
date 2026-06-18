# 유니티 몬스터 월드 스페이스 체력바(HP Bar) 제작 가이드

이 가이드는 작성된 `MonsterHealthBar.cs` 스크립트를 사용하여, 몬스터의 머리 위에 띄워질 **16비트 스타일의 체력바 UI**를 유니티 에디터에서 설정하는 단계별 매뉴얼입니다.

---

## 1단계. 몬스터 프리팹 내부에 월드 스페이스 Canvas 생성

1. **몬스터 프리팹 열기**: 프리팹 편집 화면으로 진입합니다.
2. **Canvas 오브젝트 추가**:
   - 몬스터 최상위 오브젝트 하위에 마우스 우클릭 -> **UI** -> **Canvas**를 생성합니다.
   - 이름을 `HP_Bar_Canvas`로 변경합니다.
3. **Canvas 컴포넌트 설정 (Inspector)**:
   - **Render Mode**: `World Space` 로 변경합니다. *(기존 Screen Space - Overlay에서 월드 상의 좌표를 따라다니도록 변경)*
   - **Event Camera**: 메인 카메라(`Main Camera`)를 드래그하여 연결합니다.
   - **Rect Transform**:
     - **Pos X**: `0`, **Pos Y**: `1.2` (몬스터 머리 위 알맞은 높이로 지정)
     - **Width**: `100`, **Height**: `20`
     - **Scale**: `0.01`, `0.01`, `1` *(월드 좌표 단위에 맞게 아주 작게 스케일을 낮춥니다. 보통 0.01 이하로 줄여야 게임 화면 크기에 맞게 픽셀이 출력됩니다.)*

---

## 2단계. 슬라이더(HP Bar) 오브젝트 구성

체력이 깎일 때 메인 바가 먼저 줄어들고 뒷배경의 노란 바가 늦게 줄어드는 **듀얼 바(Delayed Bar)** 연출을 위해 슬라이더를 2개 겹쳐서 배치합니다.

1. **지연 체력바 (Delayed Slider - 뒷배경 노란/하얀 바)**:
   - `HP_Bar_Canvas` 아래에 마우스 우클릭 -> **UI** -> **Slider**를 생성하고 이름을 `Delayed_HP_Slider`로 변경합니다.
   - **Interactable**: `False` (체크 해제)
   - **Transition**: `None`
   - **Navigation**: `None`
   - **하위 컴포넌트 정리**:
     - `Handle Slide Area` 자식 오브젝트는 필요 없으므로 **삭제**합니다.
   - **색상 지정**:
     - `Background` 이미지: 어두운 회색 또는 검정색 (체크가 다 닳았을 때의 바닥 배경)
     - `Fill Area -> Fill` 이미지: **노란색(Yellow) 또는 주황색**

2. **메인 체력바 (Main Slider - 앞쪽 빨간 바)**:
   - 생성한 `Delayed_HP_Slider`를 복사(Ctrl + D)하여 이름을 `Main_HP_Slider`로 변경하고, 레이어 순서 상 **아래쪽(앞에 렌더링되도록)**에 배치합니다.
   - **하위 컴포넌트 정리**:
     - `Main_HP_Slider` 하위의 `Background` 오브젝트는 필요 없으므로 **삭제**합니다. (뒷 슬라이더의 배경을 그대로 사용)
   - **색상 지정**:
     - `Fill Area -> Fill` 이미지: **빨간색(Red) 또는 초록색**

3. **두 슬라이더 크기 정렬**:
   - 두 슬라이더 모두 Rect Transform의 Anchor를 **Stretch-Stretch** (Alt를 누른 상태에서 우측 하단 상자 클릭)로 설정하여 캔버스 크기에 꽉 차도록 채웁니다.
   - 슬라이더 컴포넌트의 **Min Value**는 `0`, **Max Value**는 `1`로 설정해 둡니다.

---

## 3단계. 스크립트 연결 및 최종 셋업

1. **MonsterHealthBar 컴포넌트 추가**:
   - `HP_Bar_Canvas` 오브젝트에 제작된 **`MonsterHealthBar` 스크립트**를 붙여줍니다.
2. **컴포넌트 참조 할당 (Drag & Drop)**:
   - **Target Enemy**: (비워두면 부모 오브젝트의 `EnemyBase`를 자동으로 찾으므로 비워두셔도 됩니다.)
   - **Main Hp Slider**: `Main_HP_Slider` 오브젝트를 드래그하여 할당합니다.
   - **Delayed Hp Slider**: `Delayed_HP_Slider` 오브젝트를 드래그하여 할당합니다.
3. **세부 옵션 설정**:
   - **Auto Hide On Full Hp**: `True` (체력이 가득 찬 기본 상태에서는 체력바를 숨겨 화면을 깔끔하게 유지합니다. 한 대 맞으면 나타납니다.)
   - **Delay Duration**: `0.5` (피격된 후 0.5초 동안 노란 바가 버티다가 줄어들기 시작합니다.)
   - **Drain Speed**: `2` (지연 바가 서서히 줄어드는 속도)

---

### UI 픽셀 선명도 팁 (Pixel Art Aesthetic)
체력바의 검은 테두리나 색상 바가 뭉개지거나 흐릿하게 보인다면:
* UI 이미지에 적용한 스프라이트 에셋의 **Filter Mode**가 `Point (no filter)`로 되어 있는지 확인해 주세요.
* Canvas에 별도의 아웃라인(Outline) 컴포넌트나 그림자 효과를 붙여 16비트 레트로 느낌을 더 살려줄 수 있습니다.
