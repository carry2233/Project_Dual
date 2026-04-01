using UnityEngine;

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


[Header("잔상 생성 위치 설정")]
[SerializeField] private Transform afterimageSpawnPoint; // 잔상 생성 위치 기준 오브젝트

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

    CreateAfterimagePoolRootIfNeeded(); // 잔상 풀 부모 오브젝트 준비
}

private void Update() // 매 프레임 애니메이션 상태 처리
{
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
        ApplyCurrentFrame(); // 첫 프레임 적용
        return;
    }

    currentFrameIndex++; // 다음 프레임으로 증가
    ApplyCurrentFrame(); // 다음 프레임 적용
}

    private void ApplyCurrentFrame() // 현재 프레임 이미지를 출력기에 적용
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
    CharacterAnimationClipSO targetClip = idleAnimationClip; // 기본은 대기 클립
    bool shouldUseManualState = false; // 이번 프레임에 수동 재생 상태를 사용할지 여부
    bool targetManualLoop = false; // 이번 프레임 수동 루프 여부

    if (characterDuelAI != null && characterDuelAI.IsDashingToDuel && requestedManualAnimationClip != null)
    {
        targetClip = requestedManualAnimationClip; // 결투 돌진 중이면 요청된 수동 애니메이션 유지
        shouldUseManualState = true; // 수동 상태 사용
        targetManualLoop = requestedManualAnimationLoop; // 요청된 루프 여부 반영
    }
    else if (navigationMovementSystem != null && navigationMovementSystem.IsMoving)
    {
        targetClip = moveAnimationClip; // 이동 중이면 무조건 이동 애니메이션 사용
        shouldUseManualState = false; // 자동 상태 사용
        targetManualLoop = false; // 자동 상태는 수동 루프 아님
    }
    else if (requestedManualAnimationClip != null)
    {
        targetClip = requestedManualAnimationClip; // 이동이 아니면 요청된 수동 애니메이션 사용 가능
        shouldUseManualState = true; // 수동 상태 사용
        targetManualLoop = requestedManualAnimationLoop; // 요청된 루프 여부 반영
    }

    bool clipChanged = currentAnimationClip != targetClip; // 현재 클립 변경 여부
    bool manualStateChanged = isManualAnimationPlaying != shouldUseManualState; // 수동 상태 변경 여부
    bool manualLoopChanged = isManualAnimationLoop != targetManualLoop; // 수동 루프 여부 변경 여부

    if (!clipChanged && !manualStateChanged && !manualLoopChanged)
    {
        return; // 변경 사항이 없으면 종료
    }

    isManualAnimationPlaying = shouldUseManualState; // 현재 수동 재생 상태 반영
    isManualAnimationLoop = targetManualLoop; // 현재 수동 루프 여부 반영
    SetAnimationClip(targetClip); // 새 상태에 맞는 클립 즉시 적용
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
}