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
    [SerializeField] private Button toggleButton; // 캐릭터 관리창 토글 버튼
    [SerializeField] private GameObject characterManagementPanelObject; // 캐릭터 관리창 패널 오브젝트

    [Header("외부 참조")]
    [SerializeField] private TileSelectionManager tileSelectionManager; // 타일 선택 및 월드맵 잠금 제어 스크립트

    [Header("관리창 슬롯 생성 참조")]
[SerializeField] private Transform characterSlotParent; // Grid Layout Group이 적용된 슬롯 부모 오브젝트

[Header("외부 데이터 참조")]
[SerializeField] private CharacterInfoManager characterInfoManager; // 캐릭터 정보 매니저 참조
[SerializeField] private SaveStorage saveStorage; // 저장 데이터 매니저 참조

private readonly List<GameObject> createdSlotObjectList = new List<GameObject>(); // 현재 생성된 관리창 슬롯 목록

    private bool isPanelOpen = false; // 현재 패널 열림 상태

    public bool IsPanelOpen => isPanelOpen; // 현재 패널 열림 상태 외부 확인용

private void Awake() // 시작 전 버튼 연결 및 참조 초기화
{
    if (toggleButton != null)
    {
        toggleButton.onClick.AddListener(ToggleCharacterManagementPanel); // 버튼 클릭 시 토글 연결
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

    private void OnDestroy() // 종료 시 버튼 이벤트 해제
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveListener(ToggleCharacterManagementPanel); // 버튼 이벤트 해제
        }
    }

    public void ToggleCharacterManagementPanel() // 캐릭터 관리창 패널 토글
    {
        ApplyPanelState(isPanelOpen == false); // 현재 상태를 반전하여 적용
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
        tempSlotList.Add(slotObject); // 임시 목록에 추가
    }

    tempSlotList.Sort(CompareSlotPriority); // 우선순위 기준 정렬

    for (int i = 0; i < tempSlotList.Count; i++)
    {
        tempSlotList[i].transform.SetSiblingIndex(i); // Grid Layout Group 배치 순서 적용
        createdSlotObjectList.Add(tempSlotList[i]); // 생성 슬롯 목록에 등록
    }
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
}
}