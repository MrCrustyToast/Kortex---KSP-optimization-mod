using UnityEngine;
using HarmonyLib;
using System;
using System.Collections.Concurrent;

namespace CoreKinetics
{
    //Main thread dispatcher
    public static class MainThreadDispatcher
    {
        private static readonly ConcurrentQueue<Action> queue = new ConcurrentQueue<Action>();
        
        public static void Update() 
        { 
            //Quickly empties the queue
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

    //GC manager
    public class IncrementalGCManager : MonoBehaviour
    {
        void Awake() 
        { 
            //Force Unity GC (Garbage Collector) , smoothes out frames
            if (UnityEngine.Scripting.GarbageCollector.GCMode != UnityEngine.Scripting.GarbageCollector.Mode.Enabled)
            {
                UnityEngine.Scripting.GarbageCollector.GCMode = UnityEngine.Scripting.GarbageCollector.Mode.Enabled;
                Debug.Log("[Kortex] Incremental GC activé de force pour fluidifier le framerate.");
            }
            Destroy(this); //once finish we can destroy it
        }
    }

    //Global loader
    [KSPAddon(KSPAddon.Startup.Instantly, true)] // true = Persists without re-patch
    public class CoreLoader : MonoBehaviour
    {
        private static bool hasInitialized = false;
        private Harmony harmony;

        void Awake()
        {
            //Double instances protection on startup
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
            //Only called when KSP closes
            if (harmony != null)
            {
                harmony.UnpatchAll("com.corekinetics.global"); 
            }
        }
    }
}
