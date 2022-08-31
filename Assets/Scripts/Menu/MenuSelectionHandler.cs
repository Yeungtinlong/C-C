using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuSelectionHandler : MonoBehaviour {
    //[SerializeField] private InputReader _inputReader;
    [SerializeField] private GameObject _defaultSelection;
    public GameObject currentSelection;
    public GameObject mouseSelection;

    private void OnEnable() {

    }

    private void OnDisable() {

    }

    public void HandleMouseEnter(GameObject UIElement) {
        mouseSelection = UIElement;
        EventSystem.current.SetSelectedGameObject(UIElement);
    }

    public void HandleMouseExit(GameObject UIElement) {
        mouseSelection = null;
        EventSystem.current.SetSelectedGameObject(currentSelection);
    }

    public void UpdateSelection(GameObject UIElement) {
        
        if (UIElement.GetComponent<MultiInputSelectableElement>() != null) {
            mouseSelection = UIElement;
            currentSelection = UIElement;
        }
        
    }

    private void Update() {
        if ((EventSystem.current != null) && EventSystem.current.currentSelectedGameObject == null && (currentSelection != null)) {
            EventSystem.current.SetSelectedGameObject(currentSelection);
        }
    }
}
