using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController instance;

    [SerializeField] private Transform grabParent;
    [SerializeField] private PlayerUiController playerUiController;

    private void Awake()
    {
        instance = this;
    }

    public void Grab(Transform grabbedObject)
    {
        grabbedObject.SetParent(grabParent);
        grabbedObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
        playerUiController.OnObjectGrabbed();
    }

    public void Drop()
    {
        playerUiController.OnObjectDropped();
    }
}
