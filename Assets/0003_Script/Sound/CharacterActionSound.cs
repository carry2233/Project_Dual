using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 캐릭터 행동별 사운드 설정기
/// - 캐릭터 행동에 맞는 사운드 클립과 기본 강도 설정
/// - 결투 결과에 따라 적절한 사운드를 BattleSoundEmitter로 전달
/// </summary>
public class CharacterActionSound : MonoBehaviour
{
    [System.Serializable]
    private class DuelResultSoundData // 결투 결과별 사운드 설정 데이터
    {
        public GlobalGameRuleManager.DuelResultType duelResultType; // 결투 결과 종류
        public AudioClip audioClip; // 재생할 클립
        public int baseVolumePercent = 100; // 기본 강도
        public BattleAudioSettings.AudioGroupType audioGroupType = BattleAudioSettings.AudioGroupType.SFX; // 출력 그룹
    }

    [Header("필수 참조")]
    [SerializeField] private CharacterDuelAI characterDuelAI; // 캐릭터 결투 AI 참조
    [SerializeField] private CharacterStatSystem characterStatSystem; // 캐릭터 스탯 참조
    [SerializeField] private BattleSoundEmitter battleSoundEmitter; // 사운드 출력기 참조

    [Header("결투 결과 사운드 설정")]
    [SerializeField] private List<DuelResultSoundData> duelResultSoundList = new List<DuelResultSoundData>(); // 결과별 사운드 목록

    private readonly Dictionary<GlobalGameRuleManager.DuelResultType, DuelResultSoundData> duelResultSoundDictionary
        = new Dictionary<GlobalGameRuleManager.DuelResultType, DuelResultSoundData>(); // 빠른 조회용 딕셔너리

    private void Awake() // 초기 참조 및 사운드 딕셔너리 구성
    {
        if (characterDuelAI == null)
        {
            characterDuelAI = GetComponent<CharacterDuelAI>(); // 같은 오브젝트의 결투 AI 자동 참조
        }

        if (characterStatSystem == null)
        {
            characterStatSystem = GetComponent<CharacterStatSystem>(); // 같은 오브젝트의 스탯 자동 참조
        }

        if (battleSoundEmitter == null)
        {
            battleSoundEmitter = GetComponent<BattleSoundEmitter>(); // 같은 오브젝트의 Emitter 자동 참조
        }

        RebuildDictionary(); // 딕셔너리 구성
    }

    private void OnValidate() // 인스펙터 값 변경 시 딕셔너리 갱신
    {
        RebuildDictionary(); // 딕셔너리 재구성
    }

    public void PlayDuelResultSound(GlobalGameRuleManager.DuelResultType duelResultType) // 결투 결과 사운드 재생
    {
        if (battleSoundEmitter == null)
        {
            return; // 출력기가 없으면 종료
        }

        if (!duelResultSoundDictionary.TryGetValue(duelResultType, out DuelResultSoundData soundData))
        {
            return; // 설정된 사운드가 없으면 종료
        }

        if (soundData == null || soundData.audioClip == null)
        {
            return; // 클립이 없으면 종료
        }

        battleSoundEmitter.PlaySound(soundData.audioClip, soundData.baseVolumePercent, soundData.audioGroupType); // 출력기로 재생 요청
    }

    private void RebuildDictionary() // 리스트 기반 딕셔너리 구성
    {
        duelResultSoundDictionary.Clear(); // 기존 데이터 초기화

        if (duelResultSoundList == null)
        {
            return; // 리스트가 없으면 종료
        }

        for (int i = 0; i < duelResultSoundList.Count; i++)
        {
            DuelResultSoundData soundData = duelResultSoundList[i]; // 현재 데이터 참조

            if (soundData == null)
            {
                continue; // 비어 있는 데이터는 건너뜀
            }

            duelResultSoundDictionary[soundData.duelResultType] = soundData; // 결과 타입 기준으로 등록
        }
    }
}