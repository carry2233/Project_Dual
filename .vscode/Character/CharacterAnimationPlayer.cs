using UnityEngine;
using System.Collections; // 코루틴 사용
using System.Collections.Generic;


public class CharacterAnimationPlayer : MonoBehaviour
{
    [Header("필수 참조")]
    [SerializeField] private PerObjectTextureOverride perObjectTextureOverride; // 실제 이미지 출력 담당 참조
    [SerializeField] private NavigationMovementSystem navigationMovementSystem; // 이동 상태 확인용 참조
    [SerializeField] private CharacterDuelAI characterDuelAI; // 결투 상태 확인용 참조

    [Header("기본 자동 재생 애니메이션")]
    [SerializeField] private CharacterAnimationClipSO idleAnimationClip; // 대기 상태 자동 재생 클립
    [SerializeField] private CharacterAnimationClipSO moveAnimationClip; // 이동 상태 자동 재생 클립

    [Header("기본 이미지 변경 주기")]
    [SerializeField] private float idleDefaultImageChangeInterval = 0.2f; // 대기 상태 기본 주기
    [SerializeField] private float moveDefaultImageChangeInterval = 0.1f; // 이동 상태 기본 주기

[Header("현재 재생 상태")]
[SerializeField] private CharacterAnimationClipSO currentAnimationClip; // 현재 재생 중인 클립
[SerializeField] private int currentFrameIndex; // 현재 프레임 인덱스
[SerializeField] private float currentFrameTimer; // 현재 프레임 경과 시간
[SerializeField] private bool isManualAnimationPlaying; // 현재 수동 재생 상태로 적용 중인지 여부
[SerializeField] private bool isManualAnimationLoop; // 현재 수동 재생 루프 여부

[Header("캐릭터 비주얼 오브젝트 참조")]
[SerializeField] private Transform characterVisualObject; // 프레임 데이터에 따라 로컬 위치를 덮어쓸 비주얼 오브젝트

[Header("잔상 생성 위치 설정")]
[SerializeField] private Transform afterimageSpawnPoint; // 잔상 생성 위치 기준 오브젝트

[Header("이펙트 생성 부모 오브젝트 참조")]
[SerializeField] private Transform effectParentRoot; // 생성된 이펙트 프리팹을 자식으로 둘 부모 오브젝트

private readonly System.Collections.Generic.HashSet<int> playedEffectEventIndexSet
    = new System.Collections.Generic.HashSet<int>(); // 현재 재생 사이클에서 이미 실행한 이펙트 이벤트 인덱스 저장용

[Header("잔상 생성 설정")]
[SerializeField] private CharacterAfterimageObject afterimagePrefab; // 잔상 오브젝트 프리팹
[SerializeField] private float afterimageSpawnInterval = 0.08f; // 잔상 생성 주기
[SerializeField] private int afterimageStartAlphaPercent = 100; // 잔상 시작 투명도 퍼센트
[SerializeField] private AnimationCurve afterimageFadeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f); // 잔상 투명도 감소 커브
[SerializeField] private float afterimageFadeDuration = 0.2f; // 잔상 사라지는 시간

[Header("잔상 방향별 회전값")]
[SerializeField] private Vector3 afterimageRotationWhenFacingLeft = Vector3.zero; // 왼쪽(X-) 방향일 때 잔상 회전값
[SerializeField] private Vector3 afterimageRotationWhenFacingRight = Vector3.zero; // 오른쪽(X+) 방향일 때 잔상 회전값

[Header("잔상 풀링 상태")]
[SerializeField] private Transform afterimagePoolRoot; // 잔상 오브젝트 부모용 루트
[SerializeField] private int pooledAfterimageCount; // 현재 풀에 생성된 잔상 개수

[Header("이미지 고정 상태")]
[SerializeField] private bool isImageFrozen; // 현재 이미지 고정 상태 여부

[Header("결투 이펙트 생성 부모 오브젝트 참조")]
[SerializeField] private Transform duelEffectParentRoot; // 본인 기준 결투 이펙트 부모 오브젝트
[SerializeField] private Transform duelWorldEffectParentRoot; // 캐릭터 사이 위치 생성용 월드 이펙트 부모 오브젝트


[System.Serializable]
public class DamageFloaterSpawnSettings
{
    [Header("유지 시간")]
    [SerializeField] private float lifetime = 1f; // 플로터 전체 유지 시간

    [Header("투명 설정")]
    [SerializeField] private float fadeStartTime = 0.2f; // 텍스트 투명화 시작 시점
    [SerializeField] private int minimumAlphaPercent = 0; // 최소 투명률(%)
    [SerializeField] private float fadeDurationToMinimumAlpha = 0.3f; // 최소 투명률까지 도달 시간
    [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f); // 투명 변화 커브

    [Header("이동 설정")]
    [SerializeField] private Vector3 moveDirection = new Vector3(0.5f, 1f, 0f); // 이동 방향
    [SerializeField] private float moveStartTime = 0f; // 이동 시작 시점
    [SerializeField] private float moveDuration = 0.5f; // 이동 종료까지 걸리는 시간
    [SerializeField] private AnimationCurve moveCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f); // 이동 커브

    public float Lifetime => lifetime; // 유지 시간 반환
    public float FadeStartTime => fadeStartTime; // 투명 시작 시점 반환
    public int MinimumAlphaPercent => minimumAlphaPercent; // 최소 투명률 반환
    public float FadeDurationToMinimumAlpha => fadeDurationToMinimumAlpha; // 최소 투명률 도달 시간 반환
    public AnimationCurve FadeCurve => fadeCurve; // 투명 커브 반환
    public Vector3 MoveDirection => moveDirection; // 이동 방향 반환
    public float MoveStartTime => moveStartTime; // 이동 시작 시점 반환
    public float MoveDuration => moveDuration; // 이동 시간 반환
    public AnimationCurve MoveCurve => moveCurve; // 이동 커브 반환
}

