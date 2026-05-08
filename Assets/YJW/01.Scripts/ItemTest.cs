using UnityEngine;
public class ItemTest : MonoBehaviour
{
    [SerializeField] private FirePotion firePotion;
    private Vector2Int targetPos;
    private bool isWaitingForTarget = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            isWaitingForTarget = true;
        }

        if (isWaitingForTarget && Input.GetMouseButtonDown(0))
        {
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            targetPos = new Vector2Int(Mathf.RoundToInt(worldPos.x), Mathf.RoundToInt(worldPos.y));
            isWaitingForTarget = false;
            firePotion.Throw(targetPos);
        }
    }
}