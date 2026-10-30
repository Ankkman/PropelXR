
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class DroneCrashLandingAudio : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DroneFlightController flight;
    [SerializeField] private AudioSource impactSource;
    [SerializeField] private AudioClip crashLandingClip;

    [Header("Impact Settings")]
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private float minImpactSpeed = 0.65f;
    [SerializeField] private float impactVolume = 0.75f;

    private bool crashPending;
    private bool impactPlayed;

    private void Awake()
    {
        if (flight == null)
            flight = GetComponent<DroneFlightController>();

        if (impactSource == null)
        {
            impactSource = gameObject.AddComponent<AudioSource>();
        }

        impactSource.playOnAwake = false;
        impactSource.loop = false;
        impactSource.spatialBlend = 1f;
        impactSource.minDistance = 0.5f;
        impactSource.maxDistance = 12f;
        impactSource.rolloffMode =
            AudioRolloffMode.Logarithmic;
    }

    private void Update()
    {
        if (flight == null) return;

        if (flight.IsCrashed)
        {
            crashPending = true;
        }
        else
        {
            crashPending = false;
            impactPlayed = false;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!crashPending || impactPlayed)
            return;

        if (((1 << collision.gameObject.layer) &
             groundLayers.value) == 0)
            return;

        // Reject wall contacts using the contact normal.
        bool groundContact = false;

        foreach (ContactPoint contact in collision.contacts)
        {
            if (Vector3.Dot(contact.normal, Vector3.up) > 0.65f)
            {
                groundContact = true;
                break;
            }
        }

        if (!groundContact)
            return;

        if (collision.relativeVelocity.magnitude < minImpactSpeed)
            return;

        impactPlayed = true;
        crashPending = false;

        if (crashLandingClip != null)
        {
            impactSource.PlayOneShot(
                crashLandingClip,
                impactVolume
            );
        }
    }
}