[Header("데미지 플로터 생성 부모 오브젝트 참조")]
[SerializeField] private Transform damageFloaterSpawnParent; // 데미지 플로터를 자식으로 둘 부모 오브젝트

[Header("체력피해 플로터 설정")]
[SerializeField] private DamageFloaterCanvasObject healthDamageFloaterPrefab; // 체력피해 표시용 월드 캔버스 프리팹
[SerializeField] private DamageFloaterSpawnSettings healthDamageFloaterSettings = new DamageFloaterSpawnSettings(); // 체력피해 표시 설정

[Header("와해피해 플로터 설정")]
[SerializeField] private DamageFloaterCanvasObject staggerDamageFloaterPrefab; // 와해피해 표시용 월드 캔버스 프리팹
[SerializeField] private DamageFloaterSpawnSettings staggerDamageFloaterSettings = new DamageFloaterSpawnSettings(); // 와해피해 표시 설정

[Header("데미지 플로터 풀링 상태")]
[SerializeField] private Transform damageFloaterPoolRoot; // 데미지 플로터 풀 부모 오브젝트
[SerializeField] private int pooledHealthDamageFloaterCount; // 현재 생성된 체력피해 플로터 수
[SerializeField] private int pooledStaggerDamageFloaterCount; // 현재 생성된 와해피해 플로터 수

private readonly List<DamageFloaterCanvasObject> healthDamageFloaterPoolList
    = new List<DamageFloaterCanvasObject>(); // 체력피해 플로터 풀 리스트

private readonly List<DamageFloaterCanvasObject> staggerDamageFloaterPoolList
    = new List<DamageFloaterCanvasObject>(); // 와해피해 플로터 풀 리스트

public Transform DuelEffectParentRoot => duelEffectParentRoot; // 본인 결투 이펙트 부모 오브젝트 반환

private readonly System.Collections.Generic.List<CharacterAfterimageObject> afterimagePoolList
    = new System.Collections.Generic.List<CharacterAfterimageObject>(); // 잔상 풀 리스트

private float afterimageSpawnTimer; // 다음 잔상 생성까지 누적 시간
private bool wasDashingLastFrame; // 이전 프레임 돌진 여부

private CharacterAnimationClipSO requestedManualAnimationClip; // 외부에서 요청한 수동 애니메이션 클립
private bool requestedManualAnimationLoop; // 외부에서 요청한 수동 애니메이션 루프 여부

private void Awake() // 시작 시 참조 자동 연결
{
    if (perObjectTextureOverride == null)
    {
        perObjectTextureOverride = GetComponent<PerObjectTextureOverride>(); // 같은 오브젝트에서 자동 참조
    }

    if (navigationMovementSystem == null)
    {
        navigationMovementSystem = GetComponent<NavigationMovementSystem>(); // 같은 오브젝트에서 자동 참조
    }

    if (characterDuelAI == null)
    {
        characterDuelAI = GetComponent<CharacterDuelAI>(); // 같은 오브젝트에서 자동 참조
    }

    if (afterimageSpawnPoint == null)
    {
        afterimageSpawnPoint = transform; // 생성 위치를 따로 안 넣었으면 자기 자신 위치 사용
    }

    if (effectParentRoot == null)
    {
        effectParentRoot = transform; // 따로 지정하지 않으면 자기 자신을 부모로 사용
    }

    if (duelEffectParentRoot == null)
    {
        duelEffectParentRoot = transform; // 따로 지정하지 않으면 자기 자신을 결투 이펙트 부모로 사용
    }

    if (duelWorldEffectParentRoot == null)
    {
        duelWorldEffectParentRoot = transform; // 따로 지정하지 않으면 자기 자신을 월드 이펙트 부모로 사용
    }

    if (damageFloaterSpawnParent == null)
    {
        damageFloaterSpawnParent = transform; // 따로 지정하지 않으면 자기 자신을 데미지 플로터 부모로 사용
    }

    CreateAfterimagePoolRootIfNeeded(); // 잔상 풀 부모 오브젝트 준비
    CreateDamageFloaterPoolRootIfNeeded(); // 데미지 플로터 풀 부모 오브젝트 준비
}

private void Update() // 매 프레임 애니메이션 상태 처리
{
    if (isImageFrozen)
    {
        return; // 이미지 고정 중이면 현재 출력 이미지 유지
    }

    UpdateCurrentAnimationState(); // 현재 상태 기준으로 재생할 클립 결정
    UpdateAnimationPlayback(); // 현재 클립 재생 처리
    UpdateAfterimageGeneration(); // 돌진 잔상 생성 처리
}

public void PlayLoopAnimation(CharacterAnimationClipSO clip) // 루프 수동 애니메이션 재생 요청
{
    if (clip == null || clip.FrameCount == 0)
    {
        return; // 클립이 없거나 비어 있으면 종료
    }

    requestedManualAnimationClip = clip; // 수동 재생 요청 클립 저장
    requestedManualAnimationLoop = true; // 루프 재생 요청 저장
    UpdateCurrentAnimationState(); // 현재 상태 기준 즉시 반영
}

