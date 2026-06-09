using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Random = UnityEngine.Random;


/// <summary>
/// 저장본 생성, 삭제, 불러오기, 현재 선택 저장본 ID 관리,
/// 소유 캐릭터 목록, 캐릭터별 스탯정보, 캐릭터별 인벤토리 저장/복원을 담당한다.
/// </summary>
public class SaveStorage : MonoBehaviour
{

    [Serializable]
public class BattleEventRuntimeData
{
    public int eventID; // 전투 이벤트 ID
    public int minEnemySpawnCount; // 최소 적 생성 수
    public int maxEnemySpawnCount; // 최대 적 생성 수
    public int enemyLevelCorrectionValue; // 적 레벨 보정값
    public float minEnemySpawnDelay; // 최소 적 생성 딜레이
    public float maxEnemySpawnDelay; // 최대 적 생성 딜레이
    public List<GlobalCharacterDefinition> spawnableEnemyList = new List<GlobalCharacterDefinition>(); // 생성 가능한 적 목록

    public int minBattleRequiredMinute; // 최소 전투 소요 시간
    public int maxBattleRequiredMinute; // 최대 전투 소요 시간
}

[Serializable]
private class SaveFileData
{
    public int nextSaveId = 1; // 다음에 부여할 고유 ID
    public List<SaveEntry> saveList = new List<SaveEntry>(); // 저장본 목록
}

[Serializable]
public class NotepadPageData
{
    public int pageIndex; // 메모장 페이지 인덱스
    public string pageText; // 해당 페이지의 텍스트
}

[Header("저장 설정")]
[SerializeField] private string saveFileName = "save_data.json"; // 저장 파일 이름
[SerializeField] private int maxSaveCount = 20; // 최대 저장본 개수

[Header("타일 배치 저장 설정")]
[SerializeField] private string tileLayoutSaveFilePrefix = "HexTilePlacementSave_"; // 타일 배치 저장 파일 접두사

[Serializable]
public class OwnedCharacterData
{
    public int firstRowID; // 캐릭터 첫 번째 행 ID
    public int secondRowID; // 캐릭터 두 번째 행 ID
    public int individualID; // 캐릭터 개체별 고유 ID
    public bool isDead; // 캐릭터 사망 여부
}

[Serializable] 
public class OwnedCharacterStatData
{
    [Header("캐릭터 이름")]
    public string characterName; // 캐릭터 이름

    [Header("캐릭터 ID")]
    public int firstRowID; // 캐릭터 첫 번째 행 ID
    public int secondRowID; // 캐릭터 두 번째 행 ID
    public int individualID; // 캐릭터 개체별 고유 ID
    

    [Header("경험치")]
    public int currentExperience; // 현재 경험치
    public int levelUpRequiredExperience; // 레벨업 충족 경험치

    [Header("레벨")]
    public int levelstats; // 레벨 수치

    [Header("이속")]
    public float baseMoveSpeed; // 기본 이동속도
    public int moveSpeedPercent; // 이동속도 퍼센트
    public float finalMoveSpeed; // 최종 이동속도

    [Header("전투 스탯")]
    public int attackPower; // 공격력
    public int defenseValue; // 방어력
    public int maxHealth; // 최대 체력
    public int currentHealth; // 현재 체력

    [Header("체급")]
    public int bodySize; // 체급

    [Header("속도")]
    public int speedStat; // 속도 수치

    [Header("위력률")]
    public int powerRatePercent; // 위력률 퍼센트

    [Header("와해 스탯")]
    public int maxStaggerAmount; // 최대 와해량
    public int currentStaggerAmount; // 현재 와해량
    public int staggerResistancePercent; // 와해 저항률
    
    [Header("허기 스탯")]
    public int maxHunger; // 최대 허기
    public int currentHunger; // 현재 허기
    public bool isStarving; // 공복 여부

    [Header("무게 디버프")]
    public int overweightDebuffStackCount; // 무게 디버프 중첩값
    
}

[Serializable]
public class OwnedCharacterInventoryItemSaveData
{
    public int itemAID; // 아이템 A ID
    public int itemBID; // 아이템 B ID
    public int count; // 저장 개수
}

[Serializable]
public class OwnedCharacterInventorySaveData
{
    public int firstRowID; // 담당 캐릭터 첫 번째 행 ID
    public int secondRowID; // 담당 캐릭터 두 번째 행 ID
    public int individualID; // 담당 캐릭터 개체별 고유 ID
    public List<OwnedCharacterInventoryItemSaveData> items = new List<OwnedCharacterInventoryItemSaveData>(); // 캐릭터 인벤토리 아이템 목록
}

[Serializable]
public class FriendlyNigrumSaveData
{
    public FriendlyCharacterDefinition friendlyCharacterDefinition; // 해당될 아군 정의 에셋
    public int currentNigrumCapacity; // 현재 흑체 수용량
    public int nigrumDecreaseRemainMinute; // 흑체 감소 충족 잔여 분
}

[Serializable]
public class SoundVolumeSaveData
{
    [Range(0, 100)] public int masterVolume = 100; // 전체 사운드 볼륨
    [Range(0, 100)] public int effectVolume = 100; // 효과음 볼륨
    [Range(0, 100)] public int voiceVolume = 100; // 음성 볼륨
    [Range(0, 100)] public int bgmVolume = 100; // 음악/BGM 볼륨
    [Range(0, 100)] public int uiSoundVolume = 100; // UI 사운드 볼륨
}


[Serializable]
public class SaveEntry
{
    public int saveId; // 저장본 고유 ID
    public string saveName; // 저장본 이름
    public int saveNumber; // 화면 표시용 번호
    public List<OwnedCharacterData> ownedCharacterList = new List<OwnedCharacterData>(); // 이 저장본에 소속된 캐릭터 목록
    public List<OwnedCharacterStatData> ownedCharacterStatList = new List<OwnedCharacterStatData>(); // 이 저장본에 소속된 캐릭터 스탯정보 목록
    public List<OwnedCharacterInventorySaveData> ownedCharacterInventoryList = new List<OwnedCharacterInventorySaveData>(); // 이 저장본에 소속된 캐릭터 인벤토리 목록
    public List<NotepadPageData> notepadPageList = new List<NotepadPageData>(); // 이 저장본에 소속된 메모장 페이지 목록
    public List<FriendlyNigrumSaveData> friendlyNigrumSaveList = new List<FriendlyNigrumSaveData>(); // 이 저장본에 소속된 아군 흑체 저장 목록
    public SoundVolumeSaveData soundVolumeSaveData = new SoundVolumeSaveData(); // 이 저장본에 소속된 소리 설정

