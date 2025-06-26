using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BeyondLimitsStudios
{
    namespace VRInteractables
    {
        public class VRFollowCamera : MonoBehaviour
        {
            [SerializeField]
            private Transform target;

            [SerializeField]
            private float lerpPositionFactor = 5f;
            [SerializeField]
            private float lerpRotationFactor = 5f;

            private void Update()
            {
                this.transform.position = Vector3.Lerp(this.transform.position, target.transform.position, lerpPositionFactor * Time.deltaTime);
                this.transform.rotation = Quaternion.Lerp(this.transform.rotation, target.transform.rotation, lerpRotationFactor * Time.deltaTime);

                if (Input.GetKeyDown(KeyCode.Space))
                    this.GetComponent<Camera>().enabled = !this.GetComponent<Camera>().enabled;
            }
        }
    }
}