public void PlayOneShotAnimation(CharacterAnimationClipSO clip) // 1회 수동 애니메이션 재생 요청
{
    if (clip == null || clip.FrameCount == 0)
    {
        return; // 클립이 없거나 비어 있으면 종료
    }

    requestedManualAnimationClip = clip; // 수동 재생 요청 클립 저장
    requestedManualAnimationLoop = false; // 1회 재생 요청 저장
    UpdateCurrentAnimationState(); // 현재 상태 기준 즉시 반영
}

public void StopManualAnimation() // 수동 애니메이션 종료
{
    requestedManualAnimationClip = null; // 요청된 수동 애니메이션 제거
    requestedManualAnimationLoop = false; // 요청된 루프 정보 초기화
    isManualAnimationPlaying = false; // 현재 수동 재생 상태 해제
    isManualAnimationLoop = false; // 현재 수동 루프 상태 해제
    currentFrameTimer = 0f; // 프레임 타이머 초기화
    UpdateCurrentAnimationState(); // 현재 상태 기준 자동 애니메이션 즉시 반영
}

    private void UpdateAutoAnimationSelection() // 자동 상태 기준 클립 선택
    {
        CharacterAnimationClipSO targetClip = idleAnimationClip; // 기본은 대기 클립 사용

        if (navigationMovementSystem != null && navigationMovementSystem.IsMoving)
        {
            targetClip = moveAnimationClip; // 이동 중이면 이동 클립 사용
        }

        if (currentAnimationClip != targetClip)
        {
            SetAnimationClip(targetClip); // 현재 클립과 다르면 교체
        }
    }

private void SetAnimationClip(CharacterAnimationClipSO newClip) // 현재 클립 교체
{
    playedEffectEventIndexSet.Clear(); // 클립이 바뀌면 이펙트 실행 기록 초기화

    if (newClip == null || newClip.FrameCount == 0)
    {
        currentAnimationClip = null; // 비어 있으면 현재 클립 제거
        currentFrameIndex = 0; // 프레임 인덱스 초기화
        currentFrameTimer = 0f; // 프레임 타이머 초기화
        return;
    }

    currentAnimationClip = newClip; // 현재 클립 저장
    currentFrameIndex = 0; // 첫 프레임부터 시작
    currentFrameTimer = 0f; // 프레임 시간 초기화
    ApplyCurrentFrame(); // 첫 프레임 즉시 적용
}

    private void UpdateAnimationPlayback() // 현재 클립 재생 진행
    {
        if (currentAnimationClip == null || currentAnimationClip.FrameCount == 0)
        {
            return; // 재생할 클립이 없으면 종료
        }

        float currentInterval = GetCurrentFrameInterval(); // 현재 프레임에서 사용할 주기값 계산
        currentFrameTimer += Time.deltaTime; // 프레임 시간 누적

        if (currentFrameTimer < currentInterval)
        {
            return; // 아직 다음 프레임으로 넘어갈 시간이 아니면 종료
        }

        currentFrameTimer = 0f; // 프레임 타이머 초기화
        AdvanceFrame(); // 다음 프레임으로 진행
    }

private void AdvanceFrame() // 다음 프레임으로 이동
{
    if (currentAnimationClip == null || currentAnimationClip.FrameCount == 0)
    {
        return; // 현재 클립이 없으면 종료
    }

    int lastFrameIndex = currentAnimationClip.FrameCount - 1; // 마지막 프레임 인덱스 계산

    if (currentFrameIndex >= lastFrameIndex)
    {
        if (isManualAnimationPlaying && !isManualAnimationLoop)
        {
            requestedManualAnimationClip = null; // 1회 수동 애니메이션 재생이 끝나면 요청 제거
            requestedManualAnimationLoop = false; // 요청된 루프 정보 초기화
            isManualAnimationPlaying = false; // 현재 수동 재생 상태 해제
            isManualAnimationLoop = false; // 현재 수동 루프 상태 해제
            UpdateCurrentAnimationState(); // 현재 상태에 맞는 애니메이션으로 즉시 전환
            return;
        }

        currentFrameIndex = 0; // 루프 재생이면 첫 프레임으로 복귀
        playedEffectEventIndexSet.Clear(); // 새 루프 사이클 시작이므로 이펙트 실행 기록 초기화
        ApplyCurrentFrame(); // 첫 프레임 적용
        return;
    }

    currentFrameIndex++; // 다음 프레임으로 증가
    ApplyCurrentFrame(); // 다음 프레임 적용
}

private void ApplyCurrentFrame() // 현재 프레임 이미지와 위치/스케일 덮어쓰기 적용
{
    if (perObjectTextureOverride == null || currentAnimationClip == null)
    {
        return; // 출력기 또는 클립이 없으면 종료
    }

    CharacterAnimationClipSO.AnimationFrameData frameData = currentAnimationClip.GetFrame(currentFrameIndex); // 현재 프레임 데이터 가져오기

    if (frameData == null)
    {
        return; // 프레임 데이터가 없으면 종료
    }

    if (frameData.FrameSprite != null)
    {
        perObjectTextureOverride.SetSprite(frameData.FrameSprite); // 현재 프레임 스프라이트 적용
    }

    if (characterVisualObject != null && frameData.UseOverrideVisualLocalPosition)
    {
        characterVisualObject.localPosition = frameData.OverrideVisualLocalPosition; // 체크된 프레임에서만 비주얼 오브젝트 로컬 위치 덮어쓰기
    }

    if (characterVisualObject != null && frameData.UseOverrideVisualLocalScale)
    {
        characterVisualObject.localScale = frameData.OverrideVisualLocalScale; // 체크된 프레임에서만 비주얼 오브젝트 로컬 스케일 덮어쓰기
    }

    TrySpawnEffectEventsAtCurrentFrame(); // 현재 프레임에 해당하는 이펙트 이벤트 실행 시도
}

