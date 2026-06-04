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

    int index = 0;  
    public void ShowDialog()
    {
        dialogBackground.gameObject.SetActive(true);
        dialog.gameObject.SetActive(true);
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0))
        {
            if (index < dialogText.Length)
            {
                dialog.text = dialogText[index];
                index++;
            }
            else
            {
                dialogBackground.gameObject.SetActive(false);
                dialog.gameObject.SetActive(false);
            }
        }
    }
    public void Start()
    {
        ShowDialog();
    }
}
