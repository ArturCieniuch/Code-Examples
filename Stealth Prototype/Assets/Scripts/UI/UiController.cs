using System;
using UnityEngine;
using UnityEngine.UI;

public class UiController : MonoBehaviour, IResettable
{
    [SerializeField] private GameObject hud;
    [SerializeField] private GameObject objectiveUI;
    [SerializeField] private GameObject endGameUI;
    [SerializeField] private Button tryAgainButton;
    [SerializeField] private GameObject failedUI;
    [SerializeField] private GameObject winUI;

    private void Start()
    {
        ResetObject();

        GameManager.instance.OnGameCompleted += OnGameCompleted;
        GameManager.instance.OnGameFailed += OnGameFailed;
        GameManager.instance.OnGameReseted += ResetObject;
        tryAgainButton.onClick.AddListener(OnTryAgainButton);
    }

    private void OnGameCompleted()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        hud.SetActive(false);
        endGameUI.SetActive(true);
        winUI.SetActive(true);
    }

    private void OnGameFailed()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        hud.SetActive(false);
        endGameUI.SetActive(true);
        failedUI.SetActive(true);
    }

    public void ResetObject()
    {
        hud.SetActive(true);
        objectiveUI.SetActive(true);
        endGameUI.SetActive(false);
        winUI.SetActive(false);
        failedUI.SetActive(false);
    }

    public void OnTryAgainButton()
    {
        GameManager.instance.ResetGame();
    }
}
