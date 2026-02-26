using System;
using UnityEngine;

public abstract class ObjectiveFailureDetector : MonoBehaviour
{
    public Action<ObjectiveFailureDetector> OnFailure;
}
