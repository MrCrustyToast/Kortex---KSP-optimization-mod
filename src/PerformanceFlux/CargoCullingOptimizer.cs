using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Kortex.PerformanceFlux
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class CargoCullingOptimizer : MonoBehaviour
    {
        private float timer = 0f;
        private const float UpdateInterval = 1.0f;
        private Dictionary<MeshRenderer, bool> originalStates = new Dictionary<MeshRenderer, bool>();
        private bool _cullingBypassLogged = false;
        
        private static readonly PropertyInfo ShieldedProperty = typeof(Part).GetProperty("ShieldedFromAirflow", BindingFlags.Public | BindingFlags.Instance);

        private void Update()
        {
            timer += Time.deltaTime;
            if (timer < UpdateInterval) return;
            timer = 0f;
#if DEBUG
            UnityEngine.Debug.Log("[Kortex] CargoCullingOptimizer : Vérification et culling des pièces masquées.");
#endif

            try
            {
                var settings = HighLogic.CurrentGame?.Parameters?.CustomParams<Kortex.Configuration.KortexSettings>();
                bool bypass = settings == null || !settings.enableCargoCulling || FlightGlobals.ActiveVessel == null;
                if (bypass)
                {
                    if (!_cullingBypassLogged)
                    {
                        Debug.Log("[Kortex:Cargo] Culling bypass triggered (Disabled in settings or no active vessel). Restoring renderers.");
                        _cullingBypassLogged = true;
                    }
                    RestoreAllRenderers();
                    return;
                }

                _cullingBypassLogged = false;
                ExecuteSafeCulling();
            }
            catch (Exception ex) { UnityEngine.Debug.LogError("[Kortex] CargoCulling Error: " + ex.Message); }
        }

        private void ExecuteSafeCulling()
        {
            try
            {
                var vessel = FlightGlobals.ActiveVessel;
                if (vessel?.Parts == null) return;

                for (int i = 0; i < vessel.Parts.Count; i++)
                {
                    Part p = vessel.Parts[i];
                    if (p == null || p.localRoot == p) continue;

                    bool isHidden = false;
                    if (ShieldedProperty != null) isHidden = (bool)ShieldedProperty.GetValue(p, null);


                    var excludedTransforms = new System.Collections.Generic.HashSet<Transform>();
                    foreach (var fxGroup in p.fxGroups)
                    {
                        foreach (var ps in fxGroup.fxEmittersNewSystem)
                        {
                            if (ps == null) continue;

                            Transform t = ps.transform;
                            while (t != null && t != p.transform)
                            {
                                excludedTransforms.Add(t);
                                t = t.parent;
                            }
                        }
                    }

                    var renderers = p.GetComponentsInChildren<MeshRenderer>(true);
                    for (int j = 0; j < renderers.Length; j++)
                    {
                        var r = renderers[j];
                        if (r == null) continue;


                        if (excludedTransforms.Contains(r.transform))
                            continue;

                        if (!originalStates.ContainsKey(r)) originalStates[r] = r.enabled;
                        r.enabled = isHidden ? false : originalStates[r];
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Kortex:Cargo] Exception during safe culling loop: {ex.Message}");
            }
        }

        private void RestoreAllRenderers()
        {
            foreach (var kvp in originalStates)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.enabled = kvp.Value;
                }
            }
        }

        private void OnDestroy()
        {
            RestoreAllRenderers();
            originalStates.Clear();
        }
    }
}
