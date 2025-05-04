using UnityEngine;
public class GroundDetector : MonoBehaviour
{
    [Header("Detection Settings")]
    [Tooltip("How far down from the origin point to check.")]
    [SerializeField] private float detectionDistance = 1.1f;

    [Tooltip("Which physics layers should be considered 'ground' or 'interactable below'?")]
    [SerializeField] private LayerMask groundLayerMask;

    [Tooltip("Optional offset from the player's transform position to start the raycast.")]
    [SerializeField] private Vector3 originOffset = Vector3.zero;

    [Tooltip("Should the detection ignore trigger colliders? Usually true for ground checks.")]
    [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;


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