using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// 저장본 생성, 삭제, 불러오기, 현재 선택 저장본 ID 관리,
/// 소유 캐릭터 목록과 캐릭터별 스탯정보 저장/복원을 담당한다.
/// </summary>
public class SaveStorage : MonoBehaviour
{

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
}

[Serializable] 
public class OwnedCharacterStatData
{
    [Header("캐릭터 ID")]
    public int firstRowID; // 캐릭터 첫 번째 행 ID
    public int secondRowID; // 캐릭터 두 번째 행 ID
    public int individualID; // 캐릭터 개체별 고유 ID
    
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
}


[Serializable]
public class SaveEntry
{
    public int saveId; // 저장본 고유 ID
    public string saveName; // 저장본 이름
    public int saveNumber; // 화면 표시용 번호
    public List<OwnedCharacterData> ownedCharacterList = new List<OwnedCharacterData>(); // 이 저장본에 소속된 캐릭터 목록
    public List<OwnedCharacterStatData> ownedCharacterStatList = new List<OwnedCharacterStatData>(); // 이 저장본에 소속된 캐릭터 스탯정보 목록
    public List<NotepadPageData> notepadPageList = new List<NotepadPageData>(); // 이 저장본에 소속된 메모장 페이지 목록
}

[Header("현재 소유 캐릭터 목록")]
[SerializeField] private List<OwnedCharacterData> currentOwnedCharacterList = new List<OwnedCharacterData>(); // 현재 세이브 기준으로 사용 중인 소유 캐릭터 목록

[Header("현재 소유 캐릭터 스탯정보 목록")]
[SerializeField] private List<OwnedCharacterStatData> currentOwnedCharacterStatList = new List<OwnedCharacterStatData>(); // 현재 세이브 기준 캐릭터 스탯정보 목록

public List<OwnedCharacterStatData> CurrentOwnedCharacterStatList => GetOwnedCharacterStatListCopy(currentOwnedCharacterStatList); // 현재 캐릭터 스탯정보 목록 복사 반환


public List<OwnedCharacterData> CurrentOwnedCharacterList => GetOwnedCharacterListCopy(currentOwnedCharacterList); // 현재 소유 캐릭터 목록 복사 반환

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

        result.Add(copy); // 복사본 추가
    }

    result.Sort((a, b) => a.saveNumber.CompareTo(b.saveNumber)); // 번호 기준 정렬
    return result; // 정렬된 리스트 반환
}

    public bool CanCreateNewSave() // 새 저장본 생성 가능 여부 확인
    {
        return currentSaveFileData.saveList.Count < maxSaveCount; // 최대 개수 미만이면 생성 가능
    }

public bool CreateSave(string newSaveName, List<OwnedCharacterData> startOwnedCharacterList, List<OwnedCharacterStatData> startOwnedCharacterStatList) // 새 저장본 생성
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
    newEntry.notepadPageList = new List<NotepadPageData>(); // 새 세이브의 메모장 페이지 목록 초기화

    currentSaveFileData.nextSaveId++; // 다음 고유 ID 증가
    currentSaveFileData.saveList.Add(newEntry); // 목록에 저장본 추가

    ApplyCurrentOwnedCharacterList(newEntry.ownedCharacterList); // 현재 소유 캐릭터 목록 적용
    ApplyCurrentOwnedCharacterStatList(newEntry.ownedCharacterStatList); // 현재 소유 캐릭터 스탯정보 적용

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
        SaveToFile(); // 빈 파일 저장
        return; // 로드 종료
    }

    string json = File.ReadAllText(path); // 파일 텍스트 읽기

    if (string.IsNullOrEmpty(json))
    {
        currentSaveFileData = new SaveFileData(); // 비어 있으면 새 데이터 생성
        currentOwnedCharacterList = new List<OwnedCharacterData>(); // 현재 소유 캐릭터 목록 초기화
        currentOwnedCharacterStatList = new List<OwnedCharacterStatData>(); // 현재 캐릭터 스탯정보 목록 초기화
        SaveToFile(); // 빈 파일 저장
        return; // 로드 종료
    }

    SaveFileData loadedData = JsonUtility.FromJson<SaveFileData>(json); // JSON 역직렬화

    if (loadedData == null)
    {
        currentSaveFileData = new SaveFileData(); // 실패 시 새 데이터 생성
        currentOwnedCharacterList = new List<OwnedCharacterData>(); // 현재 소유 캐릭터 목록 초기화
        currentOwnedCharacterStatList = new List<OwnedCharacterStatData>(); // 현재 캐릭터 스탯정보 목록 초기화
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

        if (currentSaveFileData.saveList[i].notepadPageList == null)
        {
            currentSaveFileData.saveList[i].notepadPageList = new List<NotepadPageData>(); // 메모장 페이지 목록 null 방지
        }

        if (currentSaveFileData.saveList[i].saveId <= 0)
        {
            currentSaveFileData.saveList[i].saveId = currentSaveFileData.nextSaveId; // 누락된 고유 ID 보정
            currentSaveFileData.nextSaveId++; // 다음 ID 증가
        }
        else
        {
            currentSaveFileData.nextSaveId = Mathf.Max(currentSaveFileData.nextSaveId, currentSaveFileData.saveList[i].saveId + 1); // 다음 ID 보정
        }
    }

    currentOwnedCharacterList = new List<OwnedCharacterData>(); // 현재 소유 캐릭터 목록 초기화
    currentOwnedCharacterStatList = new List<OwnedCharacterStatData>(); // 현재 소유 캐릭터 스탯정보 목록 초기화
    SortAndReindex(); // 번호 정렬 및 재정렬
    SaveToFile(); // 정리된 상태 다시 저장
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

        copyList.Add(copy); // 복사본 추가
    }

    return copyList; // 복사 리스트 반환
}

public void ApplyCurrentOwnedCharacterList(List<OwnedCharacterData> sourceList) // 현재 소유 캐릭터 목록 적용
{
    currentOwnedCharacterList = GetOwnedCharacterListCopy(sourceList); // 현재 목록을 복사 적용
}

public bool LoadOwnedCharacterListFromSave(int targetSaveId) // 특정 저장본의 캐릭터 목록과 스탯정보를 현재 목록으로 적용
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


public bool LoadOwnedCharacterDataFromSave(int targetSaveId) // 특정 저장본의 캐릭터 목록과 스탯정보를 현재 데이터로 적용
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




















}