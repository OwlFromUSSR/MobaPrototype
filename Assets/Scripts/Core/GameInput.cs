using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Moba
{
    /// <summary>
    /// Единая точка ввода. Работает и с новым Input System, и со старым Input Manager,
    /// в зависимости от настройки Player Settings → Active Input Handling.
    /// </summary>
    public static class GameInput
    {
        public static Vector2 PointerPosition
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
                return Input.mousePosition;
#endif
            }
        }

        public static bool RightClickDown
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
#else
                return Input.GetMouseButtonDown(1);
#endif
            }
        }

        public static bool AbilityDown(int index)
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return false;
            switch (index)
            {
                case 0: return k.qKey.wasPressedThisFrame;
                case 1: return k.wKey.wasPressedThisFrame;
                case 2: return k.eKey.wasPressedThisFrame;
                default: return false;
            }
#else
            switch (index)
            {
                case 0: return Input.GetKeyDown(KeyCode.Q);
                case 1: return Input.GetKeyDown(KeyCode.W);
                case 2: return Input.GetKeyDown(KeyCode.E);
                default: return false;
            }
#endif
        }

        public static bool PausePressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame;
#else
                return Input.GetKeyDown(KeyCode.P);
#endif
            }
        }

        public static bool RestartPressed
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
                return Input.GetKeyDown(KeyCode.R);
#endif
            }
        }
    }
}
