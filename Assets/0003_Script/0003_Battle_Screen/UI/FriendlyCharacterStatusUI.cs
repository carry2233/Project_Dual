using System.Collections; // 게이지 연출 코루틴용
using TMPro; // TMP 텍스트 사용
using UnityEngine;
using UnityEngine.UI;

public class FriendlyCharacterStatusUI : MonoBehaviour
{
    [Header("체력 UI 참조")]
    [SerializeField] private Image healthFillImage; // 체력 메인 게이지 채움 이미지
    [SerializeField] private Image healthSubFillImage; // 체력 서브 게이지 채움 이미지
    [SerializeField] private TextMeshProUGUI healthText; // 체력 텍스트

    [Header("와해 UI 참조")]
    [SerializeField] private Image staggerFillImage; // 와해 메인 게이지 채움 이미지
    [SerializeField] private Image staggerSubFillImage; // 와해 서브 게이지 채움 이미지
    [SerializeField] private TextMeshProUGUI staggerText; // 와해 텍스트

    [Header("체력 감소 연출 설정")]
    [SerializeField] private float healthDecreaseDuration = 0.35f; // 체력 감소 시 서브게이지 이동 시간
    [SerializeField] private AnimationCurve healthDecreaseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // 체력 감소 시 서브게이지 이동 커브

    [Header("체력 증가 연출 설정")]
    [SerializeField] private float healthIncreaseDuration = 0.25f; // 체력 증가 시 메인게이지 이동 시간
    [SerializeField] private AnimationCurve healthIncreaseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // 체력 증가 시 메인게이지 이동 커브

    [Header("와해 감소 연출 설정")]
    [SerializeField] private float staggerDecreaseDuration = 0.35f; // 와해 감소 시 서브게이지 이동 시간
    [SerializeField] private AnimationCurve staggerDecreaseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // 와해 감소 시 서브게이지 이동 커브

    [Header("와해 증가 연출 설정")]
    [SerializeField] private float staggerIncreaseDuration = 0.25f; // 와해 증가 시 메인게이지 이동 시간
    [SerializeField] private AnimationCurve staggerIncreaseCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // 와해 증가 시 메인게이지 이동 커브

    [Header("현재 연결 대상")]
    [SerializeField] private CharacterStatSystem targetStatSystem; // 현재 이 UI가 갱신을 맡는 캐릭터 스탯

    private Coroutine healthGaugeAnimationCoroutine; // 체력 게이지 연출 코루틴 참조
    private Coroutine staggerGaugeAnimationCoroutine; // 와해 게이지 연출 코루틴 참조

    public CharacterStatSystem TargetStatSystem => targetStatSystem; // 현재 연결된 스탯 시스템 반환

    private void OnDisable() // 비활성화 시 기존 이벤트 연결과 연출 정리
    {
        UnsubscribeFromCurrentStatSystem(); // 기존 스탯 이벤트 구독 해제
        StopAllGaugeAnimations(); // 진행 중인 게이지 연출 즉시 정지
    }

    public void SetTargetStatSystem(CharacterStatSystem newTargetStatSystem) // 갱신 대상 스탯 시스템 설정
    {
        if (targetStatSystem == newTargetStatSystem)
        {
            RefreshUI(true); // 같은 대상이어도 현재 값 기준 즉시 동기화 갱신
            return;
        }

        UnsubscribeFromCurrentStatSystem(); // 이전 대상 이벤트 연결 해제
        StopAllGaugeAnimations(); // 이전 대상 연출 즉시 정지

        targetStatSystem = newTargetStatSystem; // 새 대상 저장

        SubscribeToCurrentStatSystem(); // 새 대상 이벤트 연결
        RefreshUI(true); // 새 대상 기준 즉시 동기화 갱신
    }

    private void SubscribeToCurrentStatSystem() // 현재 대상 스탯 이벤트 구독
    {
        if (targetStatSystem == null)
        {
            return; // 대상이 없으면 종료
        }

        targetStatSystem.OnStatusValueChanged += HandleStatusValueChanged; // 값 변경 시 UI 즉시 갱신 연결
    }

