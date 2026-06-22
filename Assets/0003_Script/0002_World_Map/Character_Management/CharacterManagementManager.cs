using UnityEngine; // Unity 기본 네임스페이스
using UnityEngine.UI; // 버튼 참조용
using System.Collections.Generic; // 리스트 사용

/// <summary>
/// 캐릭터 관리창 패널 매니저
/// - 버튼 클릭으로 캐릭터 관리창 패널 토글
/// - 관리창이 열리면 월드맵 이동/타일 선택 차단
/// - 관리창이 열릴 때 현재 소유 캐릭터 목록 기준으로 슬롯 생성
/// - 관리창이 닫힐 때 생성된 슬롯 삭제
/// </summary>
public class CharacterManagementManager : MonoBehaviour
{

    
[Header("UI 참조")]
[SerializeField] private Button openButton; // 캐릭터 관리창 활성화 버튼
[SerializeField] private Button closeButton; // 캐릭터 관리창 비활성화 버튼
[SerializeField] private GameObject characterManagementPanelObject; // 캐릭터 관리창 패널 오브젝트

    [Header("외부 참조")]
    [SerializeField] private TileSelectionManager tileSelectionManager; // 타일 선택 및 월드맵 잠금 제어 스크립트

    [Header("관리창 슬롯 생성 참조")]
[SerializeField] private Transform characterSlotParent; // Grid Layout Group이 적용된 슬롯 부모 오브젝트

[Header("외부 데이터 참조")]
[SerializeField] private CharacterInfoManager characterInfoManager; // 캐릭터 정보 매니저 참조
[SerializeField] private SaveStorage saveStorage; // 저장 데이터 매니저 참조

[Header("상세 UI 생성 참조")]
[SerializeField] private Transform detailUIParent; // 상세 UI가 생성될 부모 오브젝트

[Header("스크롤뷰 Content 크기 조절")]
[SerializeField] private RectTransform contentRectTransform; // 스크롤뷰 Content 오브젝트 RectTransform


[Header("캐릭터 이미지 UI 부모")]
public GameObject characterImageParentObject; // 이미지 부모 오브젝트

[Header("이미지 인덱스 버튼")]
[SerializeField] private Button previousImageButton; // 이전 이미지 버튼
[SerializeField] private Button nextImageButton; // 다음 이미지 버튼

private GlobalCharacterDefinition currentSelectedCharacter; // 현재 선택 캐릭터
private int currentImageIndex; // 현재 이미지 인덱스

private GameObject currentDetailUIObject; // 현재 생성된 상세 UI 오브젝트

private readonly List<GameObject> createdSlotObjectList = new List<GameObject>(); // 현재 생성된 관리창 슬롯 목록

private GameObject currentCharacterVisualObject; // 현재 생성된 캐릭터 비주얼 프리팹 오브젝트

    private bool isPanelOpen = false; // 현재 패널 열림 상태

