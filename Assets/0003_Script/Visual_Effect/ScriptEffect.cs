using System.Collections;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem; // 최신 입력 시스템 키 입력
#endif

#if UNITY_EDITOR
using UnityEditor; // 에디터 Handles 사용
#endif
[ExecuteAlways] // 에디터에서도 형태 미리보기
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ScriptEffect : MonoBehaviour
{
    [Header("기본 참조")]
    [SerializeField] private Texture2D effectTexture; // 이펙트 텍스처 이미지
    [SerializeField] private Material effectMaterial; // 이펙트 머티리얼
    [SerializeField] private ParticleSystem linkedParticleSystem; // 함께 사용할 파티클 시스템

    [Header("기본 동작")]
    [SerializeField] private bool playOnEnable = true; // 활성화 시 자동 재생 여부
    [SerializeField] private bool useUnscaledTime = false; // 타임스케일 무시 여부
    [SerializeField] private float lifeTime = 0.35f; // 재생 시간
    [SerializeField] private bool hideWhenFinished = false; // 재생 완료 후 숨김 여부

    [Header("반지름 설정")]
    [SerializeField] private float minRadius = 1.0f; // 안쪽 반지름
    [SerializeField] private float maxRadius = 2.2f; // 바깥 반지름
    [SerializeField] private AnimationCurve minRadiusOverLife = AnimationCurve.Linear(0f, 1f, 1f, 1f); // 수명 대비 안쪽 반지름 배율
    [SerializeField] private AnimationCurve maxRadiusOverLife = AnimationCurve.Linear(0f, 1f, 1f, 1f); // 수명 대비 바깥 반지름 배율

    [Header("각도 설정")]
    [SerializeField] private float startAngle = -130f; // 시작 각도
    [SerializeField] private float endAngle = 110f; // 끝 각도
    [SerializeField] private AnimationCurve startAngleOffsetOverLife = AnimationCurve.Linear(0f, 0f, 1f, 0f); // 수명 대비 시작 각도 추가값
    [SerializeField] private AnimationCurve endAngleOffsetOverLife = AnimationCurve.Linear(0f, 0f, 1f, 0f); // 수명 대비 끝 각도 추가값

[Header("회전 설정")]
[SerializeField] private float baseRotationOffset = 0f; // 기본 회전 오프셋
[SerializeField] private float rotationOverLife = -60f; // 재생 중 추가 회전량
[SerializeField] private AnimationCurve rotationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f); // 수명 대비 회전 보간
[SerializeField] private AnimationCurve segmentRotationWeight = AnimationCurve.Linear(0f, 1f, 1f, 0f); // 세그먼트별 회전 가중치(시작쪽 1, 끝쪽 0 권장)

[Header("형태 세부 설정")]
[SerializeField] private int segmentCount = 48; // 메시 분할 수
[SerializeField] private float zThickness = 0.02f; // Z축 두께
[SerializeField] private bool generateBackFace = true; // 뒷면 생성 여부
[SerializeField] private bool clampAngleOrder = false; // 시작/끝 각도를 자동 정렬할지 여부

    [Header("시각 설정")]
    [SerializeField] private Color effectColor = Color.white; // 이펙트 색상
    [SerializeField] private AnimationCurve alphaOverLife = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f); // 수명 대비 알파
    [SerializeField] private Vector2 uvTiling = Vector2.one; // UV 타일링
    [SerializeField] private Vector2 uvOffset = Vector2.zero; // UV 오프셋
    [SerializeField] private bool radialUV = true; // 각도/반지름 기반 UV 사용 여부

    [Header("에디터 미리보기")]
    [SerializeField] private bool previewInEditMode = true; // 에디터 미리보기 여부
    [SerializeField] [Range(0f, 1f)] private float previewNormalizedTime = 1f; // 에디터 미리보기 시간
    [SerializeField] private bool showGizmos = true; // 기즈모 표시 여부

    [Header("고정 표시 영역 설정")]
[SerializeField] private float visibleStartAngle = -130f; // 고정으로 보일 시작 각도
[SerializeField] private float visibleEndAngle = 110f; // 고정으로 보일 끝 각도

