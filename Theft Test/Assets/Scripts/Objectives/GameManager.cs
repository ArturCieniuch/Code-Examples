using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [SerializeField]
    private List<TheftObjective> objectives;
    public List<TheftObjective> GetObjectives => objectives;

    public Action OnGameCompleted;
    public Action OnGameFailed;
    public Action OnGameReseted;
    public Action OnGamePaused;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        foreach (var objective in objectives)
        {
            objective.onObjectiveCompleted += ObjectiveCompleted;
            objective.onObjectiveFailed += ObjectiveFailed;
        }
    }

    private void ObjectiveCompleted()
    {
        if (objectives.Count(x => x.State != TheftObjective.ObjectiveState.COMPLETED) > 0)
        {
            return;
        }

        OnGamePaused.Invoke();
        OnGameCompleted.Invoke();
    }

    private void ObjectiveFailed()
    {
        OnGamePaused.Invoke();
        OnGameFailed.Invoke();
    }

    public void ResetGame()
    {
        OnGameReseted.Invoke();
    }
}
