using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class FadeController : MonoBehaviour
{
    [SerializeField] private FadeChannelSO _fadeChannelSO;
    [SerializeField] private Image _imageComponent;

    private void OnEnable()
    {
        _fadeChannelSO.OnEventRaised += InitiateFade;
    }

    private void OnDisable()
    {
        _fadeChannelSO.OnEventRaised -= InitiateFade;
    }

    private void InitiateFade(bool isFadeIn, float duration, Color color)
    {
        _imageComponent.DOBlendableColor(color, duration);
    }
}
