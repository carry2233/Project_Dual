using UnityEngine;

[CreateAssetMenu(fileName = "NewDuelSkillDefinition", menuName = "Project Dual/결투 기술 정의")]
public class DuelSkillDefinitionSO : ScriptableObject
{
[Header("기술 이름")]
[SerializeField] private string skillName; // 기술 이름

[Header("기술 정의용 ID")]
[SerializeField] private int firstRowID; // 기술 정의용 1열 ID
[SerializeField] private int secondRowID; // 기술 정의용 2열 ID

[Header("기술 속도율 범위")]
[SerializeField] private int minimumSpeedRatePercent = 90; // 이 기술의 최소 속도율
[SerializeField] private int maximumSpeedRatePercent = 110; // 이 기술의 최대 속도율

[Header("기술 연출 애니메이션")]
[SerializeField] private CharacterAnimationClipSO dashAnimationClip; // 돌진 시 재생할 애니메이션 클립
[SerializeField] private bool useDashAnimationLoop = false; // 돌진 애니메이션 루프 재생 여부
[SerializeField] private CharacterAnimationClipSO duelResolveAnimationClip; // 결투 판정 시 재생할 애니메이션 클립
[SerializeField] private bool useDuelResolveAnimationLoop = false; // 결투 판정 애니메이션 루프 재생 여부

public string SkillName => skillName; // 기술 이름 반환
public int FirstRowID => firstRowID; // 1열 ID 반환
public int SecondRowID => secondRowID; // 2열 ID 반환
public int MinimumSpeedRatePercent => minimumSpeedRatePercent; // 최소 속도율 반환
public int MaximumSpeedRatePercent => maximumSpeedRatePercent; // 최대 속도율 반환
public CharacterAnimationClipSO DashAnimationClip => dashAnimationClip; // 돌진 애니메이션 반환
public bool UseDashAnimationLoop => useDashAnimationLoop; // 돌진 애니메이션 루프 여부 반환
public CharacterAnimationClipSO DuelResolveAnimationClip => duelResolveAnimationClip; // 판정 애니메이션 반환
public bool UseDuelResolveAnimationLoop => useDuelResolveAnimationLoop; // 판정 애니메이션 루프 여부 반환
}