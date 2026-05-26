using System;
using System.Reflection;
using UnityEngine;
using HarmonyLib;

namespace Kortex.PerformanceFlux
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class CPUOptimizations : MonoBehaviour
    {
        private static bool _patched = false;

        private void Awake()
        {
            if (_patched) return;

            var harmony = new Harmony("com.kortex.performanceflux.cpu");

            // ==========================================
            // 1. PATCH SPEEDDISPLAY (Throttling Vitesse)
            // ==========================================
            Type speedDisplayType = Type.GetType("KSP.UI.Screens.Flight.SpeedDisplay, Assembly-CSharp");
            if (speedDisplayType != null)
            {
                MethodInfo speedTarget = speedDisplayType.GetMethod("LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo speedPrefix = typeof(SpeedDisplay_LateUpdate_Patch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);

                if (speedTarget != null && speedPrefix != null)
                {
                    try
                    {
                        harmony.Patch(speedTarget, prefix: new HarmonyMethod(speedPrefix));
                        Debug.Log("[Kortex:CPU] Patch SpeedDisplay.LateUpdate (10Hz) injecté avec succès.");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Kortex:CPU] Échec patch SpeedDisplay : {ex.Message}");
                    }
                }
            }

            // ==========================================
            // 2. PATCH PORTRAITS (Culling Mode Carte)
            // ==========================================
            Type portraitGalleryType = Type.GetType("KSP.UI.Screens.Flight.KerbalPortraitGallery, Assembly-CSharp");
            if (portraitGalleryType != null)
            {
                MethodInfo portraitTarget = portraitGalleryType.GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                MethodInfo portraitPrefix = typeof(KerbalPortraitGallery_Update_Patch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);

                if (portraitTarget != null && portraitPrefix != null)
                {
                    try
                    {
                        harmony.Patch(portraitTarget, prefix: new HarmonyMethod(portraitPrefix));
                        Debug.Log("[Kortex:CPU] Patch KerbalPortraitGallery.Update (Culling) injecté avec succès.");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[Kortex:CPU] Échec patch KerbalPortraitGallery : {ex.Message}");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[Kortex:CPU] Classe KerbalPortraitGallery introuvable.");
            }

            // ==========================================
            // 3. PATCH LOG ANTI-SPAM (Silencieux Engine)
            // ==========================================
            try
            {
                MethodInfo logTarget = typeof(Debug).GetMethod("LogWarning", new Type[] { typeof(object) });
                MethodInfo logPrefix = typeof(LogAntiSpam_Patch).GetMethod("Prefix", BindingFlags.Static | BindingFlags.Public);

                if (logTarget != null && logPrefix != null)
                {
                    harmony.Patch(logTarget, prefix: new HarmonyMethod(logPrefix));
                    Debug.Log("[Kortex:CPU] Silencieux anti-spam de logs système actif.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Kortex:CPU] Échec injection silencieux de logs : {ex.Message}");
            }

            _patched = true;
        }
    }

    // ==========================================
    // PATCH 1 : CODE D'EXÉCUTION SPEEDDISPLAY
    // ==========================================
    public static class SpeedDisplay_LateUpdate_Patch
    {
        private static float _lastUpdateTime = 0f;
        private const float ThrottleInterval = 0.1f; 

        public static bool Prefix()
        {
            var settings = HighLogic.CurrentGame?.Parameters?.CustomParams<Kortex.Configuration.KortexSettings>();
            if (settings == null || !settings.enableUiThrottle) return true;

            float currentTime = Time.realtimeSinceStartup;
            if (currentTime - _lastUpdateTime < ThrottleInterval) return false;

            _lastUpdateTime = currentTime;
            return true;
        }
    }

    // ==========================================
    // PATCH 2 : CODE D'EXÉCUTION PORTRAITS
    // ==========================================
    public static class KerbalPortraitGallery_Update_Patch
    {
        private static bool _wasMapActive = false;

        public static bool Prefix()
        {
            var settings = HighLogic.CurrentGame?.Parameters?.CustomParams<Kortex.Configuration.KortexSettings>();
            // [MODIFICATION ICI] On écoute la variable spécifique aux portraits
            if (settings == null || !settings.enablePortraitCulling) return true;

            bool isMapEnabled = MapView.MapIsEnabled;

            // Log Kortex discret lors du changement d'état pour valider le fonctionnement
            if (isMapEnabled != _wasMapActive)
            {
                _wasMapActive = isMapEnabled;
                if (isMapEnabled)
                    Debug.Log("[Kortex:CPU] Mode carte détecté : mise en veille des caméras 3D des Kerbals.");
                else
                    Debug.Log("[Kortex:CPU] Mode vol détecté : réactivation des portraits des Kerbals.");
            }

            // Si la carte est active, on court-circuite l'Update (Culling total)
            if (isMapEnabled)
            {
                return false; 
            }

            return true;
        }
    }

    // ==========================================
    // PATCH 3 : CODE D'EXÉCUTION LOG ANTI-SPAM
    // ==========================================
    public static class LogAntiSpam_Patch
    {
        private static string _lastMessage = "";
        private static float _lastLogTime = 0f;
        private static int _spamCount = 0;

        public static bool Prefix(object message)
        {
            // [MODIFICATION ICI] On vérifie le réglage de l'anti-spam avant de faire quoi que ce soit
            var settings = HighLogic.CurrentGame?.Parameters?.CustomParams<Kortex.Configuration.KortexSettings>();
            if (settings == null || !settings.enableLogAntiSpam) return true;

            if (message == null) return true;
            string msgStr = message.ToString();

            // Cible les avertissements redondants générés en boucle par le moteur physique/graphique
            if (msgStr.Contains("Look rotation viewing vector is zero") || 
                msgStr.Contains("ObTI") || 
                msgStr.Contains("A Transform can't be assignment"))
            {
                float currentTime = Time.realtimeSinceStartup;

                // Si c'est le même message en moins de 5 secondes, on filtre
                if (msgStr == _lastMessage && currentTime - _lastLogTime < 5f)
                {
                    _spamCount++;
                    return false; // Intercepte et étouffe le log
                }

                // Si on a étouffé des logs, on prévient proprement avant d'afficher le suivant
                if (_spamCount > 0)
                {
                    Debug.Log($"[Kortex:CPU] Silencieux : {_spamCount} messages d'alerte identiques ont été bloqués pour économiser le CPU.");
                    _spamCount = 0;
                }

                _lastMessage = msgStr;
                _lastLogTime = currentTime;
            }

            return true;
        }
    }
}