    public bool IsPanelOpen => isPanelOpen; // 현재 패널 열림 상태 외부 확인용

private void Awake() // 시작 전 버튼 연결 및 참조 초기화
{
    if (openButton != null)
    {
        openButton.onClick.AddListener(OpenCharacterManagementPanel); // 활성화 버튼 클릭 시 관리창 열기
    }

    if (closeButton != null)
    {
        closeButton.onClick.AddListener(CloseCharacterManagementPanel); // 비활성화 버튼 클릭 시 관리창 닫기
    }

    if (previousImageButton != null)
{
    previousImageButton.onClick.AddListener(PreviousCharacterImage); // 이전 이미지 버튼 이벤트 연결
}

if (nextImageButton != null)
{
    nextImageButton.onClick.AddListener(NextCharacterImage); // 다음 이미지 버튼 이벤트 연결
}

    if (characterInfoManager == null)
    {
        characterInfoManager = CharacterInfoManager.Instance; // 전역 캐릭터 정보 매니저 참조
    }

    if (characterInfoManager == null)
    {
        characterInfoManager = FindFirstObjectByType<CharacterInfoManager>(); // 씬에서 캐릭터 정보 매니저 탐색
    }

    if (saveStorage == null)
    {
        saveStorage = SaveStorage.Instance; // 전역 저장 매니저 참조
    }

    if (saveStorage == null)
    {
        saveStorage = FindFirstObjectByType<SaveStorage>(); // 씬에서 저장 매니저 탐색
    }

    ApplyPanelState(false); // 시작 시 패널 닫힘 상태 적용
}

private void Start() // 시작 시 캐릭터 이미지 버튼 숨김
{
    SetImageButtonActive(false); // 캐릭터 선택 전 이미지 버튼 비활성화
}

private void OnDestroy() // 종료 시 버튼 이벤트 해제
{
    if (openButton != null)
    {
        openButton.onClick.RemoveListener(OpenCharacterManagementPanel); // 활성화 버튼 이벤트 해제
    }

    if (closeButton != null)
    {
        closeButton.onClick.RemoveListener(CloseCharacterManagementPanel); // 비활성화 버튼 이벤트 해제
    }

    if (previousImageButton != null)
{
    previousImageButton.onClick.RemoveListener(PreviousCharacterImage); // 이전 이미지 버튼 이벤트 해제
}

if (nextImageButton != null)
{
    nextImageButton.onClick.RemoveListener(NextCharacterImage); // 다음 이미지 버튼 이벤트 해제
}
}

    public void OpenCharacterManagementPanel() // 캐릭터 관리창 패널 열기
    {
        ApplyPanelState(true); // 열림 상태 적용
    }