[Header("에디터 기즈모 표시 설정")]
[SerializeField] private bool fillVisibleAreaGizmo = true; // 고정 표시 영역 채우기 여부
[SerializeField] private bool fillEffectSourceGizmo = true; // 실제 이펙트 형태 채우기 여부
[SerializeField] private Color visibleAreaFillColor = new Color(1f, 0.92f, 0.2f, 0.16f); // 고정 표시 영역 채움 색
[SerializeField] private Color visibleAreaLineColor = new Color(1f, 0.92f, 0.2f, 0.9f); // 고정 표시 영역 선 색
[SerializeField] private Color effectSourceFillColor = new Color(0f, 1f, 1f, 0.22f); // 실제 이펙트 채움 색
[SerializeField] private Color effectSourceLineColor = new Color(0f, 1f, 1f, 0.95f); // 실제 이펙트 선 색


[Header("입력 재생 설정")]
[SerializeField] private bool enableKeyboardPlay = true; // 키 입력으로 재생할지 여부
#if ENABLE_INPUT_SYSTEM
[SerializeField] private Key playKey = Key.Space; // 재생 키(최신 Input System)
#else
[SerializeField] private KeyCode playKey = KeyCode.Space; // 재생 키(구 입력 fallback)
#endif

    private MeshFilter meshFilter; // 메시 필터 캐시
    private MeshRenderer meshRenderer; // 메시 렌더러 캐시
    private Mesh effectMesh; // 생성 메시
    private MaterialPropertyBlock propertyBlock; // 머티리얼 속성 블록
    private Coroutine playRoutine; // 재생 코루틴
    private bool isPlaying; // 현재 재생 중 여부

    private static readonly int MainTexId = Shader.PropertyToID("_MainTex"); // 기본 텍스처 프로퍼티 ID
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap"); // URP 기본 텍스처 프로퍼티 ID
    private static readonly int ColorId = Shader.PropertyToID("_Color"); // 기본 색상 프로퍼티 ID
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP 기본 색상 프로퍼티 ID

    private void Awake() // 초기 준비
    {
        Initialize(); // 내부 참조 초기화
        ApplyMaterialReference(); // 머티리얼 참조 적용
        RebuildMesh(GetDisplayTime()); // 초기 메시 생성
    }

    private void OnEnable() // 활성화 시 처리
    {
        Initialize(); // 내부 참조 초기화
        ApplyMaterialReference(); // 머티리얼 참조 적용

        if (Application.isPlaying)
        {
            if (playOnEnable)
            {
                Play(); // 자동 재생
            }
            else
            {
                RebuildMesh(1f); // 재생 안 하면 완성 상태 표시
            }
        }
        else
        {
            RebuildMesh(GetDisplayTime()); // 에디터 미리보기 갱신
        }
    }

    private void OnDisable() // 비활성화 시 정리
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine); // 재생 코루틴 정지
            playRoutine = null;
        }

        isPlaying = false; // 재생 상태 해제
    }

    private void OnValidate() // 인스펙터 값 변경 시 갱신
    {
        lifeTime = Mathf.Max(0.01f, lifeTime); // 최소 수명 보정
        minRadius = Mathf.Max(0f, minRadius); // 최소 반지름 보정
        maxRadius = Mathf.Max(minRadius + 0.001f, maxRadius); // 최대 반지름 보정
        segmentCount = Mathf.Max(3, segmentCount); // 최소 세그먼트 보정
        zThickness = Mathf.Max(0f, zThickness); // 두께 보정

        Initialize(); // 내부 참조 초기화
        ApplyMaterialReference(); // 머티리얼 참조 적용
        RebuildMesh(GetDisplayTime()); // 형태 다시 생성
    }

