using HarmonyLib;
using UnityEngine;

namespace Kortex.PerformanceFlux
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class GPUOptimizations : MonoBehaviour
    {
        private void Awake()
        {
            var harmony = new Harmony("com.kortex.performanceflux.gpu");
            try
            {
                harmony.Patch(
                    AccessTools.Method(typeof(Part), "ModulesOnStart"),
                    postfix: new HarmonyMethod(typeof(Part_ModulesOnStart_GPUPatch), "Postfix")
                );
                Debug.Log("[Kortex:GPU] ModulesOnStart patch successfully applied.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[Kortex:GPU] ERROR: Failed to apply ModulesOnStart patch: {ex.Message}");
            }
            GameEvents.onFlightReady.Add(OnFlightReady);
        }

        private void OnFlightReady()
        {
            Part_ModulesOnStart_GPUPatch.LogSummary();
            GameEvents.onFlightReady.Remove(OnFlightReady);
        }
    }

    [HarmonyPatch(typeof(Part), "ModulesOnStart")]
    public class Part_ModulesOnStart_GPUPatch
    {
        private static int _culledPartCount = 0;

        [HarmonyPostfix]
        public static void Postfix(Part __instance)
        {
            if (__instance.mass < 0.005f)
            {
                var renderers = __instance.GetComponentsInChildren<MeshRenderer>();
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null)
                        renderers[i].shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                _culledPartCount++;
            }
        }

        public static void LogSummary()
        {
            if (_culledPartCount > 0)
                Debug.Log($"[Kortex:GPU] Shadow culling applied to {_culledPartCount} micro-parts.");
            _culledPartCount = 0;
        }
    }
}