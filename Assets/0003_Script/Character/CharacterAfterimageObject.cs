using UnityEngine;

public class CharacterAfterimageObject : MonoBehaviour
{
    [Header("필수 참조")]
    [SerializeField] private PerObjectTextureOverride perObjectTextureOverride; // 잔상 이미지/색상 적용 참조
    [SerializeField] private YToZPositionConverter yToZPositionConverter; // Y→Z 보정 참조

    [Header("현재 잔상 상태")]
    [SerializeField] private int currentStartAlphaPercent = 100; // 현재 시작 투명도 퍼센트
    [SerializeField] private float currentFadeDuration = 0.2f; // 현재 사라지는 시간
    [SerializeField] private AnimationCurve currentFadeCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f); // 현재 투명도 감소 커브

    [SerializeField] private float currentElapsedTime; // 현재 경과 시간
    [SerializeField] private bool isInitialized; // 초기화 완료 여부

    private Sprite currentSprite; // 현재 표시할 스프라이트
    private Color currentBaseColor = Color.white; // 현재 기본 색상

    private void Awake() // 초기 참조 자동 연결
    {
        if (perObjectTextureOverride == null)
        {
            perObjectTextureOverride = GetComponent<PerObjectTextureOverride>(); // 같은 오브젝트에서 자동 참조
        }

        if (yToZPositionConverter == null)
        {
            yToZPositionConverter = GetComponent<YToZPositionConverter>(); // 같은 오브젝트에서 자동 참조
        }
    }

    private void OnEnable() // 활성화 시 시간값 초기화
    {
        currentElapsedTime = 0f; // 경과 시간 초기화
    }

    private void Update() // 활성화된 잔상의 투명도 감소 처리
    {
        if (!isInitialized)
        {
            return; // 초기화 전이면 종료
        }

        if (currentFadeDuration <= 0f)
        {
            ApplyAlphaPercent(0f); // 시간이 0 이하면 즉시 0 처리
            gameObject.SetActive(false); // 즉시 비활성화
            return;
        }

        currentElapsedTime += Time.deltaTime; // 경과 시간 누적
        float normalizedTime = Mathf.Clamp01(currentElapsedTime / currentFadeDuration); // 0~1 진행도 계산

        float curveValue = currentFadeCurve != null
            ? currentFadeCurve.Evaluate(normalizedTime)
            : 1f - normalizedTime; // 커브값 계산

        float currentAlphaPercent = Mathf.Clamp(currentStartAlphaPercent * curveValue, 0f, 100f); // 현재 알파 퍼센트 계산
        ApplyAlphaPercent(currentAlphaPercent); // 현재 알파 적용

        if (currentAlphaPercent <= 0f || normalizedTime >= 1f)
        {
            gameObject.SetActive(false); // 완전히 사라졌으면 비활성화
        }
    }

    public void InitializeAfterimage( // 잔상 초기화
        Sprite sprite, // 적용할 스프라이트
        Vector3 worldPosition, // 생성 위치
        Quaternion worldRotation, // 생성 회전값
        int startAlphaPercent, // 시작 투명도 퍼센트
        AnimationCurve fadeCurve, // 투명도 감소 커브
        float fadeDuration) // 사라지는 시간
    {
        currentSprite = sprite; // 스프라이트 저장
        currentStartAlphaPercent = Mathf.Clamp(startAlphaPercent, 0, 100); // 시작 투명도 저장
        currentFadeCurve = fadeCurve != null ? fadeCurve : AnimationCurve.Linear(0f, 1f, 1f, 0f); // 커브 저장
        currentFadeDuration = Mathf.Max(0f, fadeDuration); // 시간 저장
        currentElapsedTime = 0f; // 경과 시간 초기화
        isInitialized = true; // 초기화 완료 상태 저장

        transform.position = worldPosition; // 위치 적용
        transform.rotation = worldRotation; // 회전 적용

        if (yToZPositionConverter != null)
        {
            yToZPositionConverter.ResetLocalBaseValues(); // 재사용 시 로컬 기준점 재설정
        }

        if (perObjectTextureOverride != null)
        {
            perObjectTextureOverride.SetSprite(currentSprite); // 스프라이트 적용
        }

        ApplyAlphaPercent(currentStartAlphaPercent); // 시작 투명도 즉시 적용
    }

    private void ApplyAlphaPercent(float alphaPercent) // 퍼센트 기준 알파 적용
    {
        if (perObjectTextureOverride == null)
        {
            return; // 참조가 없으면 종료
        }

        Color targetColor = currentBaseColor; // 기본 색상 복사
        targetColor.a = Mathf.Clamp01(alphaPercent / 100f); // 알파값 반영
        perObjectTextureOverride.SetColor(targetColor); // 색상 적용
    }
}