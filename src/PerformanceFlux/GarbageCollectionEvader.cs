using System;
using System.Collections.Generic;
using HarmonyLib;

namespace PerformanceFlux.Optimizations
{
    /// <summary>
    /// Gère le recyclage (Pooling) des allocations mémoire pour empêcher le déclenchement du Garbage Collector.
    /// </summary>
    public static class GarbageCollectionEvader
    {
        // Objet de verrouillage pour garantir que le thread de CoreKinetics et le thread principal d'Unity ne se croisent jamais ici.
        private static readonly object _poolLock = new object();
        
        // Liste statique pré-allouée qui servira de tampon global unique
        private static readonly List<Part> _cachedPartList = new List<Part>(512);

        /// <summary>
        /// Patch Harmony pour intercepter la recherche des pièces actives d'un vaisseau.
        /// Au lieu d'instancier une nouvelle List<Part> à chaque frame, on renvoie notre liste recyclée.
        /// </summary>
        [HarmonyPatch(typeof(Vessel), "GetActiveParts")]
        public static class VesselGetActivePartsPatch
        {
            [HarmonyPrefix]
            public static bool Prefix(Vessel __instance, ref List<Part> __result)
            {
                if (__instance == null) return true;

                // --- CONNEXION AVEC LE MENU KORTEXSETTINGS ---
                // On vérifie si la partie est chargée et si les paramètres existent
                if (HighLogic.CurrentGame != null && HighLogic.CurrentGame.Parameters != null)
                {
                    // On récupère notre nœud de paramètres personnalisés Kortex
                    var settings = HighLogic.CurrentGame.Parameters.CustomParams<Kortex.Configuration.KortexSettings>();
                    
                    // Si le joueur a décoché l'option "GC Evader", on retourne 'true' 
                    // pour exécuter la méthode d'origine de KSP sans y toucher.
                    if (settings != null && !settings.enableGcEvader)
                    {
                        return true;
                    }
                }

                // --- EXÉCUTION DU POOLING (SI ACTIVÉ) ---
                // Sécurisation absolue contre les conflits de Threads (Thread-Safety)
                lock (_poolLock)
                {
                    // 1. On vide proprement la liste tampon sans altérer sa capacité mémoire allouée
                    _cachedPartList.Clear();

                    // 2. On la remplit manuellement avec les pièces du vaisseau actuel
                    int count = __instance.parts.Count;
                    for (int i = 0; i < count; i++)
                    {
                        Part p = __instance.parts[i];
                        if (p != null && p.State == PartStates.ACTIVE)
                        {
                            _cachedPartList.Add(p);
                        }
                    }

                    // 3. On injecte notre liste recyclée directement dans le résultat natif de KSP
                    __result = _cachedPartList;
                    
                    // 4. On retourne 'false' pour court-circuiter la méthode d'origine de KSP et bloquer son allocation polluante
                    return false; 
                }
            }
        }
    }
}