private void Update() // 실행 중 키 입력 / 에디터 미리보기 처리
{
    if (Application.isPlaying)
    {
        if (enableKeyboardPlay && IsPlayKeyPressed()) // 실행 중 재생 키 입력 확인
        {
            Play(); // 이펙트 재생
        }

        return;
    }

    if (!previewInEditMode)
        return;

    RebuildMesh(previewNormalizedTime); // 에디터 미리보기 시간 기준 갱신
}

    public void Play() // 이펙트 재생 시작
    {
        if (!Application.isPlaying)
        {
            RebuildMesh(1f); // 에디터에서는 완성형 표시
            return;
        }

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine); // 기존 재생 중지
        }

        playRoutine = StartCoroutine(CoPlay()); // 새 재생 시작
    }

    public void StopEffect() // 이펙트 중지
    {
        if (playRoutine != null)
        {
            StopCoroutine(playRoutine); // 코루틴 정지
            playRoutine = null;
        }

        isPlaying = false; // 재생 상태 해제
    }

    public void RebuildNow(float normalizedTime) // 외부 강제 갱신
    {
        RebuildMesh(Mathf.Clamp01(normalizedTime)); // 메시 즉시 재생성
    }

    private IEnumerator CoPlay() // 재생 코루틴
    {
        isPlaying = true; // 재생 시작

        if (linkedParticleSystem != null)
        {
            linkedParticleSystem.Play(); // 연결 파티클 재생
        }

        float elapsed = 0f; // 누적 시간

        while (elapsed < lifeTime)
        {
            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime; // 시간 계산
            elapsed += deltaTime; // 시간 누적

            float normalizedTime = Mathf.Clamp01(elapsed / lifeTime); // 0~1 시간
            RebuildMesh(normalizedTime); // 현재 시간 기준 메시 갱신

            yield return null; // 다음 프레임 대기
        }

        RebuildMesh(1f); // 마지막 프레임 보정

        if (hideWhenFinished)
        {
            ApplyVisualAlpha(0f); // 완료 후 투명 처리
        }

        isPlaying = false; // 재생 종료
        playRoutine = null; // 코루틴 초기화
    }

    private void Initialize() // 내부 참조 초기화
    {
        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>(); // MeshFilter 캐시

        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>(); // MeshRenderer 캐시

        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock(); // PropertyBlock 생성

        if (effectMesh == null)
        {
            effectMesh = new Mesh(); // 메시 생성
            effectMesh.name = "ScriptEffect_Mesh"; // 메시 이름 설정
            effectMesh.MarkDynamic(); // 동적 메시 표시
        }

        if (meshFilter.sharedMesh != effectMesh)
        {
            meshFilter.sharedMesh = effectMesh; // 현재 메시 연결
        }
    }

    private void ApplyMaterialReference() // 머티리얼 참조 적용
    {
        if (meshRenderer == null)
            return;

        if (effectMaterial != null)
        {
            meshRenderer.sharedMaterial = effectMaterial; // 외부 지정 머티리얼 연결
        }

        ApplyVisualAlpha(alphaOverLife.Evaluate(GetDisplayTime())); // 현재 알파 반영
    }

    private void ApplyVisualAlpha(float alpha) // 색상/텍스처 반영
    {
        if (meshRenderer == null)
            return;

        if (propertyBlock == null)
            propertyBlock = new MaterialPropertyBlock(); // PropertyBlock 보장

        Color finalColor = effectColor; // 기본 색상 복사
        finalColor.a *= Mathf.Clamp01(alpha); // 알파 적용

        meshRenderer.GetPropertyBlock(propertyBlock); // 기존 블록 읽기
        propertyBlock.SetColor(ColorId, finalColor); // 기본 색상 설정
        propertyBlock.SetColor(BaseColorId, finalColor); // URP 색상 설정

        if (effectTexture != null)
        {
            propertyBlock.SetTexture(MainTexId, effectTexture); // 기본 텍스처 설정
            propertyBlock.SetTexture(BaseMapId, effectTexture); // URP 텍스처 설정
        }

        meshRenderer.SetPropertyBlock(propertyBlock); // 블록 적용
    }