    private void UnsubscribeFromCurrentStatSystem() // 현재 대상 스탯 이벤트 구독 해제
    {
        if (targetStatSystem == null)
        {
            return; // 대상이 없으면 종료
        }

        targetStatSystem.OnStatusValueChanged -= HandleStatusValueChanged; // 값 변경 이벤트 연결 해제
    }

    private void HandleStatusValueChanged(CharacterStatSystem changedStatSystem) // 스탯 값 변경 알림 수신
    {
        if (changedStatSystem != targetStatSystem)
        {
            return; // 현재 연결 대상이 아니면 무시
        }

        RefreshUI(false); // 현재 스탯 기준 연출 포함 갱신
    }

    public void RefreshUI(bool forceInstantSync = false) // 현재 스탯 기준으로 체력/와해 UI 갱신
    {
        RefreshHealthUI(forceInstantSync); // 체력 UI 갱신
        RefreshStaggerUI(forceInstantSync); // 와해 UI 갱신
    }

private void RefreshHealthUI(bool forceInstantSync) // 체력 게이지와 텍스트 갱신
{
    float targetHealthRatio = GetCurrentHealthRatio(); // 현재 체력 비율 계산

    if (healthText != null)
    {
        if (targetStatSystem == null)
        {
            healthText.text = "0 / 0"; // 대상이 없을 때 기본 표시
        }
        else
        {
            healthText.text = $"{targetStatSystem.MaxHealth} / {targetStatSystem.CurrentHealth}"; // 최대체력 / 현재체력 형식으로 표시
        }
    }

    if (healthFillImage == null && healthSubFillImage == null)
    {
        return; // 체력 게이지 참조가 둘 다 없으면 종료
    }

    if (IsSameImageReference(healthFillImage, healthSubFillImage)) // 메인/서브가 같은 Image 참조인지 검사
    {
        Debug.LogWarning($"{name}의 체력 메인게이지와 서브게이지가 같은 Image를 참조하고 있음"); // 참조 실수 경고
        SetImageFillAmount(healthFillImage, targetHealthRatio); // 같은 참조면 즉시값만 반영
        SetImageFillAmount(healthSubFillImage, targetHealthRatio); // 같은 참조면 즉시값만 반영
        return;
    }

    float currentMainFill = GetImageFillAmountOrDefault(healthFillImage, targetHealthRatio); // 현재 메인게이지 값
    float currentSubFill = GetImageFillAmountOrDefault(healthSubFillImage, currentMainFill); // 현재 서브게이지 값

    if (forceInstantSync)
    {
        StopHealthGaugeAnimation(); // 기존 체력 연출 즉시 정지
        SetImageFillAmount(healthFillImage, targetHealthRatio); // 메인게이지 즉시 동기화
        SetImageFillAmount(healthSubFillImage, targetHealthRatio); // 서브게이지 즉시 동기화
        return;
    }

    StopHealthGaugeAnimation(); // 이전 체력 연출 즉시 정지

    if (targetHealthRatio < currentSubFill) // 체력 감소 시
    {
        SetImageFillAmount(healthFillImage, targetHealthRatio); // 메인게이지를 먼저 즉시 감소
        healthGaugeAnimationCoroutine = StartCoroutine(AnimateGaugeFill(
            healthSubFillImage, // 체력 서브게이지 연출 대상
            currentSubFill, // 현재 서브게이지 값에서 시작
            targetHealthRatio, // 감소된 목표값까지 이동
            healthDecreaseDuration, // 감소 연출 시간
            healthDecreaseCurve)); // 감소 연출 커브 적용
    }
    else if (targetHealthRatio > currentMainFill) // 체력 증가 시
    {
        SetImageFillAmount(healthSubFillImage, targetHealthRatio); // 서브게이지를 먼저 즉시 증가
        healthGaugeAnimationCoroutine = StartCoroutine(AnimateGaugeFill(
            healthFillImage, // 체력 메인게이지 연출 대상
            currentMainFill, // 현재 메인게이지 값에서 시작
            targetHealthRatio, // 증가된 목표값까지 이동
            healthIncreaseDuration, // 증가 연출 시간
            healthIncreaseCurve)); // 증가 연출 커브 적용
    }
    else
    {
        SetImageFillAmount(healthFillImage, targetHealthRatio); // 메인게이지 값 정리
        SetImageFillAmount(healthSubFillImage, targetHealthRatio); // 서브게이지 값 정리
    }
}

private void RefreshStaggerUI(bool forceInstantSync) // 와해 게이지와 텍스트 갱신
{
    float targetStaggerRatio = GetCurrentStaggerRatio(); // 현재 와해량 비율 계산

    if (staggerText != null)
    {
        if (targetStatSystem == null)
        {
            staggerText.text = "0 / 0"; // 대상이 없을 때 기본 표시
        }
        else
        {
            staggerText.text = $"{targetStatSystem.MaxStaggerAmount} / {targetStatSystem.CurrentStaggerAmount}"; // 최대와해량 / 현재와해량 형식으로 표시
        }
    }

    if (staggerFillImage == null && staggerSubFillImage == null)
    {
        return; // 와해 게이지 참조가 둘 다 없으면 종료
    }

    if (IsSameImageReference(staggerFillImage, staggerSubFillImage)) // 메인/서브가 같은 Image 참조인지 검사
    {
        Debug.LogWarning($"{name}의 와해 메인게이지와 서브게이지가 같은 Image를 참조하고 있음"); // 참조 실수 경고
        SetImageFillAmount(staggerFillImage, targetStaggerRatio); // 같은 참조면 즉시값만 반영
        SetImageFillAmount(staggerSubFillImage, targetStaggerRatio); // 같은 참조면 즉시값만 반영
        return;
    }

    float currentMainFill = GetImageFillAmountOrDefault(staggerFillImage, targetStaggerRatio); // 현재 메인게이지 값
    float currentSubFill = GetImageFillAmountOrDefault(staggerSubFillImage, currentMainFill); // 현재 서브게이지 값

    if (forceInstantSync)
    {
        StopStaggerGaugeAnimation(); // 기존 와해 연출 즉시 정지
        SetImageFillAmount(staggerFillImage, targetStaggerRatio); // 메인게이지 즉시 동기화
        SetImageFillAmount(staggerSubFillImage, targetStaggerRatio); // 서브게이지 즉시 동기화
        return;
    }

    StopStaggerGaugeAnimation(); // 이전 와해 연출 즉시 정지

    if (targetStaggerRatio < currentSubFill) // 와해량 감소 시
    {
        SetImageFillAmount(staggerFillImage, targetStaggerRatio); // 메인게이지를 먼저 즉시 감소
        staggerGaugeAnimationCoroutine = StartCoroutine(AnimateGaugeFill(
            staggerSubFillImage, // 와해 서브게이지 연출 대상
            currentSubFill, // 현재 서브게이지 값에서 시작
            targetStaggerRatio, // 감소된 목표값까지 이동
            staggerDecreaseDuration, // 감소 연출 시간
            staggerDecreaseCurve)); // 감소 연출 커브 적용
    }
    else if (targetStaggerRatio > currentMainFill) // 와해량 증가 시
    {
        SetImageFillAmount(staggerSubFillImage, targetStaggerRatio); // 서브게이지를 먼저 즉시 증가
        staggerGaugeAnimationCoroutine = StartCoroutine(AnimateGaugeFill(
            staggerFillImage, // 와해 메인게이지 연출 대상
            currentMainFill, // 현재 메인게이지 값에서 시작
            targetStaggerRatio, // 증가된 목표값까지 이동
            staggerIncreaseDuration, // 증가 연출 시간
            staggerIncreaseCurve)); // 증가 연출 커브 적용
    }
    else
    {
        SetImageFillAmount(staggerFillImage, targetStaggerRatio); // 메인게이지 값 정리
        SetImageFillAmount(staggerSubFillImage, targetStaggerRatio); // 서브게이지 값 정리
    }
}

