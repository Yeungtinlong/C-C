using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class UIMainMenu : MonoBehaviour
{
    public UnityAction NewGameButtonAction;
    public UnityAction ContinueButtonAction;

    public void NewGameButton()
    {
        NewGameButtonAction.Invoke();
    }

    public void ContinueButton()
    {
        ContinueButtonAction.Invoke();
    }
}
