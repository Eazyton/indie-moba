using UnityEngine;
using UnityEngine.InputSystem;
using IndieMoba.Core;

namespace IndieMoba.Input
{
    public sealed class KeyboardMouseInputSource : MonoBehaviour, IMovementInputSource
    {
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference moveToPointAction;
        [SerializeField] private InputActionReference pointerPositionAction;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private float directionDeadZone = 0.15f;

        private void OnEnable()
        {
            SetEnabled(moveAction, true);
            SetEnabled(moveToPointAction, true);
            SetEnabled(pointerPositionAction, true);
        }

        private void OnDisable()
        {
            SetEnabled(moveAction, false);
            SetEnabled(moveToPointAction, false);
            SetEnabled(pointerPositionAction, false);
        }

        private static void SetEnabled(InputActionReference reference, bool value)
        {
            if (reference == null || reference.action == null)
            {
                return;
            }
            if (value)
            {
                reference.action.Enable();
            }
            else
            {
                reference.action.Disable();
            }
        }

        public MovementIntent ReadIntent()
        {
            if (moveAction != null && moveAction.action != null)
            {
                Vector2 move = moveAction.action.ReadValue<Vector2>();
                if (move.magnitude > directionDeadZone)
                {
                    return MovementIntent.FromDirection(move);
                }
            }
            if (moveToPointAction != null && moveToPointAction.action != null && moveToPointAction.action.IsPressed())
            {
                Camera camera = worldCamera != null ? worldCamera : Camera.main;
                if (camera != null && pointerPositionAction != null && pointerPositionAction.action != null)
                {
                    Vector2 screen = pointerPositionAction.action.ReadValue<Vector2>();
                    Vector3 world = camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -camera.transform.position.z));
                    return MovementIntent.ToDestination(world);
                }
            }
            return MovementIntent.None;
        }
    }
}
