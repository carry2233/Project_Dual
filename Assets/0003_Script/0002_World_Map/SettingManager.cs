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

        RegisterVolumeSliderEvents(); // 볼륨 슬라이더 실시간 변경 이벤트 연결
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

        UnregisterVolumeSliderEvents(); // 볼륨 슬라이더 실시간 변경 이벤트 해제
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
        SaveStorage.SoundVolumeSaveData soundData = CreateSoundDataFromSliders(); // 현재 슬라이더 값 생성
        saveStorage.ApplyCurrentSoundVolumeSaveData(soundData); // SaveStorage 인스펙터 표시값 갱신
        saveStorage.SaveGlobalSoundVolumeSaveData(soundData); // 닫기 버튼 클릭 시에만 전역 소리 설정 JSON 저장
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

    if (saveStorage != null)
    {
        saveStorage.ApplyCurrentSoundVolumeSaveData(soundData); // 슬라이더 초기 반영 시 SaveStorage 인스펙터 값도 갱신
    }
}

private SaveStorage.SoundVolumeSaveData CreateSoundDataFromSliders()
{
    SaveStorage.SoundVolumeSaveData soundData = saveStorage != null
        ? saveStorage.CurrentSoundVolumeSaveData
        : new SaveStorage.SoundVolumeSaveData(); // 기존 현재값을 기준으로 생성

    if (masterVolumeSlider != null)
    {
        soundData.masterVolume = GetSliderIntValue(masterVolumeSlider); // 전체 볼륨 반영
    }

    if (effectVolumeSlider != null)
    {
        soundData.effectVolume = GetSliderIntValue(effectVolumeSlider); // 효과음 볼륨 반영
    }

    if (voiceVolumeSlider != null)
    {
        soundData.voiceVolume = GetSliderIntValue(voiceVolumeSlider); // 음성 볼륨 반영
    }

    if (bgmVolumeSlider != null)
    {
        soundData.bgmVolume = GetSliderIntValue(bgmVolumeSlider); // BGM 볼륨 반영
    }

    if (uiSoundVolumeSlider != null)
    {
        soundData.uiSoundVolume = GetSliderIntValue(uiSoundVolumeSlider); // UI 사운드 볼륨 반영
    }

    return soundData;
}

private void SetSliderValue(Slider slider, int value)
{
    if (slider == null)
    {
        return;
    }

    slider.SetValueWithoutNotify(Mathf.Clamp01(value / 100f)); // 초기값 반영 중 이벤트 중복 호출 방지
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

private void RegisterVolumeSliderEvents() // 볼륨 슬라이더 이벤트 연결
{
    if (masterVolumeSlider != null)
    {
        masterVolumeSlider.onValueChanged.AddListener(OnVolumeSliderValueChanged);
    }

    if (effectVolumeSlider != null)
    {
        effectVolumeSlider.onValueChanged.AddListener(OnVolumeSliderValueChanged);
    }

    if (voiceVolumeSlider != null)
    {
        voiceVolumeSlider.onValueChanged.AddListener(OnVolumeSliderValueChanged);
    }

    if (bgmVolumeSlider != null)
    {
        bgmVolumeSlider.onValueChanged.AddListener(OnVolumeSliderValueChanged);
    }

    if (uiSoundVolumeSlider != null)
    {
        uiSoundVolumeSlider.onValueChanged.AddListener(OnVolumeSliderValueChanged);
    }
}

private void UnregisterVolumeSliderEvents() // 볼륨 슬라이더 이벤트 해제
{
    if (masterVolumeSlider != null)
    {
        masterVolumeSlider.onValueChanged.RemoveListener(OnVolumeSliderValueChanged);
    }

    if (effectVolumeSlider != null)
    {
        effectVolumeSlider.onValueChanged.RemoveListener(OnVolumeSliderValueChanged);
    }

    if (voiceVolumeSlider != null)
    {
        voiceVolumeSlider.onValueChanged.RemoveListener(OnVolumeSliderValueChanged);
    }

    if (bgmVolumeSlider != null)
    {
        bgmVolumeSlider.onValueChanged.RemoveListener(OnVolumeSliderValueChanged);
    }

    if (uiSoundVolumeSlider != null)
    {
        uiSoundVolumeSlider.onValueChanged.RemoveListener(OnVolumeSliderValueChanged);
    }
}

private void OnVolumeSliderValueChanged(float value) // 슬라이더 값 변경 시 SaveStorage 현재 볼륨값 실시간 갱신
{
    if (saveStorage == null)
    {
        saveStorage = SaveStorage.Instance;
    }

    if (saveStorage == null)
    {
        return; // 저장 관리자가 없으면 종료
    }

    SaveStorage.SoundVolumeSaveData soundData = CreateSoundDataFromSliders(); // 현재 슬라이더 값 기준 소리 데이터 생성
    saveStorage.ApplyCurrentSoundVolumeSaveData(soundData); // JSON 저장 없이 SaveStorage 인스펙터 표시값만 실시간 갱신
}








}