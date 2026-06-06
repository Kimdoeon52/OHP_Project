using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

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

    private string[] activeDialogText;
    private int index = 0;  
    private bool isDialogActive = false;

    // 동적으로 스폰된 연출 오브젝트들
    private GameObject spawnedAnimeEffect;
    private GameObject spawnedBossName;

    public System.Action OnDialogComplete;

    public void ShowDialog()
    {
        dialogBackground.gameObject.SetActive(true);
        dialog.gameObject.SetActive(true);
    }

    // 외부에서 특정 대화 데이터를 주입하며 시작할 때 호출
    public void StartDialog(DialogData data)
    {
        // 기존에 스폰된 이펙트 잔재 정리
        ClearSpawnedEffects();

        if (data != null)
        {
            activeDialogText = data.dialogueLines;
            
            // 데이터에 기입된 새로운 프리팹 연출 스폰
            if (data.AnimeEffect != null)
            {
                spawnedAnimeEffect = Instantiate(data.AnimeEffect, transform);
                spawnedAnimeEffect.SetActive(false);
            }
            if (data.BossName != null)
            {
                spawnedBossName = Instantiate(data.BossName, transform);
                spawnedBossName.SetActive(false);
            }
        }
        else
        {
            activeDialogText = dialogText;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && isDialogActive)
        {
            if (activeDialogText != null && index < activeDialogText.Length)
            {
                dialog.text = activeDialogText[index];
                index++;
            }
            else
            {
                isDialogActive = false;
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

        OnDialogComplete?.Invoke();
        OnDialogComplete = null;
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
        dialogBackground.gameObject.SetActive(false);
        dialog.gameObject.SetActive(false);
        if (animeEffect != null) animeEffect.SetActive(false);
        if (bossName != null) bossName.SetActive(false);
    }

    public void OnEnable()
    {
        index = 0; // 대화 시작 시 index 초기화
        isDialogActive = false; // 마우스 입력 프레임 스킵 가드를 위해 처음에는 false로 설정
        ShowDialog();

        // 만약 StartDialog가 아니라 단순 SetActive(true)로 켜졌을 때의 폴백 처리
        if (activeDialogText == null)
        {
            activeDialogText = dialogText;
        }

        ////켜지자마자 첫 번째 대사가 바로 표시되도록 설정
        //if (activeDialogText != null && activeDialogText.Length > 0)
        //{
        //    dialog.text = activeDialogText[0];
        //    index = 1;
        //}
        //else
        //{
        //    dialog.text = "";
        //}

        EnableDialogInputAsync().Forget();
    }

    private void OnDisable()
    {
        activeDialogText = null;
    }

    // 활성화된 프레임에서의 즉시 클릭을 막기 위해 한 프레임 지연 후 입력 활성화
    private async UniTask EnableDialogInputAsync()
    {
        await UniTask.Yield();
        isDialogActive = true;
    }

    private async UniTask Effect()//연출 애니메이션
    {
        if (spawnedAnimeEffect != null) spawnedAnimeEffect.SetActive(true);
        else if (animeEffect != null) animeEffect.SetActive(true);

        await UniTask.Delay(1000);

        if (spawnedBossName != null) spawnedBossName.SetActive(true);
        else if (bossName != null) bossName.SetActive(true);

        await UniTask.Delay(2000);
    }
}
