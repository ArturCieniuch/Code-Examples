using System;
using TMPro;
using UnityEngine;

public class ObjectiveUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI description;
    [SerializeField] private TextMeshProUGUI count;

    [SerializeField] private Color waitingColor;
    [SerializeField] private Color inProgressColor;
    [SerializeField] private Color failedColor;
    [SerializeField] private Color completedColor;

    public void SetUI(TheftObjective.ObjectiveState state, string descriptionText, int currentCount, int maxCount)
    {
        description.text = descriptionText;
        count.text = $"{currentCount} / {maxCount}";

        UpdateState(state);
    }

    public void UpdateCount(int currentCount, int maxCount)
    {
        count.text = $"{currentCount} / {maxCount}";
    }

    public void UpdateState(TheftObjective.ObjectiveState state)
    {
        switch (state)
        {
            case TheftObjective.ObjectiveState.WAITING:
                description.color = waitingColor;
                count.color = completedColor;
                break;
            case TheftObjective.ObjectiveState.IN_PROGRESS:
                description.color = inProgressColor;
                count.color = inProgressColor;
                break;
            case TheftObjective.ObjectiveState.FAILED:
                description.color = failedColor;
                count.color = failedColor;
                break;
            case TheftObjective.ObjectiveState.COMPLETED:
                description.color = completedColor;
                count.color = completedColor;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }
}

