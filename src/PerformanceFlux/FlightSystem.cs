using UnityEngine;
using HarmonyLib;
using System;
using System.Collections.Generic;

namespace PerformanceFlux
{
    //vessels global cache
    public static class VesselComponentCache
    {
        private static readonly Dictionary<(Guid, Type), List<PartModule>> cache = new Dictionary<(Guid, Type), List<PartModule>>();
        private static readonly List<(Guid, Type)> keysToRemove = new List<(Guid, Type)>(); // Liste tampon réutilisable
        private static readonly List<PartModule> emptyList = new List<PartModule>(); // Évite d'allouer une "new List" si null

        public static void Invalidate(Vessel v)
        {
            if (v == null) return;
            
            keysToRemove.Clear();
            
            //keeps keys without using the memory
            foreach (var k in cache.Keys) 
            {
                if (k.Item1 == v.id) keysToRemove.Add(k);
            }
            
            // securised deletion
            for (int i = 0; i < keysToRemove.Count; i++)
            {
                cache.Remove(keysToRemove[i]);
            }
            keysToRemove.Clear();
        }
        
        public static void Clear() 
        { 
            cache.Clear(); 
            keysToRemove.Clear();
        }

        public static List<PartModule> Get(Vessel v, Type t)
        {
            if (v == null || v.parts == null) return emptyList;
            
            var key = (v.id, t);
            if (cache.TryGetValue(key, out var list)) return list;

            var res = new List<PartModule>();
            int partCount = v.parts.Count;
            
            // Foreach >> for
            for (int i = 0; i < partCount; i++)
            {
                var part = v.parts[i];
                if (part == null || part.Modules == null) continue;
                
                int moduleCount = part.Modules.Count;
                for (int j = 0; j < moduleCount; j++)
                {
                    var module = part.Modules[j];
                    if (module != null && t.IsAssignableFrom(module.GetType())) 
                    {
                        res.Add(module);
                    }
                }
            }
            cache[key] = res;
            return res;
        }
    }

    [HarmonyPatch(typeof(Vessel), "FindPartModulesImplementing", new Type[] { typeof(Type) })]
    public static class VesselCachePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(Vessel __instance, Type type, ref List<PartModule> __result)
        {
            __result = VesselComponentCache.Get(__instance, type);
            return false; //Dynamic search
        }
    }

    // Lighter flight loader
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class FlightLoader : MonoBehaviour
    {
        void Awake()
        {
            // Ressources injecter
            gameObject.AddComponent<CPUManager>();
            gameObject.AddComponent<GPUManager>();

            // dynamic cache actualisation with KSP scene change
            GameEvents.onVesselWasModified.Add(VesselComponentCache.Invalidate);
            GameEvents.onVesselPartCountChanged.Add(VesselComponentCache.Invalidate);
            
            Debug.Log("[Kortex] Système de vol et cache de routage O(1) activés.");
        }

        void OnDestroy()
        {
            // Leak patcher
            GameEvents.onVesselWasModified.Remove(VesselComponentCache.Invalidate);
            GameEvents.onVesselPartCountChanged.Remove(VesselComponentCache.Invalidate);
            VesselComponentCache.Clear();
            
            Debug.Log("[Kortex] Libération des ressources de vol et vidage du cache.");
        }
    }
}
