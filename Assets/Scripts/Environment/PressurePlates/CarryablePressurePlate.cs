using UnityEngine;

public class CarryablePressurePlate : PressurePlate
{
    [SerializeField, Min(0f)] private float minimumHeight = 0f;

    protected override GameObject GetActivator(Collider other)
    {
        CarryableItem item = other.GetComponentInParent<CarryableItem>();

        if (item == null || item.IsHeld)
            return null;

        Vector3 localCenter = transform.InverseTransformPoint(other.bounds.center);

        if (localCenter.y < Mathf.Max(0f, minimumHeight))
            return null;

        return item.gameObject;
    }
}
