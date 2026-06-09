# 경보저주(Alarm Curse) 초간단 개별 감지 구현 계획

본 문서는 무기 저주 중 하나인 **경보저주(Alarm Curse)**가 발동했을 때 적들을 발동 시점의 플레이어 위치로 이동시키는 시스템의 구현 계획입니다.

---

## 1. 핵심 동작 규칙 (개별 감지 및 해제)

1. **경보 발동**: `CurseData`에 의해 `GameManager.Instance.isAlerted = true;`가 켜집니다.
2. **위치 스냅샷**: 각 적(`EnemyBase`)은 자신의 행동 턴 시작 시 전역 `isAlerted == true`를 인지하면, 그 순간의 플레이어 위치(`Player.GridPosition`)를 자신의 개별 변수(`_alarmTarget`)에 저장합니다.
3. **이동 및 해제**:
   - 시야 내에 플레이어가 없으면, 적들은 기억해둔 `_alarmTarget`을 향해 이동합니다.
   - 적이 `_alarmTarget`에 도달하면 `_alarmTarget = null;`로 상태를 해제합니다.
   - 만약 이동 중 플레이어가 시야 안에 들어오면 즉시 플레이어를 직접 추적하고 `_alarmTarget`을 지웁니다.
4. **전역 플래그 리셋**: 적들의 턴이 모두 끝난 후 `TurnManager`가 `GameManager.Instance.isAlerted = false;`로 전역 경보 신호를 꺼줍니다.

---

## 2. 파일별 수정 예정 코드

### 2.1. TurnManager.cs (`Assets/KDU/01.Scripts/Turn/TurnManager.cs`)

- **수정 내용**: 적들의 턴 업데이트가 완료된 후 전역 경보 플래그(`isAlerted`)를 자동으로 `false`로 리셋합니다.
- **코드 수정** (`ProcessEnemyTurns` 코루틴 마지막 부분):

```csharp
    private IEnumerator ProcessEnemyTurns(bool skipEnemyTurnDelay)
    {
        _processingTurn = true;
        IsPlayerTurn = false;

        var snapshot = new List<IGridEntity>(_entities);
        foreach (var entity in snapshot)
        {
            if (!IsEntityAlive(entity)) continue;
            entity.OnTurnUpdate();
        }

        if (!skipEnemyTurnDelay && enemyTurnDelay > 0f)
            yield return new WaitForSeconds(enemyTurnDelay);

        _entities.RemoveAll(e => e == null);

        // [추가] 적들의 행동이 완료되었으므로, 이번 턴에 켜진 경보 플래그 리셋
        if (GameManager.Instance != null)
        {
            GameManager.Instance.isAlerted = false;
        }

        _processingTurn = false;
        IsPlayerTurn = true;
    }
```

### 2.2. EnemyBase.cs (`Assets/KKH/02. Scripts/EnemyBase.cs`)

- **수정 내용**:
  1. 개별 경보 목표 위치를 기억할 `_alarmTarget` 필드를 추가합니다.
  2. 기존 플레이어 추적 로직을 재사용 가능하도록 `TryStepToward(Vector2Int)`로 분리합니다.
  3. `PerformMovement()`에서 전역 경보 활성 감지 시 타겟을 스냅샷하고, 이동 상태를 제어합니다.
- **코드 수정**:

```csharp
    // [추가] 개별 경보 추적 좌표 (null이면 경보 추적 안 함)
    private Vector2Int? _alarmTarget = null;

    // 1. 기존 TryStepTowardPlayer 내부 로직을 범용화하여 추출
    protected void TryStepToward(Vector2Int targetPos)
    {
        var diff = targetPos - GridPosition;
        var dirX = diff.x != 0 ? new Vector2Int(System.Math.Sign(diff.x), 0) : Vector2Int.zero;
        var dirY = diff.y != 0 ? new Vector2Int(0, System.Math.Sign(diff.y)) : Vector2Int.zero;

        bool xPrimary = Mathf.Abs(diff.x) >= Mathf.Abs(diff.y);
        var primary = xPrimary ? dirX : dirY;
        var secondary = xPrimary ? dirY : dirX;

        if (primary != Vector2Int.zero && TryStep(primary)) return;
        if (secondary != Vector2Int.zero && TryStep(secondary)) return;
    }

    // 기존 메서드는 1줄로 대체
    protected void TryStepTowardPlayer()
    {
        TryStepToward(Player.GridPosition);
    }

    // 2. PerformMovement 로직 수정
    protected virtual void PerformMovement()
    {
        if (IsDead) return;

        // A. 이번 턴에 경보가 켜졌다면 현재 플레이어의 위치를 기억
        if (GameManager.Instance != null && GameManager.Instance.isAlerted)
        {
            _alarmTarget = Player.GridPosition;
        }

        // B. 시야 범위 내에 플레이어가 들어오면 플레이어 추적을 최우선으로 처리
        if (SquaredDistanceToPlayer() <= data.visionRange * data.visionRange)
        {
            TryStepTowardPlayer();
            _alarmTarget = null; // 플레이어를 발견했으므로 경보 기억은 지움
            return;
        }

        // C. 시야 밖에 있고, 기억해둔 경보 위치가 존재한다면 경보 지점으로 이동
        if (_alarmTarget != null)
        {
            Vector2Int targetCell = _alarmTarget.Value;
            if (GridPosition != targetCell)
            {
                TryStepToward(targetCell);
            }
            else
            {
                _alarmTarget = null; // 경보 지점에 도달 완료 시 해제
            }
        }
    }
```
