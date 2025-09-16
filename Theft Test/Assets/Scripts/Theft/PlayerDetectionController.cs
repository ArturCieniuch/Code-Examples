using System;
using UnityEngine;

public class PlayerDetectionController : ObjectiveFailureDetector, IResettable
{
    [SerializeField] private float timeToDetect = 1f;
    [SerializeField] private float timeToResetDetection = 0.5f;

    [SerializeField] private PlayerUiController playerUiController;

    private float detectionMeter;

    public float GetMeterProgress => detectionMeter / timeToDetect;

    private float resetTimer;

    private void Start()
    {
        GameManager.instance.OnGameReseted += ResetObject;
    }

    public void PlayerDetected()
    {
        detectionMeter += Time.deltaTime;

        resetTimer = 0;

        if (detectionMeter >= timeToDetect)
        {
            OnFailure.Invoke(this);
        }
    }

    private void Update()
    {
        if (resetTimer >= timeToResetDetection)
        {
            detectionMeter = 0;
        }

        resetTimer += Time.deltaTime;

        playerUiController.UpdateDetectionUI(GetMeterProgress);
    }

    public void ResetObject()
    {
        detectionMeter = 0;
        resetTimer = 0;
    }
}
