# 유니티 피격 파티클 시스템 제작 가이드 (초록색 피)

이 가이드는 제미나이로 생성된 초록색 피격 이펙트 스프라이트 시트(`green_blood_splat.png`)를 활용하여, 유니티(Unity) 내에서 몬스터 피격 시 사방으로 튀고 아래로 흐르는 16비트 스타일의 파티클 프리팹을 만드는 단계별 셋업 방법입니다.

---

## 1단계. 스프라이트 시트 임포트 및 슬라이스 (Import & Slice)

1. **에셋 임포트**: `green_blood_splat.png` 이미지를 유니티 프로젝트 폴더로 드래그합니다.
2. **텍스처 설정 (Inspector)**:
   - **Texture Type**: `Sprite (2D and UI)`
   - **Sprite Mode**: `Multiple`
   - **Filter Mode**: `Point (no filter)` _(픽셀 아트를 선명하게 유지하기 위해 필수)_
   - **Compression**: `None`
3. **스프라이트 자르기 (Sprite Editor)**:
   - `Sprite Editor` 버튼을 클릭해 엽니다.
   - 상단 **Slice** 메뉴를 클릭합니다.
   - Type을 **Grid By Cell Size** 또는 **Automatic**으로 설정하고 슬라이스합니다. (생성된 스프라이트 개수인 8장으로 자릅니다.)
   - 우측 상단 **Apply** 버튼을 눌러 변경 사항을 저장합니다.

---

## 2단계. 파티클 시스템 프리팹 생성 및 설정 (Particle System Setup)

1. **오브젝트 생성**:
   - Hierarchy 창에서 마우스 우클릭 -> **Effects** -> **Particle System**을 눌러 새로운 파티클 시스템을 생성합니다.
   - 오브젝트 이름을 `GreenBlood_Hit_Effect`로 변경합니다.
2. **AutoDestroyParticle 스크립트 추가**:
   - `GreenBlood_Hit_Effect` 오브젝트에 방금 생성된 `AutoDestroyParticle` 컴포넌트를 붙여줍니다. (파티클 재생이 끝난 후 메모리에서 자동 제거됩니다.)
3. **메인 모듈 (Main Module) 설정**:
   - **Duration**: `0.3` (피격 시 순간적으로 한 번 튀도록 짧게 설정)
   - **Looping**: `False` (반복 재생 비활성화)
   - **Start Lifetime**: `0.25` ~ `0.4` (무작위 값 범위 설정 `Random Between Two Constants`)
   - **Start Speed**: `3` ~ `5` (무작위 값 범위 설정)
   - **Start Size**: `0.8` ~ `1.2` (방울 크기 무작위화)
   - **Gravity Modifier**: `1.5` (피가 아래로 후두둑 떨어지는 물리 표현을 위해 중요)
   - **Simulation Space**: `World` (몬스터가 움직여도 튄 피는 제자리에 남도록 설정)
   - **Play On Awake**: `True`
4. **방출 모듈 (Emission Module) 설정**:
   - **Rate over Time**: `0`
   - **Bursts**: 목록 추가 후 Count를 `8` ~ `12`로 설정 (생성 시점에 일시에 뿜어냄)
5. **형태 모듈 (Shape Module) 설정**:
   - **Shape**: `Cone`
   - **Angle**: `25` ~ `45` (약간 좁게 분사)
   - **Radius**: `0.1` (피격된 중심점 근처에서 생성되도록 좁게 설정)

6. **머티리얼(Material) 생성 및 연결 (가장 중요)**:
   - 렌더러 모듈에 직접 스프라이트를 넣는 대신, 파티클 전용 머티리얼을 만들어서 사용해야 합니다.
   - Project 뷰에서 마우스 우클릭 -> **Create** -> **Material**을 생성하고 이름을 `M_GreenBlood`로 설정합니다.
   - 머티리얼의 **Shader**를 아래 중 하나로 변경합니다:
     - **빌트인 3D/2D의 경우**: `Legacy Shaders/Particles/Alpha Blended Premultiply` 또는 `Particles/Standard Unlit`
     - **URP(Universal Render Pipeline)의 경우**: `Universal Render Pipeline/Particles/Unlit` (Surface Type: `Transparent`)
   - 머티리얼의 **Texture** (또는 Base Map) 슬롯에 슬라이스하지 않은 원본 `green_blood_splat` 이미지를 드래그하여 할당합니다.
   - 생성한 `M_GreenBlood` 머티리얼을 파티클 시스템의 **Renderer** 모듈의 **Material** 슬롯에 드래그하여 넣어줍니다.
   - **Render Mode**는 **Billboard** (기본값) 상태로 유지합니다.

7. **프레임 애니메이션 적용 (Texture Sheet Animation Module) 설정**:
   - 모듈 체크박스를 켜서 활성화합니다.
   - **Mode**: `Grid`
   - **Tiles**: `X: 8, Y: 1` (스프라이트가 가로로 8장 일렬 배치인 경우)
   - **Frame over Time**: 우측 그래프 아이콘 클릭 후 우상향하는 일자 사선 그래프(`Linear`)를 선택하여 파티클의 수명 동안 0번부터 7번 프레임까지 순차적으로 재생되도록 합니다.
   - **Start Frame**: `0`

설정을 마쳤다면, 해당 오브젝트를 Project 뷰로 드래그하여 **프리팹(Prefab)**으로 만듭니다.

---

## 3단계. 스크립트 연동 (`EnemyBase.cs`)

몬스터가 피격을 당하는 시점에 이 파티클 프리팹을 생성하도록 연동 코드를 추가합니다.

### 1. 필드 추가

`EnemyBase.cs` 상단 설정 영역에 피격 이펙트용 프리팹 필드를 추가합니다.

```csharp
[Header("피격 연출")]
[Tooltip("피격 당할 때 터질 파티클 프리팹 (예: GreenBlood_Hit_Effect)")]
[SerializeField] protected GameObject hitParticlePrefab;
```

### 2. TakeDamage 메서드에 스폰 코드 추가

피해를 받아 체력이 깎였을 때 파티클을 스폰합니다.

```csharp
    public virtual void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0) return;

        // 방어력 적용 후 최종 피해 산출
        int finalDamage = ApplyDefense(amount);
        CurrentHp -= finalDamage;

        if (finalDamage > 0)
        {
            GameEvents.OnAudioEventRequested?.Invoke(AudioEventId.MonsterHit);

            // 피격 파티클 시스템 스폰
            if (hitParticlePrefab != null)
            {
                Instantiate(hitParticlePrefab, transform.position, Quaternion.identity);
            }
        }

        // 살아있으면 Hit 트리거 실행
        if (_animator != null && CurrentHp > 0)
        {
            _animator.SetTrigger("Hit");
        }

        if (CurrentHp <= 0) Die();
    }
```
