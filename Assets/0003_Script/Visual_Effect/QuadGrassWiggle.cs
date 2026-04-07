using UnityEngine;

/// <summary>
/// Quad/Plane(여러 버텍스) 기준 잔디/천 "나불거림 + 출렁(휘어짐)" 버텍스 변형 스크립트
/// - Mesh.vertices를 매 프레임 수정하여 바람에 흔들리는 느낌을 만듦
/// - (1) 흔들림(Sway): 바람 방향으로 밀기
/// - (2) 휘어짐(Bend): 뿌리(또는 기준점)를 축으로 회전시키며 출렁거리게 휘기
/// - 아래쪽도 움직이게(bottomInfluence) 가능
/// </summary>
[RequireComponent(typeof(MeshFilter))]
public class QuadGrassWiggle : MonoBehaviour
{
    // ==============================
    // WIND (바람 기본)
    // ==============================

    [Header("Wind")]
    [SerializeField] private float windStrength = 0.15f; // 흔들림(밀림) 세기
    [SerializeField] private float windSpeed = 2.0f; // 흔들림 속도
    [SerializeField] private Vector3 windDirection = new Vector3(1f, 0f, 0f); // 바람 방향(로컬 기준)

    // ==============================
    // BEND (출렁/휘어짐)
    // ==============================

    [Header("Bend (Wobble)")]
    [SerializeField] private bool enableBend = true; // 휘어짐 사용 여부
    [SerializeField] private float bendStrengthDegrees = 12f; // 휘어짐 최대 각도(도)
    [SerializeField] private Vector3 bendAxis = new Vector3(0f, 0f, 1f); // 휘어질 회전축(로컬) (보통 Z축)
    [SerializeField] private bool bendFromRoot = true; // true면 뿌리(minY) 기준으로 휘어짐, false면 중심 기준

    // ==============================
    // WEIGHT (아래/위 가중치)
    // ==============================

    [Header("Weight")]
    [Range(0f, 1f)]
    [SerializeField] private float bottomInfluence = 0.0f; // 아래쪽도 움직이게 하는 최소 가중치(0=뿌리 고정)
    [SerializeField] private float rootStiffness = 2.0f; // 뿌리 고정 커브(클수록 아래가 덜 움직임)

    // ==============================
    // VARIATION (자연스러움)
    // ==============================

    [Header("Variation")]
    [SerializeField] private float noiseScale = 1.0f; // 위치 기반 위상 변화 스케일
    [SerializeField] private float phaseOffset = 0.0f; // 개체별 위상 오프셋(랜덤 권장)

    [Header("Flutter (Local Ripple)")]
    [SerializeField] private bool enableFlutter = false; // 잔잔한 잎사귀 떨림(리플) 사용 여부
    [SerializeField] private float flutterStrength = 0.04f; // 리플 세기
    [SerializeField] private float flutterFrequency = 6.0f; // 버텍스 위치 기반 리플 빈도(높을수록 촘촘)

    // ==============================
    // OPTIONAL
    // ==============================

    [Header("Optional")]
    [SerializeField] private bool recalcNormals = false; // 조명(Lit) 사용 시 필요할 수 있음
    [SerializeField] private bool recalcBounds = true; // 컬링/바운드 안정(보통 true 권장)

    // ==============================
    // INTERNAL
    // ==============================

    private MeshFilter mf; // MeshFilter 참조
    private Mesh workingMesh; // 런타임 복제 메시(원본 보호)
    private Vector3[] baseVertices; // 초기 버텍스(로컬)
    private Vector3[] deformedVertices; // 변형 버텍스(로컬)

    private float minY; // 로컬 Y 최소(아래)
    private float maxY; // 로컬 Y 최대(위)
    private Vector3 pivotLocal; // 휘어짐 기준점(로컬)

