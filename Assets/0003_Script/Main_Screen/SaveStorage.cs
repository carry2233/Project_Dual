using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class SaveStorage : MonoBehaviour
{
    [Serializable]
    public class SaveEntry
    {
        public string saveName; // 저장본 이름
        public int saveNumber; // 저장본 번호
    }

    [Serializable]
    private class SaveFileData
    {
        public List<SaveEntry> saveList = new List<SaveEntry>(); // 저장본 목록
    }

    [Header("저장 설정")]
    [SerializeField] private string saveFileName = "save_data.json"; // 저장 파일 이름
    [SerializeField] private int maxSaveCount = 20; // 최대 저장본 개수

    private SaveFileData currentSaveFileData = new SaveFileData(); // 현재 저장 파일 데이터

    public int MaxSaveCount => maxSaveCount; // 최대 저장본 개수 반환

    private void Awake() // 시작 시 저장 데이터 로드
    {
        LoadFromFile(); // 파일에서 저장 데이터 불러오기
    }

    public List<SaveEntry> GetSaveList() // 저장본 목록 복사 반환
    {
        List<SaveEntry> result = new List<SaveEntry>(); // 반환용 리스트

        for (int i = 0; i < currentSaveFileData.saveList.Count; i++)
        {
            SaveEntry source = currentSaveFileData.saveList[i]; // 원본 저장본 참조

            SaveEntry copy = new SaveEntry(); // 복사용 저장본 생성
            copy.saveName = source.saveName; // 이름 복사
            copy.saveNumber = source.saveNumber; // 번호 복사

            result.Add(copy); // 복사본 추가
        }

        result.Sort((a, b) => a.saveNumber.CompareTo(b.saveNumber)); // 번호 기준 정렬
        return result; // 정렬된 리스트 반환
    }

    public bool CanCreateNewSave() // 새 저장본 생성 가능 여부 확인
    {
        return currentSaveFileData.saveList.Count < maxSaveCount; // 최대 개수 미만이면 생성 가능
    }

    public bool CreateSave(string newSaveName) // 새 저장본 생성
    {
        if (!CanCreateNewSave()) return false; // 최대 개수면 생성 불가

        string trimmedName = newSaveName == null ? string.Empty : newSaveName.Trim(); // 공백 제거 이름 생성
        if (string.IsNullOrEmpty(trimmedName)) return false; // 이름이 비어 있으면 생성 불가

        SaveEntry newEntry = new SaveEntry(); // 새 저장본 생성
        newEntry.saveName = trimmedName; // 저장본 이름 설정
        newEntry.saveNumber = currentSaveFileData.saveList.Count + 1; // 다음 순서 번호 설정

        currentSaveFileData.saveList.Add(newEntry); // 목록에 저장본 추가
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
            SaveToFile(); // 빈 파일 저장
            return; // 로드 종료
        }

        string json = File.ReadAllText(path); // 파일 텍스트 읽기

        if (string.IsNullOrEmpty(json))
        {
            currentSaveFileData = new SaveFileData(); // 비어 있으면 새 데이터 생성
            SaveToFile(); // 빈 파일 저장
            return; // 로드 종료
        }

        SaveFileData loadedData = JsonUtility.FromJson<SaveFileData>(json); // JSON 역직렬화

        if (loadedData == null)
        {
            currentSaveFileData = new SaveFileData(); // 실패 시 새 데이터 생성
            SaveToFile(); // 빈 파일 저장
            return; // 로드 종료
        }

        currentSaveFileData = loadedData; // 로드 데이터 적용

        if (currentSaveFileData.saveList == null)
        {
            currentSaveFileData.saveList = new List<SaveEntry>(); // 리스트 null 방지
        }

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
}