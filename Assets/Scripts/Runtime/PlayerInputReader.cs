using UnityEngine;
using UnityEngine.InputSystem;

namespace RealmShift
{
    /// <summary>
    /// Keyboard, mouse, and gamepad. Input System when the backend is active, otherwise legacy axes.
    /// Gameplay code reads the properties; it does not talk to devices itself.
    /// </summary>
    public class PlayerInputReader : MonoBehaviour
    {
        public Vector2 Move { get; private set; }
        public Vector2 LookMouse { get; private set; }
        public Vector2 LookStick { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool SprintHeld { get; private set; }
        public bool UsingInputSystem { get; private set; }

        InputAction move;
        InputAction lookMouse;
        InputAction lookStick;
        InputAction jump;
        InputAction sprint;

        void Awake()
        {
            try
            {
                move = new InputAction(name: "Move", type: InputActionType.Value, expectedControlType: "Vector2");
                move.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/w")
                    .With("Down", "<Keyboard>/s")
                    .With("Left", "<Keyboard>/a")
                    .With("Right", "<Keyboard>/d");
                move.AddCompositeBinding("2DVector")
                    .With("Up", "<Keyboard>/upArrow")
                    .With("Down", "<Keyboard>/downArrow")
                    .With("Left", "<Keyboard>/leftArrow")
                    .With("Right", "<Keyboard>/rightArrow");
                move.AddBinding("<Gamepad>/leftStick");

                lookMouse = new InputAction(name: "LookMouse", type: InputActionType.Value, binding: "<Mouse>/delta", expectedControlType: "Vector2");
                lookStick = new InputAction(name: "LookStick", type: InputActionType.Value, binding: "<Gamepad>/rightStick", expectedControlType: "Vector2");

                jump = new InputAction(name: "Jump", type: InputActionType.Button);
                jump.AddBinding("<Keyboard>/space");
                jump.AddBinding("<Gamepad>/buttonSouth");

                sprint = new InputAction(name: "Sprint", type: InputActionType.Button);
                sprint.AddBinding("<Keyboard>/leftShift");
                sprint.AddBinding("<Gamepad>/leftShoulder");
                sprint.AddBinding("<Gamepad>/leftStickPress");

                move.Enable();
                lookMouse.Enable();
                lookStick.Enable();
                jump.Enable();
                sprint.Enable();
                UsingInputSystem = true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("RealmShift: Input System unavailable, using legacy input. " + e.Message);
                UsingInputSystem = false;
            }
        }

        void OnDestroy()
        {
            move?.Dispose();
            lookMouse?.Dispose();
            lookStick?.Dispose();
            jump?.Dispose();
            sprint?.Dispose();
        }

        void Update()
        {
            if (UsingInputSystem)
                ReadInputSystem();
            else
                ReadLegacy();

            if (Move.sqrMagnitude < 0.03f)
                Move = Vector2.zero;
            else if (Move.sqrMagnitude > 1f)
                Move = Move.normalized;

            if (LookStick.sqrMagnitude < 0.03f)
                LookStick = Vector2.zero;

            if (UsingInputSystem)
            {
                var kb = Keyboard.current;
                if (kb != null && kb.escapeKey.wasPressedThisFrame)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                var mouse = Mouse.current;
                if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
            else
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }

        void ReadInputSystem()
        {
            Move = move.ReadValue<Vector2>();
            LookMouse = lookMouse.ReadValue<Vector2>();
            LookStick = lookStick.ReadValue<Vector2>();
            JumpPressed = jump.WasPressedThisFrame();
            JumpHeld = jump.IsPressed();
            SprintHeld = sprint.IsPressed();
        }

        void ReadLegacy()
        {
            Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            LookMouse = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            float rx = 0f;
            float ry = 0f;
            try
            {
                rx = Input.GetAxis("RightStickX");
                ry = Input.GetAxis("RightStickY");
            }
            catch (System.Exception)
            {
                rx = 0f;
                ry = 0f;
            }
            LookStick = new Vector2(rx, ry);
            JumpPressed = Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.JoystickButton0);
            JumpHeld = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.JoystickButton0);
            SprintHeld = Input.GetKey(KeyCode.LeftShift)
                || Input.GetKey(KeyCode.JoystickButton4)
                || Input.GetKey(KeyCode.JoystickButton8);
        }
    }
}
