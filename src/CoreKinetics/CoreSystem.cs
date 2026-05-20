using UnityEngine;
using HarmonyLib;
using System;
using System.Collections.Concurrent;

namespace CoreKinetics
{
    // 1. Dispatcher Thread Principal (Optimisé et sécurisé)
    public static class MainThreadDispatcher
    {
        private static readonly ConcurrentQueue<Action> queue = new ConcurrentQueue<Action>();
        
        public static void Update() 
        { 
            // On vide la queue rapidement sans allocation
            while (queue.TryDequeue(out var action)) 
            {
                if (action != null)
                {
                    try { action.Invoke(); }
                    catch (Exception e) { Debug.LogError($"[Kortex] Erreur Dispatcher : {e.Message}"); }
                }
            } 
        }
        
        public static void RunOnMainThread(Action action) { queue.Enqueue(action); }
    }

    // 2. Gestionnaire GC (Nettoyé des fonctions bloquantes)
    public class IncrementalGCManager : MonoBehaviour
    {
        void Awake() 
        { 
            // On force l'Incremental GC d'Unity (Idéal pour lisser les pics de lag de KSP)
            if (UnityEngine.Scripting.GarbageCollector.GCMode != UnityEngine.Scripting.GarbageCollector.Mode.Enabled)
            {
                UnityEngine.Scripting.GarbageCollector.GCMode = UnityEngine.Scripting.GarbageCollector.Mode.Enabled;
                Debug.Log("[Kortex] Incremental GC activé de force pour fluidifier le framerate.");
            }
            Destroy(this); // Plus besoin de tourner dans l'Update, on peut détruire ce composant !
        }
    }

    // 3. Loader Global (Chargement UNIQUE au démarrage)
    [KSPAddon(KSPAddon.Startup.Instantly, true)] // true = Persiste à travers toutes les scènes SANS re-patcher
    public class CoreLoader : MonoBehaviour
    {
        private static bool hasInitialized = false;
        private Harmony harmony;

        void Awake()
        {
            // Sécurité pour éviter les doubles instances au démarrage
            if (hasInitialized)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
            hasInitialized = true;

            gameObject.AddComponent<IncrementalGCManager>();

            try 
            { 
                harmony = new Harmony("com.corekinetics.global"); 
                harmony.PatchAll(); 
                Debug.Log("[Kortex] Tous les patches Harmony ont été injectés avec succès.");
            } 
            catch (Exception e) 
            {
                Debug.LogError($"[Kortex] Échec critique de l'initialisation Harmony : {e.Message}");
            }
        }

        void Update() 
        { 
            MainThreadDispatcher.Update(); 
        }

        void OnDestroy() 
        { 
            // N'est appelé que si le jeu se ferme, ce qui est propre
            if (harmony != null)
            {
                harmony.UnpatchAll("com.corekinetics.global"); 
            }
        }
    }
}