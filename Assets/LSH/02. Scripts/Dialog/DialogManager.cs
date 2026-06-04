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

    // Update is called once per frame
    private async UniTask Update()
    {
        if(Input.GetMouseButtonDown(0)&&isDialogActive)
        {
            if (index < dialogText.Length)
            {
                dialog.text = dialogText[index];
                index++;
            }
            else
            {
                dialog.gameObject.SetActive(false);
                await Effect();
                dialogBackground.gameObject.SetActive(false);
                bossName.SetActive(false);
                animeEffect.SetActive(false);
                isDialogActive = false;

                OnDialogComplete?.Invoke();
                OnDialogComplete = null;
            }
        }
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
        isDialogActive = true;
        ShowDialog();
    }
    private async UniTask Effect()//연출 애니메이션
    {
        animeEffect.SetActive(true);
        await UniTask.Delay(1000);
        bossName.SetActive(true);
        await UniTask.Delay(2000);
    }
}
