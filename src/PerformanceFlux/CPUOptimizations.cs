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

            //UI throttler
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

            //Commnet throttler
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

    // Throttle UI Nativ (Ressources)
    [HarmonyPatch(typeof(ResourceDisplay), "UpdateDisplay")]
    public static class UIThrottlePatch
    {
        [HarmonyPrefix]
        public static bool Prefix() 
        { 
            //IF CpuManager not ready it waits
            return CPUManager.ShouldUpdateUI; 
        }
    }

    // Throttle CommNet Global (Huge gains for satellites constellations)
    [HarmonyPatch(typeof(CommNet.CommNetNetwork), "Update")]
    public static class CommNetThrottle
    {
        [HarmonyPrefix]
        public static bool Prefix()
        {
            // Centralised Flag
            return CPUManager.ShouldUpdateCommNet;
        }
    }
}