private float GetCurrentFrameInterval() // 현재 프레임 주기값 계산
{
    float baseInterval = GetCurrentBaseInterval(); // 상태별 기본 주기 가져오기

    if (currentAnimationClip == null)
    {
        return Mathf.Max(0.01f, baseInterval); // 클립 없으면 상태 기본값
    }

    CharacterAnimationClipSO.AnimationFrameData frameData = currentAnimationClip.GetFrame(currentFrameIndex);

    if (frameData == null)
    {
        return Mathf.Max(0.01f, baseInterval);
    }

    if (frameData.UseOverrideImageChangeInterval)
    {
        return Mathf.Max(0.01f, frameData.OverrideImageChangeInterval); // override 우선
    }

    return Mathf.Max(0.01f, baseInterval); // 상태 기본 주기 사용
}

private float GetCurrentBaseInterval() // 현재 상태에 따른 기본 주기 반환
{
    if (navigationMovementSystem != null && navigationMovementSystem.IsMoving)
    {
        return moveDefaultImageChangeInterval; // 이동 상태
    }

    return idleDefaultImageChangeInterval; // 대기 상태
}

private void UpdateCurrentAnimationState() // 현재 상태 기준으로 재생할 클립 결정 및 즉시 반영
{
    CharacterAnimationClipSO targetClip = idleAnimationClip; 
    bool shouldUseManualState = false;
    bool targetManualLoop = false;

    // ✅ 결투 보호 상태일 때만 수동 애니 허용 (핵심 수정)
    if (characterDuelAI != null
        && characterDuelAI.IsDuelAnimationProtectedState
        && requestedManualAnimationClip != null)
    {
        targetClip = requestedManualAnimationClip;
        shouldUseManualState = true;
        targetManualLoop = requestedManualAnimationLoop;
    }
    else if (navigationMovementSystem != null && navigationMovementSystem.IsMoving)
    {
        targetClip = moveAnimationClip;
        shouldUseManualState = false;
        targetManualLoop = false;
    }
    else
    {
        // ✅ 기존: 요청된 수동 애니 사용 가능
        // ❌ 수정: 결투 상태 아닐 때는 무조건 idle로 강제
        targetClip = idleAnimationClip;
        shouldUseManualState = false;
        targetManualLoop = false;
    }

    bool clipChanged = currentAnimationClip != targetClip;
    bool manualStateChanged = isManualAnimationPlaying != shouldUseManualState;
    bool manualLoopChanged = isManualAnimationLoop != targetManualLoop;

    if (!clipChanged && !manualStateChanged && !manualLoopChanged)
    {
        return;
    }

    isManualAnimationPlaying = shouldUseManualState;
    isManualAnimationLoop = targetManualLoop;
    SetAnimationClip(targetClip);
}

public void RefreshAnimationByCurrentState() // 현재 상태 기준 애니메이션 즉시 갱신
{
    UpdateCurrentAnimationState(); // 현재 상태 반영
}

private void CreateAfterimagePoolRootIfNeeded() // 잔상 풀 부모 오브젝트 생성
{
    if (afterimagePoolRoot != null)
    {
        return; // 이미 있으면 종료
    }

    GameObject poolRootObject = new GameObject($"{name}_AfterimagePoolRoot"); // 부모 오브젝트 생성
    afterimagePoolRoot = poolRootObject.transform; // 루트 저장
    afterimagePoolRoot.position = Vector3.zero; // 기본 위치 설정
    afterimagePoolRoot.rotation = Quaternion.identity; // 기본 회전 설정
}

private void UpdateAfterimageGeneration() // 돌진 상태 기준 잔상 생성 처리
{
    bool isCurrentlyDashing = characterDuelAI != null && characterDuelAI.IsDashingToDuel; // 현재 돌진 여부 확인

    if (!isCurrentlyDashing)
    {
        wasDashingLastFrame = false; // 돌진 종료 상태 저장
        afterimageSpawnTimer = 0f; // 생성 타이머 초기화
        return; // 돌진 중이 아니면 종료
    }

    if (!wasDashingLastFrame)
    {
        SpawnAfterimage(); // 돌진 시작 즉시 1회 생성
        wasDashingLastFrame = true; // 이전 프레임 돌진 상태 갱신
        afterimageSpawnTimer = 0f; // 타이머 초기화
        return;
    }

    afterimageSpawnTimer += Time.deltaTime; // 생성 타이머 누적
    float safeInterval = Mathf.Max(0.01f, afterimageSpawnInterval); // 최소 주기 보정

    while (afterimageSpawnTimer >= safeInterval)
    {
        afterimageSpawnTimer -= safeInterval; // 주기만큼 차감
        SpawnAfterimage(); // 주기마다 잔상 생성
    }
}

