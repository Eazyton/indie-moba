using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using IndieMoba.Core;

namespace IndieMoba.Input
{
    [DefaultExecutionOrder(-50)]
    public sealed class KeyboardMouseInputSource : MonoBehaviour, IMovementInputSource, IAbilityInputSource
    {
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference moveToPointAction;
        [SerializeField] private InputActionReference pointerPositionAction;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private float directionDeadZone = 0.15f;
        [SerializeField] private InputActionReference basicAttackAction;
        [SerializeField] private InputActionReference ability1Action;
        [SerializeField] private InputActionReference ability2Action;
        [SerializeField] private InputActionReference ability3Action;
        [SerializeField] private InputActionReference ultimateAction;

        private const int MaxQueuedCommands = 16;
        private readonly List<AbilityCommand> queuedCommands = new List<AbilityCommand>();

        private void OnEnable()
        {
            SetEnabled(moveAction, true);
            SetEnabled(moveToPointAction, true);
            SetEnabled(pointerPositionAction, true);
            SetEnabled(basicAttackAction, true);
            SetEnabled(ability1Action, true);
            SetEnabled(ability2Action, true);
            SetEnabled(ability3Action, true);
            SetEnabled(ultimateAction, true);
        }

        private void OnDisable()
        {
            SetEnabled(moveAction, false);
            SetEnabled(moveToPointAction, false);
            SetEnabled(pointerPositionAction, false);
            SetEnabled(basicAttackAction, false);
            SetEnabled(ability1Action, false);
            SetEnabled(ability2Action, false);
            SetEnabled(ability3Action, false);
            SetEnabled(ultimateAction, false);
            queuedCommands.Clear();
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
                if (TryGetPointerWorld(out Vector2 world))
                {
                    return MovementIntent.ToDestination(world);
                }
            }
            return MovementIntent.None;
        }

        public void DrainCommands(List<AbilityCommand> into)
        {
            into.AddRange(queuedCommands);
            queuedCommands.Clear();
        }

        private void Update()
        {
            bool hasAim = TryGetPointerWorld(out Vector2 aim);
            if (IsPressed(basicAttackAction))
            {
                Enqueue(AbilitySlot.BasicAttack, aim, hasAim);
            }
            if (WasPressed(ability1Action)) Enqueue(AbilitySlot.Ability1, aim, hasAim);
            if (WasPressed(ability2Action)) Enqueue(AbilitySlot.Ability2, aim, hasAim);
            if (WasPressed(ability3Action)) Enqueue(AbilitySlot.Ability3, aim, hasAim);
            if (WasPressed(ultimateAction)) Enqueue(AbilitySlot.Ultimate, aim, hasAim);
        }

        private void Enqueue(AbilitySlot slot, Vector2 aim, bool hasAim)
        {
            AbilityCommand command = new AbilityCommand(slot, aim, hasAim);
            for (int i = 0; i < queuedCommands.Count; i++)
            {
                if (queuedCommands[i].Slot == slot)
                {
                    queuedCommands[i] = command;
                    return;
                }
            }
            if (queuedCommands.Count < MaxQueuedCommands)
            {
                queuedCommands.Add(command);
            }
        }

        private bool TryGetPointerWorld(out Vector2 world)
        {
            world = Vector2.zero;
            Camera camera = worldCamera != null ? worldCamera : Camera.main;
            if (camera == null || pointerPositionAction == null || pointerPositionAction.action == null)
            {
                return false;
            }
            Vector2 screen = pointerPositionAction.action.ReadValue<Vector2>();
            world = camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -camera.transform.position.z));
            return true;
        }

        private static bool IsPressed(InputActionReference reference)
        {
            return reference != null && reference.action != null && reference.action.IsPressed();
        }

        private static bool WasPressed(InputActionReference reference)
        {
            return reference != null && reference.action != null && reference.action.WasPressedThisFrame();
        }
    }
}
