using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI 이미지 호버와 버튼 클릭 사운드를 SaveStorage의 UI 사운드 설정에 맞춰 재생하는 시스템입니다.
/// </summary>
public class UISoundPlaybackSystem : MonoBehaviour, IPointerEnterHandler
{
    [Header("UI 참조")]
    [SerializeField] private Image uiImage; // 마우스 커서 접촉 판정에 사용할 UI 이미지
    [SerializeField] private Button buttonComponent; // 클릭 사운드를 연결할 버튼 컴포넌트

    [Header("재생 여부")]
    [SerializeField] private bool playSoundOnImagePointerEnter = true; // 이미지 영역 마우스 커서 접촉 시 소리 재생 여부
    [SerializeField] private bool playSoundOnButtonClick = true; // 버튼 클릭 시 소리 재생 여부

    [Header("이미지 호버 사운드")]
    [SerializeField] private AudioClip imagePointerEnterSoundClip; // 이미지 영역 마우스 커서 접촉 시 재생할 소리 에셋
    [SerializeField] private float imagePointerEnterBaseVolume = 1f; // 이미지 호버 기본 소리 크기값

    [Header("버튼 클릭 사운드")]
    [SerializeField] private AudioClip buttonClickSoundClip; // 버튼 클릭 시 재생할 소리 에셋
    [SerializeField] private float buttonClickBaseVolume = 1f; // 버튼 클릭 기본 소리 크기값

    private SaveStorage saveStorage; // 소리 설정 참조용 SaveStorage
    private AudioSource audioSource; // UI 사운드 재생용 AudioSource

    private void Awake() // 시작 전 참조 보정
    {
        if (uiImage == null)
        {
            uiImage = GetComponent<Image>(); // 같은 오브젝트에서 Image 자동 참조
        }

        if (buttonComponent == null)
        {
            buttonComponent = GetComponent<Button>(); // 같은 오브젝트에서 Button 자동 참조
        }

        audioSource = GetComponent<AudioSource>(); // 같은 오브젝트에서 AudioSource 참조

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>(); // 없으면 AudioSource 추가
        }

        audioSource.playOnAwake = false; // 시작 시 자동 재생 방지
    }

    private void Start() // 씬 시작 시 SaveStorage 탐색
    {
        FindSaveStorageIfNeeded(); // SaveStorage 자동 참조
    }

    private void OnEnable() // 활성화 시 버튼 이벤트 연결
    {
        if (buttonComponent != null)
        {
            buttonComponent.onClick.AddListener(PlayButtonClickSound); // 버튼 클릭 사운드 이벤트 연결
        }
    }

    private void OnDisable() // 비활성화 시 버튼 이벤트 해제
    {
        if (buttonComponent != null)
        {
            buttonComponent.onClick.RemoveListener(PlayButtonClickSound); // 버튼 클릭 사운드 이벤트 해제
        }
    }

public void OnPointerEnter(PointerEventData eventData) // 마우스 커서가 UI 영역에 들어왔을 때 호출
{
    if (playSoundOnImagePointerEnter == false)
    {
        return; // 이미지 호버 사운드가 꺼져 있으면 종료
    }

    if (CanPlaySoundByButtonInteractable() == false)
    {
        return; // 버튼이 상호작용 불가능하면 이미지 호버 사운드도 재생하지 않음
    }

    if (uiImage == null)
    {
        return; // 이미지 참조가 없으면 종료
    }

    PlayUISound(imagePointerEnterSoundClip, imagePointerEnterBaseVolume); // 이미지 호버 사운드 재생
}

private void PlayButtonClickSound() // 버튼 클릭 사운드 재생
{
    if (playSoundOnButtonClick == false)
    {
        return; // 버튼 클릭 사운드가 꺼져 있으면 종료
    }

    if (CanPlaySoundByButtonInteractable() == false)
    {
        return; // 버튼이 상호작용 불가능하면 버튼 클릭 사운드 재생하지 않음
    }

    PlayUISound(buttonClickSoundClip, buttonClickBaseVolume); // 버튼 클릭 사운드 재생
}
    private void PlayUISound(AudioClip targetClip, float baseVolume) // UI 사운드 재생
    {
        if (targetClip == null)
        {
            return; // 재생할 클립이 없으면 종료
        }

        if (audioSource == null)
        {
            return; // 오디오 소스가 없으면 종료
        }

        FindSaveStorageIfNeeded(); // SaveStorage 참조 보정

        float finalVolume = saveStorage != null
            ? saveStorage.GetUISoundFinalVolume01(baseVolume)
            : Mathf.Max(0f, baseVolume); // 저장 설정이 있으면 UI 볼륨 반영

        audioSource.PlayOneShot(targetClip, finalVolume); // 최종 볼륨으로 사운드 재생
    }

    private void FindSaveStorageIfNeeded() // SaveStorage 참조 보정
    {
        if (saveStorage != null)
        {
            return; // 이미 참조되어 있으면 종료
        }

        saveStorage = SaveStorage.Instance != null
            ? SaveStorage.Instance
            : FindFirstObjectByType<SaveStorage>(); // 인스턴스 우선 탐색
    }

    private bool CanPlaySoundByButtonInteractable() // 버튼 상호작용 가능 여부 기준 사운드 재생 가능 여부 반환
{
    if (buttonComponent == null)
    {
        return true; // 버튼이 없는 이미지 전용 UI면 사운드 재생 가능
    }

    return buttonComponent.interactable; // 버튼이 상호작용 가능할 때만 사운드 재생
}





}