private void SpawnAfterimage() // 현재 상태 기준 잔상 생성
{
    if (afterimagePrefab == null)
    {
        return; // 프리팹이 없으면 종료
    }

    Sprite currentSprite = GetCurrentDisplayedSprite(); // 현재 표시 중인 스프라이트 가져오기

    if (currentSprite == null)
    {
        return; // 스프라이트가 없으면 종료
    }

    CharacterAfterimageObject afterimageObject = GetPooledAfterimageObject(); // 풀에서 잔상 오브젝트 가져오기

    if (afterimageObject == null)
    {
        return; // 가져오지 못했으면 종료
    }

    Quaternion spawnRotation = GetCurrentAfterimageRotation(); // 현재 방향 상태 기준 회전 계산
    Vector3 spawnPosition = afterimageSpawnPoint != null ? afterimageSpawnPoint.position : transform.position; // 참조 오브젝트 위치 기준 생성 위치 계산

    afterimageObject.gameObject.SetActive(true); // 사용 전 활성화
    afterimageObject.InitializeAfterimage(
        currentSprite, // 현재 스프라이트 전달
        spawnPosition, // 참조 오브젝트 위치 전달
        spawnRotation, // 방향 기준 회전 전달
        afterimageStartAlphaPercent, // 시작 투명도 전달
        afterimageFadeCurve, // 감소 커브 전달
        afterimageFadeDuration); // 사라지는 시간 전달
}

private CharacterAfterimageObject GetPooledAfterimageObject() // 비활성 잔상 오브젝트를 가져오거나 새로 생성
{
    for (int i = 0; i < afterimagePoolList.Count; i++)
    {
        CharacterAfterimageObject pooledObject = afterimagePoolList[i]; // 현재 풀 요소 참조

        if (pooledObject == null)
        {
            continue; // 비어 있으면 건너뜀
        }

        if (!pooledObject.gameObject.activeSelf)
        {
            return pooledObject; // 비활성 오브젝트 재사용
        }
    }

    if (afterimagePoolRoot == null)
    {
        CreateAfterimagePoolRootIfNeeded(); // 루트가 없으면 생성
    }

    CharacterAfterimageObject newObject = Instantiate(afterimagePrefab, afterimagePoolRoot); // 새 잔상 생성
    newObject.gameObject.SetActive(false); // 생성 직후 비활성화
    afterimagePoolList.Add(newObject); // 풀 목록에 등록
    pooledAfterimageCount = afterimagePoolList.Count; // 풀 개수 갱신
    return newObject; // 새 오브젝트 반환
}

private Quaternion GetCurrentAfterimageRotation() // 현재 방향 상태 기준 회전값 계산
{
    if (navigationMovementSystem == null)
    {
        return Quaternion.Euler(afterimageRotationWhenFacingRight); // 기본은 오른쪽 회전 사용
    }

    if (navigationMovementSystem.CurrentFacingXDirection == NavigationMovementSystem.FacingXDirectionType.XNegative)
    {
        return Quaternion.Euler(afterimageRotationWhenFacingLeft); // 왼쪽 방향 회전 반환
    }

    return Quaternion.Euler(afterimageRotationWhenFacingRight); // 오른쪽 방향 회전 반환
}

private Sprite GetCurrentDisplayedSprite() // 현재 재생 중 프레임의 스프라이트 반환
{
    if (currentAnimationClip == null)
    {
        return null; // 현재 클립이 없으면 종료
    }

    CharacterAnimationClipSO.AnimationFrameData frameData = currentAnimationClip.GetFrame(currentFrameIndex); // 현재 프레임 데이터 가져오기

    if (frameData == null)
    {
        return null; // 프레임 데이터가 없으면 종료
    }

    return frameData.FrameSprite; // 현재 프레임 스프라이트 반환
}

private void TrySpawnEffectEventsAtCurrentFrame() // 현재 프레임에 해당하는 이펙트 이벤트 실행 시도
{
    if (currentAnimationClip == null)
    {
        return; // 현재 클립이 없으면 종료
    }

    IReadOnlyList<CharacterAnimationClipSO.EffectSpawnEventData> effectEventList = currentAnimationClip.EffectSpawnEventList; // 현재 클립의 이펙트 이벤트 목록 참조

    if (effectEventList == null || effectEventList.Count == 0)
    {
        return; // 이벤트 목록이 없으면 종료
    }

    for (int i = 0; i < effectEventList.Count; i++)
    {
        CharacterAnimationClipSO.EffectSpawnEventData eventData = effectEventList[i]; // 현재 이벤트 데이터 참조

        if (eventData == null)
        {
            continue; // 비어 있으면 건너뜀
        }

        if (!eventData.UseEffectSpawn)
        {
            continue; // 사용하지 않는 이벤트면 건너뜀
        }

        if (eventData.SpawnFrameIndex != currentFrameIndex)
        {
            continue; // 현재 프레임과 생성 프레임이 다르면 건너뜀
        }

        if (playedEffectEventIndexSet.Contains(i))
        {
            continue; // 현재 재생 사이클에서 이미 실행한 이벤트면 건너뜀
        }

        SpawnEffectEvent(eventData); // 현재 프레임 이펙트 생성
        playedEffectEventIndexSet.Add(i); // 실행 완료한 이벤트로 기록
    }
}

