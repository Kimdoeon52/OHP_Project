using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class DialogManager : MonoBehaviour
{
    [Header("검은 배경")]
    [SerializeField] Image dialogBackground;
    [Header("기본 텍스트 입력 (폴백용)")]
    [SerializeField] string[] dialogText;
    [Header("텍스트 보여주는 곳")]
    [SerializeField] Text dialog;
    [Header("연출에니메이션 (폴백용)")]
    [SerializeField] GameObject animeEffect;
    [SerializeField] GameObject bossName;
    [Header("연출 프리팹 생성 부모 (비우면 자기 자신)")]
    [SerializeField] Transform effectParent;

    [Header("15층 보스 연출용 손 이미지 (RectTransform)")]
    [SerializeField] private RectTransform cutsceneHandImage;
    [SerializeField] private float handStartPosY = 1000f;
    [SerializeField] private float handTargetPosY = 0f;
    [SerializeField] private float handMoveDuration = 1.0f;
    [SerializeField] private float handWaitDuration = 0.5f;

    [Header("Boss Audio Timing")]
    [SerializeField, Min(0f)] private float titleSfxToBossBgmDelaySeconds = 3f;

    private string[] activeDialogText;
    private int index = 0;  
    private bool isDialogActive = false;

    // 동적으로 스폰된 연출 오브젝트들
    private GameObject spawnedAnimeEffect;
    private GameObject spawnedBossName;
    private AudioSource audioSource;
    private AudioClip activeBgm;
    private AudioClip activeIntroSfx;

    public System.Action OnDialogComplete;

    public void ShowDialog()
    {
        dialogBackground.gameObject.SetActive(true);
        dialog.gameObject.SetActive(true);
    }

    private void EnsureCorrectParenting()
    {
        if (effectParent != null)
        {
            if (dialogBackground != null && dialogBackground.transform.parent != effectParent)
            {
                dialogBackground.transform.SetParent(effectParent, true);
                dialogBackground.transform.localScale = Vector3.one;
                
                // 배경은 화면 전체를 덮도록 RectTransform 보정
                RectTransform bgRect = dialogBackground.GetComponent<RectTransform>();
                if (bgRect != null)
                {
                    bgRect.anchorMin = Vector2.zero;
                    bgRect.anchorMax = Vector2.one;
                    bgRect.anchoredPosition = Vector2.zero;
                    bgRect.sizeDelta = Vector2.zero;
                }
                Debug.Log("[DialogManager] dialogBackground 부모를 effectParent(Canvas)로 재설정하고 RectTransform을 보정했습니다.");
            }
            if (dialog != null && dialog.transform.parent != effectParent)
            {
                dialog.transform.SetParent(effectParent, true);
                dialog.transform.localScale = Vector3.one;
                
                // 텍스트 위치 보정 (이탈 시 중앙 배치)
                RectTransform textRect = dialog.GetComponent<RectTransform>();
                if (textRect != null)
                {
                    if (Mathf.Abs(textRect.anchoredPosition.x) > 500f || Mathf.Abs(textRect.anchoredPosition.y) > 300f)
                    {
                        textRect.anchoredPosition = Vector2.zero;
                    }
                }
                Debug.Log("[DialogManager] dialog 텍스트 부모를 effectParent(Canvas)로 재설정했습니다.");
            }
        }
    }

    // 외부에서 특정 대화 데이터를 주입하며 시작할 때 호출
    public void StartDialog(DialogData data)
    {
        EnsureCorrectParenting();

        // 기존에 스폰된 이펙트 잔재 정리
        ClearSpawnedEffects();

        Transform parent = effectParent != null ? effectParent : transform;

        if (data != null)
        {
            activeDialogText = data.dialogueLines;
            activeBgm = data.BossBgm;
            activeIntroSfx = data.BossTitleSfx;
            Debug.Log($"[DialogManager] StartDialog: 대화 데이터 주입됨 (BGM: {activeBgm?.name}). 대사 개수 = {activeDialogText?.Length}");
            
            // 데이터에 기입된 새로운 프리팹 연출 스폰
            if (data.AnimeEffect != null)
            {
                spawnedAnimeEffect = Instantiate(data.AnimeEffect, parent);
                RectTransform rect = spawnedAnimeEffect.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.localScale = Vector3.one;
                    rect.anchoredPosition = Vector2.zero;
                }
                spawnedAnimeEffect.SetActive(false);
                Debug.Log($"[DialogManager] AnimeEffect 스폰 완료 (RectTransform 초기화): {data.AnimeEffect.name} under parent: {parent.name}");
            }
            if (data.BossName != null)
            {
                spawnedBossName = Instantiate(data.BossName, parent);
                RectTransform rect = spawnedBossName.GetComponent<RectTransform>();
                if (rect != null)
                {
                    rect.localScale = Vector3.one;
                    rect.anchoredPosition = Vector2.zero;
                }
                spawnedBossName.SetActive(false);
                Debug.Log($"[DialogManager] BossName 스폰 완료 (RectTransform 초기화): {data.BossName.name} under parent: {parent.name}");
            }
        }
        else
        {
            activeDialogText = dialogText;
            activeBgm = null;
            activeIntroSfx = null;
            Debug.LogWarning("[DialogManager] 주입된 대화 데이터가 없어 기본 대화(dialogText)를 사용합니다.");
        }

        // 1. 대화창 게임 오브젝트 활성화 (OnEnable 실행 유도)
        gameObject.SetActive(true);

        // 2. 대화 시작 상태 초기화 (트리거 진입이므로 지연 없이 즉시 입력 활성화)
        index = 0;
        isDialogActive = true; 
        ShowDialog();

        // 3. 켜지자마자 첫 번째 대사가 바로 표시되도록 설정
        if (activeDialogText != null && activeDialogText.Length > 0)
        {
            dialog.text = ResolveLine(activeDialogText[0]);
            index = 1; // 0번 대사가 표시되었으므로 다음 대사는 index = 1
            Debug.Log($"[DialogManager] 첫 번째 대사 출력 완료: \"{dialog.text}\" (다음 index = {index})");
        }
        else
        {
            dialog.text = "";
            Debug.LogWarning("[DialogManager] activeDialogText가 비어있거나 없습니다.");
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log($"[DialogManager] 마우스 클릭 감지! isDialogActive={isDialogActive}, index={index}, activeDialogText.Length={(activeDialogText != null ? activeDialogText.Length.ToString() : "null")}");
        }

        if (Input.GetMouseButtonDown(0) && isDialogActive)
        {
            if (activeDialogText != null && index < activeDialogText.Length)
            {
                dialog.text = ResolveLine(activeDialogText[index]);
                Debug.Log($"[DialogManager] 다음 대사 출력: \"{dialog.text}\" (index={index})");
                index++;
            }
            else
            {
                isDialogActive = false;
                Debug.Log("[DialogManager] 모든 대화 완료. EndDialogSequence 시작.");
                EndDialogSequence().Forget();
            }
        }
    }

    private async UniTaskVoid EndDialogSequence()
    {
        dialog.gameObject.SetActive(false);
        await Effect();
        dialogBackground.gameObject.SetActive(false);
        
        // 연출 비활성화
        if (spawnedBossName != null) spawnedBossName.SetActive(false);
        else if (bossName != null) bossName.SetActive(false);

        if (spawnedAnimeEffect != null) spawnedAnimeEffect.SetActive(false);
        else if (animeEffect != null) animeEffect.SetActive(false);

        // 연출 정리
        ClearSpawnedEffects();

        // 15층 보스 특수 연출 (Hades 손 등장 및 시간 무한 설정)
        int currentDepth = 5;
        if (RunState.Instance != null)
        {
            currentDepth = RunState.Instance.CurrentDepth;
        }
        else if (MapGenerator.Instance != null)
        {
            currentDepth = MapGenerator.Instance.CurrentDepth;
        }

        if (currentDepth == 15 && cutsceneHandImage != null)
        {
            Debug.Log("[DialogManager] 15층 전용 보스 컷씬 연출 시작");
            cutsceneHandImage.gameObject.SetActive(true);
            cutsceneHandImage.anchoredPosition = new Vector2(cutsceneHandImage.anchoredPosition.x, handStartPosY);

            // 손 이미지가 아래로 부드럽게 이동
            await cutsceneHandImage.DOAnchorPosY(handTargetPosY, handMoveDuration)
                .SetEase(Ease.OutQuad)
                .AsyncWaitForCompletion();

            // 멈춘 후 대기
            await UniTask.Delay((int)(handWaitDuration * 1000), delayType: DelayType.Realtime);

            // 시간을 무한으로 설정
            if (TimeSystem.Instance != null)
            {
                TimeSystem.Instance.SetInfinite();
            }

            // 추가 인지 지연
            await UniTask.Delay(500, delayType: DelayType.Realtime);

            // 다시 올라감
            await cutsceneHandImage.DOAnchorPosY(handStartPosY, handMoveDuration)
                .SetEase(Ease.InQuad)
                .AsyncWaitForCompletion();

            cutsceneHandImage.gameObject.SetActive(false);
            Debug.Log("[DialogManager] 15층 전용 보스 컷씬 연출 완료");
        }

        OnDialogComplete?.Invoke();
        OnDialogComplete = null;
        
        // 대화 종료 후 오브젝트 비활성화
        gameObject.SetActive(false);
    }

    // dialogueLines 항목을 LocalizationManager 키로 보고 텍스트 조회 (매니저 없으면 원본 그대로)
    private string ResolveLine(string raw)
    {
        return LocalizationManager.Instance != null ? LocalizationManager.Instance.Get(raw) : raw;
    }

    private void ClearSpawnedEffects()
    {
        if (spawnedAnimeEffect != null)
        {
            Destroy(spawnedAnimeEffect);
            spawnedAnimeEffect = null;
        }
        if (spawnedBossName != null)
        {
            Destroy(spawnedBossName);
            spawnedBossName = null;
        }
    }

    public void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.loop = true;
        audioSource.playOnAwake = false;

        dialogBackground.gameObject.SetActive(false);
        dialog.gameObject.SetActive(false);
        if (animeEffect != null) animeEffect.SetActive(false);
        if (bossName != null) bossName.SetActive(false);
        if (cutsceneHandImage != null) cutsceneHandImage.gameObject.SetActive(false);
    }

    public void OnEnable()
    {
        EnsureCorrectParenting();

        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += RefreshCurrentLine;

        // StartDialog를 거치지 않고 직접 활성화되었을 때의 폴백 처리
        if (activeDialogText == null)
        {
            activeDialogText = dialogText;
            index = 0;
            isDialogActive = true;
            ShowDialog();

            if (activeDialogText != null && activeDialogText.Length > 0)
            {
                dialog.text = ResolveLine(activeDialogText[0]);
                index = 1;
            }
            else
            {
                dialog.text = "";
            }
        }
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= RefreshCurrentLine;

        activeDialogText = null;
    }

    // 언어 변경 시 현재 표시 중인 대사 다시 그리기
    private void RefreshCurrentLine()
    {
        if (!isDialogActive || activeDialogText == null || index <= 0 || index > activeDialogText.Length)
            return;

        dialog.text = ResolveLine(activeDialogText[index - 1]);
    }

    private async UniTask Effect()//연출 애니메이션
    {
        Debug.Log("[DialogManager] Effect 연출 애니메이션 시작");
        
        
        if (spawnedAnimeEffect != null) spawnedAnimeEffect.SetActive(true);
        else if (animeEffect != null) animeEffect.SetActive(true);

        // 일시 정지(Time.timeScale=0) 상태여도 연출이 진행될 수 있도록 Realtime 지연 처리
        await UniTask.Delay(1000, delayType: DelayType.Realtime);

        if (spawnedBossName != null) spawnedBossName.SetActive(true);
        else if (bossName != null) bossName.SetActive(true);

        PlayIntroSfx();
        if (titleSfxToBossBgmDelaySeconds > 0f)
            await UniTask.Delay((int)(titleSfxToBossBgmDelaySeconds * 1000f), delayType: DelayType.Realtime);

        PlayBossBgm();

        await UniTask.Delay(2000, delayType: DelayType.Realtime);
        
        Debug.Log("[DialogManager] Effect 연출 애니메이션 완료");
    }

    private void PlayIntroSfx()
    {
        if (activeIntroSfx == null)
            return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(activeIntroSfx);
            return;
        }

        if (audioSource != null)
            audioSource.PlayOneShot(activeIntroSfx);
    }

    private void PlayBossBgm()
    {
        if (activeBgm == null)
            return;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBgm(activeBgm, true);
            Debug.Log($"[DialogManager] BGM start via AudioManager: {activeBgm.name}");
            return;
        }

        if (audioSource != null)
        {
            audioSource.clip = activeBgm;
            audioSource.Play();
            Debug.Log($"[DialogManager] BGM start local: {activeBgm.name}");
        }
    }

    public void StopBgm()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Stop();
            audioSource.clip = null;
            Debug.Log("[DialogManager] BGM 정지");
        }
    }
}
