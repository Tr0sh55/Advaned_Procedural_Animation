using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class AdvancedSpiderMovement : MonoBehaviour
{
    [SerializeField] private Transform[] legTargets;
    [SerializeField] private Transform[] poles;
    [SerializeField] private float speed = 0.5f;
    [SerializeField] private float stepDuration = 0.1f;
    [SerializeField] private float stepHeight = 0.23f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float maxDist = 0.3f;
    [SerializeField] private float rayAngle = 45f;
    [SerializeField] private float frontRayDistance = 0.5f;
    [SerializeField] private float backRayDistance = 0.5f;
    [SerializeField] private float downRayDistance = 0.5f;
    [SerializeField] private Vector3 frontRayOffset = new Vector3(0, 0, 0.2f);
    [SerializeField] private Vector3 backRayOffset = new Vector3(0, 0, -0.2f);
    [SerializeField] private Vector3 downRayOffset = new Vector3(0, -0.3f, 0);
    [SerializeField] private Vector3 verticalBodyOffset = new Vector3(0, 0.7f, 0);
    [SerializeField] private float smoothTransitionSpeed = 1f;

    private Vector3[] maxDistSpheres;
    private bool isMovingLeg = false;
    private BoneManager boneManager;
    private Vector3 lastVelocity;

    void Start()
    {
        maxDistSpheres = new Vector3[legTargets.Length];
        for (int i = 0; i < legTargets.Length; i++)
        {
            maxDistSpheres[i] = legTargets[i].position;
        }

        boneManager = GetComponent<BoneManager>(); // Assuming BoneManager is on the same GameObject
    }

    void Update()
    {
        Vector3 velocity = MoveCharacter();
        if (velocity.magnitude > 0.01f) // Threshold to avoid jittery rotation when velocity is very low
        {
            lastVelocity = velocity;
            //RotateBodyTowardsVelocity(velocity);
        }

        for (int i = 0; i < legTargets.Length; i++)
        {
            UpdateMaxDistSpherePosition(i); // Updated to modify maxDistSphere position
        }

        TryMoveLegs();
        AdjustBodyRotation();
        AdjustBodyPosition();
    }

    private Vector3 MoveCharacter()
    {
        Vector3 movement = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")).normalized;
        movement *= speed * Time.deltaTime;
        transform.position += movement;

        // Update maxDistSpheres positions with character movement
        for (int i = 0; i < maxDistSpheres.Length; i++)
        {
            maxDistSpheres[i] += movement;
        }

        return movement;
    }


    void AdjustBodyPosition()
    {
        Vector3 averagePosition = Vector3.zero;
        for (int i = 0; i < legTargets.Length; i++)
        {
            averagePosition += legTargets[i].position;
        }

        averagePosition /= legTargets.Length;
        transform.position = averagePosition + verticalBodyOffset;
    }

    void AdjustBodyRotation()
    {
        Vector3 averageNormal = Vector3.zero;
        int hits = 0;

        // Extend raycasts to better detect walls and ground
        RaycastHit hit;
        if (Physics.Raycast(transform.position + verticalBodyOffset, transform.forward, out hit, frontRayDistance,
                groundLayer) ||
            Physics.Raycast(transform.position + verticalBodyOffset, -transform.up, out hit, downRayDistance,
                groundLayer))
        {
            averageNormal += hit.normal;
            hits++;
        }

        if (hits > 0)
        {
            averageNormal /= hits;
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, averageNormal) * transform.rotation;
            transform.rotation =
                Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothTransitionSpeed);
        }
    }


    void UpdateMaxDistSpherePosition(int legIndex)
    {
        Vector3 maxDistSphere = maxDistSpheres[legIndex];
        Vector3 characterDirection = transform.forward;
        Vector3 downDirection = Vector3.down;
        

        // Angling the raycast directions downwards at about 45 degrees forwards and backwards
        Vector3 forwardAngleDirection = Quaternion.AngleAxis(rayAngle, transform.right) *
                                        Quaternion.AngleAxis(45, transform.right) * characterDirection;
        Vector3 backwardAngleDirection = Quaternion.AngleAxis(-rayAngle, transform.right) *
                                         Quaternion.AngleAxis(-45, transform.right) * -characterDirection;

        RaycastHit hit;
        bool foundSurface = false;

        Debug.DrawRay(maxDistSphere + frontRayOffset, forwardAngleDirection * frontRayDistance, Color.red);
        Debug.DrawRay(maxDistSphere + backRayOffset, backwardAngleDirection * backRayDistance, Color.red);
        Debug.DrawRay(maxDistSphere + downRayOffset, downDirection * downRayDistance, Color.red);

        if (Physics.Raycast(maxDistSphere + frontRayOffset, forwardAngleDirection, out hit, frontRayDistance,
                groundLayer))
        {
            Debug.DrawRay(maxDistSphere + frontRayOffset, hit.point - maxDistSphere + frontRayOffset, Color.green);
            Vector3 offset = hit.point - maxDistSphere + frontRayOffset;
            maxDistSpheres[legIndex] += offset;
            foundSurface = true;
        }
        else if (Physics.Raycast(maxDistSphere + backRayOffset, backwardAngleDirection, out hit, backRayDistance,
                     groundLayer))
        {
            Debug.DrawRay(maxDistSphere + backRayOffset, hit.point - maxDistSphere + backRayOffset, Color.green);
            Vector3 offset = hit.point - maxDistSphere + backRayOffset;
            maxDistSpheres[legIndex] += offset;
            foundSurface = true;
        }
        else if (Physics.Raycast(maxDistSphere + downRayOffset, downDirection, out hit, downRayDistance, groundLayer))
        {
            Debug.DrawRay(maxDistSphere + downRayOffset, hit.point - maxDistSphere + downRayOffset, Color.green);
            Vector3 offset = hit.point - maxDistSphere + downRayOffset;
            maxDistSpheres[legIndex] += offset;
            foundSurface = true;
        }

        if (foundSurface)
        {
            // Assuming 'newSurfacePosition' is the calculated position on the surface
            Vector3 targetPosition = hit.point;
            // Smoothly move maxDistSphere towards the target position
            maxDistSpheres[legIndex] = Vector3.Lerp(maxDistSpheres[legIndex], targetPosition,
                Time.deltaTime * smoothTransitionSpeed);
        }
        else
        {
            // Keep maxDistSpheres aligned with the moving body if no suitable ground is found
            maxDistSpheres[legIndex] = transform.position + (maxDistSphere - transform.position);
        }
    }

    private IEnumerator MoveLegRoutine(int legIndex)
    {
        isMovingLeg = true;
        Transform legTarget = legTargets[legIndex];
        Vector3 startPosition = legTarget.position;
        // Ensure endPosition is calculated to be relative to the body's position, maintaining the offset
        Vector3 endPosition = transform.position + (maxDistSpheres[legIndex] - transform.position);

        float timeElapsed = 0;

        while (timeElapsed < stepDuration)
        {
            legTarget.position = Vector3.Lerp(startPosition,
                endPosition + new Vector3(0, stepHeight * Mathf.Sin(Mathf.PI * (timeElapsed / stepDuration)), 0),
                timeElapsed / stepDuration);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        legTarget.position = endPosition;
        isMovingLeg = false;
    }


    private void TryMoveLegs()
    {
        if (isMovingLeg) return;

        for (int i = 0; i < legTargets.Length; i++)
        {
            float distance = Vector3.Distance(maxDistSpheres[i], legTargets[i].position);
            if (distance > maxDist)
            {
                StartCoroutine(MoveLegRoutine(i));
                break; // Ensures that only one leg is moved at a time
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (maxDistSpheres == null) return;
        for (int i = 0; i < maxDistSpheres.Length; i++)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(maxDistSpheres[i], maxDist);
        }

        for (int i = 0; i < legTargets.Length; i++)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(legTargets[i].position, 0.1f);
        }

        for (int i = 0; i < poles.Length; i++)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(poles[i].position, 0.1f);
        }
    }
}