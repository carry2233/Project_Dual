using UnityEngine; // Unity 기본 기능 사용
using UnityEngine.UI; // Button 컴포넌트 사용
using UnityEngine.SceneManagement; // 씬 이동 기능 사용

/// <summary>
/// 씬 이동 시스템
/// - 버튼 클릭 시 설정한 씬으로 이동
/// - 전투 종료 후 원래 씬 복귀 같은 구조의 기본 이동 담당
/// </summary>
public class SceneTransitionSystem : MonoBehaviour
{
    [Header("씬 이동 버튼")]
    [SerializeField] private Button sceneTransitionButton; // 씬 이동을 실행할 버튼

    [Header("이동할 씬 설정")]
    [SerializeField] private string targetSceneName; // 이동할 씬 이름

    private void Awake() // 오브젝트 초기화 시 버튼 이벤트 연결
    {
        RegisterButtonEvent(); // 버튼 클릭 이벤트 등록
    }

    private void OnDestroy() // 오브젝트 제거 시 버튼 이벤트 해제
    {
        UnregisterButtonEvent(); // 버튼 클릭 이벤트 해제
    }

    private void RegisterButtonEvent() // 버튼 클릭 이벤트 등록
    {
        if (sceneTransitionButton == null)
        {
            return; // 버튼이 없으면 종료
        }

        sceneTransitionButton.onClick.RemoveListener(LoadTargetScene); // 중복 등록 방지
        sceneTransitionButton.onClick.AddListener(LoadTargetScene); // 버튼 클릭 시 씬 이동 등록
    }

    private void UnregisterButtonEvent() // 버튼 클릭 이벤트 해제
    {
        if (sceneTransitionButton == null)
        {
            return; // 버튼이 없으면 종료
        }

        sceneTransitionButton.onClick.RemoveListener(LoadTargetScene); // 등록된 씬 이동 이벤트 제거
    }

    private void LoadTargetScene() // 설정된 씬으로 이동
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            return; // 씬 이름이 비어 있으면 종료
        }

        SceneManager.LoadScene(targetSceneName); // 설정한 씬으로 이동
    }
}