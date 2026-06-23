using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ShopperText : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("NPC 머리 위에 뜰 말풍선 게임 오브젝트")]
    [SerializeField] private GameObject speechBubble;

    [Tooltip("말풍선 안의 대사를 출력할 UI Text 컴포넌트")]
    [SerializeField] private Text speechTextUI;

    // 만약 TextMeshPro를 사용하신다면 아래 주석을 해제하고 사용하세요.
    // [SerializeField] private TMPro.TextMeshProUGUI speechTextTMP;

    [Header("UI Position Settings (Optional)")]
    [Tooltip("말풍선 위치를 NPC 월드 좌표에 맞춰 실시간으로 추적할지 여부\n(말풍선이 메인 HUD UI Canvas 하위에 있는 경우 true로 켭니다.)")]
    [SerializeField] private bool followNpcPosition = false;

    [Tooltip("NPC 위치 기준 머리 위 오프셋 거리\n(우측 상단에 위치하려면 X: 1.0f, Y: 1.5f 정도로 설정합니다.)")]
    [SerializeField] private Vector3 speechBubbleOffset = new Vector3(1.0f, 1.5f, 0f);

    [Tooltip("렌더링 중인 메인 카메라 (비워두면 Camera.main 자동 사용)")]
    [SerializeField] private Camera targetCamera;

    [Header("UI Scale Settings (Camera Zoom Sync)")]
    [Tooltip("카메라 줌아웃에 연동하여 말풍선 크기도 함께 작아지게 할지 여부")]
    [SerializeField] private bool adjustScaleWithZoom = false;

    [Tooltip("말풍선 스케일이 1(원래 크기)일 때의 기준 카메라 크기 (Orthographic Size)\n(인게임 기본 카메라 크기에 맞추어 설정합니다.)")]
    [SerializeField] private float referenceCameraSize = 5f;

    [Header("Dialogue Settings")]
    [Tooltip("플레이어가 주변에 오면 랜덤하게 출력될 대사 목록 (기본 2개 제공)")]
    [SerializeField] private string[] dialogueLines = new string[] { "어서오세요!", "무엇을 찾으시나요?" };

    [Tooltip("말풍선이 켜진 후 자동으로 꺼지기까지의 시간(초)")]
    [SerializeField] private float autoCloseDelay = 2.0f;

    private bool wasPlayerAdjacent = false;
    private Coroutine closeBubbleCoroutine;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = targetCamera != null ? targetCamera : Camera.main;

        // 시작 시 말풍선은 비활성화 상태로 둡니다.
        if (speechBubble != null)
        {
            speechBubble.SetActive(false);

            // 텍스트 컴포넌트가 말풍선의 하위 자식으로 들어가 있지 않다면,
            // 에디터 상에 배치된 상대 좌표(오프셋)를 완벽히 유지하도록 자식으로 자동 정렬합니다.
            if (speechTextUI != null && speechTextUI.transform.parent != speechBubble.transform)
            {
                speechTextUI.transform.SetParent(speechBubble.transform, true);
            }
            // TextMeshProUGUI 주석 해제 시 아래 코드 활성화
            /*
            if (speechTextTMP != null && speechTextTMP.transform.parent != speechBubble.transform)
            {
                speechTextTMP.transform.SetParent(speechBubble.transform, true);
            }
            */
        }
    }

    private void Update()
    {
        CheckPlayerDistance();
    }

    private void LateUpdate()
    {
        // 실시간 위치 추적이 활성화되어 있고 말풍선이 켜져 있을 때 위치 보정
        if (followNpcPosition && speechBubble != null && speechBubble.activeSelf)
        {
            UpdateSpeechBubblePosition();
        }
    }

    private void UpdateSpeechBubblePosition()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        // NPC의 월드 좌표 + 오프셋 좌표를 스크린 좌표로 변환
        Vector3 worldTargetPos = transform.position + speechBubbleOffset;
        Vector3 screenPos = mainCamera.WorldToScreenPoint(worldTargetPos);

        // 오직 부모인 말풍선 UI 위치 및 스케일 갱신합니다.
        // 텍스트는 말풍선의 자식으로 지정되어 있으므로 에디터에서 정렬한 상대 좌표를 완벽히 유지하며 움직입니다.
        if (speechBubble != null)
        {
            RectTransform bubbleRect = speechBubble.GetComponent<RectTransform>();
            if (bubbleRect != null)
            {
                bubbleRect.position = screenPos;

                // 카메라 줌 상태(Orthographic Size) 또는 거리에 반비례하여 스케일 조절
                if (adjustScaleWithZoom)
                {
                    if (mainCamera.orthographic)
                    {
                        float scaleRatio = referenceCameraSize / mainCamera.orthographicSize;
                        bubbleRect.localScale = Vector3.one * scaleRatio;
                    }
                    else
                    {
                        // 3D Perspective 카메라인 경우 거리 기반 비율 조정
                        float distance = Vector3.Distance(mainCamera.transform.position, transform.position);
                        float referenceDistance = 10f; // 기준 거리
                        float scaleRatio = referenceDistance / Mathf.Max(0.1f, distance);
                        bubbleRect.localScale = Vector3.one * scaleRatio;
                    }
                }
                else
                {
                    // 줌 보정을 안 쓸 때는 기본 스케일 유지
                    bubbleRect.localScale = Vector3.one;
                }
            }
            else
            {
                speechBubble.transform.position = mainCamera.ScreenToWorldPoint(screenPos);
            }
        }
    }

    private void CheckPlayerDistance()
    {
        // GameManager와 플레이어 인스턴스가 존재하지 않으면 체크를 스킵합니다.
        if (GameManager.Instance == null || GameManager.Instance.player == null)
            return;

        // 플레이어의 Grid Entity 획득
        var playerEntity = GameManager.Instance.player.GetComponent<EntityBase>();
        if (playerEntity == null)
            return;

        Vector2Int playerGridPos = playerEntity.GridPosition;
        Vector2Int npcGridPos = Vector2Int.zero;

        // GridSystem을 사용하여 NPC의 월드 좌표를 격자 좌표로 변환
        if (GridSystem.Instance != null)
        {
            npcGridPos = GridSystem.Instance.WorldToGrid(transform.position);
        }
        else
        {
            // 폴백용 좌표 변환
            npcGridPos = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));
        }

        // 8방향 인접 판단 (플레이어가 NPC 기준 상하좌우 및 대각선 1칸 내에 있는지)
        bool isCurrentlyAdjacent = Mathf.Abs(playerGridPos.x - npcGridPos.x) <= 1 && 
                                   Mathf.Abs(playerGridPos.y - npcGridPos.y) <= 1;

        if (isCurrentlyAdjacent)
        {
            // 플레이어가 인접해 있지 않다가 인접해진 순간
            if (!wasPlayerAdjacent)
            {
                wasPlayerAdjacent = true;
                ShowSpeechBubble();
            }
        }
        else
        {
            // 플레이어가 주변에 있다가 멀어진 순간
            if (wasPlayerAdjacent)
            {
                wasPlayerAdjacent = false;
                
                // 플레이어가 범위를 벗어나면 켜져 있던 말풍선을 즉시 닫습니다.
                if (speechBubble != null && speechBubble.activeSelf)
                {
                    if (closeBubbleCoroutine != null)
                    {
                        StopCoroutine(closeBubbleCoroutine);
                        closeBubbleCoroutine = null;
                    }
                    speechBubble.SetActive(false);
                }
            }
        }
    }

    private void ShowSpeechBubble()
    {
        if (dialogueLines == null || dialogueLines.Length == 0) return;

        // 기존에 작동 중이던 자동 닫기 코루틴이 있다면 중지하여 타이머를 초기화합니다.
        if (closeBubbleCoroutine != null)
        {
            StopCoroutine(closeBubbleCoroutine);
        }

        // 대사 목록 중 하나를 랜덤으로 선택하여 출력
        int randomIndex = Random.Range(0, dialogueLines.Length);
        string selectedText = dialogueLines[randomIndex];

        SetText(selectedText);

        // 말풍선 활성화
        if (speechBubble != null)
        {
            speechBubble.SetActive(true);
            
            // 말풍선이 켜지는 순간에도 위치를 한 번 갱신해 줍니다.
            if (followNpcPosition)
            {
                UpdateSpeechBubblePosition();
            }
        }

        // autoCloseDelay(2초) 후 자동으로 꺼지는 코루틴 시작
        closeBubbleCoroutine = StartCoroutine(HideSpeechBubbleAfterDelay());
    }

    private IEnumerator HideSpeechBubbleAfterDelay()
    {
        yield return new WaitForSeconds(autoCloseDelay);

        if (speechBubble != null)
        {
            speechBubble.SetActive(false);
        }
        
        closeBubbleCoroutine = null;
    }

    private void SetText(string text)
    {
        if (speechTextUI != null)
        {
            speechTextUI.text = text;
        }
        // TextMeshProUGUI 주석 해제 시 아래 코드 사용
        /*
        if (speechTextTMP != null)
        {
            speechTextTMP.text = text;
        }
        */
    }
}
