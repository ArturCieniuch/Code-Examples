using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TheftObjective : MonoBehaviour, IResettable
{
    [SerializeField] private List<TheftObject> objectsToSteal;
    [SerializeField] private List<Collider> stealZones;
    [SerializeField] private List<ObjectiveFailureDetector> failureDetectors;
    [SerializeField] private ObjectiveState startState;

    [SerializeField]
    [Tooltip("In place of a number of items to steal place [X]")]
    private string objectiveDescription;

    public enum ObjectiveState
    {
        WAITING,
        IN_PROGRESS,
        FAILED,
        COMPLETED
    }

    public Action onObjectiveCompleted;
    public Action onObjectiveFailed;
    private ObjectiveState state;
    private ObjectiveUI connectedUI;

    public ObjectiveState State => state;

    public void CheckObjectiveProgress()
    {
        connectedUI.UpdateCount(objectsToSteal.Count(x => x.IsStolen), objectsToSteal.Count);

        bool isCompleted = true;

        foreach (TheftObject theftObject in objectsToSteal)
        {
            isCompleted &= theftObject.IsStolen;
        }

        if (isCompleted)
        {
            ObjectiveCompleted();
        }
    }

    private void Awake()
    {
        state = startState;
    }

    private void Start()
    {
        foreach (TheftObject theftObject in objectsToSteal)
        {
            theftObject.Init(this);
        }

        foreach (ObjectiveFailureDetector failureDetector in failureDetectors)
        {
            failureDetector.OnFailure += ObjectiveFailed;
        }

        GameManager.instance.OnGameReseted += ResetObject;
    }

    public void CheckIfInZone(TheftObject theftObject)
    {
        bool isStollen = true;

        foreach (var zone in stealZones)
        {
            if (zone.bounds.Contains(theftObject.transform.position))
            {
                isStollen = false;
            }
        }

        theftObject.SetSteal(isStollen);
    }

    public void ObjectiveCompleted()
    {
        state = ObjectiveState.COMPLETED;
        connectedUI.UpdateState(state);
        onObjectiveCompleted.Invoke();
    }

    public void ObjectiveFailed(ObjectiveFailureDetector failureDetector)
    {
        if (state == ObjectiveState.FAILED)
        {
            return;
        }

        state = ObjectiveState.FAILED;
        connectedUI.UpdateState(state);

        onObjectiveFailed.Invoke();
    }

    private string GetObjectiveDescription()
    {
        return objectiveDescription.Replace("[X]", objectsToSteal.Count.ToString());
    }

    public void SetAndConnectUI(ObjectiveUI uiToSet)
    {
        connectedUI = uiToSet;
        uiToSet.SetUI(state, GetObjectiveDescription(), 0, objectsToSteal.Count);
    }

    public void ResetObject()
    {
        state = startState;
        connectedUI.SetUI(state, GetObjectiveDescription(), 0, objectsToSteal.Count);
    }
}
