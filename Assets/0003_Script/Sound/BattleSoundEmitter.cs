using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 사운드 출력기
/// - 다른 스크립트가 전달한 사운드 재생
/// - 절대 거리 / 줌 / 전역 볼륨 반영
/// </summary>
public class BattleSoundEmitter : MonoBehaviour
{
private class ActiveSoundData // 현재 재생 중인 사운드 정보
{
    public AudioSource audioSource; // 실제 재생 중인 AudioSource
    public float baseVolumePercent; // 원본 기본강도 퍼센트
    public BattleAudioSettings.AudioGroupType audioGroupType; // 소속 그룹
    public bool useDistanceAndZoom; // 거리/줌 계산 사용 여부
}

    [Header("Emitter 설정")]
    [SerializeField] private Transform soundOrigin; // 소리 발생 기준 위치

    private readonly List<ActiveSoundData> activeSoundList = new List<ActiveSoundData>(); // 현재 재생 중인 사운드 목록

    private void Awake() // 초기 참조 설정
    {
        if (soundOrigin == null)
        {
            soundOrigin = transform; // 기준 위치가 없으면 자기 자신 사용
        }
    }

    private void Update() // 재생 중 사운드 갱신
    {
        for (int i = activeSoundList.Count - 1; i >= 0; i--)
        {
            ActiveSoundData activeData = activeSoundList[i]; // 현재 재생 데이터 참조

            if (activeData == null || activeData.audioSource == null)
            {
                activeSoundList.RemoveAt(i); // 잘못된 데이터 제거
                continue;
            }

            if (!activeData.audioSource.isPlaying)
            {
                AudioSourcePool.Instance?.ReturnAudioSource(activeData.audioSource); // 재생 종료 시 풀로 반환
                activeSoundList.RemoveAt(i); // 활성 목록에서 제거
                continue;
            }

            activeData.audioSource.transform.position = soundOrigin.position; // 재생 중에도 위치 갱신
            activeData.audioSource.volume = CalculateFinalVolume(activeData.baseVolumePercent, activeData.audioGroupType, activeData.useDistanceAndZoom); // 최종 볼륨 갱신
        }
    }

public void PlaySound(AudioClip clip, int baseVolumePercent, BattleAudioSettings.AudioGroupType audioGroupType) // 외부에서 사운드 재생 요청
{
    if (clip == null)
    {
        return; // 클립이 없으면 종료
    }

    if (AudioSourcePool.Instance == null)
    {
        Debug.LogWarning($"{name} : AudioSourcePool이 씬에 없습니다."); // 풀 누락 경고
        return;
    }

    AudioSource pooledSource = AudioSourcePool.Instance.GetAudioSource(); // 풀에서 AudioSource 획득

    if (pooledSource == null)
    {
        return; // 가져올 수 없으면 종료
    }

    float originalBaseVolumePercent = Mathf.Clamp(baseVolumePercent, 0f, 100f); // 원본 기본강도 퍼센트 저장용 값
    bool useDistanceAndZoom = BattleAudioSettings.Instance != null && BattleAudioSettings.Instance.UsesDistanceAndZoom(audioGroupType); // 거리/줌 사용 여부 판단

    pooledSource.transform.position = soundOrigin.position; // 초기 위치 설정
    pooledSource.clip = clip; // 클립 설정
    pooledSource.loop = false; // 루프 비활성화
    pooledSource.volume = CalculateFinalVolume(originalBaseVolumePercent, audioGroupType, useDistanceAndZoom); // 초기 볼륨 계산 적용
    pooledSource.Play(); // 재생 시작

    ActiveSoundData activeData = new ActiveSoundData(); // 재생 중 데이터 생성
    activeData.audioSource = pooledSource; // AudioSource 저장
    activeData.baseVolumePercent = originalBaseVolumePercent; // 원본 기본강도 퍼센트 저장
    activeData.audioGroupType = audioGroupType; // 그룹 저장
    activeData.useDistanceAndZoom = useDistanceAndZoom; // 거리/줌 사용 여부 저장

    activeSoundList.Add(activeData); // 활성 목록 등록
}

private float CalculateFinalVolume(float originalBaseVolumePercent, BattleAudioSettings.AudioGroupType audioGroupType, bool useDistanceAndZoom) // 최종 볼륨 계산
{
    float finalBaseVolumePercent = Mathf.Clamp(originalBaseVolumePercent, 0f, 100f); // 최종 적용할 기본강도 퍼센트

    if (BattleAudioSettings.Instance != null && useDistanceAndZoom && BattleCameraController.Instance != null)
    {
        Vector3 origin = soundOrigin.position; // 소리 발생 위치 저장
        Vector3 camera = BattleCameraController.Instance.CurrentCameraPosition; // 카메라 위치 저장
        float distance = Vector2.Distance(new Vector2(origin.x, origin.y), new Vector2(camera.x, camera.y)); // XY 평면 거리 계산

        finalBaseVolumePercent = BattleAudioSettings.Instance.CalculateDistanceAdjustedBaseVolumePercent(
            audioGroupType,
            finalBaseVolumePercent,
            distance); // 거리 기준 기본강도 감소 적용

        finalBaseVolumePercent = BattleAudioSettings.Instance.CalculateZoomAdjustedBaseVolumePercent(
            audioGroupType,
            finalBaseVolumePercent,
            BattleCameraController.Instance.CurrentOrthographicSize,
            BattleCameraController.Instance.MinOrthographicSize); // 줌 기준 기본강도 감소 적용
    }

    float finalVolume = finalBaseVolumePercent / 100f; // 최종 기본강도를 0~1로 변환

    if (BattleAudioSettings.Instance != null)
    {
        finalVolume *= BattleAudioSettings.Instance.GetMasterVolume01(); // 전체 볼륨 반영
        finalVolume *= BattleAudioSettings.Instance.GetGroupVolume01(audioGroupType); // 그룹 볼륨 반영
    }

    return Mathf.Clamp01(finalVolume); // 최종 볼륨 반환
}
}