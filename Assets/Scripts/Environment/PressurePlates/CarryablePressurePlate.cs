using UnityEngine;

public class CarryablePressurePlate : PressurePlate
{
    [SerializeField] private float minimumHeight = 0f;

    protected override GameObject GetActivator(Collider other)
    {
        CarryableItem item = other.GetComponentInParent<CarryableItem>();

        if (item == null || item.IsHeld)
            return null;

        Vector3 localCenter = transform.InverseTransformPoint(other.bounds.center);

        if (localCenter.y < minimumHeight)
            return null;

        return item.gameObject;
    }
}
