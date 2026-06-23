using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening; // DialogManager에서 검증된 DOTween 사용

public class Ending : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("엔딩 연출에 사용될 이미지")]
    [SerializeField] private Image endingImage;
    
    [Tooltip("기본 UI Text 컴포넌트")]
    [SerializeField] private Text endingTextUI;

    [Tooltip("마지막에 단독으로 출력될 최종 UI Text 컴포넌트 (예: The End 등)")]
    [SerializeField] private Text endingLastTextUI;

    // 만약 TextMeshPro를 사용하신다면 아래 주석을 해제하고 사용하세요.
    // [SerializeField] private TMPro.TextMeshProUGUI endingTextTMP;
    // [SerializeField] private TMPro.TextMeshProUGUI endingLastTextTMP;

    [Header("Fade Settings")]
    [Tooltip("진입 시 이미지가 점점 밝아지는 연출 시간(초)")]
    [SerializeField] private float fadeDuration = 2.0f;

    [Header("Dialogue Settings")]
    [Tooltip("클릭할 때마다 한 줄씩 출력될 엔딩 대사들")]
    [SerializeField] [TextArea(3, 10)] private string[] dialogueLines;
    
    [Tooltip("한 글자씩 출력되는 타이핑 속도 (0 이하로 설정 시 즉시 출력)")]
    [SerializeField] private float typingSpeed = 0.05f;

    [Header("Exit Animation Settings")]
    [Tooltip("글자가 모두 나온 후 퇴장 연출이 자동으로 시작되기까지의 대기 시간(초)")]
    [SerializeField] private float exitDelay = 2.0f;

    [Tooltip("퇴장 연출 시간 (글씨/그림 투명화 시간 - 타이밍 일치)")]
    [SerializeField] private float exitDuration = 2.0f;

    [Header("Last Dialogue Settings")]
    [Tooltip("모든 연출이 다 끝난 후 마지막에 나타날 텍스트 내용")]
    [SerializeField] [TextArea(3, 10)] private string lastDialogueText = "THE END";

    [Tooltip("마지막 텍스트가 서서히 나타나는 시간(초)")]
    [SerializeField] private float lastTextFadeDuration = 2.0f;

    [Header("Transition Settings")]
    [Tooltip("엔딩 연출이 끝난 후 이동할 씬 이름")]
    [SerializeField] private string nextSceneName = "MainScene";
    
    [Tooltip("엔딩 대사가 모두 끝난 후 추가로 실행할 이벤트")]
    [SerializeField] private UnityEngine.Events.UnityEvent onEndingFinished;

    private int currentLineIndex = 0;
    private bool isTyping = false;
    private string currentCompleteText = "";
    private Coroutine typingCoroutine;
    private Tweener fadeTweener;
    private Sequence exitSequence;
    private Coroutine exitTimerCoroutine;
    private Tweener lastTextFadeTweener;
    
    private bool isFadeComplete = false;
    private bool isExitSequenceStarted = false;
    private bool isLastTextShowing = false;

    private void Start()
    {
        // 텍스트 컴포넌트 초기화
        SetText("");

        // 마지막 텍스트 초기화 및 숨기기
        if (endingLastTextUI != null)
        {
            endingLastTextUI.text = "";
            Color c = endingLastTextUI.color;
            c.a = 0f;
            endingLastTextUI.color = c;
        }

        // 이미지 페이드인 시작
        if (endingImage != null)
        {
            // 알파값을 0으로 설정하여 투명한 상태로 시작
            Color c = endingImage.color;
            c.a = 0f;
            endingImage.color = c;

            // DOTween을 사용하여 페이드인
            fadeTweener = endingImage.DOFade(1f, fadeDuration).OnComplete(() =>
            {
                isFadeComplete = true;
                StartDialogue();
            });
        }
        else
        {
            isFadeComplete = true;
            StartDialogue();
        }
    }

    private void Update()
    {
        // 마우스 왼쪽 클릭 또는 화면 터치 시
        if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }
    }

    private void HandleClick()
    {
        // 1. 이미지가 페이드인 중일 때 클릭하면 페이드인을 즉시 완료하고 대사를 바로 시작합니다.
        if (!isFadeComplete && fadeTweener != null && fadeTweener.IsActive())
        {
            fadeTweener.Complete();
            return;
        }

        // 2. 마지막 텍스트가 표시 중이거나 이미 나타난 상태에서의 클릭 처리
        if (isLastTextShowing)
        {
            if (lastTextFadeTweener != null && lastTextFadeTweener.IsActive())
            {
                // 마지막 텍스트의 페이드인 연출을 스킵하고 즉시 100% 보이게 합니다.
                lastTextFadeTweener.Complete();
            }
            else
            {
                // 마지막 텍스트가 완전히 다 뜬 상태에서 클릭하면 최종 씬 전환을 실행합니다.
                EndEnding();
            }
            return;
        }

        // 대사 목록이 없거나 비어있는 경우 바로 퇴장 연출을 시도합니다.
        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            StartExitSequence();
            return;
        }

        // 3. 타이핑 효과가 진행 중일 때 클릭하면 현재 대사를 화면에 즉시 전체 출력합니다.
        if (isTyping)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }
            SetText(currentCompleteText);
            isTyping = false;
            OnDialogueLineComplete();
        }
        // 4. 텍스트 출력이 이미 완료된 상태에서 클릭
        else
        {
            if (currentLineIndex < dialogueLines.Length)
            {
                ShowNextLine();
            }
            else
            {
                // 모든 대사가 완전히 끝난 후 클릭 시, 자동으로 시작하는 대기 시간을 생략하고 즉시 퇴장 연출을 시작합니다.
                if (!isExitSequenceStarted)
                {
                    if (exitTimerCoroutine != null)
                    {
                        StopCoroutine(exitTimerCoroutine);
                    }
                    StartExitSequence();
                }
            }
        }
    }

    private void StartDialogue()
    {
        currentLineIndex = 0;
        if (dialogueLines != null && dialogueLines.Length > 0)
        {
            ShowNextLine();
        }
        else
        {
            Debug.LogWarning("[Ending] 표시할 대사(dialogueLines)가 설정되지 않았습니다.");
        }
    }

    private void ShowNextLine()
    {
        if (currentLineIndex >= dialogueLines.Length) return;

        currentCompleteText = dialogueLines[currentLineIndex];
        currentLineIndex++;

        if (typingSpeed > 0f)
        {
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }
            typingCoroutine = StartCoroutine(TypeText(currentCompleteText));
        }
        else
        {
            SetText(currentCompleteText);
            OnDialogueLineComplete();
        }
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        SetText("");

        string currentText = "";
        for (int i = 0; i < text.Length; i++)
        {
            currentText += text[i];
            SetText(currentText);
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        OnDialogueLineComplete();
    }

    private void OnDialogueLineComplete()
    {
        // 출력 완료된 대사가 마지막 대사일 때
        if (currentLineIndex >= dialogueLines.Length)
        {
            Debug.Log($"[Ending] 마지막 대사 출력 완료. {exitDelay}초 후 퇴장 연출이 자동으로 시작됩니다.");
            
            // 2초 뒤 퇴장 연출 자동 시작 타이머 실행
            if (exitTimerCoroutine != null)
            {
                StopCoroutine(exitTimerCoroutine);
            }
            exitTimerCoroutine = StartCoroutine(AutoExitDelayRoutine());
        }
    }

    private IEnumerator AutoExitDelayRoutine()
    {
        yield return new WaitForSeconds(exitDelay);
        StartExitSequence();
    }

    private void StartExitSequence()
    {
        if (isExitSequenceStarted) return;
        isExitSequenceStarted = true;

        Debug.Log("[Ending] 퇴장 연출 시작 (글씨 & 그림 점점 투명화)");

        exitSequence = DOTween.Sequence();

        // 1. 그림이 점점 투명해짐 (Fade Out)
        if (endingImage != null)
        {
            exitSequence.Join(endingImage.DOFade(0f, exitDuration));
        }

        // 2. 글씨가 점점 투명해짐 (Fade Out)
        if (endingTextUI != null)
        {
            exitSequence.Join(endingTextUI.DOFade(0f, exitDuration));
        }
        // TextMeshProUGUI 주석 해제 시 아래 코드 사용
        /*
        if (endingTextTMP != null)
        {
            exitSequence.Join(endingTextTMP.DOFade(0f, exitDuration));
        }
        */

        // 3. 글씨 상승과 그림 투명화 타이밍이 끝난 후 최종 텍스트 노출 연출 시작
        exitSequence.OnComplete(() =>
        {
            ShowLastText();
        });
    }

    private void ShowLastText()
    {
        isLastTextShowing = true;
        Debug.Log("[Ending] 최종 텍스트(lastDialogueText) 페이드인 연출을 시작합니다.");

        if (endingLastTextUI != null)
        {
            endingLastTextUI.text = lastDialogueText;
            
            // 확실하게 투명도를 0으로 리셋 후 1로 페이드인 진행
            Color c = endingLastTextUI.color;
            c.a = 0f;
            endingLastTextUI.color = c;
            
            lastTextFadeTweener = endingLastTextUI.DOFade(1f, lastTextFadeDuration);
        }
        // TextMeshProUGUI 주석 해제 시 아래 코드 사용
        /*
        else if (endingLastTextTMP != null)
        {
            endingLastTextTMP.text = lastDialogueText;
            Color c = endingLastTextTMP.color;
            c.a = 0f;
            endingLastTextTMP.color = c;
            lastTextFadeTweener = endingLastTextTMP.DOFade(1f, lastTextFadeDuration);
        }
        */
        else
        {
            Debug.LogWarning("[Ending] endingLastTextUI가 설정되지 않았습니다. 클릭 시 씬이 전환됩니다.");
        }
    }

    private void SetText(string text)
    {
        if (endingTextUI != null)
        {
            endingTextUI.text = text;
        }
        // 만약 TextMeshProUGUI를 활성화한다면 아래 형태로 사용 가능합니다.
        /*
        if (endingTextTMP != null)
        {
            endingTextTMP.text = text;
        }
        */
    }

    private void EndEnding()
    {
        Debug.Log("[Ending] 엔딩 및 퇴장 연출이 완전히 완료되었습니다.");
        
        // 추가로 등록된 커스텀 이벤트 호출
        onEndingFinished?.Invoke();

        // 설정된 다음 씬으로 전환
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }

    private void OnDestroy()
    {
        // 씬이 파괴될 때 동작 중이던 트윈/시퀀스 리소스 정리
        if (fadeTweener != null && fadeTweener.IsActive())
        {
            fadeTweener.Kill();
        }
        if (exitSequence != null && exitSequence.IsActive())
        {
            exitSequence.Kill();
        }
        if (lastTextFadeTweener != null && lastTextFadeTweener.IsActive())
        {
            lastTextFadeTweener.Kill();
        }
    }
}

