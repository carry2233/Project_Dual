using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 상태효과 표시 슬롯
/// - 캐릭터에게 적용 중인 상태효과 하나를 UI 슬롯으로 표시한다.
/// - 아이콘, 중첩 수치, 지속시간 수치를 갱신한다.
/// - 마우스 호버 시 GlobalCharacterManager에게 설명 UI 표시를 요청한다.
/// </summary>
public class StatusEffectDisplaySlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("현재 표시 중인 상태효과 정보")]
    [SerializeField] private int currentStatusEffectID; // 현재 표시 중인 상태효과 ID
    [SerializeField] private Sprite currentStatusEffectImage; // 현재 표시 중인 상태효과 이미지
    [SerializeField] private int currentStatusEffectStackValue; // 현재 표시 중인 상태효과 중첩 수치
    [SerializeField] private float currentStatusEffectDurationValue; // 현재 표시 중인 상태효과 지속시간 수치
    [SerializeField] private bool currentStatusEffectHasDuration; // 현재 표시 중인 상태효과가 지속시간을 사용하는지 여부

    [Header("UI 컴포넌트")]
    [SerializeField] private Image statusEffectIconImage; // 상태효과 이미지를 표시할 UI Image 컴포넌트
    [SerializeField] private TMP_Text stackValueText; // 중첩값을 표시할 TMP 텍스트
    [SerializeField] private TMP_Text durationValueText; // 지속값을 표시할 TMP 텍스트

    [Header("설명 UI 전달 대상")]
    [SerializeField] private GlobalCharacterManager globalCharacterManager; // 상태효과 설명 UI를 담당할 전역 캐릭터 매니저

    public int CurrentStatusEffectID => currentStatusEffectID; // 현재 표시 중인 상태효과 ID 반환
    public Sprite CurrentStatusEffectImage => currentStatusEffectImage; // 현재 표시 중인 상태효과 이미지 반환
    public int CurrentStatusEffectStackValue => currentStatusEffectStackValue; // 현재 중첩 수치 반환
    public float CurrentStatusEffectDurationValue => currentStatusEffectDurationValue; // 현재 지속 수치 반환

    private void Awake() // 컴포넌트 자동 참조
    {
        if (statusEffectIconImage == null)
        {
            statusEffectIconImage = GetComponent<Image>(); // 같은 오브젝트의 Image 자동 참조
        }

        if (statusEffectIconImage != null)
        {
            statusEffectIconImage.raycastTarget = true; // 마우스 호버 감지를 위해 Raycast Target 활성화
        }
    }

    public void Initialize(
        int statusEffectID,
        Sprite statusEffectImage,
        int stackValue,
        float durationValue,
        bool hasDuration,
        GlobalCharacterManager targetGlobalCharacterManager) // 슬롯 표시 정보 초기화
    {
        currentStatusEffectID = statusEffectID; // 상태효과 ID 저장
        currentStatusEffectImage = statusEffectImage; // 상태효과 이미지 저장
        currentStatusEffectStackValue = Mathf.Max(1, stackValue); // 중첩값 보정 후 저장
        currentStatusEffectDurationValue = durationValue; // 지속시간 저장
        currentStatusEffectHasDuration = hasDuration; // 지속시간 사용 여부 저장
        globalCharacterManager = targetGlobalCharacterManager; // 설명 UI 담당 매니저 저장

        RefreshSlotUI(); // 슬롯 UI 갱신
    }

    public void RefreshSlot(
        int stackValue,
        float durationValue,
        bool hasDuration) // 중첩/지속값만 갱신
    {
        currentStatusEffectStackValue = Mathf.Max(1, stackValue); // 중첩값 갱신
        currentStatusEffectDurationValue = durationValue; // 지속시간 갱신
        currentStatusEffectHasDuration = hasDuration; // 지속시간 사용 여부 갱신

        RefreshSlotUI(); // 슬롯 UI 갱신
    }

    public void SetGlobalCharacterManager(GlobalCharacterManager targetGlobalCharacterManager) // 설명 UI 담당 매니저 설정
    {
        globalCharacterManager = targetGlobalCharacterManager; // 전역 캐릭터 매니저 저장
    }

    private void RefreshSlotUI() // 슬롯 UI 표시 갱신
    {
        if (statusEffectIconImage != null)
        {
            statusEffectIconImage.sprite = currentStatusEffectImage; // 아이콘 이미지 적용
            statusEffectIconImage.enabled = currentStatusEffectImage != null; // 이미지가 있을 때만 표시
        }

        if (stackValueText != null)
        {
            if (currentStatusEffectStackValue > 1)
            {
                stackValueText.gameObject.SetActive(true); // 중첩이 2 이상이면 텍스트 표시
                stackValueText.text = currentStatusEffectStackValue.ToString(); // 중첩 수치 표시
            }
            else
            {
                stackValueText.gameObject.SetActive(false); // 중첩 1이면 숨김
                stackValueText.text = string.Empty; // 텍스트 초기화
            }
        }

        if (durationValueText != null)
        {
            if (currentStatusEffectHasDuration)
            {
                int displayDuration = Mathf.Max(0, Mathf.CeilToInt(currentStatusEffectDurationValue)); // 표시용 지속시간 정수화
                durationValueText.gameObject.SetActive(true); // 지속시간 텍스트 표시
                durationValueText.text = displayDuration.ToString(); // 지속시간 수치 표시
            }
            else
            {
                durationValueText.gameObject.SetActive(false); // 무한 지속이면 지속시간 텍스트 숨김
                durationValueText.text = string.Empty; // 텍스트 초기화
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData) // 마우스 호버 시작
    {
        if (globalCharacterManager == null)
        {
            globalCharacterManager = GlobalCharacterManager.Instance; // 전역 캐릭터 매니저 재참조
        }

        if (globalCharacterManager != null)
        {
            globalCharacterManager.ShowStatusEffectDescription(currentStatusEffectID); // 설명 UI 표시 요청
        }
    }

    public void OnPointerExit(PointerEventData eventData) // 마우스 호버 종료
    {
        if (globalCharacterManager == null)
        {
            globalCharacterManager = GlobalCharacterManager.Instance; // 전역 캐릭터 매니저 재참조
        }

        if (globalCharacterManager != null)
        {
            globalCharacterManager.HideStatusEffectDescription(); // 설명 UI 숨김 요청
        }
    }

    private void OnDisable() // 슬롯 비활성화 시 설명 UI 숨김
    {
        if (globalCharacterManager != null)
        {
            globalCharacterManager.HideStatusEffectDescription(); // 슬롯이 사라질 때 설명 UI 잔존 방지
        }
    }
}