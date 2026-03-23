using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem; // 새 Input System 사용

/// <summary>
/// 인트로 UI 연출
/// - 여러 세트를 순서대로 재생
/// - 각 세트는 이미지/TMP 텍스트를 함께 페이드 인/유지/페이드 아웃
/// - 스킵 입력으로 현재 세트를 빠르게 넘길 수 있음
/// - 마지막에는 지정 UI 이미지를 페이드 아웃하여 다음 화면을 보여줌
/// </summary>
public class IntroUISequence : MonoBehaviour
{
    [System.Serializable]
    public class IntroUISet
    {
        [Header("세트 정보")]
        public string setName; // 세트 이름

        [Header("연출 대상")]
        public List<Image> imageList = new List<Image>(); // 이 세트에서 알파 연출할 이미지 리스트
        public List<TMP_Text> textList = new List<TMP_Text>(); // 이 세트에서 알파 연출할 TMP 텍스트 리스트

        [Header("시간 설정")]
        public float fadeInTime = 1f; // 100%까지 밝아지는 시간
        public float holdTime = 1f; // 100% 상태 유지 시간
        public float fadeOutTime = 1f; // 0%까지 어두워지는 시간
        public float nextSetDelay = 0.5f; // 다음 세트 전 대기 시간
        public float skipBlockTime = 0.2f; // fadeIn 시작 후 스킵 입력을 무시할 시간

