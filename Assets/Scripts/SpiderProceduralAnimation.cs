using System.Collections;
using UnityEngine;

public class SpiderProceduralAnimation : MonoBehaviour
{
    [System.Serializable]
    public class LegAnimationSettings
    {
        [Tooltip("Maximum distance a leg must move before triggering a step.")]
        public float stepTriggerDistance = 0.15f;
        
        [Tooltip("Number of frames to smooth the step animation.")]
        public int animationSmoothness = 8;
        
        [Tooltip("Height of the leg lift during the step.")]
        public float stepLiftHeight = 0.15f;

        [Tooltip("Radius of the sphere cast used to detect ground.")]
        public float groundDetectionRadius = 0.125f;

        [Tooltip("How far beneath the spider to cast rays for ground detection.")]
        public float groundDetectionDepth = 1.5f;
        
        [Tooltip("Padding between the front and back legs.")]
        public float legPadding = 0.15f;
    }

    [System.Serializable]
    public class BodyOrientationSettings
    {
        [Tooltip("Whether to adjust the body orientation based on the leg positions.")]
        public bool adjustOrientation = true;
    }

    [Tooltip("Settings for leg animation.")]
    public LegAnimationSettings legSettings = new LegAnimationSettings();
    
    [Tooltip("Settings for body orientation adjustment.")]
    public BodyOrientationSettings bodySettings = new BodyOrientationSettings();
    
    [Tooltip("Leg target transforms.")]
    public Transform[] legTargets;

    private Vector3[] defaultLegPositions, lastLegPositions;
    private Vector3 lastBodyUp, velocity, lastVelocity, lastBodyPos;
    private bool[] legMoving;
    private int numLegs;

    [SerializeField, Tooltip("Show gizmos for debugging.")]
    private bool showGizmos = true;

    void Start()
    {
        InitializeLegPositions();
    }

    void FixedUpdate()
    {
        UpdateVelocity();
        MoveLegsIfNeeded();
        UpdateBodyOrientation();
    }

    private void InitializeLegPositions()
    {
        lastBodyUp = transform.up;
        numLegs = legTargets.Length;
        defaultLegPositions = new Vector3[numLegs];
        lastLegPositions = new Vector3[numLegs];
        legMoving = new bool[numLegs];

        for (int i = 0; i < numLegs; i++)
        {
            Vector3 position = legTargets[i].position;
            defaultLegPositions[i] = position;
            lastLegPositions[i] = position;
            legMoving[i] = false;
        }

        lastBodyPos = transform.position;
    }

    private void UpdateVelocity()
    {
        Vector3 currentVelocity = (transform.position - lastBodyPos);
        velocity = (currentVelocity.magnitude < 0.000025f) ? lastVelocity : (currentVelocity + lastVelocity * (legSettings.animationSmoothness - 1)) / legSettings.animationSmoothness;
        lastVelocity = velocity;
        lastBodyPos = transform.position;
    }

    private void MoveLegsIfNeeded()
    {
        Vector3[] desiredPositions = CalculateDesiredPositions();
        int indexToMove = GetLegIndexToMove(desiredPositions);

        if (indexToMove != -1 && !legMoving[indexToMove])
        {
            Vector3 adjustedTarget = AdjustTargetPosition(indexToMove, desiredPositions[indexToMove]);
            StartCoroutine(PerformStep(indexToMove, adjustedTarget));
        }
    }

    private Vector3[] CalculateDesiredPositions()
    {
        Vector3[] desiredPositions = new Vector3[numLegs];
        Vector3 frontLegsPadding = transform.forward * legSettings.legPadding; // Padding for front legs.
        Vector3 backLegsPadding = -transform.forward * legSettings.legPadding; // Padding for back legs.

        for (int i = 0; i < numLegs; i++)
        {
            Vector3 positionAdjustment;
            if (i == 1 || i == 3) // Front legs
            {
                positionAdjustment = frontLegsPadding;
            }
            else // i == 0 || i == 2, Back legs.
            {
                positionAdjustment = backLegsPadding;
            }

            // Apply the position adjustment to create a gap between front and back legs
            desiredPositions[i] = transform.TransformPoint(defaultLegPositions[i] + positionAdjustment) + velocity;
        }
        return desiredPositions;
    }


    private int GetLegIndexToMove(Vector3[] desiredPositions)
    {
        float maxDistance = legSettings.stepTriggerDistance;
        int indexToMove = -1;
        for (int i = 0; i < numLegs; i++)
        {
            if (!legMoving[i])
            {
                float distance = Vector3.Distance(desiredPositions[i], lastLegPositions[i]);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                    indexToMove = i;
                }
            }
        }
        return indexToMove;
    }

    private Vector3 AdjustTargetPosition(int index, Vector3 targetPosition)
    {
        RaycastHit hit;
        if (Physics.SphereCast(new Ray(targetPosition, -transform.up), legSettings.groundDetectionRadius, out hit, legSettings.groundDetectionDepth))
        {
            return hit.point;
        }
        return targetPosition;
    }

    private IEnumerator PerformStep(int index, Vector3 targetPoint)
    {
        Vector3 startPos = lastLegPositions[index];
        for (int i = 1; i <= legSettings.animationSmoothness; i++)
        {
            float lerpFactor = i / (float)(legSettings.animationSmoothness + 1);
            legTargets[index].position = Vector3.Lerp(startPos, targetPoint, lerpFactor) + transform.up * (Mathf.Sin(lerpFactor * Mathf.PI) * legSettings.stepLiftHeight);
            yield return new WaitForFixedUpdate();
        }
        legTargets[index].position = targetPoint;
        lastLegPositions[index] = targetPoint;
        legMoving[index] = false;
    }

    private void UpdateBodyOrientation()
    {
        if (numLegs > 3 && bodySettings.adjustOrientation)
        {
            Vector3 normal = CalculateSurfaceNormal();
            transform.up = Vector3.Lerp(lastBodyUp, normal, 1f / (float)(legSettings.animationSmoothness + 1));
            transform.rotation = Quaternion.LookRotation(transform.forward, transform.up);
            lastBodyUp = transform.up;
        }
    }

    private Vector3 CalculateSurfaceNormal()
    {
        Vector3 v1 = legTargets[0].position - legTargets[1].position;
        Vector3 v2 = legTargets[2].position - legTargets[3].position;
        return Vector3.Cross(v1, v2).normalized;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        for (int i = 0; i < legTargets.Length; ++i)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(legTargets[i].position, 0.1f);
        }
        
        Vector3 frontLegsPadding = transform.forward * legSettings.legPadding; // Padding for front legs.
        Vector3 backLegsPadding = -transform.forward * legSettings.legPadding; // Padding for back legs.

        for (int i = 0; i < legTargets.Length; ++i)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(legTargets[i].position, 0.1f);
            
            Vector3 positionAdjustment;
            if (i == 1 || i == 3) // Front legs
            {
                positionAdjustment = frontLegsPadding;
            }
            else // i == 0 || i == 2, Back legs.
            {
                positionAdjustment = backLegsPadding;
            }
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.TransformPoint(defaultLegPositions[i]) + (-transform.up * legSettings.groundDetectionDepth) + positionAdjustment, legSettings.stepTriggerDistance);
        }
        
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, velocity);
        
        for (int i = 0; i < legTargets.Length; ++i)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(legTargets[i].position, transform.up);
        }
    }
}