private void SpawnEffectEvent(CharacterAnimationClipSO.EffectSpawnEventData eventData) // 이펙트 이벤트 데이터 기준 프리팹 생성
{
    if (eventData == null)
    {
        return; // 이벤트 데이터가 없으면 종료
    }

    if (eventData.EffectPrefab == null)
    {
        return; // 프리팹이 없으면 종료
    }

    Transform parentRoot = effectParentRoot != null ? effectParentRoot : transform; // 실제 부모 오브젝트 결정
    GameObject spawnedEffectObject = Instantiate(eventData.EffectPrefab, parentRoot); // 부모 오브젝트 자식으로 이펙트 생성

    bool isFacingLeft = IsCurrentFacingLeft(); // 현재 캐릭터가 X- 방향인지 확인
    Vector3 localPosition = isFacingLeft
        ? eventData.SpawnLocalPositionWhenFacingLeft
        : eventData.SpawnLocalPositionWhenFacingRight; // 방향별 생성 위치 결정

    Vector3 localRotationEuler = isFacingLeft
        ? eventData.SpawnLocalRotationWhenFacingLeft
        : eventData.SpawnLocalRotationWhenFacingRight; // 방향별 생성 회전 결정

    spawnedEffectObject.transform.localPosition = localPosition; // 로컬 위치 적용
    spawnedEffectObject.transform.localRotation = Quaternion.Euler(localRotationEuler); // 로컬 회전 적용
    spawnedEffectObject.transform.localScale = Vector3.one; // 기본 스케일 유지

    CharacterEffectInstance effectInstance = spawnedEffectObject.GetComponent<CharacterEffectInstance>(); // 이펙트 인스턴스 관리 스크립트 참조

    if (effectInstance == null)
    {
        effectInstance = spawnedEffectObject.AddComponent<CharacterEffectInstance>(); // 없으면 자동 추가
    }

    effectInstance.InitializeEffectInstance(
        eventData.EffectStartTimelineTime, // 이 시점까지 재생된 상태로 시작
        eventData.EffectLifetime, // 유지 시간
        eventData.EffectPlaySpeedMultiplier); // 재생속도 배율
}

private bool IsCurrentFacingLeft() // 현재 캐릭터가 X- 방향인지 여부 반환
{
    if (navigationMovementSystem == null)
    {
        return false; // 이동 시스템이 없으면 기본은 X+ 방향 취급
    }

    return navigationMovementSystem.CurrentFacingXDirection == NavigationMovementSystem.FacingXDirectionType.XNegative; // 현재 X- 방향 여부 반환
}

public void SetImageFrozen(bool frozen) // 현재 출력 이미지를 유지한 채 애니 갱신 정지/해제
{
    isImageFrozen = frozen; // 이미지 고정 상태 저장

    if (!isImageFrozen)
    {
        UpdateCurrentAnimationState(); // 해제 시 현재 상태 기준 애니메이션 즉시 반영
    }
}

public void PlayDuelResolveEffect(
    DuelSkillDefinitionSO duelSkillDefinition, // 현재 결투 기술 정의
    GlobalGameRuleManager.DuelResultType resultType, // 이번 결투 결과
    CharacterDuelAI otherCharacter) // 결투한 상대 캐릭터
{
    if (duelSkillDefinition == null) // 기술 정의가 없으면 종료
    {
        return;
    }

    if (!duelSkillDefinition.TryGetResolveEffectData(resultType, out DuelSkillDefinitionSO.DuelResultEffectData effectData)) // 결과별 이펙트 데이터 조회
    {
        return;
    }

    if (effectData == null) // 데이터가 없으면 종료
    {
        return;
    }

    if (effectData.UseBetweenCharactersEffect) // 두 캐릭터 사이 위치 생성 사용 시
    {
        StartCoroutine(SpawnDuelEffectAfterDelay(
            effectData.BetweenCharactersEffectData, // 두 캐릭터 사이 생성 데이터
            DuelEffectSpawnType.BetweenCharacters, // 생성 방식
            otherCharacter)); // 상대 캐릭터 전달
    }

    if (effectData.UseSelfParentEffect) // 본인 부모 생성 사용 시
    {
        StartCoroutine(SpawnDuelEffectAfterDelay(
            effectData.SelfParentEffectData, // 본인 부모 생성 데이터
            DuelEffectSpawnType.SelfParent, // 생성 방식
            otherCharacter)); // 상대 캐릭터 전달
    }

    if (effectData.UseTargetParentEffect) // 상대 부모 생성 사용 시
    {
        StartCoroutine(SpawnDuelEffectAfterDelay(
            effectData.TargetParentEffectData, // 상대 부모 생성 데이터
            DuelEffectSpawnType.TargetParent, // 생성 방식
            otherCharacter)); // 상대 캐릭터 전달
    }
}

private enum DuelEffectSpawnType
{
    BetweenCharacters, // 두 캐릭터 사이 월드 위치 생성
    SelfParent, // 본인 결투 이펙트 부모 하위 생성
    TargetParent // 상대 결투 이펙트 부모 하위 생성
}

private IEnumerator SpawnDuelEffectAfterDelay(
    DuelSkillDefinitionSO.DuelEffectSpawnData spawnData, // 실제 생성 데이터
    DuelEffectSpawnType spawnType, // 생성 방식
    CharacterDuelAI otherCharacter) // 상대 캐릭터
{
    if (spawnData == null) // 생성 데이터가 없으면 종료
    {
        yield break;
    }

    if (spawnData.EffectPrefab == null) // 프리팹이 없으면 종료
    {
        yield break;
    }

    float safeDelay = Mathf.Max(0f, spawnData.SpawnDelay); // 음수 방지된 생성 딜레이

    if (safeDelay > 0f) // 생성 딜레이가 있으면 대기
    {
        yield return new WaitForSeconds(safeDelay);
    }

    SpawnDuelEffectNow(spawnData, spawnType, otherCharacter); // 실제 이펙트 생성
}