        [Header("커브 설정")]
        public AnimationCurve fadeInCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f); // 밝아질 때 사용할 커브
        public AnimationCurve fadeOutCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f); // 어두워질 때 사용할 커브
    }

    [Header("세트 목록")]
    [SerializeField] private List<IntroUISet> setList = new List<IntroUISet>(); // 순서대로 실행할 세트 리스트

    [Header("시작 / 종료 딜레이")]
    [SerializeField] private float firstSetStartDelay = 0f; // 게임 시작 후 첫 세트 시작 전 딜레이
    [SerializeField] private float lastSetEndDelay = 0f; // 마지막 세트 종료 후 마지막 페이드 전 딜레이

    [Header("스킵 입력 설정")]
    [SerializeField] private Key skipKey = Key.Space; // 세트 스킵용 키 (새 Input System 기준)

    [Header("마지막 페이드 아웃 UI 이미지")]
    [SerializeField] private Image finalFadeOutImage; // 마지막에 0%까지 낮출 UI 이미지
    [SerializeField] private float finalFadeOutTime = 1f; // 마지막 UI 이미지가 0%가 되기까지의 시간

    [Header("마지막 활성화 대상")]
    [SerializeField] private GameObject activateTargetObject; // 관련 오브젝트 비활성화 후 활성화할 오브젝트

    private Coroutine playCoroutine; // 현재 실행 중인 코루틴 참조
    private bool isPlaying; // 현재 인트로 재생 중 여부

    private void Start() // 게임 시작 시 자동 실행
    {
        InitializeAllAlphaToZero(); // 세트 대상들을 시작 전에 0%로 초기화
        playCoroutine = StartCoroutine(PlayIntroSequence()); // 인트로 전체 시퀀스 시작
    }

    /// <summary>
    /// 세트에 포함된 모든 이미지/TMP 텍스트를 시작 전에 0%로 초기화
    /// </summary>
    private void InitializeAllAlphaToZero() // 세트 대상 알파 초기화
    {
        for (int i = 0; i < setList.Count; i++)
        {
            IntroUISet currentSet = setList[i]; // 현재 세트 참조
            SetSetAlpha(currentSet, 0f); // 세트 전체 알파 0 적용
        }
    }

    /// <summary>
    /// 인트로 전체 시퀀스 재생
    /// </summary>
    private IEnumerator PlayIntroSequence() // 전체 인트로 순차 재생
    {
        if (isPlaying) yield break; // 중복 실행 방지

        isPlaying = true; // 재생 시작 상태로 변경

        if (firstSetStartDelay > 0f)
        {
            yield return new WaitForSeconds(firstSetStartDelay); // 첫 세트 시작 전 대기
        }

        for (int i = 0; i < setList.Count; i++)
        {
            yield return StartCoroutine(PlaySingleSet(setList[i])); // 세트 1개 재생
        }

        if (lastSetEndDelay > 0f)
        {
            yield return new WaitForSeconds(lastSetEndDelay); // 마지막 세트 종료 후 대기
        }

        if (finalFadeOutImage != null)
        {
            yield return StartCoroutine(FadeOutFinalImage(finalFadeOutImage, finalFadeOutTime)); // 마지막 이미지 페이드 아웃
        }
        else
        {
            DisableSetReferencedObjects(); // 세트에 참조된 오브젝트들 먼저 비활성화
            ActivateAssignedObject(); // 다음에 보여줄 오브젝트 활성화
        }

        isPlaying = false; // 재생 종료 상태로 변경
        playCoroutine = null; // 코루틴 참조 초기화
    }

    /// <summary>
    /// 세트 1개를 재생하고, 조건 만족 시 스킵 입력을 받아 다음 단계로 넘김
    /// </summary>
    private IEnumerator PlaySingleSet(IntroUISet targetSet) // 단일 세트 재생
    {
        if (targetSet == null) yield break; // 세트가 비어 있으면 종료

        SetSetAlpha(targetSet, 0f); // 세트 시작 시 0% 보정

        float setElapsedFromFadeInStart = 0f; // fadeIn 시작 시점부터 누적 시간
        bool forceMoveToFadeOut = false; // 현재 구간을 중단하고 fadeOut으로 넘어갈지 여부
        bool skipWindowOpened = false; // 스킵 허용 시작 여부
        bool skipTriggered = false; // 스킵 입력이 실제로 발생했는지 여부

        // =========================
        // fadeIn 구간
        // =========================
        if (targetSet.fadeInTime > 0f)
        {
            float elapsed = 0f; // fadeIn 경과 시간

            while (elapsed < targetSet.fadeInTime)
            {
                elapsed += Time.deltaTime; // fadeIn 시간 누적
                setElapsedFromFadeInStart += Time.deltaTime; // 전체 세트 시간 누적

                if (!skipWindowOpened && setElapsedFromFadeInStart >= targetSet.skipBlockTime)
                {
                    skipWindowOpened = true; // 스킵 허용 시작
                }

                if (skipWindowOpened && CheckSkipInput())
                {
                    skipTriggered = true; // 스킵 입력 감지
                    forceMoveToFadeOut = true; // 바로 fadeOut 구간으로 이동
                    break; // fadeIn 루프 종료
                }

                float normalized = Mathf.Clamp01(elapsed / targetSet.fadeInTime); // 0~1 정규화 시간
                float curvedAlpha = EvaluateCurveSafe(targetSet.fadeInCurve, normalized); // 커브 적용 알파값
                SetSetAlpha(targetSet, curvedAlpha); // 세트 전체 알파 적용

                yield return null; // 다음 프레임 대기
            }
        }

        if (!forceMoveToFadeOut)
        {
            SetSetAlpha(targetSet, 1f); // fadeIn 완료 시 100% 보정
        }

        // =========================
        // hold 구간
        // =========================
        if (!forceMoveToFadeOut && targetSet.holdTime > 0f)
        {
            float elapsed = 0f; // hold 경과 시간

            while (elapsed < targetSet.holdTime)
            {
                elapsed += Time.deltaTime; // hold 시간 누적
                setElapsedFromFadeInStart += Time.deltaTime; // 전체 세트 시간 누적

                if (!skipWindowOpened && setElapsedFromFadeInStart >= targetSet.skipBlockTime)
                {
                    skipWindowOpened = true; // 스킵 허용 시작
                }

                if (skipWindowOpened && CheckSkipInput())
                {
                    skipTriggered = true; // 스킵 입력 감지
                    forceMoveToFadeOut = true; // 바로 fadeOut 구간으로 이동
                    break; // hold 루프 종료
                }

                yield return null; // 다음 프레임 대기
            }
        }

        // =========================
        // fadeOut 구간
        // =========================
        if (targetSet.fadeOutTime > 0f)
        {
            float elapsed = 0f; // fadeOut 경과 시간
            float startAlpha = GetCurrentSetAlpha(targetSet); // 현재 알파값에서 자연스럽게 fadeOut 시작

            while (elapsed < targetSet.fadeOutTime)
            {
                elapsed += Time.deltaTime; // fadeOut 시간 누적
                setElapsedFromFadeInStart += Time.deltaTime; // 전체 세트 시간 누적

                if (!skipWindowOpened && setElapsedFromFadeInStart >= targetSet.skipBlockTime)
                {
                    skipWindowOpened = true; // 스킵 허용 시작
                }

                if (skipWindowOpened && CheckSkipInput())
                {
                    skipTriggered = true; // 스킵 입력 감지
                    break; // fadeOut은 즉시 종료하고 다음 단계로 넘어감
                }

                float normalized = Mathf.Clamp01(elapsed / targetSet.fadeOutTime); // 0~1 정규화 시간
                float curveValue = EvaluateCurveSafe(targetSet.fadeOutCurve, normalized); // 커브 평가값
                float curvedAlpha = Mathf.Lerp(0f, startAlpha, curveValue); // 현재 알파에서 0까지 감소값 계산
                SetSetAlpha(targetSet, curvedAlpha); // 세트 전체 알파 적용

                yield return null; // 다음 프레임 대기
            }
        }

        SetSetAlpha(targetSet, 0f); // fadeOut 종료 시 0% 보정

        // =========================
        // nextSetDelay 구간
        // =========================
        if (!skipTriggered && targetSet.nextSetDelay > 0f)
        {
            float elapsed = 0f; // nextSetDelay 경과 시간

            while (elapsed < targetSet.nextSetDelay)
            {
                elapsed += Time.deltaTime; // 딜레이 시간 누적
                setElapsedFromFadeInStart += Time.deltaTime; // 전체 세트 시간 누적

                if (!skipWindowOpened && setElapsedFromFadeInStart >= targetSet.skipBlockTime)
                {
                    skipWindowOpened = true; // 스킵 허용 시작
                }

                if (skipWindowOpened && CheckSkipInput())
                {
                    break; // 즉시 다음 세트 또는 마지막 종료 딜레이로 이동
                }

                yield return null; // 다음 프레임 대기
            }
        }
    }

    /// <summary>
    /// 세트에 포함된 이미지/TMP 텍스트 알파를 한 번에 설정
    /// </summary>
    private void SetSetAlpha(IntroUISet targetSet, float alpha) // 세트 전체 알파 적용
    {
        if (targetSet == null) return; // 세트가 없으면 종료

        alpha = Mathf.Clamp01(alpha); // 알파값 범위 제한

        for (int i = 0; i < targetSet.imageList.Count; i++)
        {
            Image image = targetSet.imageList[i]; // 현재 이미지 참조
            if (image == null) continue; // 비어 있으면 건너뜀

            Color color = image.color; // 현재 색상 복사
            color.a = alpha; // 알파 변경
            image.color = color; // 변경값 적용
        }

        for (int i = 0; i < targetSet.textList.Count; i++)
        {
            TMP_Text text = targetSet.textList[i]; // 현재 TMP 텍스트 참조
            if (text == null) continue; // 비어 있으면 건너뜀

            Color color = text.color; // 현재 색상 복사
            color.a = alpha; // 알파 변경
            text.color = color; // 변경값 적용
        }
    }

    /// <summary>
    /// 현재 세트의 알파를 가져옴
    /// - 이미지 또는 텍스트 중 첫 번째 유효 참조의 알파를 사용
    /// </summary>
    private float GetCurrentSetAlpha(IntroUISet targetSet) // 현재 세트 알파값 읽기
    {
        if (targetSet == null) return 0f; // 세트가 없으면 0 반환

        for (int i = 0; i < targetSet.imageList.Count; i++)
        {
            Image image = targetSet.imageList[i]; // 현재 이미지 참조
            if (image == null) continue; // 비어 있으면 건너뜀

            return image.color.a; // 첫 유효 이미지 알파 반환
        }

        for (int i = 0; i < targetSet.textList.Count; i++)
        {
            TMP_Text text = targetSet.textList[i]; // 현재 TMP 텍스트 참조
            if (text == null) continue; // 비어 있으면 건너뜀

            return text.color.a; // 첫 유효 텍스트 알파 반환
        }

        return 0f; // 유효 참조가 없으면 0 반환
    }