    private void Awake() // 초기화
    {
        mf = GetComponent<MeshFilter>(); // MeshFilter 가져오기

        // 원본(sharedMesh) 직접 수정 방지: 런타임 복제해서 사용
        workingMesh = Instantiate(mf.sharedMesh); // 메시 복제
        workingMesh.name = mf.sharedMesh.name + "_Runtime"; // 이름 구분
        mf.mesh = workingMesh; // 런타임 메시로 교체

        baseVertices = workingMesh.vertices; // 초기 버텍스 저장
        deformedVertices = new Vector3[baseVertices.Length]; // 변형 버퍼 생성

        // Y 범위 계산(뿌리/끝 가중치용)
        minY = float.PositiveInfinity; // 최소 초기화
        maxY = float.NegativeInfinity; // 최대 초기화

        for (int i = 0; i < baseVertices.Length; i++) // 모든 버텍스 스캔
        {
            float y = baseVertices[i].y; // 버텍스 Y
            if (y < minY) minY = y; // 최소 갱신
            if (y > maxY) maxY = y; // 최대 갱신
        }

        // 휘어짐 기준점(pivot) 설정
        if (bendFromRoot) // 뿌리 기준이면
            pivotLocal = new Vector3(0f, minY, 0f); // minY 라인 기준(로컬)
        else // 중심 기준이면
            pivotLocal = new Vector3(0f, (minY + maxY) * 0.5f, 0f); // 중간 Y 기준(로컬)

        // 바람 방향 정규화(0 벡터 방지)
        if (windDirection.sqrMagnitude < 0.0001f) // 너무 작으면
            windDirection = Vector3.right; // 기본값
        windDirection.Normalize(); // 정규화

        // 휘어짐 축 정규화(0 벡터 방지)
        if (bendAxis.sqrMagnitude < 0.0001f) // 너무 작으면
            bendAxis = Vector3.forward; // 기본값(Z)
        bendAxis.Normalize(); // 정규화
    }

    private void Update() // 매 프레임 변형
    {
        if (baseVertices == null || baseVertices.Length == 0) // 안전 체크
            return; // 중단

        // 개체 위치 기반 위상(여러 개 배치 시 동기화 방지)
        float posPhase = (transform.position.x + transform.position.z) * noiseScale; // 위치 위상

        // 공통 시간 파라미터
        float t = Time.time * windSpeed + phaseOffset + posPhase; // 시간 합성
        float wave = Mathf.Sin(t); // 기본 흔들림(-1~1)

        Vector3 localWindDir = windDirection; // 로컬 바람 방향

        // 버텍스 변형
        for (int i = 0; i < baseVertices.Length; i++) // 모든 버텍스 처리
        {
            Vector3 v = baseVertices[i]; // 원본 버텍스(로컬)

            // 높이 정규화(0=아래, 1=위)
            float height01 = (maxY - minY) > 0.0001f
                ? Mathf.InverseLerp(minY, maxY, v.y) // 0~1
                : 0f; // 예외

            // "뿌리 고정 커브" + "아래도 움직이게(bottomInfluence)" 적용
            float shaped = Mathf.Pow(height01, rootStiffness); // 아래 억제 커브
            float weight = Mathf.Lerp(bottomInfluence, 1f, shaped); // 아래 최소 영향 부여

            // ------------------------------
            // (1) SWAY: 바람 방향으로 밀기
            // ------------------------------
            Vector3 swayOffset = localWindDir * (wave * windStrength * weight); // 흔들림 오프셋

            // ------------------------------
            // (2) FLUTTER: 잔잔한 리플(선택)
            // ------------------------------
            if (enableFlutter) // 리플 사용 시
            {
                // 버텍스 위치 기반으로 서로 다른 떨림(자연스러움)
                float localPhase = (v.x + v.z) * flutterFrequency; // 위치 위상
                float flutter = Mathf.Sin(t * 1.7f + localPhase); // 빠른 떨림
                swayOffset += localWindDir * (flutter * flutterStrength * weight); // 리플을 sway에 추가
            }

            // ------------------------------
            // (3) BEND: 출렁(휘어짐) 회전 변형(선택)
            // ------------------------------
            Vector3 bentV = v; // 기본은 원본 유지

            if (enableBend) // 휘어짐 사용 시
            {
                // 위로 갈수록 더 크게 휘게(가중치 적용)
                float bendAngle = wave * bendStrengthDegrees * weight; // 각도(도)

                // pivot을 기준으로 회전(로컬 공간)
                Quaternion q = Quaternion.AngleAxis(bendAngle, bendAxis); // 회전 쿼터니언
                Vector3 fromPivot = v - pivotLocal; // pivot 기준 벡터
                bentV = pivotLocal + (q * fromPivot); // 회전 적용
            }

            // 최종 버텍스 = (휘어짐 결과) + (바람 흔들림 오프셋)
            deformedVertices[i] = bentV + swayOffset; // 최종 저장
        }

        // 메시 적용
        workingMesh.vertices = deformedVertices; // 버텍스 반영

        // 옵션 처리
        if (recalcNormals) // Lit 조명/노멀 필요 시
            workingMesh.RecalculateNormals(); // 노멀 재계산

        if (recalcBounds) // 컬링 안정 필요 시
            workingMesh.RecalculateBounds(); // 바운드 재계산
    }

    /// <summary>
    /// 여러 개 배치 시 개체별 리듬 분산용(원하면 Start에서 한 번 호출)
    /// </summary>
    public void SetRandomPhase() // 위상 랜덤화
    {
        phaseOffset = Random.Range(0f, Mathf.PI * 2f); // 0~2π
    }
}