private void SpawnDuelEffectNow(
    DuelSkillDefinitionSO.DuelEffectSpawnData spawnData, // 실제 생성 데이터
    DuelEffectSpawnType spawnType, // 생성 방식
    CharacterDuelAI otherCharacter) // 상대 캐릭터
{
    if (spawnData == null || spawnData.EffectPrefab == null) // 필수 데이터가 없으면 종료
    {
        return;
    }

    Transform parentRoot = GetDuelEffectParentBySpawnType(spawnType, otherCharacter); // 생성 방식에 맞는 부모 결정
    GameObject spawnedEffectObject = Instantiate(spawnData.EffectPrefab, parentRoot); // 이펙트 프리팹 생성

    ApplyDuelEffectTransform(
        spawnedEffectObject.transform, // 생성된 이펙트 Transform
        spawnData, // 실제 생성 데이터
        spawnType, // 생성 방식
        otherCharacter); // 상대 캐릭터 전달

    CharacterEffectInstance effectInstance = spawnedEffectObject.GetComponent<CharacterEffectInstance>(); // 이펙트 인스턴스 스크립트 참조

    if (effectInstance == null) // 스크립트가 없으면 자동 추가
    {
        effectInstance = spawnedEffectObject.AddComponent<CharacterEffectInstance>();
    }

    effectInstance.InitializeEffectInstance(
        spawnData.EffectStartTimelineTime, // 생성 시 시작 타임라인 시간
        spawnData.EffectLifetime, // 생성 후 유지 시간
        spawnData.EffectPlaySpeedMultiplier); // 이펙트 재생 기본속도 배율
}

private Transform GetDuelEffectParentBySpawnType(
    DuelEffectSpawnType spawnType, // 생성 방식
    CharacterDuelAI otherCharacter) // 상대 캐릭터
{
    switch (spawnType)
    {
        case DuelEffectSpawnType.BetweenCharacters:
            return duelWorldEffectParentRoot != null ? duelWorldEffectParentRoot : transform; // 월드 이펙트 부모 반환

        case DuelEffectSpawnType.SelfParent:
            return duelEffectParentRoot != null ? duelEffectParentRoot : transform; // 본인 결투 이펙트 부모 반환

        case DuelEffectSpawnType.TargetParent:
            if (otherCharacter != null) // 상대가 있으면 상대의 애니메이션 플레이어 참조 시도
            {
                CharacterAnimationPlayer otherAnimationPlayer = otherCharacter.GetCharacterAnimationPlayer(); // 상대 애니메이션 플레이어 가져오기

                if (otherAnimationPlayer != null && otherAnimationPlayer.DuelEffectParentRoot != null) // 상대 결투 이펙트 부모가 있으면 사용
                {
                    return otherAnimationPlayer.DuelEffectParentRoot;
                }
            }

            return transform; // 상대 부모를 못 찾으면 자기 자신 사용
    }

    return transform; // 예외 상황 기본값
}

private void ApplyDuelEffectTransform(
    Transform effectTransform, // 생성된 이펙트 Transform
    DuelSkillDefinitionSO.DuelEffectSpawnData spawnData, // 실제 생성 데이터
    DuelEffectSpawnType spawnType, // 생성 방식
    CharacterDuelAI otherCharacter) // 상대 캐릭터
{
    if (effectTransform == null) // Transform이 없으면 종료
    {
        return;
    }

    Vector3 directionalPositionOffset = GetDuelEffectDirectionalPositionOffset(spawnData); // 현재 방향 기준 위치값
    Vector3 directionalRotationOffset = GetDuelEffectDirectionalRotationOffset(spawnData); // 현재 방향 기준 회전값

    switch (spawnType)
    {
        case DuelEffectSpawnType.BetweenCharacters:
            Vector3 otherPosition = otherCharacter != null ? otherCharacter.transform.position : transform.position; // 상대 위치 계산
            Vector3 middlePosition = (transform.position + otherPosition) * 0.5f; // 두 캐릭터 사이 위치 계산

            effectTransform.position = middlePosition + directionalPositionOffset; // 원래 생성 위치에 방향별 위치값을 더해서 적용
            effectTransform.rotation = Quaternion.Euler(directionalRotationOffset); // 원래 회전에 방향별 회전값을 더한 결과 적용
            effectTransform.localScale = Vector3.one; // 기본 스케일 적용
            break;

        case DuelEffectSpawnType.SelfParent:
            effectTransform.localPosition = directionalPositionOffset; // 방향별 위치값으로 덮어쓰기
            effectTransform.localRotation = Quaternion.Euler(directionalRotationOffset); // 방향별 회전값으로 덮어쓰기
            effectTransform.localScale = Vector3.one; // 기본 스케일 적용
            break;

        case DuelEffectSpawnType.TargetParent:
            effectTransform.localPosition = directionalPositionOffset; // 방향별 위치값으로 덮어쓰기
            effectTransform.localRotation = Quaternion.Euler(directionalRotationOffset); // 방향별 회전값으로 덮어쓰기
            effectTransform.localScale = Vector3.one; // 기본 스케일 적용
            break;
    }
}

private Vector3 GetDuelEffectDirectionalPositionOffset(
    DuelSkillDefinitionSO.DuelEffectSpawnData spawnData) // 현재 방향 기준 위치값 반환
{
    if (spawnData == null)
    {
        return Vector3.zero; // 데이터가 없으면 기본값 반환
    }

    if (IsCurrentFacingLeft())
    {
        return spawnData.SpawnPositionOffsetWhenFacingLeft; // X- 방향 위치값 반환
    }

    return spawnData.SpawnPositionOffsetWhenFacingRight; // X+ 방향 위치값 반환
}

