using UnityEngine;
using UnityEngine.UIElements;

public class Movement : MonoBehaviour
{
    public Transform[] legPoints;
    [SerializeField] private float maxDist = 0.3f;
    [SerializeField] public float speed = 2.0f;
    [SerializeField] public float stepDuration = 0.5f;
    [SerializeField] private float stepHeight = 0.2f;
    [SerializeField] private LayerMask raycastLayerMask;

    private Vector3[] nextStepPositions;
    private float[] stepStartTime;
    private bool isMovingSetA = true;
    private float someMinimumHeightAboveGround = 2.5f;
    [SerializeField] private float RayCastSize = 2.1f;
    

    void Start()
    {
        nextStepPositions = new Vector3[legPoints.Length];
        stepStartTime = new float[legPoints.Length];
        for (int i = 0; i < legPoints.Length; i++)
        {
            nextStepPositions[i] = legPoints[i].position;
            stepStartTime[i] = Time.time;
        }
    }

    void Update()
    { 
        Vector3 movement = Vector3.zero;
        
        if (Input.GetKey(KeyCode.W))
        {
            movement += transform.right;
        }
        if (Input.GetKey(KeyCode.S))
        {
            movement -= transform.right;
        }


        if (Input.GetKey(KeyCode.A))
        {
            movement += transform.forward;
        }
        if (Input.GetKey(KeyCode.D))
        {
            movement -= transform.forward;
        }
        
        movement = movement.normalized * (speed * Time.deltaTime);
        
        transform.Translate(movement);

        MoveLegs(movement);
    }

    void MoveLegs(Vector3 movement)
    {
        CheckAndSwitchMovingSets();
        
        for (int i = 0; i < legPoints.Length; i++)
        {
            nextStepPositions[i] += transform.TransformDirection(movement);
            
            Vector3 raycastStartPosition = nextStepPositions[i] + (Vector3.up * RayCastSize);
            RaycastHit hit;
            if (Physics.Raycast(raycastStartPosition, Vector3.down, out hit, Mathf.Infinity, raycastLayerMask))
            {
                Debug.DrawRay(raycastStartPosition, Vector3.down * hit.distance, Color.red);
                nextStepPositions[i] = hit.point;
            }
            else
            {
                nextStepPositions[i] = raycastStartPosition + (Vector3.down * RayCastSize);
            }
        }

        for (int i = 0; i < legPoints.Length; i++)
        {
            // Determine if the current leg is part of the set that should be moving
            bool isCurrentLegMovingSet = (i % 2 == 0) ? isMovingSetA : !isMovingSetA;

            if (movement != Vector3.zero)
            {
                if (isCurrentLegMovingSet)
                {
                    float progress = (Time.time - stepStartTime[i]) / stepDuration;
                    if (progress < 1.0f)
                    {
                        // Calculate vertical lifting for stepping effect
                        Vector3 stepPositionWithHeight = nextStepPositions[i] + Vector3.up * (stepHeight * Mathf.Sin(progress * Mathf.PI));
                        legPoints[i].position = Vector3.Lerp(legPoints[i].position, stepPositionWithHeight, progress);
                    }
                    else
                    { 
                        stepStartTime[i] = Time.time; // Reset for the next step
                    }
                }
            }
        }

        AdjustBodyPositionAndRotation();
    }
    
    void AdjustBodyPositionAndRotation()
    {
        Vector3 averageLegPosition = Vector3.zero;
        Vector3 averageUp = Vector3.zero;

        for (int i = 0; i < legPoints.Length; i++)
        {
            averageLegPosition += legPoints[i].position;
            
            // Calculate the average 'up' direction based on the normal of the ground below each leg
            Vector3 raycastStartPosition = legPoints[i].position + (Vector3.up * 0.5f);
            RaycastHit hit;
            if (Physics.Raycast(raycastStartPosition, Vector3.down, out hit, Mathf.Infinity))
            {
                // Debug.DrawRay(raycastStartPosition, Vector3.down * hit.distance, Color.red);
                averageUp += hit.normal;
            }
            else
            {
                // Debug.DrawRay(raycastStartPosition, Vector3.down * 1000, Color.red);
                averageUp += Vector3.up; // Fallback to world up if no hit
            }
        }
        // Calculate the average position of the legs
        averageLegPosition /= legPoints.Length;
        averageUp /= legPoints.Length;

        // Adjust the body's height based on the average position of the legs plus an offset
        float newHeight = averageLegPosition.y + someMinimumHeightAboveGround;
        transform.position = new Vector3(transform.position.x, newHeight, transform.position.z);

        // adjust the body's rotation based on the averaged 'up' direction
        Quaternion targetRotation = Quaternion.FromToRotation(transform.up, averageUp) * transform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5.0f);
    }
    
    void CheckAndSwitchMovingSets()
    {
        bool allLegsInSetHaveFinishedMoving = true;
        for (int i = 0; i < nextStepPositions.Length; i++)
        {
            bool isCurrentLegMovingSet = (i % 2 == 0) ? isMovingSetA : !isMovingSetA;
            if (isCurrentLegMovingSet)
            {
                float progress = (Time.time - stepStartTime[i]) / stepDuration;
                if (progress < 1.0f)
                {
                    allLegsInSetHaveFinishedMoving = false;
                    break;
                }
            }
        }

        if (allLegsInSetHaveFinishedMoving)
        {
            isMovingSetA = !isMovingSetA;
        }
    }
    
    void OnDrawGizmos()
    {
        if(nextStepPositions != null)
        {
            for (int i = 0; i < nextStepPositions.Length; i++)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(nextStepPositions[i], 0.1f);
            }
        }
    }
}