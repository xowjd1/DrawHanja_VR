using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace BeyondLimitsStudios
{
    namespace VRInteractables
    {
        public class VRHandIgnoreCollision : MonoBehaviour
        {
            // private List<(Collider, Collider)> ignoreRigidbodies = new List<(Collider, Collider)>();
            //[SerializeField]
            //private List<TempStruct> ignoreRigidbodies = new List<TempStruct>();
            [SerializeField]
            private Dictionary<Rigidbody, List<Collider>> ignoreRigidbodies = new Dictionary<Rigidbody, List<Collider>>();
            private List<Collider> colliders = new List<Collider>();
            private Rigidbody rb;

            private void Awake()
            {
                rb = this.GetComponent<Rigidbody>();

                rb.GetComponentsInChildren(true, colliders);
                for (var i = colliders.Count - 1; i >= 0; i--)
                {
                    if (colliders[i].attachedRigidbody != rb)
                        colliders.RemoveAt(i);
                }
            }

            [System.Serializable]
            public struct TempStruct
            {
                public Collider Item1;
                public Collider Item2;

                public TempStruct(Collider c1, Collider c2)
                {
                    Item1 = c1;
                    Item2 = c2;
                }
            }

            private void FixedUpdate()
            {
                Bounds bounds = GetEntireBoundingBox(colliders);
                // Bounds bounds = new Bounds();

                List<Rigidbody> noOverlapingRigidbodies = new List<Rigidbody>();

                foreach (var rbColPair in ignoreRigidbodies)
                {
                    if (rbColPair.Key == null)
                    {
                        noOverlapingRigidbodies.Add(rbColPair.Key);
                    }
                    else
                    {
                        for (var i = rbColPair.Value.Count - 1; i >= 0; i--)
                        {
                            if (rbColPair.Value[i] == null)
                                rbColPair.Value.RemoveAt(i);
                        }
                    }
                }

                foreach (var rb in noOverlapingRigidbodies)
                {
                    ignoreRigidbodies.Remove(rb);
                }

                noOverlapingRigidbodies.Clear();

                foreach (var rbColPair in ignoreRigidbodies)
                {
                    if (noOverlapingRigidbodies.Contains(rbColPair.Key))
                        continue;

                    bool overlaps = false;

                    foreach (var col in rbColPair.Value)
                    {
                        if (col.bounds.Intersects(bounds))
                        {
                            overlaps = true;
                            break;
                        }
                    }

                    if (!overlaps)
                    {
                        noOverlapingRigidbodies.Add(rbColPair.Key);
                        continue;
                    }

                    overlaps = false;

                    foreach (var handCol in colliders)
                    {
                        foreach (var col in rbColPair.Value)
                        {
                            if (Physics.ComputePenetration(handCol, handCol.transform.position, handCol.transform.rotation, col, col.transform.position, col.transform.rotation, out _, out _))
                            {
                                overlaps = true;
                                break;
                            }
                        }

                        if (overlaps)
                            break;
                    }

                    if (!overlaps)
                    {
                        noOverlapingRigidbodies.Add(rbColPair.Key);
                        continue;
                    }
                }

                foreach (var rb in noOverlapingRigidbodies)
                {
                    foreach (var handCol in colliders)
                    {
                        foreach (var rbCol in ignoreRigidbodies[rb])
                        {
                            Physics.IgnoreCollision(handCol, rbCol, false);
                        }
                    }

                    ignoreRigidbodies.Remove(rb);
                }                
            }

            public void OnColliderTeleported()
            {
                Bounds bounds = GetEntireBoundingBox(colliders);                

                var overlapingColliders = Physics.OverlapBox(bounds.center, bounds.extents / 2, Quaternion.identity);

                foreach (var overlapCol in overlapingColliders)
                {
                    if (overlapCol.isTrigger)
                        continue;

                    if (overlapCol.attachedRigidbody == rb)
                        continue;

                    if (overlapCol.attachedRigidbody == null)
                        continue;

                    if (ignoreRigidbodies.ContainsKey(overlapCol.attachedRigidbody))
                        continue;

                    foreach (var handCol in colliders)
                    {
                        if (Physics.GetIgnoreCollision(handCol, overlapCol))
                            continue;

                        if (Physics.ComputePenetration(handCol, handCol.transform.position, handCol.transform.rotation, overlapCol, overlapCol.transform.position, overlapCol.transform.rotation, out _, out _))
                        {
                            ignoreRigidbodies.Add(overlapCol.attachedRigidbody, GetRigidbodyColliders(overlapCol.attachedRigidbody));
                            break;
                        }
                    }
                }

                foreach (var overlapRb in ignoreRigidbodies)
                {
                    foreach (var handCol in colliders)
                    {
                        foreach (var rbCol in overlapRb.Value)
                        {
                            Physics.IgnoreCollision(handCol, rbCol, true);
                        }
                    }
                }
            }

            private List<Collider> GetRigidbodyColliders(Rigidbody rb)
            {
                List<Collider> result = new List<Collider>();
                rb.GetComponentsInChildren(true, result);

                for (var i = result.Count - 1; i >= 0; i--)
                {
                    if (result[i].attachedRigidbody != rb || result[i].isTrigger)
                        result.RemoveAt(i);
                }

                return result;
            }

            private Bounds GetEntireBoundingBox(List<Collider> cols)
            {
                if (cols.Count == 0)
                    return new Bounds();

                Bounds bounds = new Bounds();

                bounds.center = cols[0].transform.position;
                bounds.Expand(0.2f);
                return bounds;
            }
        }
    }
}