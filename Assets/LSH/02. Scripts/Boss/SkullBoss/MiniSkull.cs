using Cysharp.Threading.Tasks;
using UnityEngine;

public class MiniSkull : MonoBehaviour
{
    bool moveStart = true;

    // 미니 해골은 8방향으로
    private static Vector2Int[] Dirs =
    {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right, Vector2Int.left + Vector2Int.up, Vector2Int.left + Vector2Int.down, Vector2Int.right + Vector2Int.up, Vector2Int.right + Vector2Int.down
    };
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private async UniTask MiniMoveStart()
    {
        while(moveStart)
        {
            
        }
    }
}
