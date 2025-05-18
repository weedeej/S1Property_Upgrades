using MelonLoader;
using UnityEngine;

[RegisterTypeInIl2Cpp]
public class GroundDetector : MonoBehaviour
{
    private float detectionDistance = 1.1f;
    
    private LayerMask groundLayerMask;
    
    private Vector3 originOffset = Vector3.zero;
    
    private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;


    private GameObject _objectCurrentlyUnderneath = null;
    public GameObject ObjectCurrentlyUnderneath => _objectCurrentlyUnderneath;

    void Start()
    {
        // Change this layer
        this.groundLayerMask = LayerMask.NameToLayer("Invisible");
    }

    void Update()
    {
        _objectCurrentlyUnderneath = FindObjectUnderneathRaycast();
    }

    public GameObject FindObjectUnderneathRaycast()
    {
        Vector3 rayOrigin = transform.position + originOffset;
        RaycastHit hitInfo;

        bool didHit = Physics.Raycast(
            rayOrigin,
            Vector3.down,
            out hitInfo,
            detectionDistance,
            groundLayerMask,
            triggerInteraction
        );

        if (didHit)
        {
            if (hitInfo.collider.transform == this.transform)
            {
                if (hitInfo.collider.gameObject == this.gameObject)
                {
                    return null;
                }

            }
            return hitInfo.collider.gameObject;
        }

        return null;
    }

    public static GameObject GetObjectUnderneath(Transform originTransform, float distance, LayerMask layers, Vector3 offset = default, QueryTriggerInteraction queryTriggers = QueryTriggerInteraction.Ignore)
    {
        Vector3 rayOrigin = originTransform.position + offset;
        RaycastHit hitInfo;

        if (Physics.Raycast(rayOrigin, Vector3.down, out hitInfo, distance, layers, queryTriggers))
        {
            if (hitInfo.collider.transform == originTransform)
            {
                return null;
            }
            return hitInfo.collider.gameObject;
        }
        return null;
    }
}