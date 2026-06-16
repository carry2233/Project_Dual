using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;





/// <summary>
/// 엔딩 화면 관리 스크립트
/// - SaveStorage의 현재 세이브 상태를 기준으로 엔딩 타입 결정
/// - 살아있는 아군의 인벤토리 총 가치값 계산
/// - 엔딩별 부모 오브젝트 활성화
/// </summary>
public class EndingManager : MonoBehaviour
{
    public enum EndingType
    {
        AllFriendlyAliveReturn, // 아군 전원생존 귀환
        SomeFriendlyDeadReturn, // 아군 일부사망 귀환
        AllFriendlyDead // 전원사망
    }

[Header("현재 담당 세이브 정보")]

[SerializeField] private SaveStorage saveStorage; // 현재 엔딩 계산에 사용할 SaveStorage 참조

[SerializeField] private int currentSaveId = -1; // 현재 선택된 세이브 ID

[SerializeField] private string currentSaveName; // 현재 선택된 세이브 이름


[Header("현재 진입한 엔딩 타입")]

[SerializeField] private EndingType currentEndingType; // 현재 실행된 엔딩 종류


[Header("스코어")]

[SerializeField] private int currentTotalAliveItemValue; // 살아있는 아군들의 모든 아이템 총 가치값


[Header("스코어 표시 텍스트")]

[SerializeField] private List<TMP_Text> scoreTextList = new List<TMP_Text>(); // 계산된 스코어를 표시할 TMP 텍스트 목록


[Header("엔딩별 부모 오브젝트")]

[SerializeField] private GameObject allFriendlyAliveReturnEndingParent; // 아군 전원 생존 귀환 엔딩 부모 오브젝트

[SerializeField] private GameObject someFriendlyDeadReturnEndingParent; // 아군 일부 사망 귀환 엔딩 부모 오브젝트

[SerializeField] private GameObject allFriendlyDeadEndingParent; // 아군 전원 사망 엔딩 부모 오브젝트


[Header("____________________________________________________")]


[Header("엔딩 진입 시점 세이브 복사 데이터")]
[SerializeField] private List<SaveStorage.OwnedCharacterData> endingOwnedCharacterList = new List<SaveStorage.OwnedCharacterData>(); // 엔딩 계산용 소유 캐릭터 복사본
[SerializeField] private List<SaveStorage.OwnedCharacterInventorySaveData> endingOwnedCharacterInventoryList = new List<SaveStorage.OwnedCharacterInventorySaveData>(); // 엔딩 계산용 인벤토리 복사본
[SerializeField] private bool endingSaveSomeFriendlyDead; // 엔딩 진입 시점 일부 사망 여부
[SerializeField] private bool endingSaveAllFriendlyDead; // 엔딩 진입 시점 전원 사망 여부

[Header("메인씬 이동")]
[SerializeField] private List<Button> mainSceneMoveButtonList = new List<Button>(); // 메인씬 이동 버튼 목록
[SerializeField] private string mainSceneName; // 이동할 메인씬 이름

[Header("엔딩 후 세이브 삭제")]
[SerializeField] private bool deleteSaveDataAfterEndingExecuted = true; // 엔딩 실행 후 현재 세이브 삭제 여부
[SerializeField] private bool hasDeletedSaveDataAfterEnding; // 엔딩 후 세이브 삭제 완료 여부

private void Start()
{
    if (saveStorage == null)
    {
        saveStorage = SaveStorage.Instance;
    }

    RegisterMainSceneMoveButtons();

    RefreshCurrentSaveInfo();
    DecideEndingType();
    ApplyEndingObjectState();
    CalculateAliveFriendlyItemValue();
    RefreshScoreTexts();
    DeleteEndingSaveDataIfNeeded();
}

private void OnDestroy()
{
    UnregisterMainSceneMoveButtons();
}

private void RefreshCurrentSaveInfo()
{
    if (saveStorage == null)
    {
        return;
    }

    currentSaveId = saveStorage.CurrentSelectedSaveId;

    SaveStorage.SaveEntry currentSaveEntry = saveStorage.GetCurrentSelectedSaveEntryCopy();

    if (currentSaveEntry == null)
    {
        currentSaveName = string.Empty;
        endingOwnedCharacterList = new List<SaveStorage.OwnedCharacterData>();
        endingOwnedCharacterInventoryList = new List<SaveStorage.OwnedCharacterInventorySaveData>();
        endingSaveSomeFriendlyDead = false;
        endingSaveAllFriendlyDead = false;
        return;
    }

    currentSaveName = currentSaveEntry.saveName;

    endingOwnedCharacterList = currentSaveEntry.ownedCharacterList;
    endingOwnedCharacterInventoryList = currentSaveEntry.ownedCharacterInventoryList;

    endingSaveSomeFriendlyDead = currentSaveEntry.hasSomeFriendlyDead;
    endingSaveAllFriendlyDead = currentSaveEntry.hasAllFriendlyDead;
}

private void DecideEndingType()
{
    if (endingSaveAllFriendlyDead)
    {
        currentEndingType = EndingType.AllFriendlyDead;
        return;
    }

    if (endingSaveSomeFriendlyDead)
    {
        currentEndingType = EndingType.SomeFriendlyDeadReturn;
        return;
    }

    currentEndingType = EndingType.AllFriendlyAliveReturn;
}

