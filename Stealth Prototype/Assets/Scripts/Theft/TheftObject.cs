using System;
using UnityEngine;

public class TheftObject : MonoBehaviour, IInteractable, IResettable
{
    [SerializeField] private Rigidbody rigidbody;

    public bool IsGrabbed { get; private set; }

    public bool IsStolen { get; private set; }

    private TheftObjective connectedObjective;

    private Vector3 initialTransform;
    private Quaternion initialRotation;

    private void Awake()
    {
        initialTransform = transform.position;
        initialRotation = transform.rotation;
    }

    private void Start()
    {
        GameManager.instance.OnGameReseted += ResetObject;
    }

    public void Init(TheftObjective objective)
    {
        connectedObjective = objective;
    }

    public void Interact()
    {
        if (IsGrabbed)
        {
            Drop();
        }
        else
        {
            Grab();
        }
    }

    private void Grab()
    {
        IsGrabbed = true;
        rigidbody.isKinematic = true;
        PlayerController.instance.Grab(transform);
    }

    private void Drop()
    {
        IsGrabbed = false;
        rigidbody.isKinematic = false;
        transform.SetParent(null);
        PlayerController.instance.Drop();
        connectedObjective.CheckIfInZone(this);
    }

    public void SetSteal(bool isStollen)
    {
        IsStolen = isStollen;
        connectedObjective.CheckObjectiveProgress();
    }

    public void ResetObject()
    {
        IsGrabbed = false;
        IsStolen = false;
        rigidbody.isKinematic = false;
        transform.SetParent(null);
        transform.SetPositionAndRotation(initialTransform, initialRotation);
    }
}
