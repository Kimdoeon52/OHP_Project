using UnityEngine;

public interface IThrowable
{
    // 실제로 던졌으면 true, 조건 미충족 등으로 던지지 못했으면 false
    bool Throw(Vector2Int targetPos);
}


