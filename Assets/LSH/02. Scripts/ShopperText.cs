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

    [Header("Dialogue Settings")]
    [Tooltip("말걸었을 때 랜덤하게 출력될 대사 목록 (기본 2개 제공)")]
    [SerializeField] private string[] dialogueLines = new string[] { "어서오세요!", "무엇을 찾으시나요?" };

    [Tooltip("말풍선이 켜진 후 자동으로 꺼지기까지의 시간(초)")]
    [SerializeField] private float autoCloseDelay = 2.0f;

    [Header("Interaction Settings")]
    [Tooltip("플레이어의 태그명 (상호작용 범위 감지용)")]
    [SerializeField] private string playerTag = "Player";

    private bool isPlayerInRange = false;
    private Coroutine closeBubbleCoroutine;

    private void Start()
    {
        // 시작 시 말풍선은 비활성화 상태로 둡니다.
        if (speechBubble != null)
        {
            speechBubble.SetActive(false);
        }
    }

    private void Update()
    {
        // 플레이어가 감지 범위 내에 있고, E 또는 스페이스바를 누른 경우
        if (isPlayerInRange && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)))
        {
            ShowSpeechBubble();
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

    // 2D 물리 트리거 진입 감지 (플레이어가 범위 내에 들어옴)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerInRange = true;
        }
    }

    // 2D 물리 트리거 이탈 감지 (플레이어가 범위를 벗어남)
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            isPlayerInRange = false;
            
            // 플레이어가 범위를 벗어났을 때 말풍선이 켜져 있었다면 즉시 비활성화합니다.
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