private void RebuildMesh(float normalizedTime) // 메시 재생성
{
    Initialize(); // 내부 참조 보장

    if (effectMesh == null)
        return;

    normalizedTime = Mathf.Clamp01(normalizedTime); // 시간 보정

    float currentMinRadius = minRadius * Mathf.Max(0f, minRadiusOverLife.Evaluate(normalizedTime)); // 현재 안쪽 반지름
    float currentMaxRadius = maxRadius * Mathf.Max(0f, maxRadiusOverLife.Evaluate(normalizedTime)); // 현재 바깥 반지름
    currentMaxRadius = Mathf.Max(currentMinRadius + 0.001f, currentMaxRadius); // 반지름 역전 방지

    float currentSourceStartAngle = startAngle + startAngleOffsetOverLife.Evaluate(normalizedTime); // 실제 이펙트 시작 각도
    float currentSourceEndAngle = endAngle + endAngleOffsetOverLife.Evaluate(normalizedTime); // 실제 이펙트 끝 각도
    float currentRotation = baseRotationOffset + rotationOverLife * rotationCurve.Evaluate(normalizedTime); // 실제 이펙트 회전값

    float currentVisibleStartAngle = visibleStartAngle; // 고정 표시 시작 각도
    float currentVisibleEndAngle = visibleEndAngle; // 고정 표시 끝 각도

    if (clampAngleOrder)
    {
        if (currentSourceStartAngle > currentSourceEndAngle)
        {
            float swap = currentSourceStartAngle; // 소스 각도 정렬용
            currentSourceStartAngle = currentSourceEndAngle; // 소스 시작 정렬
            currentSourceEndAngle = swap; // 소스 끝 정렬
        }

        if (currentVisibleStartAngle > currentVisibleEndAngle)
        {
            float swap = currentVisibleStartAngle; // 표시 각도 정렬용
            currentVisibleStartAngle = currentVisibleEndAngle; // 표시 시작 정렬
            currentVisibleEndAngle = swap; // 표시 끝 정렬
        }
    }

    float alpha = alphaOverLife.Evaluate(normalizedTime); // 현재 알파 계산
    ApplyVisualAlpha(alpha); // 시각값 반영

    BuildAnnularSectorMesh(
        currentMinRadius,
        currentMaxRadius,
        currentVisibleStartAngle,
        currentVisibleEndAngle,
        currentSourceStartAngle,
        currentSourceEndAngle,
        currentRotation); // 고정 표시 영역 안에서 잘린 이펙트 메시 생성
}

