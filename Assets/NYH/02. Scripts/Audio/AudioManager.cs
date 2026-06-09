using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    private const string MasterPrefsKey = "Audio.MasterVolume";
    private const string BgmPrefsKey = "Audio.BGMVolume";
    private const string SfxPrefsKey = "Audio.SFXVolume";

    private const string MasterMixerParameter = "MasterVolume";
    private const string BgmMixerParameter = "BGMVolume";
    private const string SfxMixerParameter = "SFXVolume";

    private const float DefaultVolume = 1f;
    private const float MinSliderVolume = 0.0001f;
    private const float MutedDecibel = -80f;

    [Header("Audio Mixer")]
    [Tooltip("Master/BGM/SFX 볼륨 파라미터가 노출된 AudioMixer입니다.")]
    [SerializeField] private AudioMixer audioMixer;

    [Header("Volume Sliders")]
    [Tooltip("전체 볼륨을 조절하는 슬라이더입니다. 씬의 MasterVolum 오브젝트를 연결합니다.")]
    [SerializeField] private Slider masterVolumeSlider;

    [Tooltip("배경음 볼륨을 조절하는 슬라이더입니다. 씬의 BGMVolum 오브젝트를 연결합니다.")]
    [SerializeField] private Slider bgmVolumeSlider;

    [Tooltip("효과음 볼륨을 조절하는 슬라이더입니다. 씬의 SFXVolum 오브젝트를 연결합니다.")]
    [SerializeField] private Slider sfxVolumeSlider;

    private bool _isInitialized;

    private void Awake()
    {
        InitializeSlider(masterVolumeSlider, MasterPrefsKey, MasterMixerParameter);
        InitializeSlider(bgmVolumeSlider, BgmPrefsKey, BgmMixerParameter);
        InitializeSlider(sfxVolumeSlider, SfxPrefsKey, SfxMixerParameter);

        _isInitialized = true;
    }

    private void OnEnable()
    {
        SubscribeSliders();
    }

    private void OnDisable()
    {
        UnsubscribeSliders();
    }

    public void SetMasterVolume(float volume)
    {
        SetVolume(MasterPrefsKey, MasterMixerParameter, volume);
    }

    public void SetBgmVolume(float volume)
    {
        SetVolume(BgmPrefsKey, BgmMixerParameter, volume);
    }

    public void SetSfxVolume(float volume)
    {
        SetVolume(SfxPrefsKey, SfxMixerParameter, volume);
    }

    private void SubscribeSliders()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);

        if (bgmVolumeSlider != null)
            bgmVolumeSlider.onValueChanged.AddListener(SetBgmVolume);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(SetSfxVolume);
    }

    private void UnsubscribeSliders()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.RemoveListener(SetMasterVolume);

        if (bgmVolumeSlider != null)
            bgmVolumeSlider.onValueChanged.RemoveListener(SetBgmVolume);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.RemoveListener(SetSfxVolume);
    }

    private void InitializeSlider(Slider slider, string prefsKey, string mixerParameter)
    {
        float savedVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(prefsKey, DefaultVolume));

        if (slider != null)
        {
            slider.minValue = 0f;
            slider.maxValue = 1f;

            // SetValueWithoutNotify를 써야 초기화 중 onValueChanged가 중복 호출되지 않습니다.
            slider.SetValueWithoutNotify(savedVolume);
        }

        ApplyMixerVolume(mixerParameter, savedVolume);
    }

    private void SetVolume(string prefsKey, string mixerParameter, float volume)
    {
        float clampedVolume = Mathf.Clamp01(volume);

        ApplyMixerVolume(mixerParameter, clampedVolume);

        // 슬라이더를 움직일 때마다 저장해서 설정창을 닫거나 씬이 바뀌어도 값이 보존되게 합니다.
        PlayerPrefs.SetFloat(prefsKey, clampedVolume);
        PlayerPrefs.Save();
    }

    private void ApplyMixerVolume(string mixerParameter, float volume)
    {
        if (audioMixer == null)
        {
            if (_isInitialized)
                Debug.LogWarning("AudioManager: AudioMixer가 연결되지 않아 볼륨을 적용할 수 없습니다.");
            return;
        }

        float decibel = ConvertSliderValueToDecibel(volume);
        if (!audioMixer.SetFloat(mixerParameter, decibel))
            Debug.LogWarning($"AudioManager: AudioMixer에 '{mixerParameter}' 노출 파라미터가 없습니다.");
    }

    private static float ConvertSliderValueToDecibel(float volume)
    {
        // AudioMixer는 선형 0~1 값이 아니라 dB 값을 사용합니다.
        // 사람이 듣는 음량 변화가 자연스럽게 느껴지도록 로그 변환을 적용합니다.
        if (volume <= 0f)
            return MutedDecibel;

        // Mathf.Log10(0)은 음의 무한대가 되므로, 0에 가까운 값은 최소값으로 보정합니다.
        float safeVolume = Mathf.Max(volume, MinSliderVolume);
        return Mathf.Log10(safeVolume) * 20f;
    }
}