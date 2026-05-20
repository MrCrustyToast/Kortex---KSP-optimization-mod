using UnityEngine;
using KSP.UI.Screens;
using HarmonyLib;
using System;

namespace PerformanceFlux
{
    public class CPUManager : MonoBehaviour
    {
        public static bool ShouldUpdateUI { get; private set; }
        public static bool ShouldUpdateCommNet { get; private set; }

        private float uiTimer = 0f;
        private float commNetTimer = 0f;

        void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;

            // 1. Gestion du Throttle de l'UI (Toutes les 0.05s ~ 20 FPS pour l'UI, largement assez fluide)
            uiTimer += deltaTime;
            if (uiTimer >= 0.05f)
            {
                uiTimer = 0f;
                ShouldUpdateUI = true;
            }
            else
            {
                ShouldUpdateUI = false;
            }

            // 2. Gestion du Throttle CommNet (Toutes les 0.2s ~ 5 fois par seconde)
            commNetTimer += deltaTime;
            if (commNetTimer >= 0.2f)
            {
                commNetTimer = 0f;
                ShouldUpdateCommNet = true;
            }
            else
            {
                ShouldUpdateCommNet = false;
            }
        }
    }

    // Throttle UI Native (Ressources)
    [HarmonyPatch(typeof(ResourceDisplay), "UpdateDisplay")]
    public static class UIThrottlePatch
    {
        [HarmonyPrefix]
        public static bool Prefix() 
        { 
            // Si le CPUManager n'est pas encore prêt, on laisse passer par sécurité
            return CPUManager.ShouldUpdateUI; 
        }
    }

    // Throttle CommNet Global (Gros gain CPU sur les réseaux de satellites)
    [HarmonyPatch(typeof(CommNet.CommNetNetwork), "Update")]
    public static class CommNetThrottle
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            // On utilise le flag centralisé et managé pour éviter les dérives de variables statiques
            return CPUManager.ShouldUpdateCommNet;
        }
    }
}