    private IEnumerator AnimateGaugeFill(Image targetImage, float startFill, float endFill, float duration, AnimationCurve curve) // 게이지 채우기값 자연스러운 이동 연출
    {
        if (targetImage == null)
        {
            yield break; // 대상 이미지가 없으면 종료
        }

        float safeDuration = Mathf.Max(0.0001f, duration); // 0초 방지용 보정 시간
        AnimationCurve safeCurve = curve != null ? curve : AnimationCurve.Linear(0f, 0f, 1f, 1f); // 커브가 없으면 선형 커브 사용

        float elapsedTime = 0f; // 현재 경과 시간

        while (elapsedTime < safeDuration)
        {
            elapsedTime += Time.deltaTime; // 경과 시간 누적
            float normalizedTime = Mathf.Clamp01(elapsedTime / safeDuration); // 0~1 정규화 시간 계산
            float curvedTime = Mathf.Clamp01(safeCurve.Evaluate(normalizedTime)); // 커브 반영 시간 계산

            targetImage.fillAmount = Mathf.Lerp(startFill, endFill, curvedTime); // 커브 기준 채우기값 이동
            yield return null; // 다음 프레임까지 대기
        }

        targetImage.fillAmount = endFill; // 마지막에 목표값으로 정확히 고정
    }

