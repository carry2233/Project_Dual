using UnityEngine;

/// <summary>
/// 배경음악 관리자
/// - 씬 시작 시 SaveStorage를 참조한다.
/// - 설정된 음악 사운드 에셋을 루프로 재생한다.
/// - SaveStorage의 현재 BGM 볼륨 설정값을 실시간으로 반영한다.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BackgroundMusicManager : MonoBehaviour
{
    [Header("저장 관리자 참조")]
    [SerializeField] private SaveStorage saveStorage; // 음악 사운드 설정 참조용 저장 관리자

    [Header("음악 사운드 에셋")]
    [SerializeField] private AudioClip musicSoundAsset; // 재생할 배경음악 오디오 클립

    [Header("음악 소리 크기")]
    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 1f; // 배경음악 자체 기본 소리 크기

    private AudioSource musicAudioSource; // 배경음악 재생용 AudioSource

    private void Awake() // 컴포넌트 초기화
    {
        musicAudioSource = GetComponent<AudioSource>(); // 같은 오브젝트의 AudioSource 참조
        musicAudioSource.playOnAwake = false; // 자동 재생 방지
        musicAudioSource.loop = true; // 배경음악 루프 재생 설정
    }

    private void Start() // 씬 시작 시 배경음악 재생
    {
        RefreshSaveStorageReference(); // 저장 관리자 참조 보정
        StartBackgroundMusic(); // 배경음악 재생 시작
        RefreshMusicVolume(); // 초기 볼륨 반영
    }

    private void Update() // 매 프레임 볼륨 실시간 반영
    {
        RefreshMusicVolume(); // SaveStorage의 현재 볼륨값에 맞춰 음악 볼륨 갱신
    }

    private void RefreshSaveStorageReference() // SaveStorage 참조 보정
    {
        if (saveStorage == null)
        {
            saveStorage = SaveStorage.Instance; // 싱글톤 저장 관리자 참조
        }

        if (saveStorage == null)
        {
            saveStorage = FindFirstObjectByType<SaveStorage>(); // 씬 안에서 저장 관리자 탐색
        }
    }

    private void StartBackgroundMusic() // 배경음악 재생 시작
    {
        if (musicAudioSource == null)
        {
            musicAudioSource = GetComponent<AudioSource>(); // AudioSource 참조 보정
        }

        if (musicSoundAsset == null)
        {
            return; // 음악 에셋이 없으면 재생하지 않음
        }

        musicAudioSource.clip = musicSoundAsset; // 재생할 음악 에셋 적용
        musicAudioSource.loop = true; // 루프 재생 설정

        if (musicAudioSource.isPlaying == false)
        {
            musicAudioSource.Play(); // 배경음악 재생
        }
    }

    private void RefreshMusicVolume() // 배경음악 볼륨 갱신
    {
        if (musicAudioSource == null)
        {
            return; // AudioSource가 없으면 종료
        }

        RefreshSaveStorageReference(); // SaveStorage 참조 보정

        if (saveStorage == null)
        {
            musicAudioSource.volume = Mathf.Clamp01(musicVolume); // 저장 관리자가 없으면 기본 음악 볼륨 사용
            return;
        }

        musicAudioSource.volume = Mathf.Clamp01(
            saveStorage.GetBGMFinalVolume01(musicVolume)); // SaveStorage 현재 BGM 설정값을 실시간 반영
    }

    public void SetMusicSoundAsset(AudioClip newMusicSoundAsset) // 외부에서 음악 에셋 교체
    {
        musicSoundAsset = newMusicSoundAsset; // 새 음악 에셋 저장
        StartBackgroundMusic(); // 새 음악 재생
        RefreshMusicVolume(); // 볼륨 갱신
    }

    public void SetMusicVolume(float newMusicVolume) // 외부에서 음악 기본 볼륨 변경
    {
        musicVolume = Mathf.Clamp01(newMusicVolume); // 음악 기본 볼륨 보정
        RefreshMusicVolume(); // 변경된 기본 볼륨 반영
    }
}