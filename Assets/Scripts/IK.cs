using UnityEngine;

public class IK : MonoBehaviour
{

    [SerializeField] public int chainLength = 3;

    [SerializeField] public Transform target;
    [SerializeField] public Transform pole;

    [SerializeField] public int iterations = 10;

    [SerializeField] public float delta = 0.001f;

    [SerializeField] public float SnapBackStrength = 0.1f;
    
    [SerializeField] public bool showGizmos = true;
    
    private float[] bonesLength;
    private float completeLength;
    private Transform[] bones;
    private Vector3[] positions;

    
    private Vector3[] startDirectionSucc;
    private Quaternion[] startRotationBone;
    private Quaternion startRotationTarget;
    private Quaternion startRotationRoot;
    
    private void Awake() {
        init();
    }

    void init() {
        bones = new Transform[chainLength + 1];
        positions = new Vector3[chainLength + 1];
        bonesLength = new float[chainLength];
        startDirectionSucc = new Vector3[chainLength + 1];
        startRotationBone = new Quaternion[chainLength + 1];
        
        completeLength = 0;
        
        startRotationTarget = target.rotation;
        
        //init data
        var current = this.transform;
        for (int i = bones.Length - 1; i >= 0; i--) {
            bones[i] = current;
            startRotationBone[i] = current.rotation;
            
            if (i == bones.Length - 1) {
                //leaf
                //positions[i] = target.position;
                startDirectionSucc[i] = target.position - current.position;
            }
            else {
                //mid bone
                //positions[i] = bones[i + 1].position;
                startDirectionSucc[i] = bones[i + 1].position - current.position;
                bonesLength[i] = (bones[i + 1].position - current.position).magnitude;
                completeLength += bonesLength[i];
            }

            current = current.parent;
        }
    }

    private void LateUpdate() {
        ResolveIK();
    }

    private void ResolveIK() {
        if (target == null) {
            return;
        }
        
        if(chainLength != bones.Length) {
            init();
        }

        //get positions
        for (int i = 0; i < bones.Length; i++) {
            positions[i] = bones[i].position;
        }
        
        var rootRot = (bones[0].parent != null) ? bones[0].parent.rotation : Quaternion.identity;
        var rootRotDiff = rootRot * Quaternion.Inverse(startRotationRoot);
        
        //if target is out of reach
        if((target.position - bones[0].position).sqrMagnitude >= completeLength * completeLength) {
            //stretch
            var direction = (target.position - positions[0]).normalized;
            for (int i = 1; i < positions.Length; i++) {
                positions[i] = positions[i - 1] + direction * bonesLength[i - 1];
            }
            
        }
        else {
            for (int i = 0; i < positions.Length - 1; i++) {
                positions[i + 1] = Vector3.Lerp(positions[i + 1], positions[i] + startDirectionSucc[i], SnapBackStrength);
            }
            
            for (int iteration = 0; iteration < iterations; iteration++) {
                //back
                positions[positions.Length - 1] = target.position;
                for (int i = positions.Length - 2; i > 0; i--) {
                    positions[i] = positions[i + 1] + (positions[i] - positions[i + 1]).normalized * bonesLength[i];
                }
                
                //forward
                positions[0] = bones[0].position;
                for (int i = 1; i < positions.Length; i++) {
                    positions[i] = positions[i - 1] + (positions[i] - positions[i - 1]).normalized * bonesLength[i - 1];
                }
                
                //close enough
                if ((positions[positions.Length - 1] - target.position).sqrMagnitude < delta * delta) {
                    break;
                }
            }
        }
        
        //pole
        if (pole != null) {
            for (int i = 1; i < positions.Length - 1; i++) {
                var plane = new Plane(positions[i + 1] - positions[i - 1], positions[i - 1]);
                var projectedPole = plane.ClosestPointOnPlane(pole.position);
                var projectedBone = plane.ClosestPointOnPlane(positions[i]);
                var angle = Vector3.SignedAngle(projectedBone - positions[i - 1], projectedPole - positions[i - 1], plane.normal);
                positions[i] = Quaternion.AngleAxis(angle, plane.normal) * (positions[i] - positions[i - 1]) + positions[i - 1];
            }
        }
        
        
        
        //set position & rotation
        for (int i = 0; i < positions.Length; i++) {
            if (i == positions.Length - 1) {
                bones[i].rotation = target.rotation * Quaternion.Inverse(startRotationTarget) * startRotationBone[i];
            }
            else {
                bones[i].rotation = Quaternion.FromToRotation(startDirectionSucc[i], positions[i + 1] - positions[i]) * startRotationBone[i];
            }
            bones[i].position = positions[i];
        }
    }

    private void OnDrawGizmos() {
        var current = this.transform;

        if (showGizmos)
        {
            for (int i = 0; i < chainLength && current != null && current.parent != null; i++) {
                var scale = Vector3.Distance(current.position, current.parent.position) * 0.1f;
                Gizmos.matrix = Matrix4x4.TRS(current.position, Quaternion.FromToRotation(Vector3.up, current.parent.position - current.position), new Vector3(scale, Vector3.Distance(current.parent.position, current.position), scale));
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(Vector3.up * 0.5f, new Vector3(1f, 1f, 1f));
                current = current.parent;
            }
        }
    }
}
