using System;
using System.Reflection;
using CitiesHarmony.API;
using HarmonyLib;
using ICities;
using ODMatrix.Aggregation;
using ODMatrix.Diagnostics;
using UnityEngine;

namespace ODMatrix.Bootstrap
{
    internal static class ModLifecycle
    {
        private const string HarmonyId = "com.yy_1070.odmatrix";

        private static Harmony s_harmony;
        private static bool s_isActive;

        internal static void OnLevelLoaded(LoadMode mode)
        {
            if (!IsSupportedGameMode(mode) || s_isActive)
            {
                return;
            }

            try
            {
                ModLogger.Initialize();
                ModLogger.Lifecycle("ODMatrix v" + ModVersion.SemanticVersion + " loading for mode " + mode + ".");

                ResidentTravelCapture.Initialize();
                ODMatrix.Diagnostics.ReflectionProbe.Run();

                HarmonyHelper.DoOnHarmonyReady(ApplyPatches);
                HarmonyHelper.EnsureHarmonyInstalled();

                s_isActive = true;
            }
            catch (Exception ex)
            {
                Debug.LogError("[ODMatrix] Failed to initialize: " + ex);
                ModLogger.Error("Failed to initialize mod lifecycle.", ex);
            }
        }

        internal static void OnLevelUnloading()
        {
            if (!s_isActive)
            {
                return;
            }

            try
            {
                RemovePatches();
                ResidentTravelCapture.Shutdown();
                ModLogger.Lifecycle("ODMatrix v" + ModVersion.SemanticVersion + " unloading.");
            }
            catch (Exception ex)
            {
                Debug.LogError("[ODMatrix] Failed to unload cleanly: " + ex);
                ModLogger.Error("Failed to unload mod lifecycle cleanly.", ex);
            }
            finally
            {
                s_isActive = false;
                ModLogger.Shutdown();
            }
        }

        private static void ApplyPatches()
        {
            if (s_harmony != null)
            {
                return;
            }

            s_harmony = new Harmony(HarmonyId);
            s_harmony.PatchAll(Assembly.GetExecutingAssembly());
            ModLogger.Info("Harmony patches applied for ODMatrix v" + ModVersion.SemanticVersion + ".");
        }

        private static void RemovePatches()
        {
            if (s_harmony == null)
            {
                return;
            }

            s_harmony.UnpatchAll(HarmonyId);
            s_harmony = null;
            ModLogger.Info("Harmony patches removed for ODMatrix v" + ModVersion.SemanticVersion + ".");
        }

        private static bool IsSupportedGameMode(LoadMode mode)
        {
            switch (mode)
            {
                case LoadMode.NewGame:
                case LoadMode.LoadGame:
                case LoadMode.LoadScenario:
                case LoadMode.NewGameFromScenario:
                    return true;
                default:
                    return false;
            }
        }
    }
}
