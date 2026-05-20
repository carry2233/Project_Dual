using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 캐릭터 상세 정보창 내부의 요소 Scroll View 크기 조정과 UI 토글 버튼들을 관리하는 스크립트입니다.
/// </summary>
public class CharacterDetailInfoWindow : MonoBehaviour
{
    [Header("캐릭터 요소 Scroll View 설정")]
    [SerializeField] private RectTransform characterElementContent; // 캐릭터 요소들이 배치되는 Content RectTransform
    [SerializeField] private GridLayoutGroup characterElementGridLayoutGroup; // 캐릭터 요소 Content에 적용된 GridLayoutGroup

    [Header("기본 활성화 설정")]
    [SerializeField] private int defaultActiveToggleIndex = 0; // 생성 시 기본으로 활성화할 토글 리스트 인덱스

    [Header("토글 UI 목록")]
    [SerializeField] private List<ToggleUIElement> toggleUIElements = new List<ToggleUIElement>(); // 토글 버튼/대상/스크롤바 목록

    [Header("캐릭터 기본 정보")]
    [SerializeField] private TMP_Text characterNameText; // 캐릭터 이름 표시 텍스트

    [Header("레벨 표시 UI")]
    [SerializeField] private TMP_Text levelText; // 레벨 값을 표시할 텍스트

    [Header("저장 스탯 표시 UI")]

[Header("이속")]
[SerializeField] private TMP_Text baseMoveSpeedText; // 기본 이동속도 표시 텍스트
[SerializeField] private TMP_Text moveSpeedPercentText; // 이동속도 퍼센트 표시 텍스트
[SerializeField] private TMP_Text finalMoveSpeedText; // 최종 이동속도 표시 텍스트

[Header("전투 스탯")]
[SerializeField] private TMP_Text attackPowerText; // 공격력 표시 텍스트
[SerializeField] private TMP_Text defenseValueText; // 방어력 표시 텍스트
[SerializeField] private TMP_Text healthText; // 최대체력 / 현재체력 표시 텍스트


[Header("체급")]
[SerializeField] private TMP_Text bodySizeText; // 체급 표시 텍스트
[Header("속도")]
[SerializeField] private TMP_Text speedStatText; // 속도 수치 표시 텍스트
[Header("위력률")]
[SerializeField] private TMP_Text powerRatePercentText; // 위력률 표시 텍스트
[Header("와해 스탯")]
[SerializeField] private TMP_Text maxStaggerAmountText; // 최대 와해량 표시 텍스트
[SerializeField] private TMP_Text staggerResistancePercentText; // 와해 저항률 표시 텍스트

[Header("허기 표시 UI")]
[SerializeField] private TMP_Text hungerText; // 최대허기 / 현재허기 표시 텍스트

[Header("상태 게이지")]
[SerializeField] private Slider healthSlider; // 현재 체력 게이지
[SerializeField] private Slider hungerSlider; // 현재 허기 게이지

private SaveStorage.OwnedCharacterStatData targetStatData; // 상세 UI가 참조할 저장 스탯정보

    private CharacterStatSystem targetStatSystem; // 상세 UI가 참조할 캐릭터 스탯 시스템

    /// <summary>
    /// 토글 버튼, 토글 대상 UI, 초기화할 Scrollbar를 묶어서 관리하는 데이터입니다.
    /// </summary>
[System.Serializable]
public class ToggleUIElement
{
    public Button toggleButton; // 활성화/비활성화 토글용 버튼
    public GameObject targetUIObject; // 토글 대상 UI 오브젝트
    public Scrollbar targetScrollbar; // 비활성화 시 값이 초기화될 Scrollbar
    public ScrollRect targetScrollRect; // Content 위치를 강제로 조정할 ScrollRect
    [Range(0f, 1f)] public float resetScrollbarValue = 0f; // 비활성화 시 적용할 스크롤 값
}

    public void Initialize(SaveStorage.OwnedCharacterStatData newTargetStatData) // 저장 스탯정보 기준으로 상세 UI를 초기화합니다.
{
    targetStatData = newTargetStatData; // 저장 스탯정보 저장
    RefreshSavedStatTexts(); // 저장 스탯 표시 갱신
}

    private void Awake()
    {
        InitializeContentWidth();
        InitializeToggleButtons();
        ApplyDefaultToggleState();
    }

