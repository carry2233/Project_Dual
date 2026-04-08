using UnityEngine;

/// <summary>
/// 캐릭터 행동 사운드 출력기
/// - 외부에서 전달받은 사운드 데이터를 BattleSoundEmitter로 재생
/// </summary>
public class CharacterActionSound : MonoBehaviour
{
    [Header("필수 참조")]
    [SerializeField] private BattleSoundEmitter battleSoundEmitter; // 사운드 출력기 참조

    private void Awake() // 초기 참조 자동 연결
    {
        if (battleSoundEmitter == null)
        {
            battleSoundEmitter = GetComponent<BattleSoundEmitter>(); // 같은 오브젝트의 Emitter 자동 참조
        }
    }

public void PlaySound(
    AudioClip audioClip, // 재생할 사운드 클립
    float volume, // 사운드 크기 값
    BattleAudioSettings.AudioGroupType audioGroupType) // 출력 그룹
{
    if (battleSoundEmitter == null)
    {
        return; // 출력기가 없으면 종료
    }

    if (audioClip == null)
    {
        return; // 클립이 없으면 종료
    }

    battleSoundEmitter.PlaySound(audioClip, volume, audioGroupType); // 출력기로 재생 요청
}
}