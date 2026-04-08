using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCharacterAnimationClip", menuName = "Project Dual/캐릭터 애니메이션 클립")]
public class CharacterAnimationClipSO : ScriptableObject
{
[System.Serializable]
public class AnimationFrameData
{
    [Header("프레임 이미지")]
    [SerializeField] private Sprite frameSprite; // 이 프레임에서 출력할 스프라이트

    [Header("주기값 덮어쓰기 설정")]
    [SerializeField] private bool useOverrideImageChangeInterval; // 이 프레임에서 주기값을 덮어쓸지 여부
    [SerializeField] private float overrideImageChangeInterval = 0.1f; // 덮어쓸 이미지 변경 주기값

    [Header("비주얼 오브젝트 위치 덮어쓰기 설정")]
    [SerializeField] private bool useOverrideVisualLocalPosition; // 이 프레임에서 비주얼 오브젝트 로컬 위치를 덮어쓸지 여부
    [SerializeField] private Vector3 overrideVisualLocalPosition; // 이 프레임에서 덮어쓸 비주얼 오브젝트 로컬 위치값

       [Header("비주얼 오브젝트 스케일 덮어쓰기 설정")]
    [SerializeField] private bool useOverrideVisualLocalScale; // 이 프레임에서 비주얼 오브젝트 로컬 스케일을 덮어쓸지 여부
    [SerializeField] private Vector3 overrideVisualLocalScale = Vector3.one; // 이 프레임에서 덮어쓸 비주얼 오브젝트 로컬 스케일값

    public Sprite FrameSprite => frameSprite; // 프레임 이미지 반환
    public bool UseOverrideImageChangeInterval => useOverrideImageChangeInterval; // 주기값 덮어쓰기 여부 반환
    public float OverrideImageChangeInterval => overrideImageChangeInterval; // 덮어쓸 주기값 반환

    public bool UseOverrideVisualLocalPosition => useOverrideVisualLocalPosition; // 비주얼 로컬 위치 덮어쓰기 여부 반환
    public Vector3 OverrideVisualLocalPosition => overrideVisualLocalPosition; // 덮어쓸 비주얼 로컬 위치값 반환

     public bool UseOverrideVisualLocalScale => useOverrideVisualLocalScale; // 비주얼 로컬 스케일 덮어쓰기 여부 반환
    public Vector3 OverrideVisualLocalScale => overrideVisualLocalScale; // 덮어쓸 비주얼 로컬 스케일값 반환
}

    [Header("프레임 목록")]
    [SerializeField] private List<AnimationFrameData> frameList = new List<AnimationFrameData>(); // 애니메이션 프레임 목록

    public IReadOnlyList<AnimationFrameData> FrameList => frameList; // 프레임 목록 반환
    public int FrameCount => frameList != null ? frameList.Count : 0; // 프레임 개수 반환

    [System.Serializable]
public class EffectSpawnEventData
{
    [Header("이펙트 생성 여부")]
    [SerializeField] private bool useEffectSpawn; // 이 프레임 이펙트 이벤트 사용 여부

    [Header("생성 적용 프레임")]
    [SerializeField] private int spawnFrameIndex; // 이펙트를 생성할 프레임 인덱스

    [Header("이펙트 생성 위치 (-X 방향)")]
    [SerializeField] private Vector3 spawnLocalPositionWhenFacingLeft; // X- 방향일 때 생성 로컬 위치

    [Header("이펙트 생성 위치 (+X 방향)")]
    [SerializeField] private Vector3 spawnLocalPositionWhenFacingRight; // X+ 방향일 때 생성 로컬 위치

    [Header("이펙트 생성 회전 (-X 방향)")]
    [SerializeField] private Vector3 spawnLocalRotationWhenFacingLeft; // X- 방향일 때 생성 로컬 회전값

    [Header("이펙트 생성 회전 (+X 방향)")]
    [SerializeField] private Vector3 spawnLocalRotationWhenFacingRight; // X+ 방향일 때 생성 로컬 회전값

    [Header("이펙트 타임라인 설정")]
    [SerializeField] private float effectStartTimelineTime; // 생성 직후 이 시점까지 재생된 상태로 시작할 시간
    [SerializeField] private float effectLifetime = 1f; // 이펙트 유지 시간

    [Header("이펙트 재생 설정")]
    [SerializeField] private float effectPlaySpeedMultiplier = 1f; // 이펙트 기본 재생속도 배율
    [SerializeField] private GameObject effectPrefab; // 생성할 이펙트 프리팹

    public bool UseEffectSpawn => useEffectSpawn; // 이펙트 생성 여부 반환
    public int SpawnFrameIndex => spawnFrameIndex; // 생성 프레임 인덱스 반환

    public Vector3 SpawnLocalPositionWhenFacingLeft => spawnLocalPositionWhenFacingLeft; // X- 방향 생성 위치 반환
    public Vector3 SpawnLocalPositionWhenFacingRight => spawnLocalPositionWhenFacingRight; // X+ 방향 생성 위치 반환

    public Vector3 SpawnLocalRotationWhenFacingLeft => spawnLocalRotationWhenFacingLeft; // X- 방향 생성 회전 반환
    public Vector3 SpawnLocalRotationWhenFacingRight => spawnLocalRotationWhenFacingRight; // X+ 방향 생성 회전 반환

    public float EffectStartTimelineTime => effectStartTimelineTime; // 이펙트 시작 타임라인 시간 반환
    public float EffectLifetime => effectLifetime; // 이펙트 유지 시간 반환
    public float EffectPlaySpeedMultiplier => effectPlaySpeedMultiplier; // 이펙트 재생속도 배율 반환
    public GameObject EffectPrefab => effectPrefab; // 이펙트 프리팹 반환
}

[Header("프레임 이펙트 이벤트 목록")]
[SerializeField] private List<EffectSpawnEventData> effectSpawnEventList = new List<EffectSpawnEventData>(); // 프레임별 이펙트 이벤트 목록

public IReadOnlyList<EffectSpawnEventData> EffectSpawnEventList => effectSpawnEventList; // 이펙트 이벤트 목록 반환

    public AnimationFrameData GetFrame(int index) // 특정 프레임 데이터 반환
    {
        if (frameList == null || frameList.Count == 0)
        {
            return null; // 프레임이 없으면 null 반환
        }

        if (index < 0 || index >= frameList.Count)
        {
            return null; // 범위를 벗어나면 null 반환
        }

        return frameList[index]; // 해당 프레임 반환
    }

    public EffectSpawnEventData GetEffectSpawnEvent(int index) // 특정 이펙트 이벤트 데이터 반환
{
    if (effectSpawnEventList == null || effectSpawnEventList.Count == 0)
    {
        return null; // 이벤트가 없으면 null 반환
    }

    if (index < 0 || index >= effectSpawnEventList.Count)
    {
        return null; // 범위를 벗어나면 null 반환
    }

    return effectSpawnEventList[index]; // 해당 이펙트 이벤트 반환
}
}