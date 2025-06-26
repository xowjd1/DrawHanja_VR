using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace BeyondLimitsStudios
{
    namespace VRInteractables
    {
        public class VRHandCollisionController : MonoBehaviour
        {
            [SerializeField]
            private CharacterController characterController;

            [SerializeField]
            protected TrackingMode trackingMode;

            [SerializeField]
            protected bool nullifyRigidbodiesParents = true;

            [SerializeField]
            protected float maxDistance = 0.2f;

            [SerializeField]
            protected CollisionDetectionMode collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            [SerializeField]
            protected List<HandRigidbodyInfo> handRigidbodyInfo = new List<HandRigidbodyInfo>();

            [SerializeField]
            protected float velocityDamping = 1f;
            [SerializeField]
            protected float velocityScale = 1f;
            [SerializeField]
            protected float angularVelocityDamping = 1f;
            [SerializeField]
            protected float angularVelocityScale = 1f;

            [SerializeField]
            protected List<Collider> handColliders = new List<Collider>();

            protected List<UnityEngine.XR.Interaction.Toolkit.Interactables.IXRInteractable> ignoringInteractables = new List<UnityEngine.XR.Interaction.Toolkit.Interactables.IXRInteractable>();
            protected List<UnityEngine.XR.Interaction.Toolkit.Interactables.IXRInteractable> stopIgnoringInteractables = new List<UnityEngine.XR.Interaction.Toolkit.Interactables.IXRInteractable>();

            protected virtual void Awake()
            {
                if (handColliders == null || handColliders.Count == 0)
                    GetColliders();

                if (characterController == null)
                    characterController = GetComponentInParent<CharacterController>();

                foreach (var col in handColliders)
                {
                    Physics.IgnoreCollision(characterController, col, true);
                }

                GetRigidbodiesAndTargets();

                switch (trackingMode)
                {
                    case TrackingMode.None:
                        break;
                    case TrackingMode.Instantaneous:
                        SetupKinematicRigidbodies();
                        break;
                    case TrackingMode.Velocity:
                        SetupVelocityRigidbodies();
                        break;
                    default:
                        break;
                }
            }

            protected virtual void Update()
            {
                switch (trackingMode)
                {
                    case TrackingMode.None:
                        break;
                    case TrackingMode.Instantaneous:
                        UpdateHandKinematic();
                        break;
                    case TrackingMode.Velocity:
                        break;
                    default:
                        break;
                }
            }

            protected virtual void FixedUpdate()
            {
                switch (trackingMode)
                {
                    case TrackingMode.None:
                        break;
                    case TrackingMode.Instantaneous:
                        break;
                    case TrackingMode.Velocity:
                        UpdateHandVelocity(Time.fixedDeltaTime);
                        break;
                    default:
                        break;
                }

                HandleIgnoredInteractables();
            }

            protected virtual void GetColliders()
            {
                handColliders = GetComponentsInChildren<Collider>().ToList();
            }

            protected virtual void GetRigidbodiesAndTargets()
            {
                if (handRigidbodyInfo == null || handRigidbodyInfo.Count == 0)
                {
                    foreach (var rb in GetComponentsInChildren<Rigidbody>())
                    {
                        handRigidbodyInfo.Add(new HandRigidbodyInfo(rb, rb.transform.parent, rb.GetComponent<VRHandIgnoreCollision>()));
                    }
                }

                if (handRigidbodyInfo == null || handRigidbodyInfo.Count == 0)
                    return;

                var cols = GetComponentsInChildren<Collider>();
                CharacterController characterController = GetComponentInParent<CharacterController>();

                foreach (var col1 in cols)
                {
                    if (col1.isTrigger)
                        continue;

                    foreach (var col2 in cols)
                    {
                        if (col2.isTrigger)
                            continue;

                        Physics.IgnoreCollision(col1, col2, true);
                    }

                    if(characterController != null)
                        Physics.IgnoreCollision(col1, characterController, true);
                }

                if (nullifyRigidbodiesParents)
                {
                    Transform rbsHolder = new GameObject("Hand Rigidbodies Holder").transform;
                    foreach (var rigidbodyTargetPair in handRigidbodyInfo)
                    {
                        rigidbodyTargetPair.colliderRigidbody.transform.SetParent(rbsHolder);
                    }
                }
            }

            protected virtual void SetupVelocityRigidbodies()
            {
                foreach (var colliderTargetPair in handRigidbodyInfo)
                {
                    if (colliderTargetPair.colliderRigidbody == null)
                        continue;

                    colliderTargetPair.colliderRigidbody.collisionDetectionMode = collisionDetectionMode;
                    colliderTargetPair.colliderRigidbody.useGravity = false;
                    colliderTargetPair.colliderRigidbody.isKinematic = false;
                    colliderTargetPair.colliderRigidbody.linearDamping = 0f;
                    colliderTargetPair.colliderRigidbody.angularDamping = 0f;
                }
            }

            protected virtual void SetupKinematicRigidbodies()
            {
                foreach (var colliderTargetPair in handRigidbodyInfo)
                {
                    if (colliderTargetPair.colliderRigidbody == null)
                        continue;

                    colliderTargetPair.colliderRigidbody.collisionDetectionMode = collisionDetectionMode;
                    colliderTargetPair.colliderRigidbody.useGravity = false;
                    colliderTargetPair.colliderRigidbody.isKinematic = true;
                    colliderTargetPair.colliderRigidbody.linearDamping = 0f;
                    colliderTargetPair.colliderRigidbody.angularDamping = 0f;
                }
            }

            protected virtual void UpdateHandVelocity(float deltaTime)
            {
                foreach (var colliderTargetPair in handRigidbodyInfo)
                {
                    if (colliderTargetPair.targetTransform == null || colliderTargetPair.colliderRigidbody == null)
                        continue;

                    if (Vector3.Distance(colliderTargetPair.colliderRigidbody.transform.position, colliderTargetPair.targetTransform.position) > maxDistance)
                    {
                        colliderTargetPair.colliderRigidbody.transform.position = colliderTargetPair.targetTransform.position;
                        colliderTargetPair.colliderRigidbody.transform.rotation = colliderTargetPair.targetTransform.rotation;

                        colliderTargetPair.colliderRigidbody.linearVelocity = Vector3.zero;
                        colliderTargetPair.colliderRigidbody.angularVelocity = Vector3.zero;

                        if(colliderTargetPair.ignoreCollision != null)
                            colliderTargetPair.ignoreCollision.OnColliderTeleported();

                        continue;
                    }

                    // Do velocity tracking
                    // Scale initialized velocity by prediction factor
                    colliderTargetPair.colliderRigidbody.linearVelocity *= (1f - velocityDamping);
                    var positionDelta = colliderTargetPair.targetTransform.position - colliderTargetPair.colliderRigidbody.transform.position;
                    var velocity = positionDelta / deltaTime;
                    colliderTargetPair.colliderRigidbody.linearVelocity += (velocity * velocityScale);

                    // Do angular velocity tracking
                    // Scale initialized velocity by prediction factor
                    colliderTargetPair.colliderRigidbody.angularVelocity *= (1f - angularVelocityDamping);
                    var rotationDelta = colliderTargetPair.targetTransform.rotation * Quaternion.Inverse(colliderTargetPair.colliderRigidbody.transform.rotation);
                    rotationDelta.ToAngleAxis(out var angleInDegrees, out var rotationAxis);
                    if (angleInDegrees > 180f)
                        angleInDegrees -= 360f;

                    if (Mathf.Abs(angleInDegrees) > Mathf.Epsilon)
                    {
                        var angularVelocity = (rotationAxis * (angleInDegrees * Mathf.Deg2Rad)) / deltaTime;
                        colliderTargetPair.colliderRigidbody.angularVelocity += (angularVelocity * angularVelocityScale);
                    }
                }
            }

            protected virtual void UpdateHandKinematic()
            {
                foreach (var colliderTargetPair in handRigidbodyInfo)
                {
                    if (colliderTargetPair.targetTransform == null || colliderTargetPair.colliderRigidbody == null)
                        continue;

                    colliderTargetPair.colliderRigidbody.transform.position = colliderTargetPair.targetTransform.position;
                    colliderTargetPair.colliderRigidbody.transform.rotation = colliderTargetPair.targetTransform.rotation;
                }
            }

            protected virtual void HandleIgnoredInteractables()
            {
                if (handColliders == null || handColliders.Count == 0)
                    return;

                Bounds handBounds = handColliders[0].bounds;

                foreach (var col in handColliders)
                {
                    handBounds.Encapsulate(col.bounds);
                }

                for (var i = stopIgnoringInteractables.Count - 1; i >= 0; i--)
                {
                    if (stopIgnoringInteractables[i] == null || stopIgnoringInteractables[i].colliders == null || stopIgnoringInteractables[i].colliders.Count == 0)
                    {
                        stopIgnoringInteractables.RemoveAt(i);
                        continue;
                    }

                    Bounds interactableBounds = stopIgnoringInteractables[i].colliders[0].bounds;

                    foreach (var col in stopIgnoringInteractables[i].colliders)
                    {
                        interactableBounds.Encapsulate(col.bounds);
                    }

                    if (!handBounds.Intersects(interactableBounds))
                    {
                        EnableGrabInteractableCollision(stopIgnoringInteractables[i]);
                        stopIgnoringInteractables.RemoveAt(i);
                        continue;
                    }

                    bool overlaps = false;

                    foreach (var col1 in handColliders)
                    {
                        foreach (var col2 in stopIgnoringInteractables[i].colliders)
                        {
                            if (Physics.ComputePenetration(col1, col1.transform.position, col1.transform.rotation, col2, col2.transform.position, col2.transform.rotation, out _, out _))
                            {
                                overlaps = true;
                                break;
                            }
                        }

                        if (overlaps)
                        {
                            break;
                        }
                    }

                    if (!overlaps)
                    {
                        EnableGrabInteractableCollision(stopIgnoringInteractables[i]);
                        stopIgnoringInteractables.RemoveAt(i);
                    }
                }
            }

            public virtual void OnInteractableSelectEntered(SelectEnterEventArgs args)
            {
                if(!ignoringInteractables.Contains(args.interactableObject))
                    ignoringInteractables.Add(args.interactableObject);

                if (stopIgnoringInteractables.Contains(args.interactableObject))
                    stopIgnoringInteractables.Remove(args.interactableObject);

                DisableGrabInteractableCollision(args.interactableObject);
            }

            public virtual void OnInteractableSelectExited(SelectExitEventArgs args)
            {
                if (!stopIgnoringInteractables.Contains(args.interactableObject))
                    stopIgnoringInteractables.Add(args.interactableObject);

                if (ignoringInteractables.Contains(args.interactableObject))
                    ignoringInteractables.Remove(args.interactableObject);

                // EnableGrabInteractableCollision(args.interactableObject);
            }

            protected virtual void DisableGrabInteractableCollision(UnityEngine.XR.Interaction.Toolkit.Interactables.IXRInteractable interactable)
            {
                if (handColliders == null || handColliders.Count == 0)
                    return;

                foreach (var col1 in interactable.colliders)
                {
                    foreach (var col2 in handColliders)
                    {
                        Physics.IgnoreCollision(col1, col2, true);
                    }
                }
            }

            protected virtual void EnableGrabInteractableCollision(UnityEngine.XR.Interaction.Toolkit.Interactables.IXRInteractable interactable)
            {
                if (handColliders == null || handColliders.Count == 0)
                    return;

                foreach (var col1 in interactable.colliders)
                {
                    foreach (var col2 in handColliders)
                    {
                        Physics.IgnoreCollision(col1, col2, false);
                    }
                }
            }

            [System.Serializable]
            protected struct HandRigidbodyInfo
            {
                public Rigidbody colliderRigidbody;
                public Transform targetTransform;
                public VRHandIgnoreCollision ignoreCollision;

                public HandRigidbodyInfo(Rigidbody rb, Transform target, VRHandIgnoreCollision ic)
                {
                    colliderRigidbody = rb;
                    targetTransform = target;
                    ignoreCollision = ic;
                }
            }

            protected enum TrackingMode
            {
                None,
                Instantaneous,
                Velocity
            }
        }
    }
}