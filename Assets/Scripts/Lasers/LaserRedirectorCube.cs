using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class LaserRedirectorCube : MonoBehaviour
{
    [SerializeField] private Vector3 mirrorDetectionSize = new Vector3(1f, 0.35f, 1f);
    [SerializeField] private LayerMask mirrorLayers = ~0;
    [SerializeField] private BoxCollider cubeCollider;

    private readonly Collider[] mirrorBuffer = new Collider[16];

    public Vector3 OutputPosition
    {
        get
        {
            GetTopFace(out Vector3 position, out _, out _, out _);
            return position;
        }
    }

    public Vector3 OutputDirection
    {
        get
        {
            GetTopFace(out _, out Vector3 normal, out _, out _);
            return normal;
        }
    }

    private void Awake()
    {
        ResolveCubeCollider();
    }

    public bool TryGetMountedMirror(out LaserMirror mirror)
    {
        GetTopFace(
            out Vector3 outputPosition,
            out Vector3 outputDirection,
            out _,
            out Vector3 faceForward
        );

        Vector3 configuredSize = SaveDataValidator.Finite(
            mirrorDetectionSize,
            new Vector3(1f, 0.35f, 1f)
        );
        Vector3 detectionSize = new Vector3(
            Mathf.Max(0.01f, Mathf.Abs(configuredSize.x)),
            Mathf.Max(0.01f, Mathf.Abs(configuredSize.y)),
            Mathf.Max(0.01f, Mathf.Abs(configuredSize.z))
        );
        Vector3 center = outputPosition + outputDirection * detectionSize.y * 0.5f;
        Vector3 halfSize = detectionSize * 0.5f;
        Quaternion detectionRotation = Quaternion.LookRotation(faceForward, outputDirection);

        int count = Physics.OverlapBoxNonAlloc(
            center,
            halfSize,
            mirrorBuffer,
            detectionRotation,
            mirrorLayers,
            QueryTriggerInteraction.Collide
        );

        float nearestDistance = float.MaxValue;
        mirror = null;

        for (int i = 0; i < count; i++)
        {
            Collider detectedCollider = mirrorBuffer[i];

            if (detectedCollider == null)
                continue;

            LaserMirror detectedMirror = detectedCollider.GetComponentInParent<LaserMirror>();

            if (detectedMirror == null)
                continue;

            float distance = (detectedCollider.ClosestPoint(outputPosition) -
                              outputPosition).sqrMagnitude;

            if (distance >= nearestDistance)
                continue;

            nearestDistance = distance;
            mirror = detectedMirror;
        }

        return mirror != null;
    }

    public void GetTopFace(
        out Vector3 position,
        out Vector3 normal,
        out Vector3 faceRight,
        out Vector3 faceForward
    )
    {
        if (cubeCollider == null)
            ResolveCubeCollider();

        if (cubeCollider == null)
        {
            position = transform.position;
            normal = GetWorldUp();
            faceRight = Vector3.ProjectOnPlane(transform.right, normal).normalized;

            if (faceRight.sqrMagnitude < 0.000001f)
                faceRight = Vector3.ProjectOnPlane(transform.forward, normal).normalized;

            if (faceRight.sqrMagnitude < 0.000001f)
                faceRight = Vector3.Cross(normal, Vector3.forward).normalized;

            faceForward = Vector3.Cross(faceRight, normal).normalized;
            return;
        }

        BoxCollider boxCollider = cubeCollider;
        Transform boxTransform = boxCollider.transform;
        Vector3 worldUp = GetWorldUp();

        float rightAlignment = Vector3.Dot(boxTransform.right, worldUp);
        float upAlignment = Vector3.Dot(boxTransform.up, worldUp);
        float forwardAlignment = Vector3.Dot(boxTransform.forward, worldUp);

        int selectedAxis;
        float selectedAlignment;

        if (Mathf.Abs(rightAlignment) >= Mathf.Abs(upAlignment) &&
            Mathf.Abs(rightAlignment) >= Mathf.Abs(forwardAlignment))
        {
            selectedAxis = 0;
            selectedAlignment = rightAlignment;
            normal = boxTransform.right * Mathf.Sign(rightAlignment);
        }
        else if (Mathf.Abs(upAlignment) >= Mathf.Abs(forwardAlignment))
        {
            selectedAxis = 1;
            selectedAlignment = upAlignment;
            normal = boxTransform.up * Mathf.Sign(upAlignment);
        }
        else
        {
            selectedAxis = 2;
            selectedAlignment = forwardAlignment;
            normal = boxTransform.forward * Mathf.Sign(forwardAlignment);
        }

        if (Mathf.Approximately(selectedAlignment, 0f))
            normal = worldUp;

        Vector3 scale = boxTransform.lossyScale;
        scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));

        Vector3 halfSize = Vector3.Scale(boxCollider.size * 0.5f, scale);
        float faceDistance = selectedAxis == 0
            ? halfSize.x
            : selectedAxis == 1
                ? halfSize.y
                : halfSize.z;

        position = boxTransform.TransformPoint(boxCollider.center) + normal * faceDistance;

        if (selectedAxis == 0)
        {
            faceRight = boxTransform.up;
            faceForward = boxTransform.forward;
        }
        else if (selectedAxis == 1)
        {
            faceRight = boxTransform.right;
            faceForward = boxTransform.forward;
        }
        else
        {
            faceRight = boxTransform.right;
            faceForward = boxTransform.up;
        }

        faceRight = Vector3.ProjectOnPlane(faceRight, normal).normalized;
        faceForward = Vector3.ProjectOnPlane(faceForward, normal).normalized;
    }

    private void ResolveCubeCollider()
    {
        if (cubeCollider != null)
            return;

        BoxCollider[] colliders = GetComponents<BoxCollider>();

        foreach (BoxCollider candidate in colliders)
        {
            if (candidate != null && candidate.enabled && !candidate.isTrigger)
            {
                cubeCollider = candidate;
                break;
            }
        }

        if (cubeCollider == null && colliders.Length > 0)
            cubeCollider = colliders[0];

        if (cubeCollider == null)
            Debug.LogWarning("LaserRedirectorCube: не найден BoxCollider", this);
    }

    private Vector3 GetWorldUp()
    {
        return Physics.gravity.sqrMagnitude > 0.0001f
            ? -Physics.gravity.normalized
            : Vector3.up;
    }
} 
