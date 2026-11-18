using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Moba
{
    public static class GameInput
    {
        // Заполняется из MatchHud.OnGUI: IMGUI знает реальную позицию курсора в Game view.
        static Vector2 guiPointer;
        static bool guiPointerValid;
        static bool guiRightClickPending;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            guiPointerValid = false;
            guiRightClickPending = false;
        }

        public static void FeedGuiEvent(Event e)
        {
            guiPointer = new Vector2(e.mousePosition.x, Screen.height - e.mousePosition.y);
            guiPointerValid = true;
            if (e.type == EventType.MouseDown && e.button == 1) guiRightClickPending = true;
        }

        public static Vector2 PointerPosition
        {
            get
            {
                if (guiPointerValid) return guiPointer;
#if ENABLE_INPUT_SYSTEM
                if (Mouse.current != null) return Mouse.current.position.ReadValue();
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                return Input.mousePosition;
#else
                return Vector2.zero;
#endif
            }
        }

        public static bool RightClickDown
        {
            get
            {
                bool down = guiRightClickPending;
                guiRightClickPending = false;
#if ENABLE_INPUT_SYSTEM
                if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame) down = true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
                if (Input.GetMouseButtonDown(1)) down = true;
#endif
                return down;
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