private void BuildAnnularSectorMesh(
    float innerRadius,
    float outerRadius,
    float visibleStartDeg,
    float visibleEndDeg,
    float sourceStartDeg,
    float sourceEndDeg,
    float sourceRotationDeg) // 고정 표시 영역 안에서 회전하는 이펙트 메시 생성
{
    int steps = Mathf.Max(1, segmentCount); // 세그먼트 최소 보장

    int frontVertexCount = (steps + 1) * 2; // 앞면 정점 수
    int backVertexCount = generateBackFace ? frontVertexCount : 0; // 뒷면 정점 수
    int totalVertexCount = frontVertexCount + backVertexCount; // 전체 정점 수

    Vector3[] vertices = new Vector3[totalVertexCount]; // 정점 배열
    Vector3[] normals = new Vector3[totalVertexCount]; // 노멀 배열
    Vector2[] uvs = new Vector2[totalVertexCount]; // UV 배열

    int frontTriangleCount = steps * 2; // 앞면 삼각형 수
    int backTriangleCount = generateBackFace ? steps * 2 : 0; // 뒷면 삼각형 수
    int[] triangles = new int[(frontTriangleCount + backTriangleCount) * 3]; // 인덱스 배열

    float halfThickness = zThickness * 0.5f; // 앞뒤 두께 절반
    float sourceAngleRange = Mathf.Max(0.001f, sourceEndDeg - sourceStartDeg); // 소스 각도 범위

    for (int i = 0; i <= steps; i++)
    {
        float displayT = (float)i / steps; // 고정 표시 영역 기준 비율
        float displayAngle = Mathf.Lerp(visibleStartDeg, visibleEndDeg, displayT); // 고정 표시 영역의 현재 각도

        float rotatedSourceAngle = displayAngle - sourceRotationDeg; // 현재 표시 각도에서 역으로 추적한 소스 각도
        float sourceT = (rotatedSourceAngle - sourceStartDeg) / sourceAngleRange; // 소스 내부에서의 위치 비율
        bool isInsideSource = sourceT >= 0f && sourceT <= 1f; // 현재 각도가 실제 이펙트 내부인지 여부

        float currentInnerRadius;
        float currentOuterRadius;

        if (isInsideSource)
        {
            currentInnerRadius = innerRadius; // 최소 반지름 그대로 사용
            currentOuterRadius = outerRadius; // 최대 반지름 그대로 사용
        }
        else
        {
            currentInnerRadius = innerRadius; // 표시 밖은 접어서 0폭 처리
            currentOuterRadius = innerRadius;
        }

        float rad = displayAngle * Mathf.Deg2Rad; // 표시 영역 각도는 고정
        Vector3 direction = new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f); // 현재 방향

        Vector3 innerPoint = new Vector3(direction.x * currentInnerRadius, direction.y * currentInnerRadius, halfThickness); // 안쪽 점
        Vector3 outerPoint = new Vector3(direction.x * currentOuterRadius, direction.y * currentOuterRadius, halfThickness); // 바깥 점

        int frontIndex = i * 2; // 앞면 인덱스
        vertices[frontIndex] = innerPoint; // 앞면 안쪽 정점
        vertices[frontIndex + 1] = outerPoint; // 앞면 바깥 정점
        normals[frontIndex] = Vector3.forward; // 앞면 노멀
        normals[frontIndex + 1] = Vector3.forward; // 앞면 노멀

        float uvT = Mathf.Clamp01(sourceT); // UV용 비율
        if (radialUV)
        {
            uvs[frontIndex] = new Vector2(uvT * uvTiling.x + uvOffset.x, 0f * uvTiling.y + uvOffset.y); // 안쪽 UV
            uvs[frontIndex + 1] = new Vector2(uvT * uvTiling.x + uvOffset.x, 1f * uvTiling.y + uvOffset.y); // 바깥 UV
        }
        else
        {
            float uvRadiusBase = Mathf.Max(0.001f, outerRadius); // 평면 UV 기준 반지름
            uvs[frontIndex] = new Vector2(((innerPoint.x / uvRadiusBase) * 0.5f + 0.5f) * uvTiling.x + uvOffset.x, ((innerPoint.y / uvRadiusBase) * 0.5f + 0.5f) * uvTiling.y + uvOffset.y); // 평면 안쪽 UV
            uvs[frontIndex + 1] = new Vector2(((outerPoint.x / uvRadiusBase) * 0.5f + 0.5f) * uvTiling.x + uvOffset.x, ((outerPoint.y / uvRadiusBase) * 0.5f + 0.5f) * uvTiling.y + uvOffset.y); // 평면 바깥 UV
        }

        if (generateBackFace)
        {
            int backIndex = frontVertexCount + frontIndex; // 뒷면 인덱스

            Vector3 innerBackPoint = innerPoint; // 뒤 안쪽 점
            Vector3 outerBackPoint = outerPoint; // 뒤 바깥 점
            innerBackPoint.z = -halfThickness; // 뒤 안쪽 Z
            outerBackPoint.z = -halfThickness; // 뒤 바깥 Z

            vertices[backIndex] = innerBackPoint; // 뒤 안쪽 정점
            vertices[backIndex + 1] = outerBackPoint; // 뒤 바깥 정점
            normals[backIndex] = Vector3.back; // 뒤 노멀
            normals[backIndex + 1] = Vector3.back; // 뒤 노멀
            uvs[backIndex] = uvs[frontIndex]; // 뒤 안쪽 UV
            uvs[backIndex + 1] = uvs[frontIndex + 1]; // 뒤 바깥 UV
        }
    }

    int triangleIndex = 0; // 삼각형 기록 시작 위치

    for (int i = 0; i < steps; i++)
    {
        int root = i * 2; // 현재 앞면 시작 정점

        triangles[triangleIndex++] = root; // 앞면 삼각형 1
        triangles[triangleIndex++] = root + 1; // 앞면 삼각형 1
        triangles[triangleIndex++] = root + 2; // 앞면 삼각형 1

        triangles[triangleIndex++] = root + 1; // 앞면 삼각형 2
        triangles[triangleIndex++] = root + 3; // 앞면 삼각형 2
        triangles[triangleIndex++] = root + 2; // 앞면 삼각형 2
    }

    if (generateBackFace)
    {
        for (int i = 0; i < steps; i++)
        {
            int root = frontVertexCount + i * 2; // 현재 뒤면 시작 정점

            triangles[triangleIndex++] = root; // 뒤면 삼각형 1
            triangles[triangleIndex++] = root + 2; // 뒤면 삼각형 1
            triangles[triangleIndex++] = root + 1; // 뒤면 삼각형 1

            triangles[triangleIndex++] = root + 1; // 뒤면 삼각형 2
            triangles[triangleIndex++] = root + 2; // 뒤면 삼각형 2
            triangles[triangleIndex++] = root + 3; // 뒤면 삼각형 2
        }
    }

    effectMesh.Clear(); // 기존 메시 초기화
    effectMesh.vertices = vertices; // 정점 적용
    effectMesh.normals = normals; // 노멀 적용
    effectMesh.uv = uvs; // UV 적용
    effectMesh.triangles = triangles; // 인덱스 적용
    effectMesh.RecalculateBounds(); // 바운드 재계산
}

    private float GetDisplayTime() // 현재 표시용 시간 반환
    {
        if (Application.isPlaying)
            return isPlaying ? 0f : 1f; // 실행 중이면 재생 전/후 값 사용

        if (!previewInEditMode)
            return 1f; // 미리보기 꺼져 있으면 완성형

        return previewNormalizedTime; // 에디터 미리보기 시간 사용
    }