private Vector3 GetDuelEffectDirectionalRotationOffset(
    DuelSkillDefinitionSO.DuelEffectSpawnData spawnData) // 현재 방향 기준 회전값 반환
{
    if (spawnData == null)
    {
        return Vector3.zero; // 데이터가 없으면 기본값 반환
    }

    if (IsCurrentFacingLeft())
    {
        return spawnData.SpawnRotationOffsetWhenFacingLeft; // X- 방향 회전값 반환
    }

    return spawnData.SpawnRotationOffsetWhenFacingRight; // X+ 방향 회전값 반환
}


public void ShowHealthDamageFloater(int finalAppliedDamage) // 최종 체력피해 숫자 표시
{
    if (finalAppliedDamage <= 0)
    {
        return; // 표시할 값이 없으면 종료
    }

    if (healthDamageFloaterPrefab == null)
    {
        return; // 프리팹이 없으면 종료
    }

    DamageFloaterCanvasObject floaterObject = GetPooledHealthDamageFloaterObject(); // 체력피해 플로터 풀에서 가져오기

    if (floaterObject == null)
    {
        return; // 가져오지 못하면 종료
    }

    floaterObject.transform.SetParent(damageFloaterSpawnParent, false); // 피해 캐릭터의 플로터 부모 자식으로 배치
    floaterObject.gameObject.SetActive(true); // 사용 전 활성화
    floaterObject.InitializeFloater(finalAppliedDamage.ToString(), healthDamageFloaterSettings); // 숫자와 설정 전달
}

public void ShowStaggerDamageFloater(int finalAppliedDamage) // 최종 와해피해 숫자 표시
{
    if (finalAppliedDamage <= 0)
    {
        return; // 표시할 값이 없으면 종료
    }

    if (staggerDamageFloaterPrefab == null)
    {
        return; // 프리팹이 없으면 종료
    }

    DamageFloaterCanvasObject floaterObject = GetPooledStaggerDamageFloaterObject(); // 와해피해 플로터 풀에서 가져오기

    if (floaterObject == null)
    {
        return; // 가져오지 못하면 종료
    }

    floaterObject.transform.SetParent(damageFloaterSpawnParent, false); // 피해 캐릭터의 플로터 부모 자식으로 배치
    floaterObject.gameObject.SetActive(true); // 사용 전 활성화
    floaterObject.InitializeFloater(finalAppliedDamage.ToString(), staggerDamageFloaterSettings); // 숫자와 설정 전달
}

private void CreateDamageFloaterPoolRootIfNeeded() // 데미지 플로터 풀 부모 오브젝트 생성
{
    if (damageFloaterPoolRoot != null)
    {
        return; // 이미 있으면 종료
    }

    Transform baseParent = damageFloaterSpawnParent != null ? damageFloaterSpawnParent : transform; // 기본 부모 결정
    GameObject poolRootObject = new GameObject($"{name}_DamageFloaterPoolRoot"); // 풀 루트 오브젝트 생성
    damageFloaterPoolRoot = poolRootObject.transform; // 루트 저장
    damageFloaterPoolRoot.SetParent(baseParent, false); // 데미지 플로터 생성 부모 하위로 배치
    damageFloaterPoolRoot.localPosition = Vector3.zero; // 로컬 위치 초기화
    damageFloaterPoolRoot.localRotation = Quaternion.identity; // 로컬 회전 초기화
    damageFloaterPoolRoot.localScale = Vector3.one; // 로컬 스케일 초기화
}

private DamageFloaterCanvasObject GetPooledHealthDamageFloaterObject() // 체력피해 플로터를 가져오거나 새로 생성
{
    return GetPooledDamageFloaterObject(
        healthDamageFloaterPrefab, // 체력피해 프리팹 사용
        healthDamageFloaterPoolList, // 체력피해 풀 리스트 사용
        ref pooledHealthDamageFloaterCount); // 체력피해 풀 개수 갱신
}

private DamageFloaterCanvasObject GetPooledStaggerDamageFloaterObject() // 와해피해 플로터를 가져오거나 새로 생성
{
    return GetPooledDamageFloaterObject(
        staggerDamageFloaterPrefab, // 와해피해 프리팹 사용
        staggerDamageFloaterPoolList, // 와해피해 풀 리스트 사용
        ref pooledStaggerDamageFloaterCount); // 와해피해 풀 개수 갱신
}

private DamageFloaterCanvasObject GetPooledDamageFloaterObject(
    DamageFloaterCanvasObject prefab, // 사용할 프리팹
    List<DamageFloaterCanvasObject> poolList, // 사용할 풀 리스트
    ref int pooledCount) // 풀 개수 저장 변수
{
    if (prefab == null)
    {
        return null; // 프리팹이 없으면 종료
    }

    for (int i = 0; i < poolList.Count; i++)
    {
        DamageFloaterCanvasObject pooledObject = poolList[i]; // 현재 풀 오브젝트 참조

        if (pooledObject == null)
        {
            continue; // 비어 있으면 건너뜀
        }

        if (!pooledObject.gameObject.activeSelf)
        {
            return pooledObject; // 비활성 오브젝트 재사용
        }
    }

    if (damageFloaterPoolRoot == null)
    {
        CreateDamageFloaterPoolRootIfNeeded(); // 풀 루트가 없으면 생성
    }

    DamageFloaterCanvasObject newObject = Instantiate(prefab, damageFloaterPoolRoot); // 새 플로터 생성
    newObject.gameObject.SetActive(false); // 생성 직후 비활성화
    poolList.Add(newObject); // 풀 목록에 등록
    pooledCount = poolList.Count; // 현재 풀 개수 갱신

    return newObject; // 새 플로터 반환
}
}