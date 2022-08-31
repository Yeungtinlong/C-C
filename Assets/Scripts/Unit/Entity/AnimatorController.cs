using UnityEngine;

public class AnimatorController : MonoBehaviour
{
    [SerializeField] private Animator _manualAnimator;
    public Animator ManualAnimator => _manualAnimator;
}
