using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using static UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable;

namespace BeyondLimitsStudios
{
    namespace VRInteractables
    {
        public class VRHandAnimatorController : MonoBehaviour
        {
            [SerializeField]
            protected InputActionReference gripInput;
            [SerializeField]
            protected InputActionReference triggerInput;
            [SerializeField]
            protected InputActionReference triggerTouchedInput;
            [SerializeField]
            protected InputActionReference primaryButtonInput;
            [SerializeField]
            protected InputActionReference primaryButtonTouchedInput;
            [SerializeField]
            protected InputActionReference secondaryButtonInput;
            [SerializeField]
            protected InputActionReference secondaryButtonTouchedInput;

            [SerializeField]
            protected Animator animator;

            [FormerlySerializedAs("_grip")]
            protected float grip;
            [FormerlySerializedAs("_trigger")]
            protected float trigger;

            [FormerlySerializedAs("_triggerTouched")]
            protected bool triggerTouched;

            [FormerlySerializedAs("_primary")]
            protected float primary;
            [FormerlySerializedAs("_secondary")]
            protected float secondary;

            [FormerlySerializedAs("_primaryTouched")]
            protected bool primaryTouched;
            [FormerlySerializedAs("_secondaryTouched")]
            protected bool secondaryTouched;

            protected virtual void Awake()
            {
                if(animator == null)
                    animator = GetComponent<Animator>();
            }

            protected virtual void OnEnable()
            {
                gripInput.action.Enable();
                triggerInput.action.Enable();
                triggerTouchedInput.action.Enable();
                primaryButtonInput.action.Enable();
                primaryButtonTouchedInput.action.Enable();
                secondaryButtonInput.action.Enable();
                secondaryButtonTouchedInput.action.Enable();

            }

            protected virtual void Update()
            {
                if (animator == null)
                    return;

                UpdateControllerInputs();
                SetValues();
            }

            protected virtual void UpdateControllerInputs()
            {
                grip = gripInput.action.ReadValue<float>();
                trigger = triggerInput.action.ReadValue<float>();

                triggerTouched = triggerTouchedInput.action.IsPressed();

                primary = primaryButtonInput.action.IsPressed() ? 1f : 0f;
                secondary = secondaryButtonInput.action.IsPressed() ? 1f : 0f;

                primaryTouched = primaryButtonTouchedInput.action.IsPressed();
                secondaryTouched = secondaryButtonTouchedInput.action.IsPressed();
            }

            protected virtual void SetValues()
            {
                animator.SetFloat("Grip", grip);
                animator.SetFloat("Trigger", trigger);
                animator.SetFloat("Thumb", Mathf.Max(primary + secondary));
                animator.SetBool("TriggerTouched", triggerTouched);
                animator.SetBool("ThumbTouched", primaryTouched || secondaryTouched);
            }            
        }
    }
}