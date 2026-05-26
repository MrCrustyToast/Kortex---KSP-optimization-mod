using System;
using System.Reflection;

namespace Kortex.Configuration
{
    public class KortexSettings : GameParameters.CustomParameterNode
    {
        public override string Title => "Kortex  |  Performance Suite";
        public override GameParameters.GameMode GameMode => GameParameters.GameMode.ANY;
        public override string Section => "Kortex";
        public override string DisplaySection => "Kortex";
        public override int SectionOrder => 1;
        public override bool HasPresets => false;



        [GameParameters.CustomStringParameterUI("===== COREKINETICS (Système & Disque) =====", autoPersistance = false)]
        public string headerCore = "";

        [GameParameters.CustomParameterUI("Async Autosave", toolTip = "Replaces KSP's blocking autosave with a background thread save, eliminating game freezes during flight.")]
        public bool enableAutosave = true;

        [GameParameters.CustomIntParameterUI("Autosave Interval  (minutes)", minValue = 1, maxValue = 30, toolTip = "How often Kortex saves in the background. Lower = safer, higher = less disk activity.")]
        public int autosaveInterval = 5;

        [GameParameters.CustomIntParameterUI("Max Backup Rotations", minValue = 3, maxValue = 20, toolTip = "Number of rotating autosave files kept in your save folder before the oldest is overwritten.")]
        public int maxBackupSaves = 10;


        [GameParameters.CustomStringParameterUI("", autoPersistance = false)]
        public string spacer1 = "";

        [GameParameters.CustomStringParameterUI("===== PERFORMANCEFLUX (CPU & Mémoire) =====", autoPersistance = false)]
        public string headerCpu = "";


        [GameParameters.CustomParameterUI("GC Evader (Memory Pooling)", toolTip = "Replaces KSP's heavy list allocations with static pools. Drastically reduces Garbage Collector stutters.")]
        public bool enableGcEvader = true;

        [GameParameters.CustomParameterUI("UI Throttle", toolTip = "Caps heavy UI element refresh rate to 10 Hz instead of every frame. Significant CPU savings under high part count.")]
        public bool enableUiThrottle = true;

        [GameParameters.CustomParameterUI("Adaptive Frame Stabilizer", toolTip = "Dynamically tightens the physics timestep under load. Trades mild time dilation (yellow clock) to fully eliminate micro-stutters.")]
        public bool enableFrameStabilizer = true;

        [GameParameters.CustomParameterUI("Map Mode Portrait Culling", toolTip = "Stops rendering animated 3D Kerbal portraits while in map view. Frees meaningful CPU headroom during complex missions.")]
        public bool enablePortraitCulling = true;

        [GameParameters.CustomParameterUI("Log Anti-Spam", toolTip = "Intercepts and silences repetitive Unity engine warning loops. Prevents micro-stutters caused by excessive disk I/O.")]
        public bool enableLogAntiSpam = true;


        [GameParameters.CustomStringParameterUI("", autoPersistance = false)]
        public string spacer2 = "";

        [GameParameters.CustomStringParameterUI("===== PERFORMANCEFLUX (GPU & Rendu) =====", autoPersistance = false)]
        public string headerGpu = "";

        [GameParameters.CustomParameterUI("Cargo & Fairing Mesh Culling", toolTip = "Hides the meshes of parts buried inside closed fairings and cargo bays. Reduces GPU draw calls and VRAM pressure.")]
        public bool enableCargoCulling = true;

        [GameParameters.CustomParameterUI("Shadow Cascade Governor", toolTip = "Restructures KSP's shadow pipeline per scene. Forces StableFit projection to eliminate flicker. +10 to +15 FPS on launchpad.")]
        public bool enableDynamicShadows = true;



        public override bool Enabled(MemberInfo member, GameParameters parameters)
        {
            if (member.Name == "autosaveInterval" || member.Name == "maxBackupSaves")
            {
                return enableAutosave;
            }

            if (member.Name == "headerCore" || member.Name == "headerCpu" || member.Name == "headerGpu" || member.Name.StartsWith("spacer"))
            {
                return true; 
            }
            
            return true;
        }
    }
}