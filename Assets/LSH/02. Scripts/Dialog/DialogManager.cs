using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class DialogManager : MonoBehaviour
{
    [Header("검은 배경")]
    [SerializeField] Image dialogBackground;
    [Header("텍스트 입력")]
    [SerializeField] string[] dialogText;
    [Header("택스트 보여주는 곳")]
    [SerializeField] Text dialog;
    [Header("연출에니메이션")]
    [SerializeField] GameObject animeEffect;
    [SerializeField] GameObject bossName;
    int index = 0;  
    bool isDialogActive = false;

    public System.Action OnDialogComplete;

    public void ShowDialog()
    {
        dialogBackground.gameObject.SetActive(true);
        dialog.gameObject.SetActive(true);
    }

    // Unity의 생명주기 메서드인 Update는 void 반환타입을 가져야 매 프레임 정상 호출됩니다.
    private void Update()
    {
        if (Input.GetMouseButtonDown(0) && isDialogActive)
        {
            if (index < dialogText.Length)
            {
                dialog.text = dialogText[index];
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
        bossName.SetActive(false);
        animeEffect.SetActive(false);

        OnDialogComplete?.Invoke();
        OnDialogComplete = null;
    }

    public void Awake()
    {
        dialogBackground.gameObject.SetActive(false);
        dialog.gameObject.SetActive(false);
        animeEffect.SetActive(false);
        bossName.SetActive(false);
        gameObject.SetActive(false);
    }

    public void OnEnable()
    {
        index = 0; // 대화 시작 시 index 초기화
        isDialogActive = true;
        ShowDialog();

        // 켜지자마자 첫 번째 대사가 바로 표시되도록 설정
        if (dialogText != null && dialogText.Length > 0)
        {
            dialog.text = dialogText[0];
            index = 1;
        }
        else
        {
            dialog.text = "";
        }
    }

    private async UniTask Effect()//연출 애니메이션
    {
        animeEffect.SetActive(true);
        await UniTask.Delay(1000);
        bossName.SetActive(true);
        await UniTask.Delay(2000);
    }
}
