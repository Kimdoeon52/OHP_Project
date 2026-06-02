using UnityEngine;

/// <summary>
/// 플레이어 입력을 GridPlayer의 이동/근접 공격/포션 사용 흐름으로 전달합니다.
/// 투척 무기는 인벤토리의 YJW 아이템 사용 흐름에서 처리합니다.
/// </summary>
public class GridPlayerInput : MonoBehaviour
{
    private GridPlayer _player;

    public void Init(GridPlayer player)
    {
        _player = player;
    }

    /// <summary>
    /// 현재 누르고 있는 이동 방향을 한 칸 이동 기준으로 반환합니다.
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
        if (_player.Movement.IsMoving) return;
        if (TimeSystem.Instance.TimeRemaining <= 0f) return;

        bool canAct = _player.ActionDelay == null || _player.ActionDelay.CanAct;
        bool canMove = canAct || _player.Movement.IgnoresActionDelayForMovement;

        if (canMove && TryGetHeldMoveDirection(out Vector2Int moveDirection))
        {
            _player.Movement.TryMove(moveDirection);
            return;
        }

        if (!canAct) return;

        if (Input.GetMouseButton(0))
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                Vector3 worldPos = cam.ScreenToWorldPoint(Input.mousePosition);
                Vector2Int clickedCell = GridSystem.Instance.WorldToGrid(worldPos);
                _player.Combat.TryMeleeAt(clickedCell);
            }
            return;
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            _player.Health.TryUsePotion();
        }
    }
}
