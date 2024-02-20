using UnityEngine;
using System.Collections.Generic;

public class BoneManager : MonoBehaviour
{
    private List<GameObject> bones = new List<GameObject>();
    private List<Quaternion> initialRotations = new List<Quaternion>();
    
    void Start()
    {
        GameObject[] legs = GameObject.FindGameObjectsWithTag("Leg");
        
        foreach (var leg in legs)
        {
            
            Transform[] boneTransforms = leg.GetComponentsInChildren<Transform>(true);
            foreach (var boneTransform in boneTransforms)
            {
                if (boneTransform.name.Contains("Bone"))
                {
                    bones.Add(boneTransform.gameObject);
                    initialRotations.Add(boneTransform.localRotation);
                }
            }
        }
    }
    
    public void ResetBoneRotations()
    {
        for (int i = 0; i < bones.Count; i++)
        {
            bones[i].transform.localRotation = initialRotations[i];
        }
    }
}