    private float GetCurrentHealthRatio() // 현재 체력 비율 계산
    {
        if (targetStatSystem == null || targetStatSystem.MaxHealth <= 0)
        {
            return 0f; // 대상이 없거나 최대 체력이 0 이하이면 0 반환
        }

        return Mathf.Clamp01((float)targetStatSystem.CurrentHealth / targetStatSystem.MaxHealth); // 현재 체력 비율 반환
    }

    private float GetCurrentStaggerRatio() // 현재 와해량 비율 계산
    {
        if (targetStatSystem == null || targetStatSystem.MaxStaggerAmount <= 0)
        {
            return 0f; // 대상이 없거나 최대 와해량이 0 이하이면 0 반환
        }

        return Mathf.Clamp01((float)targetStatSystem.CurrentStaggerAmount / targetStatSystem.MaxStaggerAmount); // 현재 와해량 비율 반환
    }

    private float GetImageFillAmountOrDefault(Image targetImage, float defaultValue) // 이미지가 없을 경우 기본값을 반환하는 채우기값 읽기
    {
        if (targetImage == null)
        {
            return Mathf.Clamp01(defaultValue); // 이미지가 없으면 기본값 사용
        }

        return Mathf.Clamp01(targetImage.fillAmount); // 이미지 채우기값 반환
    }

    private void SetImageFillAmount(Image targetImage, float fillAmount) // 이미지 채우기값 안전 적용
    {
        if (targetImage == null)
        {
            return; // 이미지가 없으면 종료
        }

        targetImage.fillAmount = Mathf.Clamp01(fillAmount); // 0~1 범위로 제한 후 적용
    }

    private void StopAllGaugeAnimations() // 체력/와해 게이지 연출 전체 정지
    {
        StopHealthGaugeAnimation(); // 체력 게이지 연출 정지
        StopStaggerGaugeAnimation(); // 와해 게이지 연출 정지
    }

    private void StopHealthGaugeAnimation() // 체력 게이지 연출 정지
    {
        if (healthGaugeAnimationCoroutine == null)
        {
            return; // 진행 중인 연출이 없으면 종료
        }

        StopCoroutine(healthGaugeAnimationCoroutine); // 진행 중인 체력 연출 코루틴 정지
        healthGaugeAnimationCoroutine = null; // 참조 초기화
    }

    private void StopStaggerGaugeAnimation() // 와해 게이지 연출 정지
    {
        if (staggerGaugeAnimationCoroutine == null)
        {
            return; // 진행 중인 연출이 없으면 종료
        }

        StopCoroutine(staggerGaugeAnimationCoroutine); // 진행 중인 와해 연출 코루틴 정지
        staggerGaugeAnimationCoroutine = null; // 참조 초기화
    }

    private bool IsSameImageReference(Image firstImage, Image secondImage) // 두 게이지 이미지가 같은 참조인지 검사
{
    if (firstImage == null || secondImage == null)
    {
        return false; // 하나라도 없으면 같은 참조로 보지 않음
    }

    return firstImage == secondImage; // 동일한 Image 참조 여부 반환
}

}