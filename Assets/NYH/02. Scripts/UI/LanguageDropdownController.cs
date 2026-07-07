using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PrototypeRT
{
    // 언어 선택 드롭다운. 선택을 LocalizationManager에 반영하고 PlayerPrefs에 저장한다.
    public class LanguageDropdownController : MonoBehaviour
    {
        private const string LanguagePrefsKey = "Localization.Language";

        [Tooltip("언어를 고르는 드롭다운. 옵션은 코드가 채운다.")]
        [SerializeField] private TMP_Dropdown dropdown;

        // ── 언어 추가 방법 ─────────────────────────────────────────────
        // 새 언어를 추가하려면:
        //   1) LocalizationManager.Language enum에 새 언어를 추가한다. (예: Japanese)
        //   2) 아래 Languages 배열과 Labels 배열에 "같은 순서"로 한 줄씩 추가한다. (미리 만들어 둔 주석 줄을 해제하면 됨)
        //   3) LocalizationManager의 texts 딕셔너리에 그 언어 번역을 채운다.
        // Languages와 Labels의 인덱스는 반드시 서로 일치해야 드롭다운 항목과 실제 언어가 맞는다.
        private static readonly LocalizationManager.Language[] Languages =
        {
            LocalizationManager.Language.Korean,
            LocalizationManager.Language.English,
            // LocalizationManager.Language.Japanese,   // ← 언어 추가 시 이 줄 주석 해제 (enum에도 먼저 추가)
        };

        // 드롭다운에 표시될 언어 이름. 언어 이름 자체는 보통 원어로 그대로 쓴다(한국어=한국어, 영어=English 등).
        // 위 Languages 배열과 "같은 순서"로 유지할 것.
        private static readonly string[] Labels =
        {
            "한국어",
            "English",
            // "日本語",   // ← 언어 추가 시 이 줄 주석 해제 (위 Languages와 같은 순서)
        };

        private void Awake()
        {
            if (dropdown == null)
                dropdown = GetComponent<TMP_Dropdown>();
        }

        private void Start()
        {
            LoadSavedLanguage();
            PopulateDropdown();

            if (dropdown != null)
                dropdown.onValueChanged.AddListener(OnDropdownChanged);
        }

        private void OnDestroy()
        {
            if (dropdown != null)
                dropdown.onValueChanged.RemoveListener(OnDropdownChanged);
        }

        // 저장된 언어를 불러와 즉시 반영한다. 없으면 현재 언어를 유지한다.
        private void LoadSavedLanguage()
        {
            if (LocalizationManager.Instance == null)
                return;

            int saved = PlayerPrefs.GetInt(LanguagePrefsKey, (int)LocalizationManager.Instance.currentLanguage);
            LocalizationManager.Instance.SetLanguage((LocalizationManager.Language)saved);
        }

        private void PopulateDropdown()
        {
            if (dropdown == null)
                return;

            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(Labels));
            dropdown.SetValueWithoutNotify(CurrentLanguageIndex());
            dropdown.RefreshShownValue();
        }

        private void OnDropdownChanged(int index)
        {
            if (index < 0 || index >= Languages.Length || LocalizationManager.Instance == null)
                return;

            LocalizationManager.Instance.SetLanguage(Languages[index]);
            PlayerPrefs.SetInt(LanguagePrefsKey, (int)Languages[index]);
            PlayerPrefs.Save();
        }

        private int CurrentLanguageIndex()
        {
            if (LocalizationManager.Instance == null)
                return 0;

            LocalizationManager.Language current = LocalizationManager.Instance.currentLanguage;
            for (int i = 0; i < Languages.Length; i++)
            {
                if (Languages[i] == current)
                    return i;
            }

            return 0;
        }
    }
}