    public int currentDay; // 현재 일차
    public int currentHour; // 현재 시간
    public int currentMinute; // 현재 분
}


[Header("현재 소유 캐릭터 목록")] 
[SerializeField] private List<OwnedCharacterData> currentOwnedCharacterList = new List<OwnedCharacterData>(); // 현재 세이브 기준으로 사용 중인 소유 캐릭터 목록

[Header("현재 소유 캐릭터 스탯정보 목록")]
[SerializeField] private List<OwnedCharacterStatData> currentOwnedCharacterStatList = new List<OwnedCharacterStatData>(); // 현재 세이브 기준 캐릭터 스탯정보 목록

[Header("현재 소유 캐릭터 인벤토리 목록")]
[SerializeField] private List<OwnedCharacterInventorySaveData> currentOwnedCharacterInventoryList = new List<OwnedCharacterInventorySaveData>(); // 현재 세이브 기준 캐릭터 인벤토리 목록

[Header("현재 아군 흑체 저장 목록")]
[SerializeField] private List<FriendlyNigrumSaveData> currentFriendlyNigrumSaveList = new List<FriendlyNigrumSaveData>(); // 현재 세이브 기준 아군 흑체 저장 목록

public List<FriendlyNigrumSaveData> CurrentFriendlyNigrumSaveList => GetFriendlyNigrumSaveListCopy(currentFriendlyNigrumSaveList); // 현재 아군 흑체 목록 복사 반환

[Header("현재 소리 설정")]
[SerializeField] private SoundVolumeSaveData currentSoundVolumeSaveData = new SoundVolumeSaveData(); // 현재 세이브 기준 소리 설정

public SoundVolumeSaveData CurrentSoundVolumeSaveData => GetSoundVolumeSaveDataCopy(currentSoundVolumeSaveData); // 현재 소리 설정 복사 반환

[Header("현재 전투 이벤트 런타임 데이터")]
[SerializeField] private BattleEventRuntimeData currentBattleEventRuntimeData; // 씬 이동용 전투 이벤트 임시 데이터

[Header("직전 전투 이벤트 상태")]
[SerializeField] private int lastExecutedEventID = -1; // 직전에 실행한 이벤트 ID
[SerializeField] private bool hasExecutedBattle; // 전투 수행 여부

public int LastExecutedEventID => lastExecutedEventID; // 직전 실행 이벤트 ID 반환
public bool HasExecutedBattle => hasExecutedBattle; // 전투 수행 여부 반환

public bool HasBattleEventRuntimeData => currentBattleEventRuntimeData != null; // 전투 이벤트 데이터 존재 여부

public List<OwnedCharacterInventorySaveData> CurrentOwnedCharacterInventoryList => GetOwnedCharacterInventoryListCopy(currentOwnedCharacterInventoryList); // 현재 캐릭터 인벤토리 목록 복사 반환
public List<OwnedCharacterStatData> CurrentOwnedCharacterStatList => GetOwnedCharacterStatListCopy(currentOwnedCharacterStatList); // 현재 캐릭터 스탯정보 목록 복사 반환


public List<OwnedCharacterData> CurrentOwnedCharacterList => GetOwnedCharacterListCopy(currentOwnedCharacterList); // 현재 소유 캐릭터 목록 복사 


private static SaveStorage instance; // 전역 인스턴스
private SaveFileData currentSaveFileData = new SaveFileData(); // 현재 저장 파일 데이터
private int currentSelectedSaveId = -1; // 현재 선택된 저장본 ID

public static SaveStorage Instance => instance; // 전역 인스턴스 반환
public int CurrentSelectedSaveId => currentSelectedSaveId; // 현재 선택된 저장본 ID 반환
public int MaxSaveCount => maxSaveCount; // 최대 저장본 개수 반환

private void Awake() // 시작 시 전역 인스턴스 및 저장 데이터 로드
{
    if (instance != null && instance != this)
    {
        Destroy(gameObject); // 중복 인스턴스 제거
        return;
    }

    instance = this; // 전역 인스턴스 등록
    DontDestroyOnLoad(gameObject); // 씬 전환 시 유지
    LoadFromFile(); // 파일에서 저장 데이터 불러오기
}

public List<SaveEntry> GetSaveList() // 저장본 목록 복사 반환
{
    List<SaveEntry> result = new List<SaveEntry>(); // 반환용 리스트

    for (int i = 0; i < currentSaveFileData.saveList.Count; i++)
    {
        SaveEntry source = currentSaveFileData.saveList[i]; // 원본 저장본 참조

        SaveEntry copy = new SaveEntry(); // 복사용 저장본 생성
        copy.saveId = source.saveId; // 고유 ID 복사
        copy.saveName = source.saveName; // 이름 복사
        copy.saveNumber = source.saveNumber; // 번호 복사
        copy.ownedCharacterList = GetOwnedCharacterListCopy(source.ownedCharacterList); // 소유 캐릭터 목록 복사
        copy.ownedCharacterStatList = GetOwnedCharacterStatListCopy(source.ownedCharacterStatList); // 소유 캐릭터 스탯정보 목록 복사
        copy.notepadPageList = GetNotepadPageListCopy(source.notepadPageList); // 메모장 페이지 목록 복사
        copy.ownedCharacterInventoryList = GetOwnedCharacterInventoryListCopy(source.ownedCharacterInventoryList); // 소유 캐릭터 인벤토리 목록 복사
        copy.friendlyNigrumSaveList = GetFriendlyNigrumSaveListCopy(source.friendlyNigrumSaveList); // 아군 흑체 저장 목록 복사
        copy.soundVolumeSaveData = GetSoundVolumeSaveDataCopy(source.soundVolumeSaveData); // 소리 설정 복사
        copy.currentDay = source.currentDay; // 현재 일차 복사
        copy.currentHour = source.currentHour; // 현재 시간 복사
        copy.currentMinute = source.currentMinute; // 현재 분 복사
        

        result.Add(copy); // 복사본 추가
    }

    
    result.Sort((a, b) => a.saveNumber.CompareTo(b.saveNumber)); // 번호 기준 정렬
    return result; // 정렬된 리스트 반환
}

    public bool CanCreateNewSave() // 새 저장본 생성 가능 여부 확인
    {
        return currentSaveFileData.saveList.Count < maxSaveCount; // 최대 개수 미만이면 생성 가능
    }

public bool CreateSave(
    string newSaveName,
    List<OwnedCharacterData> startOwnedCharacterList,
    List<OwnedCharacterStatData> startOwnedCharacterStatList,
    List<OwnedCharacterInventorySaveData> startOwnedCharacterInventoryList,
    List<FriendlyNigrumSaveData> startFriendlyNigrumSaveList,
    SoundVolumeSaveData startSoundVolumeSaveData,
    int startHour,
    int startMinute,
    int startDay) // 새 저장본 생성
{
    if (!CanCreateNewSave()) return false; // 최대 개수면 생성 불가

    string trimmedName = newSaveName == null ? string.Empty : newSaveName.Trim(); // 공백 제거 이름 생성
    if (string.IsNullOrEmpty(trimmedName)) return false; // 이름이 비어 있으면 생성 불가

    SaveEntry newEntry = new SaveEntry(); // 새 저장본 생성
    newEntry.saveId = currentSaveFileData.nextSaveId; // 고유 ID 부여
    newEntry.saveName = trimmedName; // 저장본 이름 설정
    newEntry.saveNumber = currentSaveFileData.saveList.Count + 1; // 표시용 번호 설정
    newEntry.ownedCharacterList = GetOwnedCharacterListCopy(startOwnedCharacterList); // 시작 캐릭터 목록 저장
    newEntry.ownedCharacterStatList = GetOwnedCharacterStatListCopy(startOwnedCharacterStatList); // 시작 캐릭터 스탯정보 저장
    newEntry.ownedCharacterInventoryList = GetOwnedCharacterInventoryListCopy(startOwnedCharacterInventoryList); // 시작 캐릭터 인벤토리 저장
    newEntry.friendlyNigrumSaveList = GetFriendlyNigrumSaveListCopy(startFriendlyNigrumSaveList); // 시작 아군 흑체 저장 목록 저장
    newEntry.soundVolumeSaveData = GetSoundVolumeSaveDataCopy(startSoundVolumeSaveData); // 시작 소리 설정 저장
    newEntry.notepadPageList = new List<NotepadPageData>(); // 새 세이브의 메모장 페이지 목록 초기화
    newEntry.currentDay = Mathf.Max(0, startDay); // 시작 일차 저장
    newEntry.currentHour = Mathf.Clamp(startHour, 0, 23); // 시작 시간 저장
    newEntry.currentMinute = Mathf.Clamp(startMinute, 0, 59); // 시작 분 저장

    currentSaveFileData.nextSaveId++; // 다음 고유 ID 증가
    currentSaveFileData.saveList.Add(newEntry); // 목록에 저장본 추가

    ApplyCurrentOwnedCharacterList(newEntry.ownedCharacterList); // 현재 소유 캐릭터 목록 적용
    ApplyCurrentOwnedCharacterStatList(newEntry.ownedCharacterStatList); // 현재 소유 캐릭터 스탯정보 적용
    ApplyCurrentOwnedCharacterInventoryList(newEntry.ownedCharacterInventoryList); // 현재 캐릭터 인벤토리 적용
    ApplyCurrentFriendlyNigrumSaveList(newEntry.friendlyNigrumSaveList); // 현재 아군 흑체 저장 목록 적용
    ApplyCurrentSoundVolumeSaveData(newEntry.soundVolumeSaveData); // 현재 소리 설정 적용

    SortAndReindex(); // 번호 정렬 및 재정렬
    SaveToFile(); // 파일 저장

    return true; // 생성 성공 반환
}
    public bool DeleteSaveByNumber(int targetSaveNumber) // 번호 기준 저장본 삭제
    {
        int removeIndex = -1; // 삭제할 인덱스

        for (int i = 0; i < currentSaveFileData.saveList.Count; i++)
        {
            if (currentSaveFileData.saveList[i].saveNumber == targetSaveNumber)
            {
                removeIndex = i; // 삭제 대상 인덱스 기록
                break; // 탐색 종료
            }
        }

        if (removeIndex < 0) return false; // 삭제 대상이 없으면 실패

        currentSaveFileData.saveList.RemoveAt(removeIndex); // 저장본 삭제
        SortAndReindex(); // 번호 재정렬
        SaveToFile(); // 파일 저장

        return true; // 삭제 성공 반환
    }

public void LoadFromFile() // 파일에서 저장 데이터 불러오기
{
    string path = GetSaveFilePath(); // 저장 파일 경로 가져오기

if (!File.Exists(path))
{
    currentSaveFileData = new SaveFileData(); // 파일이 없으면 새 데이터 생성
    currentOwnedCharacterList = new List<OwnedCharacterData>(); // 현재 소유 캐릭터 목록 초기화
    currentOwnedCharacterStatList = new List<OwnedCharacterStatData>(); // 현재 캐릭터 스탯정보 목록 초기화
    currentOwnedCharacterInventoryList = new List<OwnedCharacterInventorySaveData>(); // 현재 캐릭터 인벤토리 목록 초기화
    currentFriendlyNigrumSaveList = new List<FriendlyNigrumSaveData>(); // 현재 아군 흑체 저장 목록 초기화
    currentSoundVolumeSaveData = new SoundVolumeSaveData(); // 현재 소리 설정 초기화
    SaveToFile(); // 빈 파일 저장
    return; // 로드 종료
}

    string json = File.ReadAllText(path); // 파일 텍스트 읽기

if (string.IsNullOrEmpty(json))
{
    currentSaveFileData = new SaveFileData(); // 비어 있으면 새 데이터 생성
    currentOwnedCharacterList = new List<OwnedCharacterData>(); // 현재 소유 캐릭터 목록 초기화
    currentOwnedCharacterStatList = new List<OwnedCharacterStatData>(); // 현재 캐릭터 스탯정보 목록 초기화
    currentOwnedCharacterInventoryList = new List<OwnedCharacterInventorySaveData>(); // 현재 캐릭터 인벤토리 목록 초기화
    currentFriendlyNigrumSaveList = new List<FriendlyNigrumSaveData>(); // 현재 아군 흑체 저장 목록 초기화
    SaveToFile(); // 빈 파일 저장
    return; // 로드 종료
}

    SaveFileData loadedData = JsonUtility.FromJson<SaveFileData>(json); // JSON 역직렬화

if (loadedData == null)
{
    currentSaveFileData = new SaveFileData(); // 실패 시 새 데이터 생성
    currentOwnedCharacterList = new List<OwnedCharacterData>(); // 현재 소유 캐릭터 목록 초기화
    currentOwnedCharacterStatList = new List<OwnedCharacterStatData>(); // 현재 캐릭터 스탯정보 목록 초기화
    currentOwnedCharacterInventoryList = new List<OwnedCharacterInventorySaveData>(); // 현재 캐릭터 인벤토리 목록 초기화
    currentFriendlyNigrumSaveList = new List<FriendlyNigrumSaveData>(); // 현재 아군 흑체 저장 목록 초기화
    SaveToFile(); // 빈 파일 저장
    return; // 로드 종료
}

    currentSaveFileData = loadedData; // 로드 데이터 적용

    if (currentSaveFileData.saveList == null)
    {
        currentSaveFileData.saveList = new List<SaveEntry>(); // 리스트 null 방지
    }

    if (currentSaveFileData.nextSaveId <= 0)
    {
        currentSaveFileData.nextSaveId = 1; // 고유 ID 시작값 보정
    }

    for (int i = 0; i < currentSaveFileData.saveList.Count; i++)
    {
        if (currentSaveFileData.saveList[i].ownedCharacterList == null)
        {
            currentSaveFileData.saveList[i].ownedCharacterList = new List<OwnedCharacterData>(); // 캐릭터 목록 null 방지
        }

        if (currentSaveFileData.saveList[i].ownedCharacterStatList == null)
        {
            currentSaveFileData.saveList[i].ownedCharacterStatList = new List<OwnedCharacterStatData>(); // 캐릭터 스탯정보 목록 null 방지
        }

        if (currentSaveFileData.saveList[i].ownedCharacterInventoryList == null)
        {
            currentSaveFileData.saveList[i].ownedCharacterInventoryList = new List<OwnedCharacterInventorySaveData>(); // 캐릭터 인벤토리 목록 null 방지
        }

        if (currentSaveFileData.saveList[i].friendlyNigrumSaveList == null)
        {
            currentSaveFileData.saveList[i].friendlyNigrumSaveList = new List<FriendlyNigrumSaveData>(); // 아군 흑체 저장 목록 null 방지
        }

        if (currentSaveFileData.saveList[i].soundVolumeSaveData == null)
        {
            currentSaveFileData.saveList[i].soundVolumeSaveData = new SoundVolumeSaveData(); // 소리 설정 null 방지
        }

        if (currentSaveFileData.saveList[i].notepadPageList == null)
        {
            currentSaveFileData.saveList[i].notepadPageList = new List<NotepadPageData>(); // 메모장 페이지 목록 null 방지
        }

        if (currentSaveFileData.saveList[i].saveId <= 0)
        {
            currentSaveFileData.saveList[i].saveId = currentSaveFileData.nextSaveId; // 잘못된 ID 보정
            currentSaveFileData.nextSaveId++; // 다음 ID 증가
        }

        currentSaveFileData.saveList[i].currentDay = Mathf.Max(0, currentSaveFileData.saveList[i].currentDay); // 일차 음수 방지
        currentSaveFileData.saveList[i].currentHour = Mathf.Clamp(currentSaveFileData.saveList[i].currentHour, 0, 23); // 시간 범위 보정
        currentSaveFileData.saveList[i].currentMinute = Mathf.Clamp(currentSaveFileData.saveList[i].currentMinute, 0, 59); // 분 범위 보정
    }
    

currentOwnedCharacterList = new List<OwnedCharacterData>(); // 현재 소유 캐릭터 목록 초기화
currentOwnedCharacterStatList = new List<OwnedCharacterStatData>(); // 현재 캐릭터 스탯정보 목록 초기화
currentOwnedCharacterInventoryList = new List<OwnedCharacterInventorySaveData>(); // 현재 캐릭터 인벤토리 목록 초기화
currentFriendlyNigrumSaveList = new List<FriendlyNigrumSaveData>(); // 현재 아군 흑체 저장 목록 초기화
currentSoundVolumeSaveData = new SoundVolumeSaveData(); // 현재 소리 설정 초기화

    SortAndReindex(); // 저장본 번호 정렬
}

