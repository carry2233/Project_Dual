using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 설정창 관리 스크립트
/// - 현재는 소리 볼륨 설정만 담당
/// - 설정창 활성화 중 월드맵 이동/타일 선택 잠금
/// - 설정창 닫기 시 전역 소리 설정 JSON 저장
/// </summary>
public class SettingManager : MonoBehaviour
{
    [Header("볼륨 설정 슬라이더")]
    [SerializeField] private Slider masterVolumeSlider; // 전체 볼륨
    [SerializeField] private Slider effectVolumeSlider; // 효과음 볼륨
    [SerializeField] private Slider voiceVolumeSlider; // 음성 볼륨
    [SerializeField] private Slider bgmVolumeSlider; // BGM 볼륨
    [SerializeField] private Slider uiSoundVolumeSlider; // UI 사운드 볼륨

    [Header("설정창 버튼")]
    [SerializeField] private Button settingOpenButton; // 설정창 활성화 버튼
    [SerializeField] private Button settingCloseButton; // 설정창 비활성화 버튼

    [Header("설정창 패널")]
    [SerializeField] private GameObject settingPanel; // 설정창 패널

    [Header("잠금 대상")]
    [SerializeField] private TileSelectionManager tileSelectionManager; // 타일 선택 잠금 대상
    [SerializeField] private WorldMapCameraController worldMapCameraController; // 월드맵 이동 잠금 대상

    private SaveStorage saveStorage; // 저장 관리자 참조

    private void Awake()
    {
        saveStorage = SaveStorage.Instance;

        if (tileSelectionManager == null)
        {
            tileSelectionManager = FindFirstObjectByType<TileSelectionManager>();
        }

        if (worldMapCameraController == null)
        {
            worldMapCameraController = FindFirstObjectByType<WorldMapCameraController>();
        }

        if (settingPanel != null)
        {
            settingPanel.SetActive(false); // 씬 시작 시 설정창 비활성화
        }

        if (settingOpenButton != null)
        {
            settingOpenButton.onClick.AddListener(OpenSettingPanel);
        }

        if (settingCloseButton != null)
        {
            settingCloseButton.onClick.AddListener(CloseSettingPanel);
        }
    }

    private void OnDestroy()
    {
        if (settingOpenButton != null)
        {
            settingOpenButton.onClick.RemoveListener(OpenSettingPanel);
        }

        if (settingCloseButton != null)
        {
            settingCloseButton.onClick.RemoveListener(CloseSettingPanel);
        }
    }

    public void OpenSettingPanel()
    {
        if (saveStorage == null)
        {
            saveStorage = SaveStorage.Instance;
        }

        if (saveStorage != null)
        {
            saveStorage.LoadGlobalSoundVolumeSaveData(); // 전역 소리 설정 JSON 불러오기
            ApplySoundDataToSliders(saveStorage.CurrentSoundVolumeSaveData); // 저장값을 슬라이더에 반영
        }

        if (settingPanel != null)
        {
            settingPanel.SetActive(true);
        }

        SetWorldInputLocked(true);
    }

    public void CloseSettingPanel()
    {
        if (saveStorage == null)
        {
            saveStorage = SaveStorage.Instance;
        }

        if (saveStorage != null)
        {
            SaveStorage.SoundVolumeSaveData soundData = CreateSoundDataFromSliders();
            saveStorage.ApplyCurrentSoundVolumeSaveData(soundData);
            saveStorage.SaveGlobalSoundVolumeSaveData(soundData); // 전역 소리 설정 JSON 저장
        }

        if (settingPanel != null)
        {
            settingPanel.SetActive(false);
        }

        SetWorldInputLocked(false);
    }

    private void ApplySoundDataToSliders(SaveStorage.SoundVolumeSaveData soundData)
    {
        if (soundData == null)
        {
            soundData = new SaveStorage.SoundVolumeSaveData();
        }

        SetSliderValue(masterVolumeSlider, soundData.masterVolume);
        SetSliderValue(effectVolumeSlider, soundData.effectVolume);
        SetSliderValue(voiceVolumeSlider, soundData.voiceVolume);
        SetSliderValue(bgmVolumeSlider, soundData.bgmVolume);
        SetSliderValue(uiSoundVolumeSlider, soundData.uiSoundVolume);
    }

    private SaveStorage.SoundVolumeSaveData CreateSoundDataFromSliders()
    {
        SaveStorage.SoundVolumeSaveData soundData = new SaveStorage.SoundVolumeSaveData();

        soundData.masterVolume = GetSliderIntValue(masterVolumeSlider);
        soundData.effectVolume = GetSliderIntValue(effectVolumeSlider);
        soundData.voiceVolume = GetSliderIntValue(voiceVolumeSlider);
        soundData.bgmVolume = GetSliderIntValue(bgmVolumeSlider);
        soundData.uiSoundVolume = GetSliderIntValue(uiSoundVolumeSlider);

        return soundData;
    }

private void SetSliderValue(Slider slider, int value)
{
    if (slider == null)
    {
        return;
    }

    slider.value = Mathf.Clamp01(value / 100f);
}

private int GetSliderIntValue(Slider slider)
{
    if (slider == null)
    {
        return 100;
    }

    return Mathf.Clamp(Mathf.RoundToInt(slider.value * 100f), 0, 100);
}

    private void SetWorldInputLocked(bool isLocked)
    {
        if (tileSelectionManager != null)
        {
            tileSelectionManager.SetSettingUIOpen(isLocked);
        }
        else if (worldMapCameraController != null)
        {
            worldMapCameraController.SetMovementLock(isLocked);
        }
    }










}