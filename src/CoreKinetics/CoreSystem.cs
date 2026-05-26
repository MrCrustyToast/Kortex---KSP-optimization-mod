using System;
using System.IO;
using System.Linq;
using Kortex.Configuration;
using System.Reflection;
using UnityEngine;
using HarmonyLib;

namespace Kortex.CoreKinetics
{
    /// <summary>
    /// Point d'entrée principal de l'assembly CoreKinetics.
    /// Gère la détection de compatibilité globale au démarrage pour éviter les conflits d'injection.
    /// </summary>
    [KSPAddon(KSPAddon.Startup.Instantly, true)]
    public class CoreSystem : MonoBehaviour
    {
        /// <summary>
        /// Flag global indiquant la présence de KSPCommunityFixes dans l'environnement d'exécution.
        /// Permet aux différents patches de l'assembly d'adapter dynamiquement leur comportement.
        /// </summary>
        public static bool IsKSPCFDetected { get; private set; } = false;

        private void Awake()
        {
            // Persistance de l'instance pour centraliser l'état global du cycle de vie du mod
            DontDestroyOnLoad(this);

            // PROBLEME C : Résolution du conflit potentiel de redondance avec KSPCF
            // Scan unique et thread-safe au démarrage pour identifier l'assembly de KSPCF.
            // L'évaluation par chaîne de caractères évite une liaison statique forte (TypeLoadException) si absent.
            IsKSPCFDetected = AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => a.GetName().Name == "KSPCommunityFixes");

            var harmony = new Harmony("com.kortex.corekinetics");

            if (IsKSPCFDetected)
            {
                Debug.Log("[Kortex:CoreKinetics] KSPCommunityFixes détecté. Activation du mode de compatibilité passive.");
            }
            else
            {
                Debug.Log("[Kortex:CoreKinetics] KSPCommunityFixes non détecté. Initialisation du set d'optimisations standards.");
            }

            // Exécution de l'injection globale. La sémantique de filtrage est déléguée aux déclarations [HarmonyPrepare].
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            UnityEngine.Debug.Log("[Kortex] CoreSystem et patchs Harmony initialisés.");
        }
    }

    /// <summary>
    /// Patch d'optimisation de la boucle de rafraîchissement CommNet.
    /// </summary>
    [HarmonyPatch(typeof(CommNet.CommNetNetwork), "Update")]
    public class CommNetNetwork_Update_Patch
    {
        private static float _lastCommNetUpdate = 0f;
        private const float CommNetInterval = 2.5f;

        [HarmonyPrepare]
        public static bool Prepare()
        {
            bool isKSPCFDetected = System.Linq.Enumerable.Any(AssemblyLoader.loadedAssemblies, a => a.name == "KSPCommunityFixes");
            if (isKSPCFDetected)
            {
                UnityEngine.Debug.LogWarning("[Kortex:CommNet] KSPCommunityFixes detected. Bypassing CommNet patch to prevent conflicts.");
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        public static bool Prefix()
        {
            float now = Time.realtimeSinceStartup;
            if (now - _lastCommNetUpdate < CommNetInterval) return false; 
            _lastCommNetUpdate = now;
#if DEBUG
            UnityEngine.Debug.Log("[Kortex] Throttling CommNet activé : scan exécuté.");
#endif
            return true;
        }
    }

    /// <summary>
    /// Utilitaire statique pour appliquer la rotation des sauvegardes de secours.
    /// </summary>
    public static class BackupRotation
    {
        public static void EnforceRotation()
        {
            try
            {
                int maxBackups = 10;

                if (HighLogic.CurrentGame != null)
                {
                    try
                    {
                        var settings = HighLogic.CurrentGame.Parameters.CustomParams<KortexSettings>();
                        if (settings != null)
                        {
                            maxBackups = Math.Max(0, settings.maxBackupSaves);
                        }
                    }
                    catch (Exception)
                    {
                        // If reading settings fails, keep default.
                    }
                }

                string backupFolder = Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "Kortex", "Backups");
                if (!Directory.Exists(backupFolder)) return;

                var files = Directory.GetFiles(backupFolder, "*backup*.sfs");
                Debug.Log("[Kortex] CoreKinetics: Found " + files.Length + " backup files. Max allowed: " + maxBackups);

                var fileInfos = files.Select(f => new FileInfo(f))
                                     .OrderByDescending(fi => fi.LastWriteTime)
                                     .ToArray();

                if (fileInfos.Length <= maxBackups) return;

                for (int i = maxBackups; i < fileInfos.Length; i++)
                {
                    try
                    {
                        var fi = fileInfos[i];
                        fi.Delete();
                        Debug.Log($"[Kortex:CoreKinetics] Backup rotation deleted: {fi.Name}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[Kortex:CoreKinetics] Failed to delete old backup {fileInfos[i].Name}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Kortex:CoreKinetics] EnforceRotation failed: {ex.Message}");
            }
        }
    }

}