using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 아군별 흑체 복용 수용량 감소 규칙을 관리하는 매니저
/// </summary>
public class FriendlyNigrumIntakeManager : MonoBehaviour
{
    [Serializable]
    public class FriendlyNigrumIntakeRule
    {
        [Header("대상 아군")]
        public FriendlyCharacterDefinition friendlyCharacterDefinition; // 해당될 아군 정의 에셋

        [Header("흑체 수용량 설정")]
        public int maxNigrumCapacity; // 최대 흑체 수용량

        [Header("흑체 감소 설정")]
        public int decreaseIntervalMinute = 1; // 몇 분마다 감소할지
        public int nigrumDecreaseAmountPerInterval = 1; // 주기마다 감소할 흑체 수용량
    }

    [Header("저장 참조")]
    [SerializeField] private SaveStorage saveStorage; // 저장 데이터 관리 스크립트

    [Header("아군 흑체 복용 규칙 목록")]
    [SerializeField] private List<FriendlyNigrumIntakeRule> nigrumIntakeRuleList = new List<FriendlyNigrumIntakeRule>(); // 아군별 흑체 감소 규칙 목록

    private static FriendlyNigrumIntakeManager instance; // 전역 인스턴스

    public static FriendlyNigrumIntakeManager Instance => instance; // 전역 인스턴스 반환

    private void Awake() // 시작 시 전역 인스턴스 설정
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject); // 중복 인스턴스 제거
            return;
        }

        instance = this; // 전역 인스턴스 등록
        DontDestroyOnLoad(gameObject); // 씬 전환 시 유지
    }

    private void Start() // 씬 시작 시 SaveStorage 참조
    {
        FindSaveStorageIfNeeded(); // SaveStorage 자동 참조
    }

    private void FindSaveStorageIfNeeded() // SaveStorage 참조 보정
    {
        if (saveStorage != null)
        {
            return; // 이미 참조가 있으면 종료
        }

        saveStorage = SaveStorage.Instance != null ? SaveStorage.Instance : FindFirstObjectByType<SaveStorage>(); // 인스턴스 우선 탐색
    }

    public void ApplyPassedMinute(int passedMinute) // 흐른 시간만큼 흑체 수용량 감소 처리
    {
        if (passedMinute <= 0)
        {
            return; // 시간이 흐르지 않았으면 종료
        }

        FindSaveStorageIfNeeded(); // SaveStorage 참조 보정

        if (saveStorage == null)
        {
            return; // 저장 참조가 없으면 종료
        }

        for (int i = 0; i < nigrumIntakeRuleList.Count; i++)
        {
            FriendlyNigrumIntakeRule rule = nigrumIntakeRuleList[i]; // 현재 규칙 참조

            if (rule == null)
            {
                continue; // 규칙이 비어 있으면 건너뜀
            }

            if (rule.friendlyCharacterDefinition == null)
            {
                continue; // 대상 아군이 없으면 건너뜀
            }

            saveStorage.ApplyFriendlyNigrumDecrease(
                rule.friendlyCharacterDefinition,
                rule.maxNigrumCapacity,
                rule.decreaseIntervalMinute,
                rule.nigrumDecreaseAmountPerInterval,
                passedMinute
            ); // SaveStorage의 흑체 저장값 갱신
        }
    }

    public int GetMaxNigrumCapacity(FriendlyCharacterDefinition friendlyCharacterDefinition) // 아군 최대 흑체 수용값 반환
{
    if (friendlyCharacterDefinition == null)
    {
        return 0; // 대상 아군이 없으면 0 반환
    }

    for (int i = 0; i < nigrumIntakeRuleList.Count; i++)
    {
        FriendlyNigrumIntakeRule rule = nigrumIntakeRuleList[i]; // 현재 규칙 참조

        if (rule == null || rule.friendlyCharacterDefinition == null)
        {
            continue; // 규칙이 비어 있으면 건너뜀
        }

        if (rule.friendlyCharacterDefinition == friendlyCharacterDefinition)
        {
            return Mathf.Max(0, rule.maxNigrumCapacity); // 최대 수용값 보정 후 반환
        }
    }

    return 0; // 규칙을 찾지 못하면 0 반환
}

public bool HasNigrumIntakeRule(FriendlyCharacterDefinition friendlyCharacterDefinition) // 아군 흑체 복용 규칙 존재 여부 반환
{
    if (friendlyCharacterDefinition == null)
    {
        return false; // 대상 아군이 없으면 false 반환
    }

    for (int i = 0; i < nigrumIntakeRuleList.Count; i++)
    {
        FriendlyNigrumIntakeRule rule = nigrumIntakeRuleList[i]; // 현재 규칙 참조

        if (rule == null || rule.friendlyCharacterDefinition == null)
        {
            continue; // 규칙이 비어 있으면 건너뜀
        }

        if (rule.friendlyCharacterDefinition == friendlyCharacterDefinition)
        {
            return true; // 규칙 목록에 있으면 true 반환
        }
    }

    return false; // 규칙 목록에 없으면 false 반환
}








}