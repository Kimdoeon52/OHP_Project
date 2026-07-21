using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace PrototypeRT
{
    // 해상도와 화면 모드(전체화면/테두리없는 창/창)를 설정하고 PlayerPrefs에 저장한다.
    public class VideoSettingsController : MonoBehaviour
    {
        private const string ResWidthKey = "Video.ResWidth";
        private const string ResHeightKey = "Video.ResHeight";
        private const string ScreenModeKey = "Video.ScreenMode";

        [Tooltip("해상도 선택 드롭다운. 옵션은 코드가 채운다.")]
        [SerializeField] private TMP_Dropdown resolutionDropdown;

        [Tooltip("화면 모드 드롭다운(전체화면/테두리없는 창/창). 옵션은 코드가 채운다.")]
        [SerializeField] private TMP_Dropdown screenModeDropdown;

        [Tooltip("켜면 16:9 비율 해상도만 목록에 표시한다. 다른 비율에서 생기는 화면 여백/잘림을 막는다.")]
        [SerializeField] private bool only16By9 = true;

        // 중복 없는 해상도 목록(너비 x 높이)
        private readonly List<Vector2Int> _resolutions = new();

        // 16:9 비율 판정용 (오차 허용)
        private const float Aspect16By9 = 16f / 9f;
        private const float AspectTolerance = 0.02f;

        // 드롭다운 순서와 맞춘 화면 모드
        private static readonly FullScreenMode[] ScreenModes =
        {
            FullScreenMode.ExclusiveFullScreen, // 전체화면
            FullScreenMode.FullScreenWindow,    // 테두리 없는 창(화면 가득)
            FullScreenMode.Windowed             // 창
        };

        // 화면 모드 드롭다운 라벨의 LocalizationManager 키. ScreenModes 배열과 "같은 순서(인덱스)"여야 한다.
        private static readonly string[] ScreenModeKeys = { "msg_전체화면", "msg_테두리없는창", "msg_창모드" };

        // Alt+Enter 등 외부 요인으로 화면 모드/해상도가 바뀌었는지 감지하기 위한 마지막 확인값.
        private FullScreenMode _lastKnownMode;
        private int _lastKnownWidth;
        private int _lastKnownHeight;
        private float _externalChangeCheckTimer;

        private const float ExternalChangeCheckInterval = 0.25f;

        private void Start()
        {
            BuildResolutionList();
            PopulateResolutionDropdown();
            PopulateScreenModeDropdown();
            ApplySaved();

            if (resolutionDropdown != null)
                resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            if (screenModeDropdown != null)
                screenModeDropdown.onValueChanged.AddListener(OnScreenModeChanged);
            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLanguageChanged += RefreshScreenModeLabels;
        }

        private void OnEnable()
        {
            // 시스템창을 닫아뒀다 다시 켰을 때, 그 사이 Alt+Enter 등으로 바뀐 실제 화면 상태를 반영한다.
            if (_resolutions.Count > 0)
                SyncDropdownsFromCurrentScreen();
        }

        private void OnDestroy()
        {
            if (resolutionDropdown != null)
                resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);
            if (screenModeDropdown != null)
                screenModeDropdown.onValueChanged.RemoveListener(OnScreenModeChanged);
            if (LocalizationManager.Instance != null)
                LocalizationManager.Instance.OnLanguageChanged -= RefreshScreenModeLabels;
        }

        // Alt+Enter, 창 드래그 등 우리 코드를 거치지 않은 화면 변경을 주기적으로 감지해 드롭다운에 반영한다.
        private void Update()
        {
            _externalChangeCheckTimer += Time.unscaledDeltaTime;
            if (_externalChangeCheckTimer < ExternalChangeCheckInterval)
                return;

            _externalChangeCheckTimer = 0f;

            if (Screen.fullScreenMode != _lastKnownMode || Screen.width != _lastKnownWidth || Screen.height != _lastKnownHeight)
                SyncDropdownsFromCurrentScreen();
        }

        // 실제 Screen 상태를 기준으로 드롭다운 표시만 맞춘다. ApplySettings를 다시 부르지 않아 무한 루프를 막는다.
        private void SyncDropdownsFromCurrentScreen()
        {
            _lastKnownMode = Screen.fullScreenMode;
            _lastKnownWidth = Screen.width;
            _lastKnownHeight = Screen.height;

            int resIndex = FindResolutionIndex(Screen.width, Screen.height);
            int modeIndex = FindScreenModeIndex(Screen.fullScreenMode);

            if (resolutionDropdown != null)
            {
                resolutionDropdown.SetValueWithoutNotify(resIndex);
                resolutionDropdown.RefreshShownValue();
            }

            if (screenModeDropdown != null)
            {
                screenModeDropdown.SetValueWithoutNotify(modeIndex);
                screenModeDropdown.RefreshShownValue();
            }

            Save();
        }

        private void BuildResolutionList()
        {
            _resolutions.Clear();
            HashSet<long> seen = new HashSet<long>();
            foreach (Resolution res in Screen.resolutions)
            {
                // 16:9만 남겨 다른 비율에서 생기는 화면 여백/잘림을 막는다.
                if (only16By9 && Mathf.Abs((float)res.width / res.height - Aspect16By9) > AspectTolerance)
                    continue;

                // 같은 해상도가 주사율만 다르게 여러 개 들어오므로 너비x높이로 중복을 제거한다.
                long key = ((long)res.width << 32) | (uint)res.height;
                if (!seen.Add(key))
                    continue;

                _resolutions.Add(new Vector2Int(res.width, res.height));
            }

            // 지원 목록이 비면(모니터에 16:9가 없거나 필터가 다 걸러낸 경우) 최소한 현재 해상도라도 넣는다.
            if (_resolutions.Count == 0)
                _resolutions.Add(new Vector2Int(Screen.width, Screen.height));
        }

        private void PopulateResolutionDropdown()
        {
            if (resolutionDropdown == null)
                return;

            List<string> labels = new List<string>();
            foreach (Vector2Int r in _resolutions)
                labels.Add($"{r.x} x {r.y}");

            resolutionDropdown.ClearOptions();
            resolutionDropdown.AddOptions(labels);
        }

        private void PopulateScreenModeDropdown()
        {
            if (screenModeDropdown == null)
                return;

            screenModeDropdown.ClearOptions();
            screenModeDropdown.AddOptions(BuildScreenModeLabels());
        }

        // 언어가 바뀌면 선택값은 유지한 채 옵션 텍스트만 새로 채운다.
        private void RefreshScreenModeLabels()
        {
            if (screenModeDropdown == null)
                return;

            int current = screenModeDropdown.value;
            screenModeDropdown.ClearOptions();
            screenModeDropdown.AddOptions(BuildScreenModeLabels());
            screenModeDropdown.SetValueWithoutNotify(current);
            screenModeDropdown.RefreshShownValue();
        }

        private List<string> BuildScreenModeLabels()
        {
            List<string> labels = new List<string>();
            foreach (string key in ScreenModeKeys)
                labels.Add(LocalizationManager.GetOrDefault(key, key));
            return labels;
        }

        private void ApplySaved()
        {
            int width = PlayerPrefs.GetInt(ResWidthKey, Screen.width);
            int height = PlayerPrefs.GetInt(ResHeightKey, Screen.height);
            FullScreenMode mode = (FullScreenMode)PlayerPrefs.GetInt(ScreenModeKey, (int)Screen.fullScreenMode);

            int resIndex = FindResolutionIndex(width, height);
            int modeIndex = FindScreenModeIndex(mode);

            if (resolutionDropdown != null)
                resolutionDropdown.SetValueWithoutNotify(resIndex);
            if (screenModeDropdown != null)
                screenModeDropdown.SetValueWithoutNotify(modeIndex);

            ApplySettings(resIndex, modeIndex);
        }

        private void OnResolutionChanged(int index)
        {
            ApplySettings(index, CurrentModeIndex());
            Save();
        }

        private void OnScreenModeChanged(int index)
        {
            ApplySettings(CurrentResIndex(), index);
            Save();
        }

        private void ApplySettings(int resIndex, int modeIndex)
        {
            if (resIndex < 0 || resIndex >= _resolutions.Count)
                return;

            if (modeIndex < 0 || modeIndex >= ScreenModes.Length)
                modeIndex = 0;

            Vector2Int res = _resolutions[resIndex];
            FullScreenMode mode = ScreenModes[modeIndex];

            // 창모드 + 모니터 해상도와 완전히 같은 해상도 조합은 타이틀바 높이만큼 창이 화면보다 커져
            // 아래쪽이 화면 밖으로 밀려난다. 테두리 없는 창모드로 자동 전환해 화면에 정확히 맞춘다.
            if (mode == FullScreenMode.Windowed
                && res.x == Screen.currentResolution.width
                && res.y == Screen.currentResolution.height)
                mode = FullScreenMode.FullScreenWindow;

            Screen.SetResolution(res.x, res.y, mode);
            StartCoroutine(ForceTmpRedrawNextFrame());
        }

        // 화면 모드 전환(디바이스 재생성) 직후 TMP 텍스트가 다시 그려지지 않는 빌드 전용 버그 대응.
        private IEnumerator ForceTmpRedrawNextFrame()
        {
            yield return null;
            TMP_Text[] texts = FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
            foreach (TMP_Text t in texts)
                t.SetAllDirty();
        }

        private void Save()
        {
            int resIndex = CurrentResIndex();
            if (resIndex >= 0 && resIndex < _resolutions.Count)
            {
                PlayerPrefs.SetInt(ResWidthKey, _resolutions[resIndex].x);
                PlayerPrefs.SetInt(ResHeightKey, _resolutions[resIndex].y);
            }

            int modeIndex = Mathf.Clamp(CurrentModeIndex(), 0, ScreenModes.Length - 1);
            PlayerPrefs.SetInt(ScreenModeKey, (int)ScreenModes[modeIndex]);
            PlayerPrefs.Save();
        }

        private int CurrentResIndex()
        {
            return resolutionDropdown != null ? resolutionDropdown.value : 0;
        }

        private int CurrentModeIndex()
        {
            return screenModeDropdown != null ? screenModeDropdown.value : 0;
        }

        private int FindResolutionIndex(int width, int height)
        {
            for (int i = 0; i < _resolutions.Count; i++)
            {
                if (_resolutions[i].x == width && _resolutions[i].y == height)
                    return i;
            }

            // 저장값이 목록에 없으면 가장 큰(마지막) 해상도를 기본으로 쓴다.
            return _resolutions.Count - 1;
        }

        private int FindScreenModeIndex(FullScreenMode mode)
        {
            for (int i = 0; i < ScreenModes.Length; i++)
            {
                if (ScreenModes[i] == mode)
                    return i;
            }

            return 0;
        }
    }
}