    /// <summary>
    /// GridLayoutGroup의 X축 셀 크기와 Content 자식 수를 곱해 Content 너비를 설정합니다.
    /// </summary>
    private void InitializeContentWidth()
    {
        if (characterElementContent == null || characterElementGridLayoutGroup == null)
        {
            return;
        }

        float cellWidth = characterElementGridLayoutGroup.cellSize.x;
        int childCount = characterElementContent.childCount;
        float targetWidth = cellWidth * childCount;

        characterElementContent.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
    }

    /// <summary>
    /// 토글 리스트에 등록된 버튼들에 클릭 이벤트를 연결합니다.
    /// </summary>
    private void InitializeToggleButtons()
    {
        for (int i = 0; i < toggleUIElements.Count; i++)
        {
            int index = i;

            if (toggleUIElements[index] == null || toggleUIElements[index].toggleButton == null)
            {
                continue;
            }

            toggleUIElements[index].toggleButton.onClick.RemoveListener(() => ToggleTargetUI(index));
            toggleUIElements[index].toggleButton.onClick.AddListener(() => ToggleTargetUI(index));
        }
    }

    /// <summary>
    /// 생성 시 지정된 인덱스의 UI만 활성화하고 나머지는 비활성화합니다.
    /// </summary>
    private void ApplyDefaultToggleState()
    {
        for (int i = 0; i < toggleUIElements.Count; i++)
        {
            bool isDefaultActive = i == defaultActiveToggleIndex;

            SetTargetUIActive(i, isDefaultActive);
        }
    }


private void ToggleTargetUI(int index) // 선택한 토글 UI를 활성화하고, 이미 활성화된 경우 아무 동작도 하지 않습니다.
{
    if (IsInvalidIndex(index))
    {
        return;
    }

    GameObject targetObject = toggleUIElements[index].targetUIObject;

    if (targetObject == null)
    {
        return;
    }

    // 이미 활성화 상태면 아무것도 하지 않음
    if (targetObject.activeSelf)
    {
        return;
    }

    // 비활성 상태일 때만 선택된 UI 활성화 + 나머지 비활성화
    SetOnlyTargetUIActive(index);
}

    /// <summary>
    /// 지정된 인덱스의 UI 활성 상태를 설정하고, 비활성화 시 Scrollbar 값을 0으로 초기화합니다.
    /// </summary>
private void SetTargetUIActive(int index, bool isActive) // 지정된 인덱스의 UI 활성 상태를 설정합니다.
{
    if (IsInvalidIndex(index))
    {
        return; // 잘못된 인덱스면 종료
    }

    ToggleUIElement toggleElement = toggleUIElements[index]; // 토글 요소 참조

    if (toggleElement.targetUIObject != null)
    {
        toggleElement.targetUIObject.SetActive(isActive); // 대상 UI 활성 상태 적용
    }

    if (isActive == false)
    {
        ApplyScrollReset(toggleElement); // 비활성화 시 스크롤 위치 초기화
    }
}

    /// <summary>
    /// 리스트 인덱스가 유효한지 검사합니다.
    /// </summary>
    private bool IsInvalidIndex(int index)
    {
        return index < 0 || index >= toggleUIElements.Count || toggleUIElements[index] == null;
    }

