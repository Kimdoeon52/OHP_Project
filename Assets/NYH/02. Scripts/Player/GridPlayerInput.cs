using UnityEngine;

/// <summary>
/// WASD/방향키 이동, Space 던지기, Q 포션. 플레이어 턴이 아닐 때 입력 무시.
/// </summary>
public class GridPlayerInput : MonoBehaviour
{
    private GridPlayer _player;

    public void Init(GridPlayer player)
    {
        _player = player;
    }

    /// <summary>
    /// 현재 누르고 있는 이동 방향을 한 칸 이동 기준으로 반환한다.
    /// 이동과 애니메이션이 서로 다른 방향 우선순위를 쓰면 홀드 이동 중 끊김이 생길 수 있어 한 곳에 모았다.
    /// </summary>
    public bool TryGetHeldMoveDirection(out Vector2Int direction)
    {
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            direction = Vector2Int.up;
            return true;
        }
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            direction = Vector2Int.down;
            return true;
        }
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            direction = Vector2Int.left;
            return true;
        }
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            direction = Vector2Int.right;
            return true;
        }

        direction = Vector2Int.zero;
        return false;
    }

    private void Update()
    {
        if (TurnManager.Instance == null || !TurnManager.Instance.IsPlayerTurn) return;
        if (_player.Health.IsDead) return;
        // 한 칸 이동 보간 중에는 다음 입력을 막아 턴 처리와 위치가 어긋나지 않게 한다.
        if (_player.Movement.IsMoving) return;
        if (_player.ActionDelay != null && !_player.ActionDelay.CanAct) return;
        if (TimeSystem.Instance.TimeRemaining <= 0f) return;

        // 이동 (단일 키 처리 — 동시 입력 시 위 우선)
        if (TryGetHeldMoveDirection(out Vector2Int moveDirection))
        {
            _player.Movement.TryMove(moveDirection);
            return;
        }

        // 좌클릭 근접 공격
        if (Input.GetMouseButtonDown(0))
        {
            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 worldPos = cam.ScreenToWorldPoint(Input.mousePosition);
                var clickedCell = GridSystem.Instance.WorldToGrid(worldPos);
                _player.Combat.TryMeleeAt(clickedCell);
            }
            return;
        }

        // 단검 던지기
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _player.Combat.TryThrow();
            return;
        }

        // 포션 사용
        if (Input.GetKeyDown(KeyCode.Q))
        {
            _player.Health.TryUsePotion();
            return;
        }
    }
}
