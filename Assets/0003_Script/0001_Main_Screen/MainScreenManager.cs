using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainScreenManager : MonoBehaviour
{
    [System.Serializable]
    public class UITargetMoveData
    {
        [Header("이동 대상 UI")]
        public RectTransform targetUI; // 이동시킬 UI RectTransform

        [Header("이동 시작 기준 위치")]
        public Vector3 initialLocalPosition; // 시작 로컬 위치값

        [Header("이동 방향")]
        public Vector3 moveDirection = Vector3.right; // 캠페인 이동 시 사용할 방향값

        [Header("이동 거리")]
        public float moveDistance = 300f; // 캠페인 이동 시 이동할 거리값
    }

    [Header("메인 버튼 참조")]
    [SerializeField] private Button exitSuggestButton; // 종료제시버튼
    [SerializeField] private Button campaignButton; // 캠페인버튼
    [SerializeField] private Button backButton; // 돌아가기버튼

    [Header("종료 확인 UI")]
    [SerializeField] private GameObject exitConfirmUI; // 종료 확인 UI창
    [SerializeField] private Button exitButton; // 실제 종료버튼
    [SerializeField] private Button exitCancelButton; // 종료취소버튼

    [Header("이동 대상 UI 리스트")]
    [SerializeField] private List<UITargetMoveData> moveTargetList = new List<UITargetMoveData>(); // 이동시킬 UI 정보 리스트

    [Header("캠페인 이동 설정")]
    [SerializeField] private float campaignMoveDuration = 0.5f; // 캠페인 이동 시간
    [SerializeField] private AnimationCurve campaignMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // 캠페인 이동 커브

    [Header("돌아가기 이동 설정")]
    [SerializeField] private float backMoveDuration = 0.5f; // 돌아가기 이동 시간
    [SerializeField] private AnimationCurve backMoveCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // 돌아가기 이동 커브

    [Header("현재 상태")]
    [SerializeField] private bool isCampaignState; // 현재 캠페인 버튼 클릭 후 상태인지 여부

    private Coroutine moveCoroutine; // 현재 실행 중인 이동 코루틴

    private void Awake() // 시작 전 초기 참조 상태 세팅
    {
        CacheInitialPositions(); // 각 UI의 시작 위치 저장
        SetInitialUIState(); // 시작 UI 상태 적용
    }

    private void OnEnable() // 활성화 시 버튼 이벤트 연결
    {
        AddButtonEvents(); // 버튼 클릭 이벤트 등록
    }

    private void OnDisable() // 비활성화 시 버튼 이벤트 해제
    {
        RemoveButtonEvents(); // 버튼 클릭 이벤트 제거
    }

    private void CacheInitialPositions() // 각 UI의 시작 로컬 위치 저장
    {
        for (int i = 0; i < moveTargetList.Count; i++)
        {
            UITargetMoveData data = moveTargetList[i]; // 현재 이동 데이터 참조
            if (data == null || data.targetUI == null) continue; // 비어 있으면 건너뜀

            data.initialLocalPosition = data.targetUI.localPosition; // 시작 위치 저장
        }
    }

    private void SetInitialUIState() // 시작 시 UI 상태 초기화
    {
        if (exitConfirmUI != null)
        {
            exitConfirmUI.SetActive(false); // 종료 확인 UI 비활성화
        }

        isCampaignState = false; // 시작 상태는 기본 메인 상태
    }

    private void AddButtonEvents() // 버튼 이벤트 연결
    {
        if (exitSuggestButton != null)
        {
            exitSuggestButton.onClick.AddListener(OpenExitConfirmUI); // 종료제시버튼 클릭 시 종료창 열기
        }

        if (campaignButton != null)
        {
            campaignButton.onClick.AddListener(OnClickCampaignButton); // 캠페인버튼 클릭 시 정방향 이동
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(OnClickBackButton); // 돌아가기버튼 클릭 시 반대방향 이동
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(QuitGame); // 종료버튼 클릭 시 게임 종료
        }

        if (exitCancelButton != null)
        {
            exitCancelButton.onClick.AddListener(CloseExitConfirmUI); // 종료취소버튼 클릭 시 종료창 닫기
        }
    }

    private void RemoveButtonEvents() // 버튼 이벤트 해제
    {
        if (exitSuggestButton != null)
        {
            exitSuggestButton.onClick.RemoveListener(OpenExitConfirmUI); // 종료제시버튼 이벤트 해제
        }

        if (campaignButton != null)
        {
            campaignButton.onClick.RemoveListener(OnClickCampaignButton); // 캠페인버튼 이벤트 해제
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveListener(OnClickBackButton); // 돌아가기버튼 이벤트 해제
        }

        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(QuitGame); // 종료버튼 이벤트 해제
        }

        if (exitCancelButton != null)
        {
            exitCancelButton.onClick.RemoveListener(CloseExitConfirmUI); // 종료취소버튼 이벤트 해제
        }
    }

    private void OpenExitConfirmUI() // 종료 확인 UI 열기
    {
        if (exitConfirmUI == null) return; // 참조가 없으면 종료
        exitConfirmUI.SetActive(true); // 종료 확인 UI 활성화
    }

    private void CloseExitConfirmUI() // 종료 확인 UI 닫기
    {
        if (exitConfirmUI == null) return; // 참조가 없으면 종료
        exitConfirmUI.SetActive(false); // 종료 확인 UI 비활성화
    }

    private void OnClickCampaignButton() // 캠페인버튼 클릭 처리
    {
        if (isCampaignState) return; // 이미 캠페인 상태면 중복 실행 방지

        StartMove(true, campaignMoveDuration, campaignMoveCurve); // 정방향 이동 시작
        isCampaignState = true; // 캠페인 상태로 변경
    }

    private void OnClickBackButton() // 돌아가기버튼 클릭 처리
    {
        if (!isCampaignState) return; // 캠페인 상태가 아니면 실행 안함

        StartMove(false, backMoveDuration, backMoveCurve); // 반대방향 이동 시작
        isCampaignState = false; // 기본 상태로 변경
    }

    private void StartMove(bool moveForward, float duration, AnimationCurve curve) // 이동 실행 시작
    {
        if (moveCoroutine != null)
        {
            StopCoroutine(moveCoroutine); // 이전 이동 코루틴 중지
        }

        moveCoroutine = StartCoroutine(MoveUIRoutine(moveForward, duration, curve)); // 새 이동 코루틴 실행
    }

    private IEnumerator MoveUIRoutine(bool moveForward, float duration, AnimationCurve curve) // UI 이동 코루틴
    {
        List<Vector3> startPositionList = new List<Vector3>(); // 시작 위치 저장 리스트
        List<Vector3> targetPositionList = new List<Vector3>(); // 목표 위치 저장 리스트

        for (int i = 0; i < moveTargetList.Count; i++)
        {
            UITargetMoveData data = moveTargetList[i]; // 현재 이동 데이터 참조

            if (data == null || data.targetUI == null)
            {
                startPositionList.Add(Vector3.zero); // 빈 데이터용 더미값 추가
                targetPositionList.Add(Vector3.zero); // 빈 데이터용 더미값 추가
                continue; // 다음 대상으로 진행
            }

            Vector3 direction = data.moveDirection.sqrMagnitude > 0f ? data.moveDirection.normalized : Vector3.zero; // 방향 정규화
            Vector3 offset = direction * data.moveDistance; // 기본 이동 오프셋 계산

            if (!moveForward)
            {
                offset = -offset; // 돌아가기일 때 반대 방향 오프셋 적용
            }

            Vector3 basePosition = moveForward ? data.initialLocalPosition : data.initialLocalPosition + (direction * data.moveDistance); // 이동 기준 시작점 계산
            Vector3 targetPosition = moveForward ? data.initialLocalPosition + (direction * data.moveDistance) : data.initialLocalPosition; // 이동 목표 위치 계산

            if (data.targetUI.localPosition != basePosition)
            {
                basePosition = data.targetUI.localPosition; // 현재 위치 기준으로 자연스럽게 시작
            }

            startPositionList.Add(basePosition); // 시작 위치 저장
            targetPositionList.Add(targetPosition); // 목표 위치 저장
        }

        if (duration <= 0f)
        {
            for (int i = 0; i < moveTargetList.Count; i++)
            {
                UITargetMoveData data = moveTargetList[i]; // 현재 이동 데이터 참조
                if (data == null || data.targetUI == null) continue; // 비어 있으면 건너뜀

                data.targetUI.localPosition = targetPositionList[i]; // 즉시 목표 위치 적용
            }

            moveCoroutine = null; // 코루틴 참조 초기화
            yield break; // 코루틴 종료
        }

        float elapsed = 0f; // 경과 시간

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime; // 시간 누적
            float normalizedTime = Mathf.Clamp01(elapsed / duration); // 0~1 정규화 시간
            float curveValue = EvaluateCurveSafe(curve, normalizedTime); // 커브 평가값 계산

            for (int i = 0; i < moveTargetList.Count; i++)
            {
                UITargetMoveData data = moveTargetList[i]; // 현재 이동 데이터 참조
                if (data == null || data.targetUI == null) continue; // 비어 있으면 건너뜀

                data.targetUI.localPosition = Vector3.LerpUnclamped(startPositionList[i], targetPositionList[i], curveValue); // 커브값 기준 위치 보간
            }

            yield return null; // 다음 프레임 대기
        }

        for (int i = 0; i < moveTargetList.Count; i++)
        {
            UITargetMoveData data = moveTargetList[i]; // 현재 이동 데이터 참조
            if (data == null || data.targetUI == null) continue; // 비어 있으면 건너뜀

            data.targetUI.localPosition = targetPositionList[i]; // 마지막 위치 보정
        }

        moveCoroutine = null; // 코루틴 참조 초기화
    }

    private float EvaluateCurveSafe(AnimationCurve curve, float time) // 커브 안전 평가
    {
        if (curve == null || curve.length == 0)
        {
            return time; // 커브가 없으면 선형값 사용
        }

        return curve.Evaluate(time); // 커브 평가값 반환
    }

    private void QuitGame() // 게임 종료 처리
    {
        Application.Quit(); // 빌드된 게임 종료

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // 에디터 실행 중이면 플레이 종료
#endif
    }
}