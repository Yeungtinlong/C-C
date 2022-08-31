using UnityEngine;
using UnityEngine.UI;

public class DamageableUICanvas : MonoBehaviour
{
    [SerializeField] private Slider _healthBar = default;
    [SerializeField] private Text _healthText = default;
    [SerializeField] private float _sizeRatio;

    private Camera _mainCamera;
    [SerializeField] private Color _alliesColor = default;
    [SerializeField] private Color _enemyColor = default;

    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    private void LateUpdate()
    {
        AdaptSize();
        LookAtCamera();
    }

    public void SetHealthValue(int currentHealth, int maxHealth)
    {
        _healthText.text = currentHealth.ToString() + "/" + maxHealth.ToString();
        _healthBar.value = (float)currentHealth / (float)maxHealth;
    }

    public void ToggleHealthBar(bool isToggled, bool isAllies)
    {
        _healthBar.gameObject.SetActive(isToggled);
        _healthText.gameObject.SetActive(isToggled);

        if (isAllies)
            _healthBar.fillRect.GetComponent<Image>().color = _alliesColor;
        else
            _healthBar.fillRect.GetComponent<Image>().color = _enemyColor;
    }

    private void AdaptSize()
    {
        transform.localScale = Vector3.one * (transform.position - _mainCamera.transform.position).magnitude * _sizeRatio;
    }

    private void LookAtCamera()
    {
        transform.rotation = _mainCamera.transform.rotation;
    }
}