    public void CloseCharacterManagementPanel() // 캐릭터 관리창 패널 닫기
    {
        ApplyPanelState(false); // 닫힘 상태 적용
    }

private void ApplyPanelState(bool shouldOpen) // 패널 상태 적용 및 외부 시스템 연동
{
    isPanelOpen = shouldOpen; // 현재 열림 상태 저장

    if (characterManagementPanelObject != null)
    {
        characterManagementPanelObject.SetActive(isPanelOpen); // 패널 활성/비활성 적용
    }

    RefreshToggleButtonState(); // 관리창 상태에 따른 버튼 활성 상태 갱신

    if (tileSelectionManager != null)
    {
        tileSelectionManager.SetCharacterManagementUIOpen(isPanelOpen); // 타일 선택 및 월드맵 조작 차단 상태 전달
    }

    if (isPanelOpen == true)
    {
        CreateCharacterManagementSlots(); // 관리창이 열리면 슬롯 생성
    }
    else
    {
        ClearCharacterManagementSlots(); // 관리창이 닫히면 슬롯 삭제
    }
}

private void CreateCharacterManagementSlots() // 현재 소유 캐릭터 목록 기준으로 관리창 슬롯 생성
{
    ClearCharacterManagementSlots(); // 기존 슬롯 먼저 삭제

    if (characterSlotParent == null)
    {
        return; // 슬롯 부모가 없으면 종료
    }

    if (characterInfoManager == null)
    {
        characterInfoManager = CharacterInfoManager.Instance; // 캐릭터 정보 매니저 재참조
    }

    if (saveStorage == null)
    {
        saveStorage = SaveStorage.Instance; // 저장 매니저 재참조
    }

    if (characterInfoManager == null || saveStorage == null)
    {
        return; // 필수 참조가 없으면 종료
    }

    List<SaveStorage.OwnedCharacterData> ownedCharacterList = saveStorage.CurrentOwnedCharacterList; // 현재 소유 캐릭터 목록 가져오기
    List<GameObject> tempSlotList = new List<GameObject>(); // 정렬 전 임시 슬롯 목록

    for (int i = 0; i < ownedCharacterList.Count; i++)
    {
        SaveStorage.OwnedCharacterData ownedData = ownedCharacterList[i]; // 현재 소유 캐릭터 정보

        if (ownedData == null)
        {
            continue; // 비어 있으면 건너뜀
        }

        GlobalCharacterDefinition definition = characterInfoManager.FindDefinitionByID(ownedData.firstRowID, ownedData.secondRowID); // ID에 맞는 캐릭터 정의 탐색

        if (definition == null)
        {
            continue; // 정의가 없으면 건너뜀
        }

        GameObject slotPrefab = definition.ManagementCharacterSlotPrefab; // 관리창 슬롯 프리팹 가져오기

        if (slotPrefab == null)
        {
            continue; // 프리팹이 없으면 건너뜀
        }

GameObject slotObject = Instantiate(slotPrefab, characterSlotParent); // 슬롯 생성

CharacterManagementSlot managementSlot = slotObject.GetComponent<CharacterManagementSlot>(); // 슬롯 스크립트 참조

if (managementSlot != null)
{
    managementSlot.Initialize(
        this,
        ownedData.firstRowID,
        ownedData.secondRowID,
        ownedData.individualID,
        ownedData.isDead); // 슬롯 담당 캐릭터 정보와 사망 여부 전달
}

tempSlotList.Add(slotObject); // 임시 목록에 추가
    }

    tempSlotList.Sort(CompareSlotPriority); // 우선순위 기준 정렬

    for (int i = 0; i < tempSlotList.Count; i++)
    {
        tempSlotList[i].transform.SetSiblingIndex(i); // Grid Layout Group 배치 순서 적용
        createdSlotObjectList.Add(tempSlotList[i]); // 생성 슬롯 목록에 등록
    }

    RefreshContentRightValue(); // 생성된 슬롯 수 기준으로 Content 오른쪽값 갱신
}


private int CompareSlotPriority(GameObject a, GameObject b) // 슬롯 우선순위 비교
{
    int priorityA = GetSlotPriority(a); // A 슬롯 우선순위
    int priorityB = GetSlotPriority(b); // B 슬롯 우선순위

    return priorityA.CompareTo(priorityB); // 작은 값이 앞에 오도록 정렬
}

private int GetSlotPriority(GameObject slotObject) // 슬롯 오브젝트의 우선순위값 반환
{
    if (slotObject == null)
    {
        return int.MaxValue; // 오브젝트가 없으면 가장 뒤
    }

    CharacterManagementSlot slot = slotObject.GetComponent<CharacterManagementSlot>(); // 슬롯 정보 스크립트 가져오기

    if (slot == null)
    {
        return int.MaxValue; // 슬롯 스크립트가 없으면 가장 뒤
    }

    return slot.SlotSortPriority; // 슬롯 우선순위 반환
}

private void ClearCharacterManagementSlots() // 생성된 관리창 슬롯 삭제
{
    for (int i = 0; i < createdSlotObjectList.Count; i++)
    {
        if (createdSlotObjectList[i] != null)
        {
            Destroy(createdSlotObjectList[i]); // 생성 슬롯 삭제
        }
    }

    createdSlotObjectList.Clear(); // 생성 슬롯 목록 초기화
    RefreshContentRightValue(); // 슬롯 삭제 후 Content 너비값 초기화
    ClearCharacterDetailUI(); // 관리창 슬롯 삭제 시 상세 UI도 같이 삭제
    ClearCharacterImageUI(); // 선택 캐릭터 이미지 UI 초기화
}

public void OpenCharacterDetailUI(int firstRowID, int secondRowID, int individualID) // 선택 캐릭터 상세 UI 생성
{
    if (detailUIParent == null)
    {
        return; // 상세 UI 부모가 없으면 종료
    }

    if (characterInfoManager == null)
    {
        characterInfoManager = CharacterInfoManager.Instance; // 캐릭터 정보 매니저 재참조
    }

    if (saveStorage == null)
    {
        saveStorage = SaveStorage.Instance; // 저장 매니저 재참조
    }

    if (characterInfoManager == null || saveStorage == null)
    {
        return; // 필수 참조가 없으면 종료
    }

    GlobalCharacterDefinition definition = characterInfoManager.FindDefinitionByID(firstRowID, secondRowID); // 캐릭터 정의 탐색
    if (definition == null || definition.DetailUIPrefab == null)
    {
        return; // 상세 UI 프리팹이 없으면 종료
    }

    ClearCharacterDetailUI(); // 기존 상세 UI 제거

    currentDetailUIObject = Instantiate(definition.DetailUIPrefab, detailUIParent); // 상세 UI 생성

    CharacterDetailInfoWindow detailInfoWindow = currentDetailUIObject.GetComponent<CharacterDetailInfoWindow>(); // 상세 UI 스크립트 참조
    if (detailInfoWindow == null)
    {
        return; // 상세 UI 스크립트가 없으면 종료
    }

    SaveStorage.OwnedCharacterStatData statData = saveStorage.FindCurrentOwnedCharacterStatData(firstRowID, secondRowID, individualID); // 저장된 스탯 정보 탐색
    detailInfoWindow.Initialize(statData); // 상세 UI에 스탯 정보 전달
}

private void ClearCharacterDetailUI() // 생성된 상세 UI 삭제
{
    if (currentDetailUIObject != null)
    {
        Destroy(currentDetailUIObject); // 기존 상세 UI 삭제
        currentDetailUIObject = null; // 참조 초기화
    }
}

private void RefreshToggleButtonState() // 관리창 상태에 따라 활성화/비활성화 버튼 표시 상태 갱신
{
    if (openButton != null)
    {
        openButton.gameObject.SetActive(isPanelOpen == false); // 관리창이 닫혀 있을 때만 활성화 버튼 표시
    }

    if (closeButton != null)
    {
        closeButton.gameObject.SetActive(isPanelOpen == true); // 관리창이 열려 있을 때만 비활성화 버튼 표시
    }
}

private void RefreshContentRightValue() // 슬롯 수와 Grid Layout Group 셀 크기 기준으로 Content 오른쪽값 갱신
{
    if (contentRectTransform == null)
    {
        return; // Content RectTransform이 없으면 종료
    }

    if (characterSlotParent == null)
    {
        return; // 슬롯 부모가 없으면 종료
    }

    GridLayoutGroup gridLayoutGroup = characterSlotParent.GetComponent<GridLayoutGroup>(); // 슬롯 부모의 Grid Layout Group 참조

    if (gridLayoutGroup == null)
    {
        return; // Grid Layout Group이 없으면 종료
    }

    int slotCount = createdSlotObjectList.Count; // 현재 생성된 슬롯 수
    float rightValue = slotCount * gridLayoutGroup.cellSize.x; // 슬롯 수 * 셀 X 크기

    Vector2 size = contentRectTransform.sizeDelta; // 현재 Content 너비/높이 값
    size.x = rightValue; // 슬롯 수 * 셀 크기 값을 너비로 적용
    contentRectTransform.sizeDelta = size; // 변경된 너비 적용
}

public void SelectCharacterByID(int firstRowID, int secondRowID) // ID 기준으로 선택 캐릭터 이미지 표시
{
    if (characterInfoManager == null)
    {
        characterInfoManager = CharacterInfoManager.Instance; // 캐릭터 정보 매니저 재참조
    }

    if (characterInfoManager == null)
    {
        ClearCharacterImageUI(); // 참조 실패 시 이미지 UI 초기화
        return;
    }

    GlobalCharacterDefinition definition = characterInfoManager.FindDefinitionByID(firstRowID, secondRowID); // ID에 맞는 캐릭터 정의 탐색

    if (definition == null)
    {
        ClearCharacterImageUI(); // 캐릭터 정의가 없으면 이미지 UI 초기화
        return;
    }

    currentSelectedCharacter = definition; // 현재 선택 캐릭터 저장
    currentImageIndex = 0; // 이미지 인덱스 0으로 초기화

    UpdateCharacterImage(); // 캐릭터 이미지 갱신
}

public void NextCharacterImage() // 다음 캐릭터 비주얼 프리팹 표시
{
    if (currentSelectedCharacter == null)
    {
        return;
    }

    List<GameObject> visualPrefabList = currentSelectedCharacter.CharacterIndexVisualPrefabList; // 현재 캐릭터 비주얼 프리팹 목록

    if (visualPrefabList == null || visualPrefabList.Count == 0)
    {
        ClearCharacterImageUI(); // 비주얼 프리팹이 없으면 UI 초기화
        return;
    }

    currentImageIndex++; // 다음 인덱스로 이동

    if (currentImageIndex >= visualPrefabList.Count)
    {
        currentImageIndex = 0; // 마지막 다음은 0번으로 순환
    }

    UpdateCharacterImage(); // 캐릭터 비주얼 갱신
}

public void PreviousCharacterImage() // 이전 캐릭터 비주얼 프리팹 표시
{
    if (currentSelectedCharacter == null)
    {
        return;
    }

    List<GameObject> visualPrefabList = currentSelectedCharacter.CharacterIndexVisualPrefabList; // 현재 캐릭터 비주얼 프리팹 목록

    if (visualPrefabList == null || visualPrefabList.Count == 0)
    {
        ClearCharacterImageUI(); // 비주얼 프리팹이 없으면 UI 초기화
        return;
    }

    currentImageIndex--; // 이전 인덱스로 이동

    if (currentImageIndex < 0)
    {
        currentImageIndex = visualPrefabList.Count - 1; // 0번 이전은 마지막으로 순환
    }

    UpdateCharacterImage(); // 캐릭터 비주얼 갱신
}

private void UpdateCharacterImage() // 현재 인덱스의 캐릭터 비주얼 프리팹 갱신
{
    if (currentSelectedCharacter == null)
    {
        ClearCharacterImageUI(); // 선택 캐릭터가 없으면 UI 초기화
        return;
    }

    List<GameObject> visualPrefabList = currentSelectedCharacter.CharacterIndexVisualPrefabList; // 현재 캐릭터 비주얼 프리팹 목록

    if (visualPrefabList == null || visualPrefabList.Count == 0)
    {
        ClearCharacterImageUI(); // 비주얼 프리팹 목록이 비었으면 UI 초기화
        return;
    }

    if (currentImageIndex < 0 || currentImageIndex >= visualPrefabList.Count)
    {
        currentImageIndex = 0; // 잘못된 인덱스면 0으로 보정
    }

    GameObject visualPrefab = visualPrefabList[currentImageIndex]; // 현재 생성할 캐릭터 비주얼 프리팹

    if (visualPrefab == null)
    {
        ClearCurrentCharacterVisualObject(); // 기존 생성 비주얼 제거
        SetImageButtonActive(false); // 버튼 비활성화
        return;
    }

    if (characterImageParentObject != null)
    {
        characterImageParentObject.SetActive(true); // 비주얼 부모 오브젝트 활성화
    }

    ClearCurrentCharacterVisualObject(); // 기존 생성된 비주얼 프리팹 제거

    currentCharacterVisualObject = Instantiate(
        visualPrefab,
        characterImageParentObject.transform,
        false); // 프리팹의 UI 위치/크기/회전 설정을 유지한 채 부모 아래 생성

    SetImageButtonActive(true); // 캐릭터 비주얼이 표시되는 동안 버튼 활성화
}

private void ClearCharacterImageUI() // 캐릭터 비주얼 UI 초기화
{
    currentSelectedCharacter = null; // 현재 선택 캐릭터 초기화
    currentImageIndex = 0; // 이미지/비주얼 인덱스 초기화

    ClearCurrentCharacterVisualObject(); // 현재 생성된 캐릭터 비주얼 프리팹 제거

    if (characterImageParentObject != null)
    {
        characterImageParentObject.SetActive(false); // 비주얼 부모 오브젝트 비활성화
    }

    SetImageButtonActive(false); // 이전/다음 버튼 비활성화
}

private void SetImageButtonActive(bool isActive) // 이미지 이전/다음 버튼 활성 상태 적용
{
    if (previousImageButton != null)
    {
        previousImageButton.gameObject.SetActive(isActive); // 이전 버튼 오브젝트 활성 상태 적용
    }

    if (nextImageButton != null)
    {
        nextImageButton.gameObject.SetActive(isActive); // 다음 버튼 오브젝트 활성 상태 적용
    }
}

private void ClearCurrentCharacterVisualObject() // 현재 생성된 캐릭터 비주얼 프리팹 제거
{
    if (currentCharacterVisualObject != null)
    {
        Destroy(currentCharacterVisualObject); // 기존 비주얼 프리팹 삭제
        currentCharacterVisualObject = null; // 참조 초기화
    }
}



}