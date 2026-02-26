using System;
using UnityEngine;
using UnityEngine.Rendering;

public class CameraController : MonoBehaviour, IResettable
{
    [Header("Components")]
    [SerializeField] private PlayerDetectionController playerDetectionController;
    [SerializeField] private Transform cameraObject;
    [SerializeField] private SphereCollider trigger;

    [Header("Rotation Settings")]
    [SerializeField][Range(0, 180)] private int rotationLeftAngle;
    [SerializeField][Range(0, 180)] private int rotationRightAngle;
    [SerializeField] private float rotationSpeed = 15.0f;
    [Header("FOV Settings")]
    [SerializeField] private float fovAngle = 50f;
    [SerializeField] private float cameraRange = 15.0f;
    [SerializeField] private LayerMask cameraLayerMask;

    private Quaternion initialRotation;
    private Quaternion currentRotationTarget;
    private Quaternion targetLeftRotation;
    private Quaternion targetRightRotation;

    private Vector3 initialForward;
    private Vector3 initialRight;
    private const int GizmoSteps = 8;

    private void Awake()
    {
        targetLeftRotation = Quaternion.Euler(new Vector3(0, transform.rotation.eulerAngles.y - rotationLeftAngle, 0));
        targetRightRotation = Quaternion.Euler(new Vector3(0, transform.rotation.eulerAngles.y + rotationRightAngle, 0));
        currentRotationTarget = GetNewRotationTarget();

        trigger.radius = cameraRange;

        initialForward = cameraObject.forward;
        initialRight = transform.right;
        initialRotation = transform.rotation;
    }

    private void Start()
    {
        GameManager.instance.OnGameReseted += ResetObject;
        GameManager.instance.OnGamePaused += () => enabled = false;
    }

    private void Update()
    {
        Rotate();
    }

    private void Rotate()
    {
        transform.rotation = Quaternion.RotateTowards(transform.rotation, currentRotationTarget, rotationSpeed * Time.deltaTime);

        if (Quaternion.Angle(transform.rotation, currentRotationTarget) < 1f)
        {
            currentRotationTarget = GetNewRotationTarget();
        }
    }

    private Quaternion GetNewRotationTarget()
    {
        return Quaternion.Angle(targetLeftRotation, currentRotationTarget) < 1f ? targetRightRotation : targetLeftRotation;
    }

    private void OnTriggerStay(Collider other)
    {
        Vector3 directionToPlayer = (other.transform.position - cameraObject.position).normalized;

        if (Vector3.Angle(directionToPlayer, cameraObject.forward) <= fovAngle / 2f)
        {
            if (Physics.Linecast(transform.position, other.transform.position, out RaycastHit hit, cameraLayerMask, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.CompareTag("Player"))
                {
                    playerDetectionController.PlayerDetected();
                }
            }
        }
    }

    public void ResetObject()
    {
        enabled = true;
        transform.rotation = initialRotation;
        currentRotationTarget = GetNewRotationTarget();
    }

    void OnDrawGizmosSelected()
    {
        DrawRotationGizmos();
        DrawCameraFovGizmos();
        DrawPhysicalDetectionGizmos();
    }

