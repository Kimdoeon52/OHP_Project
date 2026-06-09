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

    [Header("BGM Playback")]
    [Tooltip("배경음악을 재생할 AudioSource입니다. 비워두면 같은 오브젝트에서 찾고, 없으면 자동으로 추가합니다.")]
    [SerializeField] private AudioSource bgmSource;

    [Tooltip("BGM AudioSource의 Output에 연결할 AudioMixerGroup입니다. AudioMixer의 BGM 그룹을 연결하면 BGM 슬라이더가 이 소리를 조절합니다.")]
    [SerializeField] private AudioMixerGroup bgmOutputGroup;

    [Tooltip("게임 시작 시 자동으로 재생할 기본 배경음악 클립입니다.")]
    [SerializeField] private AudioClip startBgmClip;

    [Tooltip("게임 시작 시 startBgmClip을 자동 재생할지 정합니다.")]
    [SerializeField] private bool playBgmOnStart = true;

    [Tooltip("시작 배경음악을 반복 재생할지 정합니다.")]
    [SerializeField] private bool loopStartBgm = true;

    private bool _isInitialized;

    private void Awake()
    {
        ResolveBgmSource();
        InitializeSlider(masterVolumeSlider, MasterPrefsKey, MasterMixerParameter);
        InitializeSlider(bgmVolumeSlider, BgmPrefsKey, BgmMixerParameter);
        InitializeSlider(sfxVolumeSlider, SfxPrefsKey, SfxMixerParameter);

        _isInitialized = true;
    }

    private void Start()
    {
        if (playBgmOnStart && startBgmClip != null)
            PlayBgm(startBgmClip, loopStartBgm);
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

    public void PlayBgm(AudioClip clip)
    {
        PlayBgm(clip, true);
    }

    public void PlayBgm(AudioClip clip, bool loop)
    {
        if (clip == null)
        {
            Debug.LogWarning("AudioManager: 재생할 BGM AudioClip이 비어 있습니다.");
            return;
        }

        ResolveBgmSource();
        if (bgmSource == null)
            return;

        // 같은 곡이 이미 재생 중이면 다시 처음부터 틀지 않습니다.
        // 장면 UI를 다시 열거나 설정값이 갱신될 때 음악이 튀는 것을 막기 위해서입니다.
        if (bgmSource.clip == clip && bgmSource.isPlaying)
        {
            bgmSource.loop = loop;
            return;
        }

        bgmSource.clip = clip;
        bgmSource.loop = loop;
        bgmSource.Play();
    }

    public void StopBgm()
    {
        if (bgmSource == null)
            return;

        bgmSource.Stop();
    }

    private void ResolveBgmSource()
    {
        if (bgmSource == null)
            bgmSource = GetComponent<AudioSource>();

        if (bgmSource == null)
            bgmSource = gameObject.AddComponent<AudioSource>();

        // BGM은 효과음과 다른 Mixer Group으로 보내야 BGM 슬라이더만으로 따로 줄일 수 있습니다.
        if (bgmOutputGroup != null)
            bgmSource.outputAudioMixerGroup = bgmOutputGroup;

        bgmSource.playOnAwake = false;
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
