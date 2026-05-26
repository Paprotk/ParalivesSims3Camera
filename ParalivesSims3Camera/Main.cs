using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine.InputSystem;

namespace ParalivesMouseSwap
{
    [BepInPlugin("com.arro.paramouseswap", "Paralives Mouse Button Swap to match The Sims 3", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            new Harmony("com.arro.paramouseswap").PatchAll();
            Log.LogInfo("Mouse Button Swap loaded");
        }
    }

    [HarmonyPatch(typeof(Setting.KeyBindings), "LoadAndApplyKeyRebindings")]
    public class PatchKeyBindings
    {
        static void Postfix()
        {
            try
            {
                var playerInput = PlayerManager.Instance.HybridPlayer1.GetComponent<PlayerInput>();
                var actions = playerInput.actions;

                var rightClick = actions["RightClick"];
                var middleClick = actions["MiddleClick"];

                int rightIndex = KeyRebindingManager.GetBindingIndex(rightClick, false, 0, false);
                int middleIndex = KeyRebindingManager.GetBindingIndex(middleClick, false, 0, false);

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
}