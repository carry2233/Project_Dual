using UnityEngine;

/// <summary>
/// Y축 위치값에 비례하여 Z축 위치를 실시간으로 변경하는 스크립트
/// - 로컬 좌표 사용 시: 시작 로컬 Y값 기준 변화량으로 Z를 계산
/// - 월드 좌표 사용 시: 현재 월드 Y값 자체를 기준으로 Z를 계산
/// - "Y축값 n당 Z축값 N 변화" 규칙으로 Z를 갱신
/// - Y 기준 방향 / Z 변화 방향을 각각 양수/음수로 설정 가능
/// </summary>
public class YToZPositionConverter : MonoBehaviour
{
    /// <summary>
    /// 방향 부호 선택용 enum
    /// </summary>
    public enum AxisSign
    {
        Positive = 1,   // + 방향
        Negative = -1   // - 방향
    }

    [Header("기준 설정")]
    [SerializeField] private float yBaseValue = 1f; // Y축값 n의 크기
    [SerializeField] private float zChangeValue = 1f; // Z축값 N의 크기

    [Header("방향 설정")]
    [SerializeField] private AxisSign yBaseDirection = AxisSign.Negative; // Y 기준 방향(+n / -n)
    [SerializeField] private AxisSign zChangeDirection = AxisSign.Negative; // Z 변화 방향(+N / -N)

    [Header("좌표 기준")]
    [SerializeField] private bool useLocalPosition = true; // true면 localPosition 기준, false면 worldPosition 기준

    [Header("월드 좌표 기준 설정")]
    [SerializeField] private float baseWorldZValue = 0f; // 월드 좌표 기준 사용 시 Z 계산의 기준값

    private float startLocalYValue; // 시작 시점 로컬 Y값 저장
    private float startLocalZValue; // 시작 시점 로컬 Z값 저장

    private void Start() // 시작 시 기준 위치 저장
    {
        if (useLocalPosition) // 로컬 좌표 기준일 때만 시작 기준 저장
        {
            startLocalYValue = transform.localPosition.y; // 시작 로컬 Y 저장
            startLocalZValue = transform.localPosition.z; // 시작 로컬 Z 저장
        }
    }

    private void Update() // 매 프레임 Z값 갱신
    {
        if (Mathf.Approximately(yBaseValue, 0f)) // 0으로 나누기 방지
            return; // 기준값 0이면 중단

        float signedYBaseValue = yBaseValue * (int)yBaseDirection; // 선택한 Y 방향 부호 적용
        float signedZChangeValue = zChangeValue * (int)zChangeDirection; // 선택한 Z 방향 부호 적용

        if (useLocalPosition) // 로컬 좌표 기준 계산
        {
            float currentLocalYValue = transform.localPosition.y; // 현재 로컬 Y값 가져오기
            float currentLocalYDelta = currentLocalYValue - startLocalYValue; // 시작 로컬 Y 기준 변화량 계산

            float ratio = currentLocalYDelta / signedYBaseValue; // 현재 Y 변화가 기준 Y의 몇 배인지 계산
            float calculatedZDelta = ratio * signedZChangeValue; // Z 변화량 계산
            float finalLocalZValue = startLocalZValue + calculatedZDelta; // 시작 로컬 Z 기준 최종 Z 계산

            Vector3 currentLocalPosition = transform.localPosition; // 현재 로컬 위치 가져오기
            currentLocalPosition.z = finalLocalZValue; // 계산된 Z 적용
            transform.localPosition = currentLocalPosition; // 위치 반영
        }
        else // 월드 좌표 기준 계산
        {
            float currentWorldYValue = transform.position.y; // 현재 월드 Y값 자체를 가져오기

            float ratio = currentWorldYValue / signedYBaseValue; // 현재 월드 Y값이 기준 Y의 몇 배인지 계산
            float calculatedZDelta = ratio * signedZChangeValue; // Z 변화량 계산
            float finalWorldZValue = baseWorldZValue + calculatedZDelta; // 기준 월드 Z값에 계산된 변화량 적용

            Vector3 currentWorldPosition = transform.position; // 현재 월드 위치 가져오기
            currentWorldPosition.z = finalWorldZValue; // 계산된 Z 적용
            transform.position = currentWorldPosition; // 위치 반영
        }
    }

    /// <summary>
    /// 현재 로컬 위치를 새로운 기준점으로 다시 저장
    /// - 로컬 좌표 기준 사용 시에만 의미가 있음
    /// </summary>
    public void ResetLocalBaseValues() // 현재 로컬 위치를 시작 기준으로 재설정
    {
        if (!useLocalPosition) // 월드 좌표 기준 사용 중이면
            return; // 중단

        startLocalYValue = transform.localPosition.y; // 현재 로컬 Y를 기준값으로 저장
        startLocalZValue = transform.localPosition.z; // 현재 로컬 Z를 기준값으로 저장
    }
}