    private void ApplyEndingObjectState()
    {
        if (allFriendlyAliveReturnEndingParent != null)
        {
            allFriendlyAliveReturnEndingParent.SetActive(currentEndingType == EndingType.AllFriendlyAliveReturn);
        }

        if (someFriendlyDeadReturnEndingParent != null)
        {
            someFriendlyDeadReturnEndingParent.SetActive(currentEndingType == EndingType.SomeFriendlyDeadReturn);
        }

        if (allFriendlyDeadEndingParent != null)
        {
            allFriendlyDeadEndingParent.SetActive(currentEndingType == EndingType.AllFriendlyDead);
        }
    }

private void CalculateAliveFriendlyItemValue()
{
    currentTotalAliveItemValue = 0;

    if (endingOwnedCharacterInventoryList == null)
    {
        return;
    }

    for (int i = 0; i < endingOwnedCharacterInventoryList.Count; i++)
    {
        SaveStorage.OwnedCharacterInventorySaveData inventoryData = endingOwnedCharacterInventoryList[i];

        if (inventoryData == null)
        {
            continue;
        }

        bool isDead = IsEndingOwnedCharacterDead(
            inventoryData.firstRowID,
            inventoryData.secondRowID,
            inventoryData.individualID);

        if (isDead)
        {
            continue;
        }

        if (inventoryData.items == null)
        {
            continue;
        }

        for (int j = 0; j < inventoryData.items.Count; j++)
        {
            SaveStorage.OwnedCharacterInventoryItemSaveData itemData = inventoryData.items[j];

            if (itemData == null || itemData.count <= 0)
            {
                continue;
            }

            GlobalItemDefinition itemDefinition = null;

            if (ItemDefinitionList.Instance != null)
            {
                itemDefinition = ItemDefinitionList.Instance.GetItemDefinition(itemData.itemAID, itemData.itemBID);
            }

            if (itemDefinition == null)
            {
                continue;
            }

            currentTotalAliveItemValue += itemDefinition.itemValue * itemData.count;
        }
    }
}

private bool IsEndingOwnedCharacterDead(int firstRowID, int secondRowID, int individualID) // 엔딩 진입 시점 복사 데이터 기준 사망 여부 확인
{
    if (endingOwnedCharacterList == null)
    {
        return false;
    }

    for (int i = 0; i < endingOwnedCharacterList.Count; i++)
    {
        SaveStorage.OwnedCharacterData ownedData = endingOwnedCharacterList[i];

        if (ownedData == null)
        {
            continue;
        }

        if (ownedData.firstRowID == firstRowID &&
            ownedData.secondRowID == secondRowID &&
            ownedData.individualID == individualID)
        {
            return ownedData.isDead;
        }
    }

    return false;
}

    private void RefreshScoreTexts()
    {
        for (int i = 0; i < scoreTextList.Count; i++)
        {
            TMP_Text scoreText = scoreTextList[i];

            if (scoreText == null)
            {
                continue;
            }

            scoreText.text = currentTotalAliveItemValue.ToString();
        }
    }

    private void DeleteEndingSaveDataIfNeeded() // 엔딩 실행 후 현재 세이브 데이터 삭제
{
    if (!deleteSaveDataAfterEndingExecuted)
    {
        return;
    }

    if (hasDeletedSaveDataAfterEnding)
    {
        return;
    }

    if (saveStorage == null)
    {
        return;
    }

    if (currentSaveId <= 0)
    {
        return;
    }

    bool deleted = saveStorage.DeleteCurrentSelectedSave();

    if (deleted)
    {
        hasDeletedSaveDataAfterEnding = true;
    }
}

private void RegisterMainSceneMoveButtons() // 메인씬 이동 버튼 이벤트 등록
{
    for (int i = 0; i < mainSceneMoveButtonList.Count; i++)
    {
        Button button = mainSceneMoveButtonList[i];

        if (button == null)
        {
            continue;
        }

        button.onClick.AddListener(MoveToMainScene);
    }
}

private void UnregisterMainSceneMoveButtons() // 메인씬 이동 버튼 이벤트 해제
{
    for (int i = 0; i < mainSceneMoveButtonList.Count; i++)
    {
        Button button = mainSceneMoveButtonList[i];

        if (button == null)
        {
            continue;
        }

        button.onClick.RemoveListener(MoveToMainScene);
    }
}

public void MoveToMainScene() // 설정된 메인씬으로 이동
{
    if (string.IsNullOrEmpty(mainSceneName))
    {
        return;
    }

    SceneManager.LoadScene(mainSceneName);
}








}