using UnityEngine;

public class ObjectiveUiManager : MonoBehaviour
{
    [SerializeField] private ObjectiveUI objectiveUiPrefab;

    [SerializeField] private Transform objectiveContainer;


    void Start()
    {
        foreach (var objective in GameManager.instance.GetObjectives)
        {
            ObjectiveUI newObjectiveUI = Instantiate(objectiveUiPrefab, objectiveContainer);
            objective.SetAndConnectUI(newObjectiveUI);
        }
    }
}
