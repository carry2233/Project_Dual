using UnityEngine;

/// <summary>
/// 결투 샌드백 대상 설정 스크립트
/// - 이 컴포넌트가 붙은 캐릭터는 샌드백 대상으로 취급
/// - 스스로 행동하지 않고, 다른 캐릭터의 결투 테스트 대상이 됨
/// </summary>
public class DuelSandbagTarget : MonoBehaviour
{
    [Header("샌드백 설정")]
    [SerializeField] private bool isSandbagTarget = true; // 현재 오브젝트를 샌드백 대상으로 사용할지 여부

    public bool IsSandbagTarget => isSandbagTarget; // 샌드백 대상 여부 반환
}