    public void SaveToFile() // 현재 저장 데이터 파일 저장
    {
        string path = GetSaveFilePath(); // 저장 파일 경로 가져오기
        string json = JsonUtility.ToJson(currentSaveFileData, true); // JSON 문자열 생성
        File.WriteAllText(path, json); // 파일에 저장
    }

    private void SortAndReindex() // 저장본 번호 정렬 및 재부여
    {
        currentSaveFileData.saveList.Sort((a, b) => a.saveNumber.CompareTo(b.saveNumber)); // 번호 기준 정렬

        for (int i = 0; i < currentSaveFileData.saveList.Count; i++)
        {
            currentSaveFileData.saveList[i].saveNumber = i + 1; // 1부터 순차 번호 재설정
        }
    }

    private string GetSaveFilePath() // 저장 파일 전체 경로 반환
    {
        return Path.Combine(Application.persistentDataPath, saveFileName); // 영구 저장 경로와 파일명 결합
    }

public void SetCurrentSelectedSaveId(int targetSaveId) // 현재 선택된 저장본 ID 설정
{
    currentSelectedSaveId = targetSaveId; // 현재 선택된 저장본 ID 저장
}

public bool DeleteSaveById(int targetSaveId) // 고유 ID 기준 저장본 삭제
{
    int removeIndex = -1; // 삭제할 인덱스

    for (int i = 0; i < currentSaveFileData.saveList.Count; i++)
    {
        if (currentSaveFileData.saveList[i].saveId == targetSaveId)
        {
            removeIndex = i; // 삭제 대상 인덱스 기록
            break; // 탐색 종료
        }
    }

    if (removeIndex < 0) return false; // 삭제 대상이 없으면 실패

    currentSaveFileData.saveList.RemoveAt(removeIndex); // 저장본 삭제

    if (currentSelectedSaveId == targetSaveId)
    {
        currentSelectedSaveId = -1; // 현재 선택된 저장본이 삭제되면 초기화
    }

    DeleteTileLayoutSaveFile(targetSaveId); // 연결된 타일 배치 저장본도 같이 삭제
    SortAndReindex(); // 화면 표시용 번호 재정렬
    SaveToFile(); // 파일 저장

    return true; // 삭제 성공 반환
}

public string GetTileLayoutSaveFilePath(int targetSaveId) // 저장본 ID에 대응하는 타일 저장 파일 경로 반환
{
    return Path.Combine(Application.persistentDataPath, $"{tileLayoutSaveFilePrefix}{targetSaveId}.json"); // 저장 경로 생성
}

public bool HasTileLayoutSaveFile(int targetSaveId) // 해당 저장본 ID의 타일 저장본 존재 여부 확인
{
    return File.Exists(GetTileLayoutSaveFilePath(targetSaveId)); // 파일 존재 여부 반환
}

public void DeleteTileLayoutSaveFile(int targetSaveId) // 해당 저장본 ID의 타일 저장 파일 삭제
{
    string tileSavePath = GetTileLayoutSaveFilePath(targetSaveId); // 삭제 대상 파일 경로

    if (File.Exists(tileSavePath) == true)
    {
        File.Delete(tileSavePath); // 타일 저장 파일 삭제
    }
}

private List<OwnedCharacterData> GetOwnedCharacterListCopy(List<OwnedCharacterData> sourceList) // 캐릭터 목록 깊은 복사 반환
{
    List<OwnedCharacterData> copyList = new List<OwnedCharacterData>(); // 복사용 리스트

    if (sourceList == null)
    {
        return copyList; // 원본이 없으면 빈 리스트 반환
    }

    for (int i = 0; i < sourceList.Count; i++)
    {
        OwnedCharacterData source = sourceList[i]; // 원본 데이터 참조
        if (source == null) continue; // 비어 있으면 건너뜀

        OwnedCharacterData copy = new OwnedCharacterData(); // 복사 데이터 생성
        copy.firstRowID = source.firstRowID; // 첫 번째 행 ID 복사
        copy.secondRowID = source.secondRowID; // 두 번째 행 ID 복사
        copy.individualID = source.individualID; // 개체별 고유 ID 복사
        copy.isDead = source.isDead; // 사망 여부 복사
        

        copyList.Add(copy); // 복사본 추가
    }

    return copyList; // 복사 리스트 반환
}

public void ApplyCurrentOwnedCharacterList(List<OwnedCharacterData> sourceList) // 현재 소유 캐릭터 목록 적용
{
    currentOwnedCharacterList = GetOwnedCharacterListCopy(sourceList); // 현재 목록을 복사 적용
}

public bool LoadOwnedCharacterListFromSave(int targetSaveId) // 특정 저장본의 캐릭터 목록, 스탯정보, 인벤토리를 현재 목록으로 적용
{
    for (int i = 0; i < currentSaveFileData.saveList.Count; i++)
    {
        SaveEntry entry = currentSaveFileData.saveList[i]; // 현재 저장본 참조

        if (entry.saveId != targetSaveId)
        {
            continue; // 다른 저장본이면 건너뜀
        }

        ApplyCurrentOwnedCharacterList(entry.ownedCharacterList); // 현재 소유 캐릭터 목록 적용
        ApplyCurrentOwnedCharacterStatList(entry.ownedCharacterStatList); // 현재 소유 캐릭터 스탯정보 적용
        ApplyCurrentOwnedCharacterInventoryList(entry.ownedCharacterInventoryList); // 현재 캐릭터 인벤토리 적용

        return true; // 적용 성공
    }

    return false; // 대상 저장본 없음
}

public List<OwnedCharacterData> GetOwnedCharacterListBySaveId(int targetSaveId) // 특정 저장본의 캐릭터 목록 복사 반환
{
    for (int i = 0; i < currentSaveFileData.saveList.Count; i++)
    {
        SaveEntry entry = currentSaveFileData.saveList[i]; // 현재 저장본 참조

        if (entry.saveId == targetSaveId)
        {
            return GetOwnedCharacterListCopy(entry.ownedCharacterList); // 복사본 반환
        }
    }

    return new List<OwnedCharacterData>(); // 없으면 빈 리스트 반환
}

public bool LoadOwnedCharacterDataFromSave(int targetSaveId) // 특정 저장본의 캐릭터 목록, 스탯정보, 인벤토리, 흑체 저장 목록, 소리 설정을 현재 데이터로 적용
{
    for (int i = 0; i < currentSaveFileData.saveList.Count; i++)
    {
        SaveEntry entry = currentSaveFileData.saveList[i]; // 현재 저장본 참조

        if (entry.saveId != targetSaveId)
        {
            continue; // 다른 저장본이면 건너뜀
        }

        ApplyCurrentOwnedCharacterList(entry.ownedCharacterList); // 현재 소유 캐릭터 목록에 적용
        ApplyCurrentOwnedCharacterStatList(entry.ownedCharacterStatList); // 현재 소유 캐릭터 스탯정보 목록에 적용
        ApplyCurrentOwnedCharacterInventoryList(entry.ownedCharacterInventoryList); // 현재 캐릭터 인벤토리 목록에 적용
        ApplyCurrentFriendlyNigrumSaveList(entry.friendlyNigrumSaveList); // 현재 아군 흑체 저장 목록에 적용
        ApplyCurrentSoundVolumeSaveData(entry.soundVolumeSaveData); // 현재 소리 설정에 적용

        return true; // 적용 성공
    }

    return false; // 대상 저장본 없음
}

public List<OwnedCharacterStatData> GetOwnedCharacterStatListBySaveId(int targetSaveId) // 특정 저장본의 캐릭터 스탯정보 목록 복사 반환
{
    for (int i = 0; i < currentSaveFileData.saveList.Count; i++)
    {
        SaveEntry entry = currentSaveFileData.saveList[i]; // 현재 저장본 참조

        if (entry.saveId == targetSaveId)
        {
            return GetOwnedCharacterStatListCopy(entry.ownedCharacterStatList); // 복사본 반환
        }
    }

    return new List<OwnedCharacterStatData>(); // 없으면 빈 리스트 반환
}

private List<OwnedCharacterStatData> GetOwnedCharacterStatListCopy(List<OwnedCharacterStatData> sourceList) // 캐릭터 스탯정보 목록 깊은 복사 반환
{
    List<OwnedCharacterStatData> copyList = new List<OwnedCharacterStatData>(); // 복사용 리스트

    if (sourceList == null)
    {
        return copyList; // 원본이 없으면 빈 리스트 반환
    }

    for (int i = 0; i < sourceList.Count; i++)
    {
        OwnedCharacterStatData source = sourceList[i]; // 원본 데이터 참조
        if (source == null) continue; // 비어 있으면 건너뜀

        OwnedCharacterStatData copy = new OwnedCharacterStatData(); // 복사 데이터 생성
        copy.firstRowID = source.firstRowID; // 첫 번째 행 ID 복사
        copy.secondRowID = source.secondRowID; // 두 번째 행 ID 복사
        copy.individualID = source.individualID; // 개체별 고유 ID 복사

        copy.currentExperience = source.currentExperience; // 현재 경험치 복사
        copy.levelUpRequiredExperience = source.levelUpRequiredExperience; // 레벨업 충족 경험치 복사
        copy.levelstats = source.levelstats; // 레벨 복사

        copy.baseMoveSpeed = source.baseMoveSpeed; // 기본 이동속도 복사
        copy.moveSpeedPercent = source.moveSpeedPercent; // 이동속도 퍼센트 복사
        copy.finalMoveSpeed = source.finalMoveSpeed; // 최종 이동속도 복사

        copy.attackPower = source.attackPower; // 공격력 복사
        copy.defenseValue = source.defenseValue; // 방어력 복사
        copy.maxHealth = source.maxHealth; // 최대 체력 복사
        copy.currentHealth = source.currentHealth; // 현재 체력 복사
        copy.bodySize = source.bodySize; // 체급 복사
        copy.speedStat = source.speedStat; // 속도 수치 복사
        copy.powerRatePercent = source.powerRatePercent; // 위력률 복사
        copy.maxStaggerAmount = source.maxStaggerAmount; // 최대 와해량 복사
        copy.currentStaggerAmount = source.currentStaggerAmount; // 현재 와해량 복사
        copy.staggerResistancePercent = source.staggerResistancePercent; // 와해 저항률 복사

        copy.characterName = source.characterName; // 캐릭터 이름 복사
        copy.maxHunger = source.maxHunger; // 최대 허기 복사
        copy.currentHunger = source.currentHunger; // 현재 허기 복사
        copy.isStarving = source.isStarving; // 공복 여부 복사
        copy.overweightDebuffStackCount = source.overweightDebuffStackCount; // 무게 디버프 중첩값 복사
        
        

        copyList.Add(copy); // 복사본 추가
    }

    return copyList; // 복사 리스트 반환
}

public void ApplyCurrentOwnedCharacterStatList(List<OwnedCharacterStatData> sourceList) // 현재 소유 캐릭터 스탯정보 목록 적용
{
    currentOwnedCharacterStatList = GetOwnedCharacterStatListCopy(sourceList); // 현재 스탯정보 목록 복사 적용
}

public OwnedCharacterStatData FindCurrentOwnedCharacterStatData(int firstRowID, int secondRowID, int individualID) // 현재 소유 캐릭터 스탯정보 탐색
{
    for (int i = 0; i < currentOwnedCharacterStatList.Count; i++)
    {
        OwnedCharacterStatData statData = currentOwnedCharacterStatList[i]; // 현재 스탯정보 참조

        if (statData == null)
        {
            continue; // 비어 있으면 건너뜀
        }

        if (statData.firstRowID != firstRowID)
        {
            continue; // 첫 번째 행 ID가 다르면 건너뜀
        }

        if (statData.secondRowID != secondRowID)
        {
            continue; // 두 번째 행 ID가 다르면 건너뜀
        }

        if (statData.individualID != individualID)
        {
            continue; // 개체별 ID가 다르면 건너뜀
        }

        return statData; // 일치하는 스탯정보 반환
    }

    return null; // 찾지 못했으면 null 반환
}

public string GetCurrentSelectedNotepadPageText(int pageIndex) // 현재 선택 저장본의 특정 메모장 페이지 텍스트 반환
{
    if (pageIndex <= 0) return string.Empty; // 잘못된 페이지면 빈 텍스트 반환
    if (currentSelectedSaveId <= 0) return string.Empty; // 선택된 저장본이 없으면 빈 텍스트 반환

    for (int i = 0; i < currentSaveFileData.saveList.Count; i++)
    {
        SaveEntry entry = currentSaveFileData.saveList[i]; // 현재 저장본 참조

        if (entry.saveId != currentSelectedSaveId)
        {
            continue; // 현재 선택 저장본이 아니면 건너뜀
        }

        if (entry.notepadPageList == null)
        {
            entry.notepadPageList = new List<NotepadPageData>(); // 메모장 목록 null 방지
        }

        for (int j = 0; j < entry.notepadPageList.Count; j++)
        {
            NotepadPageData pageData = entry.notepadPageList[j]; // 현재 페이지 데이터 참조
            if (pageData == null) continue; // 비어 있으면 건너뜀

            if (pageData.pageIndex == pageIndex)
            {
                return pageData.pageText == null ? string.Empty : pageData.pageText; // 저장된 텍스트 반환
            }
        }

        return string.Empty; // 해당 페이지 저장값이 없으면 빈 텍스트 반환
    }

    return string.Empty; // 저장본을 못 찾으면 빈 텍스트 반환
}

public void SetCurrentSelectedNotepadPageText(int pageIndex, string pageText) // 현재 선택 저장본의 특정 메모장 페이지 텍스트 저장
{
    if (pageIndex <= 0) return; // 잘못된 페이지면 종료
    if (currentSelectedSaveId <= 0) return; // 선택된 저장본이 없으면 종료

    for (int i = 0; i < currentSaveFileData.saveList.Count; i++)
    {
        SaveEntry entry = currentSaveFileData.saveList[i]; // 현재 저장본 참조

        if (entry.saveId != currentSelectedSaveId)
        {
            continue; // 현재 선택 저장본이 아니면 건너뜀
        }

        if (entry.notepadPageList == null)
        {
            entry.notepadPageList = new List<NotepadPageData>(); // 메모장 목록 null 방지
        }

        for (int j = 0; j < entry.notepadPageList.Count; j++)
        {
            NotepadPageData pageData = entry.notepadPageList[j]; // 현재 페이지 데이터 참조
            if (pageData == null) continue; // 비어 있으면 건너뜀

            if (pageData.pageIndex == pageIndex)
            {
                pageData.pageText = pageText == null ? string.Empty : pageText; // 기존 페이지 텍스트 갱신
                SaveToFile(); // 파일 저장
                return; // 저장 종료
            }
        }

        NotepadPageData newPageData = new NotepadPageData(); // 새 페이지 데이터 생성
        newPageData.pageIndex = pageIndex; // 페이지 인덱스 저장
        newPageData.pageText = pageText == null ? string.Empty : pageText; // 페이지 텍스트 저장

        entry.notepadPageList.Add(newPageData); // 메모장 페이지 목록에 추가
        entry.notepadPageList.Sort((a, b) => a.pageIndex.CompareTo(b.pageIndex)); // 페이지 인덱스 기준 정렬
        SaveToFile(); // 파일 저장
        return; // 저장 종료
    }
}

private List<NotepadPageData> GetNotepadPageListCopy(List<NotepadPageData> sourceList) // 메모장 페이지 목록 깊은 복사 반환
{
    List<NotepadPageData> copyList = new List<NotepadPageData>(); // 복사용 리스트

    if (sourceList == null)
    {
        return copyList; // 원본이 없으면 빈 리스트 반환
    }

    for (int i = 0; i < sourceList.Count; i++)
    {
        NotepadPageData source = sourceList[i]; // 원본 페이지 데이터 참조
        if (source == null) continue; // 비어 있으면 건너뜀

        NotepadPageData copy = new NotepadPageData(); // 복사 데이터 생성
        copy.pageIndex = source.pageIndex; // 페이지 인덱스 복사
        copy.pageText = source.pageText; // 페이지 텍스트 복사

        copyList.Add(copy); // 복사본 추가
    }

    return copyList; // 복사 리스트 반환
}

private List<OwnedCharacterInventorySaveData> GetOwnedCharacterInventoryListCopy(List<OwnedCharacterInventorySaveData> sourceList) // 캐릭터 인벤토리 목록 깊은 복사 반환
{
    List<OwnedCharacterInventorySaveData> copyList = new List<OwnedCharacterInventorySaveData>(); // 복사용 리스트

    if (sourceList == null)
    {
        return copyList; // 원본이 없으면 빈 리스트 반환
    }

    for (int i = 0; i < sourceList.Count; i++)
    {
        OwnedCharacterInventorySaveData source = sourceList[i]; // 원본 인벤토리 데이터 참조
        if (source == null) continue; // 비어 있으면 건너뜀

        OwnedCharacterInventorySaveData copy = new OwnedCharacterInventorySaveData(); // 복사 데이터 생성
        copy.firstRowID = source.firstRowID; // 첫 번째 행 ID 복사
        copy.secondRowID = source.secondRowID; // 두 번째 행 ID 복사
        copy.individualID = source.individualID; // 개체별 고유 ID 복사
        copy.items = GetOwnedCharacterInventoryItemListCopy(source.items); // 아이템 목록 복사

        copyList.Add(copy); // 복사본 추가
    }

    return copyList; // 복사 리스트 반환
}

private List<OwnedCharacterInventoryItemSaveData> GetOwnedCharacterInventoryItemListCopy(List<OwnedCharacterInventoryItemSaveData> sourceList) // 인벤토리 아이템 목록 깊은 복사 반환
{
    List<OwnedCharacterInventoryItemSaveData> copyList = new List<OwnedCharacterInventoryItemSaveData>(); // 복사용 리스트

    if (sourceList == null)
    {
        return copyList; // 원본이 없으면 빈 리스트 반환
    }

    for (int i = 0; i < sourceList.Count; i++)
    {
        OwnedCharacterInventoryItemSaveData source = sourceList[i]; // 원본 아이템 데이터 참조
        if (source == null) continue; // 비어 있으면 건너뜀

        OwnedCharacterInventoryItemSaveData copy = new OwnedCharacterInventoryItemSaveData(); // 복사 데이터 생성
        copy.itemAID = source.itemAID; // 아이템 A ID 복사
        copy.itemBID = source.itemBID; // 아이템 B ID 복사
        copy.count = source.count; // 개수 복사

        copyList.Add(copy); // 복사본 추가
    }

    return copyList; // 복사 리스트 반환
}

public void ApplyCurrentOwnedCharacterInventoryList(List<OwnedCharacterInventorySaveData> sourceList) // 현재 캐릭터 인벤토리 목록 적용
{
    currentOwnedCharacterInventoryList = GetOwnedCharacterInventoryListCopy(sourceList); // 현재 목록을 복사 적용
}

public List<OwnedCharacterInventorySaveData> GetCurrentOwnedCharacterInventoryList() // 현재 캐릭터 인벤토리 목록 복사 반환
{
    return GetOwnedCharacterInventoryListCopy(currentOwnedCharacterInventoryList); // 현재 목록 복사 반환
}

public List<OwnedCharacterInventorySaveData> GetOwnedCharacterInventoryListBySaveId(int targetSaveId) // 특정 저장본의 캐릭터 인벤토리 목록 복사 반환
{
    for (int i = 0; i < currentSaveFileData.saveList.Count; i++)
    {
        SaveEntry entry = currentSaveFileData.saveList[i]; // 현재 저장본 참조

        if (entry.saveId == targetSaveId)
        {
            return GetOwnedCharacterInventoryListCopy(entry.ownedCharacterInventoryList); // 복사본 반환
        }
    }

    return new List<OwnedCharacterInventorySaveData>(); // 없으면 빈 리스트 반환
}

public OwnedCharacterInventorySaveData FindCurrentOwnedCharacterInventoryData(int firstRowID, int secondRowID, int individualID) // 현재 캐릭터 인벤토리 데이터 탐색
{
    for (int i = 0; i < currentOwnedCharacterInventoryList.Count; i++)
    {
        OwnedCharacterInventorySaveData inventoryData = currentOwnedCharacterInventoryList[i]; // 현재 인벤토리 데이터 참조
        if (inventoryData == null) continue; // 비어 있으면 건너뜀

        bool isSameCharacter =
            inventoryData.firstRowID == firstRowID &&
            inventoryData.secondRowID == secondRowID &&
            inventoryData.individualID == individualID; // 같은 캐릭터인지 확인

        if (isSameCharacter)
        {
            List<OwnedCharacterInventorySaveData> copyList = GetOwnedCharacterInventoryListCopy(new List<OwnedCharacterInventorySaveData> { inventoryData }); // 단일 데이터 복사
            return copyList.Count > 0 ? copyList[0] : null; // 복사본 반환
        }
    }

    return null; // 찾지 못하면 null 반환
}

public void SetCurrentOwnedCharacterInventoryData(OwnedCharacterInventorySaveData targetInventoryData) // 현재 캐릭터 인벤토리 데이터 갱신
{
    if (targetInventoryData == null)
    {
        return; // 대상이 없으면 종료
    }

    for (int i = 0; i < currentOwnedCharacterInventoryList.Count; i++)
    {
        OwnedCharacterInventorySaveData inventoryData = currentOwnedCharacterInventoryList[i]; // 현재 인벤토리 데이터 참조
        if (inventoryData == null) continue; // 비어 있으면 건너뜀

        bool isSameCharacter =
            inventoryData.firstRowID == targetInventoryData.firstRowID &&
            inventoryData.secondRowID == targetInventoryData.secondRowID &&
            inventoryData.individualID == targetInventoryData.individualID; // 같은 캐릭터인지 확인

        if (isSameCharacter)
        {
            currentOwnedCharacterInventoryList[i] = GetOwnedCharacterInventoryListCopy(new List<OwnedCharacterInventorySaveData> { targetInventoryData })[0]; // 기존 데이터 교체
            SaveCurrentOwnedCharacterInventoryListToSelectedSave(); // 현재 선택 저장본에 반영
            return; // 갱신 종료
        }
    }

    currentOwnedCharacterInventoryList.Add(GetOwnedCharacterInventoryListCopy(new List<OwnedCharacterInventorySaveData> { targetInventoryData })[0]); // 새 데이터 추가
    SaveCurrentOwnedCharacterInventoryListToSelectedSave(); // 현재 선택 저장본에 반영
}

public bool SaveCurrentOwnedCharacterInventoryListToSelectedSave() // 현재 캐릭터 인벤토리 목록을 선택 저장본에 저장
{
    if (currentSelectedSaveId < 0)
    {
        return false; // 선택된 저장본이 없으면 실패
    }

    for (int i = 0; i < currentSaveFileData.saveList.Count; i++)
    {
        SaveEntry entry = currentSaveFileData.saveList[i]; // 현재 저장본 참조

        if (entry.saveId != currentSelectedSaveId)
        {
            continue; // 선택 저장본이 아니면 건너뜀
        }

        entry.ownedCharacterInventoryList = GetOwnedCharacterInventoryListCopy(currentOwnedCharacterInventoryList); // 현재 인벤토리 목록 저장본에 반영
        SaveToFile(); // 파일 저장
        return true; // 저장 성공
    }

    return false; // 대상 저장본 없음
}

public void StoreBattleEventRuntimeData(BattleOccurrenceEvent battleEvent) // 전투 이벤트 데이터 저장
{
    if (battleEvent == null)
    {
        return; // 이벤트가 없으면 종료
    }

    lastExecutedEventID = battleEvent.EventID; // 전투씬 이동 전 직전 실행 이벤트 ID 저장
    hasExecutedBattle = false; // 아직 전투씬에서 데이터 전달 전이므로 false로 초기화

    currentBattleEventRuntimeData = new BattleEventRuntimeData(); // 새 런타임 데이터 생성
    currentBattleEventRuntimeData.eventID = battleEvent.EventID; // 이벤트 ID 저장
    currentBattleEventRuntimeData.minEnemySpawnCount = battleEvent.MinEnemySpawnCount; // 최소 생성 수 저장
    currentBattleEventRuntimeData.maxEnemySpawnCount = battleEvent.MaxEnemySpawnCount; // 최대 생성 수 저장
    currentBattleEventRuntimeData.enemyLevelCorrectionValue = battleEvent.EnemyLevelCorrectionValue; // 레벨 보정값 저장
    currentBattleEventRuntimeData.minEnemySpawnDelay = battleEvent.MinEnemySpawnDelay; // 최소 딜레이 저장
    currentBattleEventRuntimeData.maxEnemySpawnDelay = battleEvent.MaxEnemySpawnDelay; // 최대 딜레이 저장
    currentBattleEventRuntimeData.minBattleRequiredMinute = battleEvent.MinBattleRequiredMinute; // 최소 전투 소요 시간 저장
    currentBattleEventRuntimeData.maxBattleRequiredMinute = battleEvent.MaxBattleRequiredMinute; // 최대 전투 소요 시간 저장
    currentBattleEventRuntimeData.spawnableEnemyList = new List<GlobalCharacterDefinition>(battleEvent.SpawnableEnemyList); // 적 목록 복사
}

public void SendBattleEventRuntimeDataToEnemySpawnManager(EnemySpawnManager enemySpawnManager) // 적 생성 관리자에게 전투 데이터 전달
{
    if (enemySpawnManager == null)
    {
        return; // 대상이 없으면 종료
    }

    if (currentBattleEventRuntimeData == null)
    {
        return; // 저장된 전투 데이터가 없으면 종료
    }

    int minMinute = Mathf.Min(currentBattleEventRuntimeData.minBattleRequiredMinute, currentBattleEventRuntimeData.maxBattleRequiredMinute); // 최소값 보정
    int maxMinute = Mathf.Max(currentBattleEventRuntimeData.minBattleRequiredMinute, currentBattleEventRuntimeData.maxBattleRequiredMinute); // 최대값 보정
    int battleRequiredMinute = Random.Range(minMinute, maxMinute + 1); // 전투 소요 시간 랜덤 결정

    AddMinutesToCurrentSelectedTime(battleRequiredMinute); // 전투 소요 시간만큼 현재 시간 증가

    TimeSystemManager timeSystemManager = FindFirstObjectByType<TimeSystemManager>(); // 시간 UI 관리자 탐색
    if (timeSystemManager != null)
    {
        timeSystemManager.RefreshTimeUI(); // 시간 UI 갱신
    }

    enemySpawnManager.ReceiveBattleEventData(currentBattleEventRuntimeData); // 전투 데이터 전달
    hasExecutedBattle = true; // 전투씬으로 넘어가 전투 데이터가 사용되었음을 표시
    ClearBattleEventRuntimeData(); // 전달 후 초기화
}

public void ClearBattleEventRuntimeData() // 전투 이벤트 임시 데이터 초기화
{
    currentBattleEventRuntimeData = null; // 전투 데이터 제거
}

public void ClearBattleReturnState() // 전투 복귀 처리 상태 초기화
{
    hasExecutedBattle = false; // 전투 수행 여부 초기화
    lastExecutedEventID = -1; // 직전 실행 이벤트 ID 초기화
}

public bool SetCurrentSelectedTime(int day, int hour, int minute) // 현재 선택 저장본 시간 직접 설정
{
    if (currentSelectedSaveId <= 0) return false; // 선택 저장본 없으면 실패

    for (int i = 0; i < currentSaveFileData.saveList.Count; i++)
    {
        SaveEntry entry = currentSaveFileData.saveList[i]; // 현재 저장본 참조

        if (entry.saveId != currentSelectedSaveId)
        {
            continue; // 선택 저장본이 아니면 건너뜀
        }

        entry.currentDay = Mathf.Max(0, day); // 일차 적용
        entry.currentHour = Mathf.Clamp(hour, 0, 23); // 시간 적용
        entry.currentMinute = Mathf.Clamp(minute, 0, 59); // 분 적용

        SaveToFile(); // 파일 저장
        return true; // 성공 반환
    }

    return false; // 대상 저장본 없음
}


public bool TryGetCurrentSelectedTime(out int day, out int hour, out int minute) // 현재 선택 저장본 시간 반환
{
    day = 0; // 기본 일차
    hour = 0; // 기본 시간
    minute = 0; // 기본 분

    if (currentSelectedSaveId <= 0) return false; // 선택 저장본 없으면 실패

    for (int i = 0; i < currentSaveFileData.saveList.Count; i++)
    {
        SaveEntry entry = currentSaveFileData.saveList[i]; // 현재 저장본 참조

        if (entry.saveId != currentSelectedSaveId)
        {
            continue; // 선택 저장본이 아니면 건너뜀
        }

        day = entry.currentDay; // 일차 반환
        hour = entry.currentHour; // 시간 반환
        minute = entry.currentMinute; // 분 반환
        return true; // 성공 반환
    }

    return false; // 대상 저장본 없음
}

public bool AddMinutesToCurrentSelectedTime(int addMinute) // 현재 선택 저장본 시간에 분 추가
{
    if (currentSelectedSaveId <= 0) return false; // 선택 저장본 없으면 실패

    for (int i = 0; i < currentSaveFileData.saveList.Count; i++)
    {
        SaveEntry entry = currentSaveFileData.saveList[i]; // 현재 저장본 참조

        if (entry.saveId != currentSelectedSaveId)
        {
            continue; // 선택 저장본이 아니면 건너뜀
        }

        int passedMinute = Mathf.Max(0, addMinute); // 실제 흐른 시간 계산

        int totalMinute = entry.currentHour * 60 + entry.currentMinute + addMinute; // 전체 분 계산
        int passedDay = Mathf.FloorToInt(totalMinute / 1440f); // 지난 일차 계산
        int remainMinute = totalMinute % 1440; // 하루 기준 남은 분 계산

        if (remainMinute < 0)
        {
            remainMinute += 1440; // 음수 분 보정
            passedDay--; // 일차 보정
        }

        entry.currentDay = Mathf.Max(0, entry.currentDay + passedDay); // 일차 갱신
        entry.currentHour = remainMinute / 60; // 시간 갱신
        entry.currentMinute = remainMinute % 60; // 분 갱신

        ApplyHungerDecreaseByPassedMinute(passedMinute); // 흐른 시간만큼 허기 감소 적용
        SaveCurrentOwnedCharacterStatListToSelectedSave(); // 변경된 허기값을 선택 저장본과 JSON에 저장

        FriendlyNigrumIntakeManager nigrumIntakeManager =
            FriendlyNigrumIntakeManager.Instance != null
                ? FriendlyNigrumIntakeManager.Instance
                : FindFirstObjectByType<FriendlyNigrumIntakeManager>(); // 흑체 복용 관리자 탐색

        if (nigrumIntakeManager != null)
        {
            nigrumIntakeManager.ApplyPassedMinute(passedMinute); // 흐른 시간만큼 흑체 수용량 감소 적용
        }

        SaveToFile(); // 시간 변경값까지 파일 저장
        return true; // 성공 반환
    }

    return false; // 대상 저장본 없음
}

public void SetCurrentOwnedCharacterOverweightStackCount(
    int firstRowID,
    int secondRowID,
    int individualID,
    int overweightStackCount) // 캐릭터 무게 디버프 중첩값 저장
{
    OwnedCharacterStatData statData = FindCurrentOwnedCharacterStatData(firstRowID, secondRowID, individualID); // 저장 스탯 탐색

    if (statData == null)
    {
        return; // 대상 캐릭터 스탯정보가 없으면 종료
    }

    statData.overweightDebuffStackCount = Mathf.Max(0, overweightStackCount); // 음수 방지 후 중첩값 저장
    SaveCurrentOwnedCharacterStatListToSelectedSave(); // 현재 스탯 목록을 선택 저장본에 반영
}

private void ApplyHungerDecreaseByPassedMinute(int passedMinute) // 흐른 시간 기준 허기 감소 처리
{
    if (passedMinute <= 0)
    {
        return; // 시간이 흐르지 않았으면 종료
    }

    CharacterInfoManager characterInfoManager = CharacterInfoManager.Instance; // 전역 캐릭터 정보 매니저 참조

    if (characterInfoManager == null)
    {
        characterInfoManager = FindFirstObjectByType<CharacterInfoManager>(); // 없으면 씬에서 탐색
    }

    if (characterInfoManager == null)
    {
        return; // 캐릭터 정의를 찾을 수 없으면 종료
    }

    for (int i = 0; i < currentOwnedCharacterStatList.Count; i++)
    {
        OwnedCharacterStatData statData = currentOwnedCharacterStatList[i]; // 현재 캐릭터 스탯 참조

        if (statData == null)
        {
            continue; // 비어 있으면 건너뜀
        }

        GlobalCharacterDefinition definition =
            characterInfoManager.FindDefinitionByID(statData.firstRowID, statData.secondRowID); // 캐릭터 정의 탐색

        if (definition == null)
        {
            continue; // 정의가 없으면 건너뜀
        }

        int intervalMinute = Mathf.Max(1, definition.HungerDecreaseIntervalMinute); // 허기 감소 주기 보정
        int decreaseCount = passedMinute / intervalMinute; // 흐른 시간 안에서 감소 실행 횟수 계산

        if (decreaseCount <= 0)
        {
            statData.isStarving = statData.currentHunger <= 0; // 허기 상태만 갱신
            continue; // 아직 감소 주기에 도달하지 않았으면 건너뜀
        }

        int decreaseAmount = Mathf.Max(0, definition.HungerDecreaseAmount) * decreaseCount; // 총 허기 감소량 계산

        statData.currentHunger = Mathf.Max(0, statData.currentHunger - decreaseAmount); // 허기 감소 적용
        statData.isStarving = statData.currentHunger <= 0; // 현재 허기가 0이면 공복 상태
    }
}

public bool SaveCurrentOwnedCharacterStatListToSelectedSave() // 현재 캐릭터 스탯정보 목록을 선택 저장본에 저장
{
    if (currentSelectedSaveId < 0)
    {
        return false; // 선택된 저장본이 없으면 실패
    }

    for (int i = 0; i < currentSaveFileData.saveList.Count; i++)
    {
        SaveEntry entry = currentSaveFileData.saveList[i]; // 현재 저장본 참조

        if (entry.saveId != currentSelectedSaveId)
        {
            continue; // 선택 저장본이 아니면 건너뜀
        }

        entry.ownedCharacterStatList = GetOwnedCharacterStatListCopy(currentOwnedCharacterStatList); // 현재 스탯 목록 반영
        SaveToFile(); // 파일 저장
        return true; // 저장 성공
    }

    return false; // 대상 저장본 없음
}

private List<FriendlyNigrumSaveData> GetFriendlyNigrumSaveListCopy(List<FriendlyNigrumSaveData> sourceList) // 아군 흑체 저장 목록 깊은 복사 반환
{
    List<FriendlyNigrumSaveData> copyList = new List<FriendlyNigrumSaveData>(); // 복사용 리스트

    if (sourceList == null)
    {
        return copyList; // 원본이 없으면 빈 리스트 반환
    }

    for (int i = 0; i < sourceList.Count; i++)
    {
        FriendlyNigrumSaveData source = sourceList[i]; // 원본 데이터 참조

        if (source == null)
        {
            continue; // 비어 있으면 건너뜀
        }

        FriendlyNigrumSaveData copy = new FriendlyNigrumSaveData(); // 복사 데이터 생성
        copy.friendlyCharacterDefinition = source.friendlyCharacterDefinition; // 대상 아군 에셋 복사
        copy.currentNigrumCapacity = source.currentNigrumCapacity; // 현재 흑체 수용량 복사
        copy.nigrumDecreaseRemainMinute = source.nigrumDecreaseRemainMinute; // 흑체 감소 잔여값 복사

        copyList.Add(copy); // 복사본 추가
    }

    return copyList; // 복사 리스트 반환
}

public void ApplyCurrentFriendlyNigrumSaveList(List<FriendlyNigrumSaveData> sourceList) // 현재 아군 흑체 저장 목록 적용
{
    currentFriendlyNigrumSaveList = GetFriendlyNigrumSaveListCopy(sourceList); // 현재 흑체 목록 복사 적용
}

public FriendlyNigrumSaveData FindCurrentFriendlyNigrumSaveData(FriendlyCharacterDefinition friendlyCharacterDefinition) // 현재 아군 흑체 저장 데이터 탐색
{
    if (friendlyCharacterDefinition == null)
    {
        return null; // 대상 에셋이 없으면 null 반환
    }

    for (int i = 0; i < currentFriendlyNigrumSaveList.Count; i++)
    {
        FriendlyNigrumSaveData saveData = currentFriendlyNigrumSaveList[i]; // 현재 흑체 데이터 참조

        if (saveData == null)
        {
            continue; // 비어 있으면 건너뜀
        }

        if (saveData.friendlyCharacterDefinition == friendlyCharacterDefinition)
        {
            return saveData; // 같은 아군 에셋이면 반환
        }
    }

    return null; // 찾지 못했으면 null 반환
}

public bool SaveCurrentFriendlyNigrumSaveListToSelectedSave() // 현재 아군 흑체 저장 목록을 선택 저장본에 저장
{
    if (currentSelectedSaveId <= 0)
    {
        return false; // 선택 저장본 없으면 실패
    }

    for (int i = 0; i < currentSaveFileData.saveList.Count; i++)
    {
        SaveEntry entry = currentSaveFileData.saveList[i]; // 현재 저장본 참조

        if (entry.saveId != currentSelectedSaveId)
        {
            continue; // 선택 저장본이 아니면 건너뜀
        }

        entry.friendlyNigrumSaveList = GetFriendlyNigrumSaveListCopy(currentFriendlyNigrumSaveList); // 현재 흑체 목록 반영
        SaveToFile(); // 파일 저장
        return true; // 저장 성공
    }

    return false; // 대상 저장본 없음
}

public void ApplyFriendlyNigrumDecrease(
    FriendlyCharacterDefinition friendlyCharacterDefinition,
    int maxNigrumCapacity,
    int decreaseIntervalMinute,
    int decreaseAmountPerInterval,
    int passedMinute) // 아군 흑체 감소 규칙 적용
{
    if (friendlyCharacterDefinition == null)
    {
        return; // 대상 아군이 없으면 종료
    }

    if (passedMinute <= 0)
    {
        return; // 시간이 흐르지 않았으면 종료
    }

    int safeIntervalMinute = Mathf.Max(1, decreaseIntervalMinute); // 감소 주기 보정
    int safeDecreaseAmount = Mathf.Max(0, decreaseAmountPerInterval); // 감소량 보정
    int safeMaxCapacity = Mathf.Max(0, maxNigrumCapacity); // 최대 수용량 보정

    FriendlyNigrumSaveData saveData = FindCurrentFriendlyNigrumSaveData(friendlyCharacterDefinition); // 저장 데이터 탐색

    if (saveData == null)
    {
        saveData = new FriendlyNigrumSaveData(); // 없으면 새 데이터 생성
        saveData.friendlyCharacterDefinition = friendlyCharacterDefinition; // 대상 아군 저장
        saveData.currentNigrumCapacity = safeMaxCapacity; // 기본값은 최대 수용량으로 설정
        saveData.nigrumDecreaseRemainMinute = 0; // 잔여값 초기화
        currentFriendlyNigrumSaveList.Add(saveData); // 현재 목록에 추가
    }

    int totalRemainMinute = saveData.nigrumDecreaseRemainMinute + passedMinute; // 기존 잔여값과 흐른 시간 합산
    int decreaseCount = totalRemainMinute / safeIntervalMinute; // 감소 실행 횟수 계산
    int newRemainMinute = totalRemainMinute % safeIntervalMinute; // 새 잔여값 계산

    int totalDecreaseAmount = decreaseCount * safeDecreaseAmount; // 총 감소량 계산

    saveData.currentNigrumCapacity = Mathf.Clamp(
        saveData.currentNigrumCapacity - totalDecreaseAmount,
        0,
        safeMaxCapacity
    ); // 현재 흑체 수용량 감소 적용

    saveData.nigrumDecreaseRemainMinute = newRemainMinute; // 감소 후 잔여값 저장

    SaveCurrentFriendlyNigrumSaveListToSelectedSave(); // 선택 저장본에 반영
}

public void ApplyConsumableValueToCurrentOwnedCharacterStat(
    int firstRowID,
    int secondRowID,
    int individualID,
    int applyHealthValue,
    int applyHungerValue) // 소모 아이템 체력/허기 적용
{
    OwnedCharacterStatData statData = FindCurrentOwnedCharacterStatData(firstRowID, secondRowID, individualID); // 캐릭터 스탯 탐색

    if (statData == null)
    {
        return; // 대상 스탯이 없으면 종료
    }

    statData.currentHealth = Mathf.Clamp(
        statData.currentHealth + applyHealthValue,
        0,
        statData.maxHealth
    ); // 체력 적용 후 범위 제한

    statData.currentHunger = Mathf.Clamp(
        statData.currentHunger + applyHungerValue,
        0,
        statData.maxHunger
    ); // 허기 적용 후 범위 제한

    statData.isStarving = statData.currentHunger <= 0; // 공복 여부 갱신

    SaveCurrentOwnedCharacterStatListToSelectedSave(); // 선택 저장본에 스탯 반영
}

public void ApplyFriendlyNigrumCapacityValue(
    FriendlyCharacterDefinition friendlyCharacterDefinition,
    int maxNigrumCapacity,
    int applyNigrumCapacityValue) // 소모 아이템 흑체 수용값 적용
{
    if (friendlyCharacterDefinition == null)
    {
        return; // 대상 아군 정의가 없으면 종료
    }

    int safeMaxCapacity = Mathf.Max(0, maxNigrumCapacity); // 최대 수용값 보정

    FriendlyNigrumSaveData saveData = FindCurrentFriendlyNigrumSaveData(friendlyCharacterDefinition); // 흑체 저장 데이터 탐색

    if (saveData == null)
    {
        saveData = new FriendlyNigrumSaveData(); // 없으면 새 데이터 생성
        saveData.friendlyCharacterDefinition = friendlyCharacterDefinition; // 대상 아군 저장
        saveData.currentNigrumCapacity = safeMaxCapacity; // 기본값은 최대 수용값
        saveData.nigrumDecreaseRemainMinute = 0; // 감소 잔여 시간 초기화
        currentFriendlyNigrumSaveList.Add(saveData); // 현재 흑체 목록에 추가
    }

    saveData.currentNigrumCapacity = Mathf.Clamp(
        saveData.currentNigrumCapacity + applyNigrumCapacityValue,
        0,
        safeMaxCapacity
    ); // 흑체 수용값 적용 후 최대값 제한

    SaveCurrentFriendlyNigrumSaveListToSelectedSave(); // 선택 저장본에 흑체 목록 반영
}

private SoundVolumeSaveData GetSoundVolumeSaveDataCopy(SoundVolumeSaveData sourceData) // 소리 설정 깊은 복사 반환
{
    SoundVolumeSaveData copyData = new SoundVolumeSaveData(); // 복사용 데이터 생성

    if (sourceData == null)
    {
        return copyData; // 원본이 없으면 기본값 반환
    }

    copyData.masterVolume = Mathf.Clamp(sourceData.masterVolume, 0, 100); // 전체 사운드 복사
    copyData.effectVolume = Mathf.Clamp(sourceData.effectVolume, 0, 100); // 효과음 복사
    copyData.voiceVolume = Mathf.Clamp(sourceData.voiceVolume, 0, 100); // 음성 복사
    copyData.bgmVolume = Mathf.Clamp(sourceData.bgmVolume, 0, 100); // BGM 복사
    copyData.uiSoundVolume = Mathf.Clamp(sourceData.uiSoundVolume, 0, 100); // UI 사운드 복사

    return copyData; // 복사본 반환
}

public void ApplyCurrentSoundVolumeSaveData(SoundVolumeSaveData sourceData) // 현재 소리 설정 적용
{
    currentSoundVolumeSaveData = GetSoundVolumeSaveDataCopy(sourceData); // 현재 소리 설정 복사 적용
}

public float GetUISoundFinalVolume01(float baseVolume) // UI 사운드 최종 볼륨 반환
{
    float safeBaseVolume = Mathf.Max(0f, baseVolume); // 기본 볼륨 음수 방지
    float masterRate = Mathf.Clamp(currentSoundVolumeSaveData.masterVolume, 0, 100) / 100f; // 전체 사운드 비율
    float uiRate = Mathf.Clamp(currentSoundVolumeSaveData.uiSoundVolume, 0, 100) / 100f; // UI 사운드 비율

    return safeBaseVolume * masterRate * uiRate; // 최종 UI 사운드 볼륨 반환
}

public bool SetOwnedCharacterDeadState(int firstRowID, int secondRowID, int individualID, bool deadState) // 현재 소유 캐릭터 사망 상태 설정
{
    for (int i = 0; i < currentOwnedCharacterList.Count; i++)
    {
        OwnedCharacterData ownedData = currentOwnedCharacterList[i]; // 현재 소유 캐릭터 데이터

        if (ownedData == null)
        {
            continue; // 비어 있으면 건너뜀
        }

        if (ownedData.firstRowID != firstRowID)
        {
            continue; // 첫 번째 행 ID가 다르면 건너뜀
        }

        if (ownedData.secondRowID != secondRowID)
        {
            continue; // 두 번째 행 ID가 다르면 건너뜀
        }

        if (ownedData.individualID != individualID)
        {
            continue; // 개체별 ID가 다르면 건너뜀
        }

        ownedData.isDead = deadState; // 사망 상태 저장
        SaveCurrentOwnedCharacterListToSelectedSave(); // 현재 선택 저장본에 반영
        return true; // 처리 성공
    }

    return false; // 대상 없음
}

public bool SaveCurrentOwnedCharacterListToSelectedSave() // 현재 소유 캐릭터 목록을 선택 저장본에 저장
{
    if (currentSelectedSaveId < 0)
    {
        return false; // 선택된 저장본이 없으면 실패
    }

    for (int i = 0; i < currentSaveFileData.saveList.Count; i++)
    {
        SaveEntry entry = currentSaveFileData.saveList[i]; // 현재 저장본 참조

        if (entry.saveId != currentSelectedSaveId)
        {
            continue; // 선택 저장본이 아니면 건너뜀
        }

        entry.ownedCharacterList = GetOwnedCharacterListCopy(currentOwnedCharacterList); // 현재 소유 캐릭터 목록 저장본에 반영
        SaveToFile(); // 파일 저장
        return true; // 저장 성공
    }

    return false; // 대상 저장본 없음
}

public bool ApplyBattleCharacterStatToCurrentSave(
    int firstRowID,
    int secondRowID,
    int individualID,
    CharacterStatSystem statSystem) // 전투 캐릭터 현재 스탯을 저장 데이터에 반영
{
    if (statSystem == null)
    {
        return false; // 스탯 시스템이 없으면 실패
    }

    OwnedCharacterStatData statData = FindCurrentOwnedCharacterStatData(firstRowID, secondRowID, individualID); // 저장 스탯 탐색

    if (statData == null)
    {
        return false; // 저장 데이터가 없으면 실패
    }

    statData.levelstats = statSystem.LevelStats; // 레벨 저장
    statData.attackPower = statSystem.AttackPower; // 공격력 저장
    statData.defenseValue = statSystem.DefenseValue; // 방어율 저장
    statData.maxHealth = statSystem.MaxHealth; // 최대체력 저장
    statData.currentHealth = statSystem.CurrentHealth; // 현재체력 저장
    statData.bodySize = statSystem.BodySize; // 체급 저장
    statData.speedStat = statSystem.SpeedStat; // 속도 저장
    statData.powerRatePercent = statSystem.PowerRatePercent; // 위력률 저장
    statData.baseMoveSpeed = statSystem.BaseMoveSpeed; // 기본 이동속도 저장
    statData.moveSpeedPercent = statSystem.MoveSpeedPercent; // 이동속도율 저장
    statData.finalMoveSpeed = statSystem.FinalMoveSpeed; // 최종 이동속도 저장
    statData.currentStaggerAmount = statSystem.CurrentStaggerAmount; // 현재 와해량 저장

    SetOwnedCharacterDeadState(firstRowID, secondRowID, individualID, statSystem.IsDead); // 사망 여부 저장
    SaveCurrentOwnedCharacterStatListToSelectedSave(); // 스탯 목록 저장

    return true; // 저장 성공
}









}