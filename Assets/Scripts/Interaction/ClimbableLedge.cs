using UnityEngine;

public class ClimbableLedge : MonoBehaviour
{
    [SerializeField] private Transform landingPoint;

    public Vector3 LandingPosition => landingPoint != null
        ? landingPoint.position
        : transform.position;
}
