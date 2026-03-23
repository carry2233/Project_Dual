using UnityEngine;

/// <summary>
/// 전투 사운드 전역 설정 저장
/// - 전체 / 효과음 / 음악 / 음성 / UI 볼륨 저장
/// - 절대 거리 기반 감쇠 계수와 줌 보정 규칙 제공
/// </summary>
public class BattleAudioSettings : MonoBehaviour
{
    public enum AudioGroupType
    {
        Master, // 전체 사운드
        SFX,    // 효과음
        BGM,    // 음악
        Voice,  // 음성
        UI      // UI 사운드
    }

    public static BattleAudioSettings Instance { get; private set; } // 싱글톤 인스턴스

    [Header("전역 볼륨 (0~100)")]
    [Range(0, 100)] [SerializeField] private int masterVolume = 100; // 전체 볼륨
    [Range(0, 100)] [SerializeField] private int sfxVolume = 100; // 효과음 볼륨
    [Range(0, 100)] [SerializeField] private int bgmVolume = 100; // 음악 볼륨
    [Range(0, 100)] [SerializeField] private int voiceVolume = 100; // 음성 볼륨
    [Range(0, 100)] [SerializeField] private int uiVolume = 100; // UI 볼륨

[Header("거리 기준 기본강도 감소 규칙")]
[SerializeField] private float sfxDistanceUnit = 1f; // 효과음 거리 기준 단위
[SerializeField] private float voiceDistanceUnit = 1f; // 음성 거리 기준 단위
[SerializeField] private float sfxDistanceDecreasePerUnit = 5f; // 효과음 거리 1단위당 감소할 기본강도 퍼센트
[SerializeField] private float voiceDistanceDecreasePerUnit = 4f; // 음성 거리 1단위당 감소할 기본강도 퍼센트

[Header("줌 기준 기본강도 감소 규칙")]
[SerializeField] private bool useZoomFactorForSFX = true; // 효과음 줌 감소 사용 여부
[SerializeField] private bool useZoomFactorForVoice = true; // 음성 줌 감소 사용 여부
[SerializeField] private float sfxZoomSizeUnit = 1f; // 효과음 줌 크기 기준 단위
[SerializeField] private float voiceZoomSizeUnit = 1f; // 음성 줌 크기 기준 단위
[SerializeField] private float sfxZoomDecreasePerUnit = 6f; // 효과음 줌 크기 1단위당 감소할 기본강도 퍼센트
[SerializeField] private float voiceZoomDecreasePerUnit = 5f; // 음성 줌 크기 1단위당 감소할 기본강도 퍼센트

[Header("최소 기본강도 퍼센트")]
[SerializeField] private float sfxMinimumBaseVolumePercent = 10f; // 효과음 최소 기본강도 퍼센트
[SerializeField] private float voiceMinimumBaseVolumePercent = 15f; // 음성 최소 기본강도 퍼센트

    private void Awake() // 싱글톤 초기화
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); // 중복 인스턴스 제거
            return;
        }

        Instance = this; // 싱글톤 인스턴스 저장
    }

    public float GetMasterVolume01() // 전체 볼륨을 0~1로 반환
    {
        return masterVolume / 100f; // 정수 볼륨값 정규화
    }

    public float GetGroupVolume01(AudioGroupType groupType) // 그룹 볼륨을 0~1로 반환
    {
        switch (groupType)
        {
            case AudioGroupType.Master:
                return masterVolume / 100f; // 전체 볼륨 반환

            case AudioGroupType.SFX:
                return sfxVolume / 100f; // 효과음 볼륨 반환

            case AudioGroupType.BGM:
                return bgmVolume / 100f; // 음악 볼륨 반환

            case AudioGroupType.Voice:
                return voiceVolume / 100f; // 음성 볼륨 반환

            case AudioGroupType.UI:
                return uiVolume / 100f; // UI 볼륨 반환
        }

        return 1f; // 예외 상황 기본값
    }

    public bool UsesDistanceAndZoom(AudioGroupType groupType) // 거리/줌 규칙 사용 여부 반환
    {
        return groupType == AudioGroupType.SFX || groupType == AudioGroupType.Voice; // 효과음과 음성만 적용
    }