private void OnDrawGizmosSelected() // 선택 시 기즈모 표시
{
    if (!showGizmos)
        return;

    float previewTime = Application.isPlaying ? 1f : previewNormalizedTime; // 기즈모 기준 시간

    float currentMinRadius = minRadius * Mathf.Max(0f, minRadiusOverLife.Evaluate(previewTime)); // 현재 안쪽 반지름
    float currentMaxRadius = maxRadius * Mathf.Max(0f, maxRadiusOverLife.Evaluate(previewTime)); // 현재 바깥 반지름
    currentMaxRadius = Mathf.Max(currentMinRadius + 0.001f, currentMaxRadius); // 반지름 역전 방지

    float currentRotation = baseRotationOffset + rotationOverLife * rotationCurve.Evaluate(previewTime); // 현재 소스 회전값
    float currentSourceStartAngle = startAngle + startAngleOffsetOverLife.Evaluate(previewTime); // 현재 소스 시작 각도
    float currentSourceEndAngle = endAngle + endAngleOffsetOverLife.Evaluate(previewTime); // 현재 소스 끝 각도

    if (clampAngleOrder)
    {
        if (currentSourceStartAngle > currentSourceEndAngle)
        {
            float swap = currentSourceStartAngle; // 소스 각도 정렬용 임시값
            currentSourceStartAngle = currentSourceEndAngle; // 소스 시작 각도 정렬
            currentSourceEndAngle = swap; // 소스 끝 각도 정렬
        }
    }

#if UNITY_EDITOR
    if (fillVisibleAreaGizmo) // 고정 표시 영역 채움 표시
    {
        DrawFilledRingSectorHandles(
            currentMinRadius,
            currentMaxRadius,
            visibleStartAngle,
            visibleEndAngle,
            visibleAreaFillColor); // 고정 표시 영역 채우기
    }

    if (fillEffectSourceGizmo) // 실제 이펙트 형태 채움 표시
    {
        DrawFilledEffectWindowHandles(
            currentMinRadius,
            currentMaxRadius,
            visibleStartAngle,
            visibleEndAngle,
            currentSourceStartAngle,
            currentSourceEndAngle,
            currentRotation,
            effectSourceFillColor); // 현재 이펙트 형태 채우기
    }
#endif

    Gizmos.color = visibleAreaLineColor; // 고정 표시 영역 선 색
    DrawArcGizmo(currentMinRadius, visibleStartAngle, visibleEndAngle, 0f); // 고정 안쪽 원호
    DrawArcGizmo(currentMaxRadius, visibleStartAngle, visibleEndAngle, 0f); // 고정 바깥 원호
    DrawRadialBoundaryGizmo(currentMinRadius, currentMaxRadius, visibleStartAngle); // 고정 시작 경계선
    DrawRadialBoundaryGizmo(currentMinRadius, currentMaxRadius, visibleEndAngle); // 고정 끝 경계선

    Gizmos.color = effectSourceLineColor; // 실제 이펙트 선 색
    DrawEffectWindowOutlineGizmo(
        currentMinRadius,
        currentMaxRadius,
        visibleStartAngle,
        visibleEndAngle,
        currentSourceStartAngle,
        currentSourceEndAngle,
        currentRotation); // 실제 이펙트 형태 윤곽선
}

    private void DrawArcGizmo(float radius, float startDeg, float endDeg, float rotationDeg) // 원호 기즈모 그리기
    {
        const int gizmoSteps = 48; // 기즈모 분할 수
        Vector3 prev = transform.TransformPoint(GetPointOnCircle(radius, startDeg + rotationDeg)); // 이전 점

        for (int i = 1; i <= gizmoSteps; i++)
        {
            float t = (float)i / gizmoSteps; // 보간 비율
            float angle = Mathf.Lerp(startDeg, endDeg, t) + rotationDeg; // 현재 각도
            Vector3 next = transform.TransformPoint(GetPointOnCircle(radius, angle)); // 현재 점
            Gizmos.DrawLine(prev, next); // 선 그리기
            prev = next; // 이전 점 갱신
        }
    }

    private Vector3 GetPointOnCircle(float radius, float angleDeg) // 원 위 점 계산
    {
        float rad = angleDeg * Mathf.Deg2Rad; // 라디안 변환
        return new Vector3(Mathf.Cos(rad) * radius, Mathf.Sin(rad) * radius, 0f); // XY 평면 점 반환
    }