    private void SetOnlyTargetUIActive(int activeIndex) // 선택한 인덱스의 UI만 활성화하고 나머지 UI는 모두 비활성화합니다.
{
    for (int i = 0; i < toggleUIElements.Count; i++)
    {
        bool isActiveTarget = i == activeIndex;

        SetTargetUIActive(i, isActiveTarget);
    }
}

public void Initialize(CharacterStatSystem newTargetStatSystem) // 상세 UI에 표시할 스탯 시스템을 연결합니다.
{
    if (targetStatSystem != null)
    {
        targetStatSystem.OnStatusValueChanged -= RefreshLevelText; // 기존 대상 이벤트 연결 해제
    }

    targetStatSystem = newTargetStatSystem; // 새 스탯 시스템 저장

    if (targetStatSystem != null)
    {
        targetStatSystem.OnStatusValueChanged += RefreshLevelText; // 스탯 변경 이벤트 연결
    }

    RefreshLevelText(targetStatSystem); // 최초 레벨 표시 갱신
}

private void OnDestroy() // 상세 UI 삭제 시 이벤트 연결을 해제합니다.
{
    if (targetStatSystem != null)
    {
        targetStatSystem.OnStatusValueChanged -= RefreshLevelText; // 이벤트 중복 참조 방지
    }
}

private void RefreshLevelText(CharacterStatSystem statSystem) // 현재 레벨 값을 텍스트에 표시합니다.
{
    if (levelText == null)
    {
        return; // 텍스트가 없으면 종료
    }

    if (statSystem == null)
    {
        levelText.text = "LV.-"; // 대상이 없을 때 표시
        return;
    }

    levelText.text = "LV." + statSystem.LevelStats; // 레벨 표시
}

private void RefreshSavedStatTexts() // 저장된 캐릭터 스탯을 텍스트와 게이지에 표시합니다.
{
    if (targetStatData == null)
    {
        SetText(characterNameText, "이름: -");
        SetText(levelText, "LV.-");
        SetText(baseMoveSpeedText, "기본 이동속도: -");
        SetText(moveSpeedPercentText, "이동속도율: -");
        SetText(finalMoveSpeedText, "이동속도: -");
        SetText(attackPowerText, "공격력: -");
        SetText(defenseValueText, "방어율: -");
        SetText(healthText, "- / -");
        SetText(hungerText, "- / -");
        SetText(bodySizeText, "체급: -");
        SetText(speedStatText, "속도: -");
        SetText(powerRatePercentText, "위력률: -");
        SetText(maxStaggerAmountText, "최대 와해량: -");
        SetText(staggerResistancePercentText, "와해 저항률: -");

        SetSliderValue(healthSlider, 0, 0);
        SetSliderValue(hungerSlider, 0, 0);
        return;
    }

    SetText(characterNameText, "이름: " + targetStatData.characterName);
    SetText(levelText, "LV." + targetStatData.levelstats);
    SetText(baseMoveSpeedText, "기본 이동속도: " + targetStatData.baseMoveSpeed);
    SetText(moveSpeedPercentText, "이동속도율: " + targetStatData.moveSpeedPercent + "%");
    SetText(finalMoveSpeedText, "이동속도: " + targetStatData.finalMoveSpeed);

    SetText(attackPowerText, "공격력: " + targetStatData.attackPower);
    SetText(defenseValueText, "방어율: " + targetStatData.defenseValue + "%");
    SetText(healthText, "체력: " + targetStatData.maxHealth + " / " + targetStatData.currentHealth);
    SetText(hungerText, "허기: " + targetStatData.maxHunger + " / " + targetStatData.currentHunger);

    SetText(bodySizeText, "체급: " + targetStatData.bodySize);
    SetText(speedStatText, "속도: " + targetStatData.speedStat);
    SetText(powerRatePercentText, "위력률: " + targetStatData.powerRatePercent + "%");
    SetText(maxStaggerAmountText, "최대 와해량: " + targetStatData.maxStaggerAmount);
    SetText(staggerResistancePercentText, "와해 저항률: " + targetStatData.staggerResistancePercent + "%");

    SetSliderValue(healthSlider, targetStatData.currentHealth, targetStatData.maxHealth);
    SetSliderValue(hungerSlider, targetStatData.currentHunger, targetStatData.maxHunger);
}

private void SetText(TMP_Text targetText, string value) // TMP 텍스트 값을 안전하게 설정합니다.
{
    if (targetText == null)
    {
        return; // 텍스트가 없으면 종료
    }

    targetText.text = value; // 텍스트 표시
}

private void ApplyScrollReset(ToggleUIElement toggleElement) // 스크롤바와 Content 위치를 설정값으로 초기화합니다.
{
    if (toggleElement == null)
    {
        return; // 대상 없으면 종료
    }

    float resetValue = Mathf.Clamp01(toggleElement.resetScrollbarValue); // 0~1 범위 보정

    Canvas.ForceUpdateCanvases(); // UI 레이아웃 즉시 갱신

    if (toggleElement.targetScrollbar != null)
    {
        toggleElement.targetScrollbar.value = resetValue; // 스크롤바 값 초기화
    }

    if (toggleElement.targetScrollRect != null)
    {
        toggleElement.targetScrollRect.verticalNormalizedPosition = resetValue; // Content 세로 위치 초기화
        toggleElement.targetScrollRect.horizontalNormalizedPosition = resetValue; // Content 가로 위치 초기화
    }

    Canvas.ForceUpdateCanvases(); // 변경된 위치 즉시 반영
}

private void SetSliderValue(Slider targetSlider, int currentValue, int maxValue) // 현재/최대값 기준으로 슬라이더를 갱신합니다.
{
    if (targetSlider == null)
    {
        return; // 슬라이더가 없으면 종료
    }

    targetSlider.minValue = 0f; // 최소값 설정
    targetSlider.maxValue = 1f; // 최대값 설정
    targetSlider.value = maxValue <= 0 ? 0f : Mathf.Clamp01((float)currentValue / maxValue); // 비율 적용
}










}