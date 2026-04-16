using TMPro;
using UnityEngine;

public class DamageFloaterCanvasObject : MonoBehaviour
{
    [Header("필수 참조")]
    [SerializeField] private TMP_Text damageText; // 숫자를 표시할 TMP 텍스트 참조

    [Header("비활성화 시 위치 초기화 설정")]
    [SerializeField] private bool resetToConfiguredLocalPositionOnDisable = true; // 비활성화 시 설정한 로컬 위치로 초기화할지 여부
    [SerializeField] private Vector3 configuredResetLocalPosition = Vector3.zero; // 비활성화 시 되돌릴 로컬 위치값

    [Header("현재 플로터 상태")]
    [SerializeField] private float currentElapsedTime; // 현재 활성화 후 경과 시간
    [SerializeField] private float currentLifetime = 1f; // 전체 유지 시간

    [SerializeField] private float currentFadeStartTime; // 텍스트 투명화 시작 시점
    [SerializeField] private int currentMinimumAlphaPercent; // 최소 투명률(%)
    [SerializeField] private float currentFadeDurationToMinimumAlpha = 0.3f; // 최소 투명률까지 도달 시간
    [SerializeField] private AnimationCurve currentFadeCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f); // 투명 변화 커브

    [SerializeField] private Vector3 currentMoveDirection = new Vector3(0.5f, 1f, 0f); // 이동 방향
    [SerializeField] private float currentMoveStartTime; // 이동 시작 시점
    [SerializeField] private float currentMoveDuration = 0.5f; // 이동 종료까지 걸리는 시간
    [SerializeField] private AnimationCurve currentMoveCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f); // 이동 변화 커브

    [SerializeField] private bool isInitialized; // 초기화 완료 여부

    private Vector3 initialLocalPosition; // 초기 로컬 위치 저장
    private Color initialTextColor = Color.white; // 초기 텍스트 색상 저장

    private void Awake() // 시작 시 참조 자동 연결
    {
        if (damageText == null)
        {
            damageText = GetComponentInChildren<TMP_Text>(true); // 자식 포함 TMP 텍스트 자동 탐색
        }

        if (damageText != null)
        {
            initialTextColor = damageText.color; // 초기 텍스트 색상 저장
        }
    }

    private void OnEnable() // 활성화 시 기본 상태 초기화
    {
        currentElapsedTime = 0f; // 경과 시간 초기화
    }

    private void Update() // 매 프레임 이동/투명/종료 처리
    {
        if (!isInitialized)
        {
            return; // 초기화 전이면 종료
        }

        currentElapsedTime += Time.deltaTime; // 경과 시간 누적

        UpdateMoveState(); // 이동 상태 갱신
        UpdateFadeState(); // 투명 상태 갱신

        if (currentElapsedTime >= currentLifetime)
        {
            gameObject.SetActive(false); // 유지 시간이 끝나면 비활성화
        }
    }

    public void InitializeFloater(
        string displayText, // 표시할 문자열
        CharacterAnimationPlayer.DamageFloaterSpawnSettings settings) // 플로터 적용 설정
    {
        if (damageText == null)
        {
            damageText = GetComponentInChildren<TMP_Text>(true); // 안전하게 다시 탐색
        }

        currentElapsedTime = 0f; // 경과 시간 초기화
        currentLifetime = settings != null ? Mathf.Max(0f, settings.Lifetime) : 1f; // 유지 시간 저장

        currentFadeStartTime = settings != null ? Mathf.Max(0f, settings.FadeStartTime) : 0f; // 투명 시작 시점 저장
        currentMinimumAlphaPercent = settings != null ? Mathf.Clamp(settings.MinimumAlphaPercent, 0, 100) : 0; // 최소 투명률 저장
        currentFadeDurationToMinimumAlpha = settings != null ? Mathf.Max(0f, settings.FadeDurationToMinimumAlpha) : 0.3f; // 투명 도달 시간 저장
        currentFadeCurve = settings != null && settings.FadeCurve != null
            ? settings.FadeCurve
            : AnimationCurve.Linear(0f, 0f, 1f, 1f); // 투명 커브 저장

        currentMoveDirection = settings != null ? settings.MoveDirection : new Vector3(0.5f, 1f, 0f); // 이동 방향 저장
        currentMoveStartTime = settings != null ? Mathf.Max(0f, settings.MoveStartTime) : 0f; // 이동 시작 시점 저장
        currentMoveDuration = settings != null ? Mathf.Max(0f, settings.MoveDuration) : 0.5f; // 이동 시간 저장
        currentMoveCurve = settings != null && settings.MoveCurve != null
            ? settings.MoveCurve
            : AnimationCurve.Linear(0f, 0f, 1f, 1f); // 이동 커브 저장

        initialLocalPosition = transform.localPosition; // 현재 로컬 위치를 시작 위치로 저장
        isInitialized = true; // 초기화 완료 상태 저장

        if (damageText != null)
        {
            damageText.text = displayText; // 표시 문자열 적용

            Color resetColor = initialTextColor; // 초기 색상 복사
            resetColor.a = 1f; // 텍스트는 시작 시 완전 불투명
            damageText.color = resetColor; // 텍스트 색상 초기화
        }

        transform.localPosition = initialLocalPosition; // 시작 위치 즉시 반영
    }

    private void UpdateMoveState() // 이동 상태 처리
    {
        if (currentElapsedTime < currentMoveStartTime)
        {
            transform.localPosition = initialLocalPosition; // 이동 시작 전에는 시작 위치 유지
            return;
        }

        if (currentMoveDuration <= 0f)
        {
            transform.localPosition = initialLocalPosition + currentMoveDirection; // 시간이 0이면 즉시 최종 위치 적용
            return;
        }

        float normalizedTime = Mathf.Clamp01((currentElapsedTime - currentMoveStartTime) / currentMoveDuration); // 이동 진행도 계산
        float curveValue = currentMoveCurve != null ? currentMoveCurve.Evaluate(normalizedTime) : normalizedTime; // 이동 커브값 계산

        transform.localPosition = initialLocalPosition + (currentMoveDirection * curveValue); // 커브값 반영 위치 적용
    }

    private void UpdateFadeState() // 텍스트 투명 상태 처리
    {
        if (damageText == null)
        {
            return; // 텍스트가 없으면 종료
        }

        float targetAlpha01 = 1f; // 기본 알파값

        if (currentElapsedTime >= currentFadeStartTime)
        {
            if (currentFadeDurationToMinimumAlpha <= 0f)
            {
                targetAlpha01 = currentMinimumAlphaPercent / 100f; // 시간이 0이면 즉시 최소 투명률 적용
            }
            else
            {
                float normalizedTime = Mathf.Clamp01((currentElapsedTime - currentFadeStartTime) / currentFadeDurationToMinimumAlpha); // 투명 진행도 계산
                float curveValue = currentFadeCurve != null ? currentFadeCurve.Evaluate(normalizedTime) : normalizedTime; // 투명 커브값 계산
                float minimumAlpha01 = Mathf.Clamp01(currentMinimumAlphaPercent / 100f); // 최소 투명률 0~1 변환

                targetAlpha01 = Mathf.Lerp(1f, minimumAlpha01, curveValue); // 현재 알파값 계산
            }
        }

        Color nextColor = damageText.color; // 현재 색상 복사
        nextColor.a = Mathf.Clamp01(targetAlpha01); // 텍스트 알파만 변경
        damageText.color = nextColor; // 텍스트 색상 적용
    }

    private void OnDisable() // 비활성화 시 상태와 위치 초기화
{
    currentElapsedTime = 0f; // 경과 시간 초기화
    isInitialized = false; // 초기화 완료 상태 해제

    if (resetToConfiguredLocalPositionOnDisable)
    {
        transform.localPosition = configuredResetLocalPosition; // 설정된 로컬 위치값으로 초기화
    }

    if (damageText != null)
    {
        Color resetColor = initialTextColor; // 초기 텍스트 색상 복사
        resetColor.a = 1f; // 텍스트 알파를 완전 불투명으로 초기화
        damageText.color = resetColor; // 텍스트 색상 초기화
    }
}
}