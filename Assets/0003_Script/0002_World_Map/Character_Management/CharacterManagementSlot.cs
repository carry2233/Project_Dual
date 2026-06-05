using UnityEngine; // Unity 기본 기능
using UnityEngine.UI; // 버튼 참조용

/// <summary>
/// 캐릭터 관리창 슬롯 정보 스크립트
/// - 관리창 캐릭터 슬롯 프리팹에 붙임
/// - 슬롯 나열 우선순위값을 제공
/// - CharacterManagementManager가 이 값을 기준으로 작은 순서대로 배치
/// </summary>
public class CharacterManagementSlot : MonoBehaviour
{
    [Header("슬롯 정렬 설정")]
    [SerializeField] private int slotSortPriority = 0; // 슬롯 나열 우선순위값

    [Header("클릭 버튼 참조")]
[SerializeField] private Button slotButton; // 슬롯 클릭을 인식할 버튼 컴포넌트

[Header("사망 표시 UI")]
[SerializeField] private GameObject deathMarkUIObject; // 사망한 캐릭터임을 표시할 UI 오브젝트

private bool isDead; // 슬롯 담당 캐릭터 사망 여부

private CharacterManagementManager characterManagementManager; // 캐릭터 관리창 매니저 참조
private int firstRowID; // 슬롯이 담당하는 캐릭터 첫 번째 행 ID
private int secondRowID; // 슬롯이 담당하는 캐릭터 두 번째 행 ID
private int individualID; // 슬롯이 담당하는 캐릭터 개체별 고유 ID

    public int SlotSortPriority => slotSortPriority; // 슬롯 나열 우선순위값 반환

    private void Awake() // 시작 전 버튼 이벤트 연결
{
    if (slotButton != null)
    {
        slotButton.onClick.AddListener(OnClickSlotButton); // 버튼 클릭 시 슬롯 선택 처리
    }
}

private void OnDestroy() // 삭제 시 버튼 이벤트 해제
{
    if (slotButton != null)
    {
        slotButton.onClick.RemoveListener(OnClickSlotButton); // 버튼 이벤트 해제
    }
}

public void Initialize(CharacterManagementManager newManager, int newFirstRowID, int newSecondRowID, int newIndividualID, bool newIsDead) // 슬롯 담당 캐릭터 정보 초기화
{
    characterManagementManager = newManager; // 관리창 매니저 저장
    firstRowID = newFirstRowID; // 첫 번째 행 ID 저장
    secondRowID = newSecondRowID; // 두 번째 행 ID 저장
    individualID = newIndividualID; // 개체별 고유 ID 저장
    isDead = newIsDead; // 사망 여부 저장

    RefreshDeathMarkUI(); // 사망 표시 UI 갱신
}

private void OnClickSlotButton() // 슬롯 버튼 클릭 처리
{
    if (characterManagementManager == null)
    {
        return; // 매니저가 없으면 종료
    }

    characterManagementManager.SelectCharacterByID(firstRowID, secondRowID); // 선택 캐릭터 이미지 표시 요청

    characterManagementManager.OpenCharacterDetailUI(firstRowID, secondRowID, individualID); // 상세 UI 생성 요청
}

private void RefreshDeathMarkUI() // 사망 표시 UI 갱신
{
    if (deathMarkUIObject == null)
    {
        return; // 사망 표시 UI가 없으면 종료
    }

    deathMarkUIObject.SetActive(isDead); // 사망 상태면 활성화, 아니면 비활성화
}





}