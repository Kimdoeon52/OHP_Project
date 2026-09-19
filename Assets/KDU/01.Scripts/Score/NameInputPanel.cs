using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 이름 입력 — 시작 시 이름이 없으면 언어에 맞춰 자동 생성, 설정에서 수정
// 별도 창으로 쓸 때만 panelRoot 연결 (설정 화면에 바로 넣었다면 비워둠)
public class NameInputPanel : MonoBehaviour
{
    [Tooltip("입력 창 루트")]
    [SerializeField] private GameObject panelRoot;

    [Tooltip("이름 입력 필드")]
    [SerializeField] private TMP_InputField nameField;

    [Tooltip("확인")]
    [SerializeField] private Button confirmButton;

    [Tooltip("취소")]
    [SerializeField] private Button cancelButton;

    [Tooltip("입력 규칙 안내")]
    [SerializeField] private TMP_Text hintText;

    private void Awake()
    {
        if (nameField != null)
        {
            nameField.characterLimit = PlayerProfile.MaxLength;
            nameField.lineType = TMP_InputField.LineType.SingleLine;
            nameField.onValueChanged.AddListener(OnValueChanged);
        }

        if (confirmButton != null)
            confirmButton.onClick.AddListener(Confirm);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(Cancel);

        // 별도 창으로 쓸 때만 닫아둠 — 설정 패널에 직접 붙였으면 상위 메뉴가 껐다 켬
        if (panelRoot != null && panelRoot != gameObject)
            panelRoot.SetActive(false);
    }

    // 켜질 때마다 현재 이름 표시 — 없으면 언어에 맞춰 자동 생성
    private void OnEnable()
    {
        Refresh();
    }

    // 입력 창 열기 — 별도 창으로 쓸 때만 필요
    public void Open()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        Refresh();
    }

    // 별도 창으로 쓸 때만 닫음 — 설정 패널에 직접 붙였으면 상위 메뉴가 전환
    public void Close()
    {
        if (panelRoot != null && panelRoot != gameObject)
            panelRoot.SetActive(false);
    }

    // 저장 후 닫기. 빈 이름이면 무시
    public void Confirm()
    {
        if (nameField == null || !PlayerProfile.Save(nameField.text))
            return;

        Close();
    }

    // 저장하지 않고 되돌림
    public void Cancel()
    {
        Refresh();
        Close();
    }

    // 현재 이름·안내 문구 표시
    private void Refresh()
    {
        if (hintText != null)
            hintText.text = LocalizationManager.GetOrDefault("profile.name_hint", $"한글·영문·숫자 {PlayerProfile.MaxLength}자까지");

        if (nameField != null)
            nameField.SetTextWithoutNotify(PlayerProfile.EnsureName());

        RefreshConfirm();
    }

    // 허용 밖 문자는 즉시 제거
    private void OnValueChanged(string value)
    {
        string clean = PlayerProfile.Sanitize(value);
        if (clean != value && nameField != null)
            nameField.SetTextWithoutNotify(clean);

        RefreshConfirm();
    }

    private void RefreshConfirm()
    {
        if (confirmButton == null)
            return;

        confirmButton.interactable = nameField != null && !string.IsNullOrEmpty(PlayerProfile.Sanitize(nameField.text));
    }
}