private bool IsPlayKeyPressed() // 재생 키 입력 확인
{
#if ENABLE_INPUT_SYSTEM
    return Keyboard.current != null && Keyboard.current[playKey].wasPressedThisFrame; // 최신 Input System 키 입력
#else
    return Input.GetKeyDown(playKey); // 구 입력 시스템 fallback
#endif
}

private void DrawRadialBoundaryGizmo(float innerRadius, float outerRadius, float angleDeg) // 반지름 경계선 기즈모
{
    Vector3 innerPoint = transform.TransformPoint(GetPointOnCircle(innerRadius, angleDeg)); // 안쪽 점
    Vector3 outerPoint = transform.TransformPoint(GetPointOnCircle(outerRadius, angleDeg)); // 바깥 점
    Gizmos.DrawLine(innerPoint, outerPoint); // 경계선 그리기
}

private void DrawEffectWindowOutlineGizmo(
    float innerRadius,
    float outerRadius,
    float visibleStartDeg,
    float visibleEndDeg,
    float sourceStartDeg,
    float sourceEndDeg,
    float sourceRotationDeg) // 실제 이펙트 현재 형태 윤곽선 기즈모
{
    const int gizmoSteps = 64; // 윤곽선 분할 수
    float sourceAngleRange = Mathf.Max(0.001f, sourceEndDeg - sourceStartDeg); // 소스 각도 범위

    Vector3 prevInner = Vector3.zero; // 이전 안쪽 점
    Vector3 prevOuter = Vector3.zero; // 이전 바깥 점
    bool hasPrev = false; // 이전 점 존재 여부

    for (int i = 0; i <= gizmoSteps; i++)
    {
        float displayT = (float)i / gizmoSteps; // 표시 영역 비율
        float displayAngle = Mathf.Lerp(visibleStartDeg, visibleEndDeg, displayT); // 현재 고정 표시 각도

        float rotatedSourceAngle = displayAngle - sourceRotationDeg; // 역회전 기준 소스 각도
        float sourceT = (rotatedSourceAngle - sourceStartDeg) / sourceAngleRange; // 소스 비율
        bool isInsideSource = sourceT >= 0f && sourceT <= 1f; // 실제 이펙트 내부 여부

        float currentInnerRadius;
        float currentOuterRadius;

        if (isInsideSource)
        {
            currentInnerRadius = innerRadius; // 최소 반지름 그대로 사용
            currentOuterRadius = outerRadius; // 최대 반지름 그대로 사용
        }
        else
        {
            currentInnerRadius = innerRadius; // 표시 밖은 접어서 0폭 처리
            currentOuterRadius = innerRadius;
        }

        Vector3 innerPoint = transform.TransformPoint(GetPointOnCircle(currentInnerRadius, displayAngle)); // 안쪽 점
        Vector3 outerPoint = transform.TransformPoint(GetPointOnCircle(currentOuterRadius, displayAngle)); // 바깥 점

        if (hasPrev)
        {
            Gizmos.DrawLine(prevInner, innerPoint); // 안쪽 윤곽선
            Gizmos.DrawLine(prevOuter, outerPoint); // 바깥 윤곽선
        }

        prevInner = innerPoint; // 이전 안쪽 점 갱신
        prevOuter = outerPoint; // 이전 바깥 점 갱신
        hasPrev = true; // 이전 점 존재 표시
    }

    Vector3 startInner = transform.TransformPoint(GetPointOnCircle(innerRadius, visibleStartDeg)); // 표시 시작 안쪽 기준점
    Vector3 startOuter = transform.TransformPoint(GetPointOnCircle(innerRadius, visibleStartDeg)); // 표시 시작 바깥 기준점
    Vector3 endInner = transform.TransformPoint(GetPointOnCircle(innerRadius, visibleEndDeg)); // 표시 끝 안쪽 기준점
    Vector3 endOuter = transform.TransformPoint(GetPointOnCircle(innerRadius, visibleEndDeg)); // 표시 끝 바깥 기준점

    Gizmos.DrawLine(startInner, startOuter); // 표시 시작 닫힘선
    Gizmos.DrawLine(endInner, endOuter); // 표시 끝 닫힘선
}

