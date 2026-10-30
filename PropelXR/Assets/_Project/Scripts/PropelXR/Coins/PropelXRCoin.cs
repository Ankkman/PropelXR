using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class PropelXRCoin : MonoBehaviour
{
    [SerializeField] private float spinDegreesPerSecond = 90f;
    [SerializeField] private float floatAmplitude = 0.035f;
    [SerializeField] private float floatFrequency = 1.2f;
    private Vector3 initialPosition;
    private PropelXRCoinField owner;
    private bool collected;

    public void Initialize(PropelXRCoinField field)
    {
        owner = field;
        initialPosition = transform.position;
        SphereCollider trigger = GetComponent<SphereCollider>();
        trigger.isTrigger = true;
    }

    private void Update()
    {
        transform.Rotate(0f, spinDegreesPerSecond * Time.deltaTime, 0f, Space.World);
        transform.position = initialPosition + Vector3.up *
            (Mathf.Sin(Time.time * floatFrequency * Mathf.PI * 2f) * floatAmplitude);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected || owner == null) return;
        // Only the intended drone Rigidbody can collect, not user hands/room geometry.
        Rigidbody attachedBody = other.attachedRigidbody;
        if (attachedBody == null || attachedBody != owner.DroneBody) return;
        if (!owner.CanCollect) return;
        collected = true;
        owner.Collect(this);
    }
}
