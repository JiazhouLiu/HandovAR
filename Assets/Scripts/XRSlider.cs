using System;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

namespace UnityEngine.XR.Content.Interaction
{
    public class XRSlider : XRBaseInteractable
    {
        [Serializable]
        public class ValueChangeEvent : UnityEvent<float> { }

        public enum SlideAxis { X, Y, Z }

        [Header("Interaction Settings")]
        [SerializeField] Transform track;
        [SerializeField] Transform handle;
        [SerializeField] SlideAxis slideAxis = SlideAxis.X;
        [SerializeField] float heightOffset = 0.05f;

        [Header("Slider Config")]
        [SerializeField, Range(0f, 1f)] float value = 0.0f;
        [SerializeField] int steps = 0; // 0 = continuous
        
        public ValueChangeEvent onValueChange = new ValueChangeEvent();

        // State
        IXRSelectInteractor currentInteractor;
        Vector3 startPos;
        Vector3 endPos;

        public float Value
        {
            get => value;
            set => UpdateValue(value);
        }

        // Snap the ray to the handle mesh instead of the track center
        public override Transform GetAttachTransform(IXRInteractor interactor)
        {
            return handle != null ? handle : base.GetAttachTransform(interactor);
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            selectEntered.AddListener(StartGrab);
            selectExited.AddListener(EndGrab);
            
            RecalculateBounds();
            UpdateHandleVisuals();
        }

        protected override void OnDisable()
        {
            selectEntered.RemoveListener(StartGrab);
            selectExited.RemoveListener(EndGrab);
            base.OnDisable();
        }

        void RecalculateBounds()
        {
            if (track == null) return;

            Vector3 dir = Vector3.zero;
            float length = 0f;

            switch (slideAxis)
            {
                case SlideAxis.X: dir = track.right; length = track.localScale.x; break;
                case SlideAxis.Y: dir = track.up;    length = track.localScale.y; break;
                case SlideAxis.Z: dir = track.forward; length = track.localScale.z; break;
            }

            // Offset calculation to keep handle on the rail surface
            float halfLen = (length * 0.5f) - 0.02f; // slight padding
            Vector3 center = track.position;
            Vector3 up = track.up;

            startPos = center - (dir * halfLen) + (up * heightOffset);
            endPos   = center + (dir * halfLen) + (up * heightOffset);
        }

        void StartGrab(SelectEnterEventArgs args)
        {
            currentInteractor = args.interactorObject;
            RecalculateBounds(); // specific fix for if the object scaled at runtime
        }

        void EndGrab(SelectExitEventArgs args)
        {
            currentInteractor = null;
            UpdateValue(value); // Snap to step on release
        }

        public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
        {
            base.ProcessInteractable(updatePhase);

            if (updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic && isSelected && currentInteractor != null)
            {
                CalculateValueFromHand();
            }
        }

        void CalculateValueFromHand()
        {
            Vector3 handPos = currentInteractor.GetAttachTransform(this).position;
            
            Vector3 railVec = endPos - startPos;
            Vector3 handVec = handPos - startPos;

            // Project hand position onto the rail vector
            float t = Vector3.Dot(handVec, railVec) / railVec.sqrMagnitude;
            UpdateValue(t);
        }

        void UpdateValue(float rawValue)
        {
            float newVal = Mathf.Clamp01(rawValue);

            if (steps > 1)
            {
                float stepSize = 1f / (steps - 1);
                newVal = Mathf.Round(newVal / stepSize) * stepSize;
            }

            // Only fire event if value actually changed
            if (Mathf.Abs(value - newVal) > Mathf.Epsilon)
            {
                value = newVal;
                onValueChange.Invoke(value);
            }

            UpdateHandleVisuals();
        }

        void UpdateHandleVisuals()
        {
            if (handle == null || track == null) return;

            if (startPos == Vector3.zero) RecalculateBounds();

            handle.position = Vector3.Lerp(startPos, endPos, value);
            handle.rotation = track.rotation;
        }

        void OnDrawGizmos()
        {
            if (track != null)
            {
                RecalculateBounds();
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(startPos, endPos);
                Gizmos.DrawSphere(startPos, 0.01f);
                Gizmos.DrawSphere(endPos, 0.01f);
            }
        }
    }
}