#if UNITY_EDITOR
private void DrawFilledRingSectorHandles(float innerRadius, float outerRadius, float startDeg, float endDeg, Color fillColor) // 채워진 환형 부채꼴 표시
{
    const int steps = 64; // 채움 분할 수
    Vector3[] points = new Vector3[(steps + 1) * 2]; // 외곽/내곽 폴리곤 점 배열

    for (int i = 0; i <= steps; i++)
    {
        float t = (float)i / steps; // 각도 보간 비율
        float angle = Mathf.Lerp(startDeg, endDeg, t); // 현재 각도

        points[i] = transform.TransformPoint(GetPointOnCircle(outerRadius, angle)); // 바깥쪽 점
        points[points.Length - 1 - i] = transform.TransformPoint(GetPointOnCircle(innerRadius, angle)); // 안쪽 점 역순 저장
    }

    Handles.color = fillColor; // 채움 색 적용
    Handles.DrawAAConvexPolygon(points); // 채워진 폴리곤 그리기
}

private void DrawFilledEffectWindowHandles(
    float innerRadius,
    float outerRadius,
    float visibleStartDeg,
    float visibleEndDeg,
    float sourceStartDeg,
    float sourceEndDeg,
    float sourceRotationDeg,
    Color fillColor) // 고정 표시 영역 안의 실제 이펙트 형태 채우기
{
    const int steps = 64; // 채움 분할 수

    Vector3[] outerPoints = new Vector3[steps + 1]; // 바깥 윤곽 점
    Vector3[] innerPoints = new Vector3[steps + 1]; // 안쪽 윤곽 점

    float sourceAngleRange = Mathf.Max(0.001f, sourceEndDeg - sourceStartDeg); // 소스 각도 범위

    for (int i = 0; i <= steps; i++)
    {
        float displayT = (float)i / steps; // 표시 영역 기준 비율
        float displayAngle = Mathf.Lerp(visibleStartDeg, visibleEndDeg, displayT); // 현재 고정 표시 각도

        float rotatedSourceAngle = displayAngle - sourceRotationDeg; // 소스 역추적 각도
        float sourceT = (rotatedSourceAngle - sourceStartDeg) / sourceAngleRange; // 소스 내부 위치
        bool isInsideSource = sourceT >= 0f && sourceT <= 1f; // 실제 이펙트 내부 여부

        float currentInnerRadius;
        float currentOuterRadius;

        if (isInsideSource)
        {
            currentInnerRadius = innerRadius; // 최소 반지름 그대로 사용
            currentOuterRadius = outerRadius; // 최대 반지름 그대로 사용
        }
        else
        {
            currentInnerRadius = innerRadius; // 표시 밖은 접어서 0폭 처리
            currentOuterRadius = innerRadius;
        }

        innerPoints[i] = transform.TransformPoint(GetPointOnCircle(currentInnerRadius, displayAngle)); // 안쪽 윤곽 점
        outerPoints[i] = transform.TransformPoint(GetPointOnCircle(currentOuterRadius, displayAngle)); // 바깥 윤곽 점
    }

    Vector3[] polygon = new Vector3[(steps + 1) * 2]; // 최종 채움 폴리곤

    for (int i = 0; i <= steps; i++)
    {
        polygon[i] = outerPoints[i]; // 바깥 윤곽 정순
        polygon[polygon.Length - 1 - i] = innerPoints[i]; // 안쪽 윤곽 역순
    }

    Handles.color = fillColor; // 채움 색 적용
    Handles.DrawAAConvexPolygon(polygon); // 채워진 실제 이펙트 형태 그리기
}
#endif
}