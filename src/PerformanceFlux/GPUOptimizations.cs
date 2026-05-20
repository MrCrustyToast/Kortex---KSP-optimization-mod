using UnityEngine;
using System.Collections.Generic;

namespace PerformanceFlux
{
    public class GPUManager : MonoBehaviour
    {
        private float timer = 0f;
        private float stockShadowDist;
        private int stockShadowCascades;

        // Cache pour stocker les MeshRenderers des pièces et éviter le GetComponentsInChildren
        private readonly Dictionary<Part, MeshRenderer[]> rendererCache = new Dictionary<Part, MeshRenderer[]>();
        private readonly List<Part> deadParts = new List<Part>(); // Liste tampon pour nettoyer le cache

        void Start()
        {
            stockShadowDist = QualitySettings.shadowDistance;
            stockShadowCascades = QualitySettings.shadowCascades;
        }

        void Update()
        {
            timer += Time.unscaledDeltaTime;
            if (timer >= 1f)
            {
                timer = 0f;
                OptimizeShadows();
                OptimizeVesselLOD();
            }
        }

        private void OptimizeShadows()
        {
            Vessel v = FlightGlobals.ActiveVessel;
            if (v != null && v.mainBody != null && v.altitude > (v.mainBody.atmosphereDepth + 15000))
            {
                // En orbite ou haute atmosphère, on réduit drastiquement les ombres globales
                QualitySettings.shadowDistance = 30f;
                QualitySettings.shadowCascades = 2;
            }
            else
            {
                // Retour aux paramètres d'origine du joueur
                QualitySettings.shadowDistance = stockShadowDist;
                QualitySettings.shadowCascades = stockShadowCascades;
            }
        }

        private void OptimizeVesselLOD()
        {
            Vessel active = FlightGlobals.ActiveVessel;
            if (active == null || FlightGlobals.VesselsLoaded == null) return;

            Vector3 activePos = active.transform.position;
            float maxDistSqr = 400f * 400f; // 400 mètres au carré pour s'affranchir de la racine carrée (Vector3.Distance)

            int loadedVesselCount = FlightGlobals.VesselsLoaded.Count;
            for (int i = 0; i < loadedVesselCount; i++)
            {
                Vessel v = FlightGlobals.VesselsLoaded[i];
                if (v == null || v == active || v.parts == null) continue;

                // Calcul ultra-rapide de la distance au carré
                float sqrDist = (activePos - v.transform.position).sqrMagnitude;
                bool isFar = sqrDist > maxDistSqr;

                int partCount = v.parts.Count;
                for (int j = 0; j < partCount; j++)
                {
                    Part p = v.parts[j];
                    if (p == null) continue;

                    // Récupération ou mise en cache des renderers (ZÉRO allocation après le premier passage)
                    if (!rendererCache.TryGetValue(p, out MeshRenderer[] renderers))
                    {
                        renderers = p.GetComponentsInChildren<MeshRenderer>();
                        rendererCache[p] = renderers;
                    }

                    if (renderers == null) continue;

                    // Application du mode d'ombre
                    var targetMode = isFar ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
                    for (int r = 0; r < renderers.Length; r++)
                    {
                        if (renderers[r] != null && renderers[r].shadowCastingMode != targetMode)
                        {
                            renderers[r].shadowCastingMode = targetMode;
                        }
                    }
                }
            }

            // Nettoyage périodique du cache pour éviter de garder les pièces détruites en mémoire
            CleanRendererCache();
        }

        private void CleanRendererCache()
        {
            deadParts.Clear();
            foreach (var kp in rendererCache.Keys)
            {
                if (kp == null) deadParts.Add(kp);
            }
            for (int i = 0; i < deadParts.Count; i++)
            {
                rendererCache.Remove(deadParts[i]);
            }
            deadParts.Clear();
        }

        void OnDestroy()
        {
            rendererCache.Clear();
            deadParts.Clear();
        }
    }
}