public float CalculateDistanceAdjustedBaseVolumePercent(AudioGroupType groupType, float originalBaseVolumePercent, float distance) // 거리 기준 기본강도 계산
{
    float distanceUnit = 1f; // 거리 단위
    float decreasePerUnit = 0f; // 거리 단위당 감소량
    float minimumBaseVolumePercent = 0f; // 최소 기본강도 퍼센트

    switch (groupType)
    {
        case AudioGroupType.SFX:
            distanceUnit = Mathf.Max(0.0001f, sfxDistanceUnit); // 효과음 거리 단위 적용
            decreasePerUnit = Mathf.Max(0f, sfxDistanceDecreasePerUnit); // 효과음 거리 감소량 적용
            minimumBaseVolumePercent = Mathf.Clamp(sfxMinimumBaseVolumePercent, 0f, 100f); // 효과음 최소 기본강도 제한
            break;

        case AudioGroupType.Voice:
            distanceUnit = Mathf.Max(0.0001f, voiceDistanceUnit); // 음성 거리 단위 적용
            decreasePerUnit = Mathf.Max(0f, voiceDistanceDecreasePerUnit); // 음성 거리 감소량 적용
            minimumBaseVolumePercent = Mathf.Clamp(voiceMinimumBaseVolumePercent, 0f, 100f); // 음성 최소 기본강도 제한
            break;

        default:
            return Mathf.Clamp(originalBaseVolumePercent, 0f, 100f); // 거리 감소 미사용 그룹은 원본 반환
    }

    float clampedOriginalBaseVolumePercent = Mathf.Clamp(originalBaseVolumePercent, 0f, 100f); // 원본 기본강도 제한
    float maxReducibleAmount = Mathf.Max(0f, clampedOriginalBaseVolumePercent - minimumBaseVolumePercent); // 최대 감소 가능량 계산

    if (maxReducibleAmount <= 0f)
    {
        return clampedOriginalBaseVolumePercent; // 더 줄일 수 없으면 그대로 반환
    }

    float rawDecreaseAmount = (distance / distanceUnit) * decreasePerUnit; // 거리 기준 실제 감소량 계산
    float normalizedT = Mathf.Clamp01(rawDecreaseAmount / maxReducibleAmount); // 감소 진행도 0~1 계산
    float smoothT = Mathf.SmoothStep(0f, 1f, normalizedT); // 자연스러운 감소용 보간값 계산

    return Mathf.Lerp(clampedOriginalBaseVolumePercent, minimumBaseVolumePercent, smoothT); // 거리 반영 기본강도 반환
}

public float CalculateZoomAdjustedBaseVolumePercent(AudioGroupType groupType, float currentBaseVolumePercent, float currentOrthographicSize, float minOrthographicSize) // 줌 기준 기본강도 계산
{
    float zoomSizeUnit = 1f; // 줌 크기 단위
    float decreasePerUnit = 0f; // 줌 단위당 감소량
    float minimumBaseVolumePercent = 0f; // 최소 기본강도 퍼센트
    bool useZoomDecrease = false; // 줌 감소 사용 여부

    switch (groupType)
    {
        case AudioGroupType.SFX:
            useZoomDecrease = useZoomFactorForSFX; // 효과음 줌 감소 사용 여부 적용
            zoomSizeUnit = Mathf.Max(0.0001f, sfxZoomSizeUnit); // 효과음 줌 단위 적용
            decreasePerUnit = Mathf.Max(0f, sfxZoomDecreasePerUnit); // 효과음 줌 감소량 적용
            minimumBaseVolumePercent = Mathf.Clamp(sfxMinimumBaseVolumePercent, 0f, 100f); // 효과음 최소 기본강도 제한
            break;

        case AudioGroupType.Voice:
            useZoomDecrease = useZoomFactorForVoice; // 음성 줌 감소 사용 여부 적용
            zoomSizeUnit = Mathf.Max(0.0001f, voiceZoomSizeUnit); // 음성 줌 단위 적용
            decreasePerUnit = Mathf.Max(0f, voiceZoomDecreasePerUnit); // 음성 줌 감소량 적용
            minimumBaseVolumePercent = Mathf.Clamp(voiceMinimumBaseVolumePercent, 0f, 100f); // 음성 최소 기본강도 제한
            break;

        default:
            return Mathf.Clamp(currentBaseVolumePercent, 0f, 100f); // 줌 감소 미사용 그룹은 현재값 반환
    }

    if (!useZoomDecrease)
    {
        return Mathf.Clamp(currentBaseVolumePercent, 0f, 100f); // 줌 감소 비활성화면 현재값 반환
    }

    float clampedCurrentBaseVolumePercent = Mathf.Clamp(currentBaseVolumePercent, 0f, 100f); // 현재 기본강도 제한
    float maxReducibleAmount = Mathf.Max(0f, clampedCurrentBaseVolumePercent - minimumBaseVolumePercent); // 최대 감소 가능량 계산

    if (maxReducibleAmount <= 0f)
    {
        return clampedCurrentBaseVolumePercent; // 더 줄일 수 없으면 그대로 반환
    }

    float sizeIncrease = Mathf.Max(0f, currentOrthographicSize - minOrthographicSize); // 최소 카메라 크기 대비 증가량 계산
    float rawDecreaseAmount = (sizeIncrease / zoomSizeUnit) * decreasePerUnit; // 줌 기준 실제 감소량 계산
    float normalizedT = Mathf.Clamp01(rawDecreaseAmount / maxReducibleAmount); // 감소 진행도 0~1 계산
    float smoothT = Mathf.SmoothStep(0f, 1f, normalizedT); // 자연스러운 감소용 보간값 계산

    return Mathf.Lerp(clampedCurrentBaseVolumePercent, minimumBaseVolumePercent, smoothT); // 줌 반영 기본강도 반환
}

}