    private void DrawCameraFovGizmos()
    {
        float halfFOV = fovAngle / 2.0f;

        Vector3 forwardRange = cameraObject.forward * cameraRange;

        Gizmos.color = Color.green;

        for (int step = 0; step <= GizmoSteps; step++)
        {
            Quaternion rayRotation = Quaternion.AngleAxis(Mathf.Lerp(-halfFOV, halfFOV, (float)step / GizmoSteps), cameraObject.up);

            Vector3 rayDirection = rayRotation * forwardRange;

            Gizmos.DrawRay(cameraObject.position, rayDirection);
        }

        for (int step = 0; step <= GizmoSteps; step++)
        {
            Quaternion rayRotation = Quaternion.AngleAxis(Mathf.Lerp(-halfFOV, halfFOV, (float)step / GizmoSteps), cameraObject.right);

            Vector3 rayDirection = rayRotation * forwardRange;

            Gizmos.DrawRay(cameraObject.position, rayDirection);
        }

        Quaternion leftRayRotation = Quaternion.AngleAxis(-halfFOV, cameraObject.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(halfFOV, cameraObject.up);
        Quaternion upRayRotation = Quaternion.AngleAxis(-halfFOV, cameraObject.right);
        Quaternion downRayRotation = Quaternion.AngleAxis(halfFOV, cameraObject.right);

        Vector3 leftRayDirection = cameraObject.position + leftRayRotation * forwardRange;
        Vector3 rightRayDirection = cameraObject.position + rightRayRotation * forwardRange;
        Vector3 upRayDirection = cameraObject.position + upRayRotation * forwardRange;
        Vector3 downRayDirection = cameraObject.position + downRayRotation * forwardRange;

        Gizmos.DrawLineStrip(new Vector3[]
        {
            leftRayDirection,
            upRayDirection,
            rightRayDirection,
            downRayDirection
        }, true);

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(cameraObject.position + forwardRange, 0.25f);
        Gizmos.DrawRay(cameraObject.position, forwardRange);
    }

    private void DrawPhysicalDetectionGizmos()
    {
        float halfFOV = fovAngle / 2.0f;

        Vector3 forwardRange = cameraObject.forward * cameraRange;

        Gizmos.color = Color.red;

        for (int step = 0; step <= GizmoSteps; step++)
        {
            Quaternion rayRotation = Quaternion.AngleAxis(Mathf.Lerp(-halfFOV, halfFOV, (float)step / GizmoSteps), cameraObject.up);

            Vector3 rayDirection = rayRotation * forwardRange;

            Ray ray = new Ray(cameraObject.position, rayDirection);
            if (Physics.Raycast(ray, out RaycastHit hit, cameraRange, cameraLayerMask, QueryTriggerInteraction.Ignore))
            {
                Gizmos.DrawSphere(hit.point, 0.15f);
                Gizmos.DrawLine(hit.point, cameraObject.position + rayDirection);
            }

        }

        for (int step = 0; step <= GizmoSteps; step++)
        {
            Quaternion rayRotation = Quaternion.AngleAxis(Mathf.Lerp(-halfFOV, halfFOV, (float)step / GizmoSteps), cameraObject.right);

            Vector3 rayDirection = rayRotation * forwardRange;

            Ray ray = new Ray(cameraObject.position, rayDirection);
            if (Physics.Raycast(ray, out RaycastHit hit, cameraRange, cameraLayerMask, QueryTriggerInteraction.Ignore))
            {
                Gizmos.DrawSphere(hit.point, 0.15f);
                Gizmos.DrawLine(hit.point, cameraObject.position + rayDirection);
            }
        }
    }

    private void DrawRotationGizmos()
    {
        float halfFOV = fovAngle / 2.0f;

        Vector3 forwardRange;

        if (initialRight != Vector3.zero)
        {
            forwardRange = initialRight * cameraRange;
        }
        else
        {
            forwardRange = transform.right * cameraRange;
        }

        Gizmos.color = Color.green;

        Quaternion leftRayRotation = Quaternion.AngleAxis(-rotationLeftAngle - halfFOV, transform.up);
        Quaternion rightRayRotation = Quaternion.AngleAxis(rotationRightAngle + halfFOV, transform.up);

        leftRayRotation = Quaternion.Euler(new Vector3(0,  -rotationLeftAngle - halfFOV, 0));
        rightRayRotation = Quaternion.Euler(new Vector3(0,  rotationRightAngle + halfFOV, 0));

        Vector3 leftRayDirection = leftRayRotation * forwardRange;
        Vector3 rightRayDirection = rightRayRotation * forwardRange;

        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(cameraObject.position, leftRayDirection);
        Gizmos.DrawRay(cameraObject.position, rightRayDirection);

        Gizmos.DrawSphere(transform.position+initialForward, 0.5f);
    }
}


