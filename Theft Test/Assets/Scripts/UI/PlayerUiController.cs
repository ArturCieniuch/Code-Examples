using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerUiController : MonoBehaviour, IResettable
{
    [SerializeField]
    private GameObject detectionUI;
    [SerializeField]
    private Image detectionMeter;
    [SerializeField]
    private GameObject pickUpHint;
    [SerializeField]
    private GameObject dropHint;
    [SerializeField]
    private TextMeshProUGUI pickUpHintButton;
    [SerializeField]
    private TextMeshProUGUI dropHintButton;

    [SerializeField]
    private InteractionController interactionController;

    private void Awake()
    {
        ResetObject();
    }

    private void Start()
    {
        pickUpHintButton.text = interactionController.interactKey.ToString();
        dropHintButton.text = interactionController.interactKey.ToString();

        GameManager.instance.OnGameReseted += ResetObject;
    }

    public void OnObjectGrabbed()
    {
        pickUpHint.gameObject.SetActive(false);
        dropHint.gameObject.SetActive(true);
    }

    public void OnObjectDropped()
    {
        pickUpHint.gameObject.SetActive(true);
        dropHint.gameObject.SetActive(false);
    }

    public void UpdateDetectionUI(float detectionAmount)
    {
        if (detectionAmount == 0)
        {
            detectionUI.SetActive(false);
            return;
        }

        if (!detectionUI.activeInHierarchy)
        {
            detectionUI.SetActive(true);
        }

        detectionMeter.fillAmount = detectionAmount;
    }

    public void ResetObject()
    {
        detectionUI.gameObject.SetActive(false);
        pickUpHint.gameObject.SetActive(true);
        dropHint.gameObject.SetActive(false);
    }
}