/// <summary>
/// 스킵 입력이 들어왔는지 확인
/// </summary>
private bool CheckSkipInput() // 새 Input System 기준 스킵 입력 검사
{
    bool leftMouseClicked = Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame; // 마우스 좌클릭 감지
    bool rightMouseClicked = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame; // 마우스 우클릭 감지
    bool keyboardPressed = Keyboard.current != null && Keyboard.current[skipKey].wasPressedThisFrame; // 설정 키 입력 감지

    return leftMouseClicked || rightMouseClicked || keyboardPressed; // 하나라도 입력되면 true 반환
}

    /// <summary>
    /// 마지막 UI 이미지를 페이드 아웃
    /// - 시작 전에 관련 오브젝트 비활성화
    /// - 그 다음 활성화 대상 오브젝트 활성화
    /// - 그 다음 마지막 이미지 알파를 낮춤
    /// </summary>
    private IEnumerator FadeOutFinalImage(Image targetImage, float duration) // 마지막 이미지 페이드 아웃
    {
        if (targetImage == null)
        {
            DisableSetReferencedObjects(); // 세트 참조 오브젝트들 먼저 비활성화
            ActivateAssignedObject(); // 활성화 대상 오브젝트 활성화
            yield break;
        }

        DisableSetReferencedObjects(); // finalFadeOutImage가 어두워지기 전에 관련 오브젝트들 비활성화
        ActivateAssignedObject(); // 그 다음 보여줄 오브젝트 활성화

        Color startColor = targetImage.color; // 시작 색상 저장
        float startAlpha = startColor.a; // 시작 알파 저장

        if (duration <= 0f)
        {
            startColor.a = 0f; // 즉시 0% 설정
            targetImage.color = startColor; // 즉시 적용
            targetImage.gameObject.SetActive(false); // 마지막 이미지 오브젝트 비활성화
            yield break;
        }

        float elapsed = 0f; // 페이드 경과 시간

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime; // 시간 누적

            float normalized = Mathf.Clamp01(elapsed / duration); // 0~1 정규화 시간
            float newAlpha = Mathf.Lerp(startAlpha, 0f, normalized); // 현재 알파에서 0으로 감소

            Color color = targetImage.color; // 현재 색상 복사
            color.a = newAlpha; // 새 알파 적용
            targetImage.color = color; // 변경값 반영

            yield return null; // 다음 프레임 대기
        }

        Color finalColor = targetImage.color; // 최종 색상 복사
        finalColor.a = 0f; // 0% 보정
        targetImage.color = finalColor; // 최종 적용
        targetImage.gameObject.SetActive(false); // 마지막 이미지 오브젝트 비활성화
    }

    /// <summary>
    /// 세트 리스트에 참조된 모든 오브젝트를 비활성화
    /// - finalFadeOutImage는 여기서 비활성화하지 않음
    /// </summary>
    private void DisableSetReferencedObjects() // 세트 참조 오브젝트 비활성화
    {
        HashSet<GameObject> targetObjectSet = new HashSet<GameObject>(); // 중복 제거용 집합

        for (int i = 0; i < setList.Count; i++)
        {
            IntroUISet currentSet = setList[i]; // 현재 세트 참조
            if (currentSet == null) continue; // 비어 있으면 건너뜀

            for (int j = 0; j < currentSet.imageList.Count; j++)
            {
                Image image = currentSet.imageList[j]; // 현재 이미지 참조
                if (image == null) continue; // 비어 있으면 건너뜀

                targetObjectSet.Add(image.gameObject); // 비활성화 대상 추가
            }

            for (int j = 0; j < currentSet.textList.Count; j++)
            {
                TMP_Text text = currentSet.textList[j]; // 현재 TMP 텍스트 참조
                if (text == null) continue; // 비어 있으면 건너뜀

                targetObjectSet.Add(text.gameObject); // 비활성화 대상 추가
            }
        }

        foreach (GameObject targetObject in targetObjectSet)
        {
            if (targetObject == null) continue; // 비어 있으면 건너뜀
            targetObject.SetActive(false); // 오브젝트 비활성화
        }
    }

    /// <summary>
    /// 마지막에 지정된 오브젝트 활성화
    /// </summary>
    private void ActivateAssignedObject() // 지정 오브젝트 활성화
    {
        if (activateTargetObject == null) return; // 할당이 없으면 종료
        activateTargetObject.SetActive(true); // 오브젝트 활성화
    }

    /// <summary>
    /// 커브가 없을 때도 안전하게 값 반환
    /// </summary>
    private float EvaluateCurveSafe(AnimationCurve curve, float time) // 커브 안전 평가
    {
        if (curve == null || curve.length == 0)
        {
            return time; // 커브가 없으면 선형값 사용
        }

        return Mathf.Clamp01(curve.Evaluate(time)); // 커브 평가 후 0~1 범위 제한
    }

    /// <summary>
    /// 외부에서 인트로를 강제 중지할 때 사용
    /// </summary>
    public void StopSequence() // 인트로 강제 중지
    {
        if (playCoroutine != null)
        {
            StopCoroutine(playCoroutine); // 실행 중 코루틴 중지
            playCoroutine = null; // 코루틴 참조 초기화
        }

        isPlaying = false; // 재생 상태 해제
    }
}