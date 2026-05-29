using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Setting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ParalivesSims3Camera
{
    [BepInPlugin("com.arro.ParalivesSims3Camera", "Paralives Sims3 Camera", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;
        private void Awake()
        {
            Log = Logger;
            new Harmony("com.arro.ParalivesSims3Camera").PatchAll();
            Log.LogInfo("Mouse Button Swap loaded");
        }
    }

    [HarmonyPatch(typeof(PlayerManager), "Awake")]
    public class PatchKeyBindings
    {
        static void Postfix() => SwapButtons.Run();
    }

    [HarmonyPatch(typeof(Setting.KeyBindings), "OnCompiled")]
    public class PatchKeyBindingsOnCompiled
    {
        static void Postfix() => SwapButtons.Run();
    }

    public static class SwapButtons
    {
        public static void Run()
        {
            try
            {
                var playerInput = PlayerManager.Instance.HybridPlayer1.GetComponent<PlayerInput>();
                var actions = playerInput.actions;
                var rightClick = actions["RightClick"];
                var middleClick = actions["MiddleClick"];
                var rightIndex = KeyRebindingManager.GetBindingIndex(rightClick, false, 0, false);
                var middleIndex = KeyRebindingManager.GetBindingIndex(middleClick, false, 0, false);
                rightClick.ApplyBindingOverride(rightIndex, "<Mouse>/middleButton");
                middleClick.ApplyBindingOverride(middleIndex, "<Mouse>/rightButton");
                Plugin.Log.LogInfo("Mouse buttons swapped successfully");
            }
            catch (System.Exception e)
            {
                Plugin.Log.LogError("Swap error: " + e.Message);
            }
        }
    }

    [HarmonyPatch(typeof(UpdateFreeCamera), "UpdateForPlayer")]
    public class PatchFreeCameraUpdate
    {
        static Vector2 panStartPosition;
        static bool isPanning = false;

        static void Prefix(Player player)
        {
            var hybridPlayer = PlayerManager.Instance.GetHybridPlayer(player.PlayerIndex);
            var freeCamera = hybridPlayer.HybridCamera.FreeCamera;
            var inputManager = InputManager.Instance;
            
            var rightDown    = hybridPlayer.ButtonMiddleClick.Down;
            var rightPressed = hybridPlayer.ButtonMiddleClick.Pressed;

            if (rightDown)
            {
                isPanning = true;
                panStartPosition = inputManager.GetCursorPosition(player.PlayerIndex);
            }
            else if (!rightPressed)
            {
                isPanning = false;
            }
            
            if (isPanning)
            {
                CursorManager.Instance.CurrentCursorGUID = Settings.Get<Cursors>().MoveItemCursor;
                player.CameraCurrentCharacterFollowTarget = 0UL;
                player.IsMouseRotatingTheCamera = false;
                freeCamera.IsMouseDraggingView = false;
                
                freeCamera.MouseDragStartPosition = Vector3.zero;

                Vector2 currentPos = inputManager.GetCursorPosition(player.PlayerIndex);
                var offset = currentPos - panStartPosition;

                if (offset.magnitude < 5f) return;

                var settings = Settings.Get<FreeCamera>();
                var generalSettings = Settings.Get<GeneralOptions>();

                var time = (freeCamera.Distance - settings.MinZoomDistance) /
                           (settings.MaxZoomDistance - settings.MinZoomDistance);
                var speedAttenuation = settings.SpeedToDistanceAttenuationCurve.Evaluate(time, false);

                var moveSpeed = settings.MoveSpeed
                                * speedAttenuation
                                * (generalSettings.CameraMoveSensitivity * 0.01f)
                                * Time.unscaledDeltaTime
                                * 4.5f;

                var translation = new Vector3(
                    (offset.x / Screen.width)  * moveSpeed,
                    0f,
                    (offset.y / Screen.height) * moveSpeed
                );

                freeCamera.LookTarget.Translate(translation, Space.Self);
            }
        }
    }
}