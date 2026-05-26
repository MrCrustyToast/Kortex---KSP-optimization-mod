using System;
using UnityEngine;
using Kortex.Configuration;

namespace Kortex.PerformanceFlux
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class DynamicShadowManager : MonoBehaviour
    {

        private float _originalShadowDistance;
        private int _originalShadowCascades;
        private ShadowProjection _originalShadowProjection;
        private float _originalNearPlaneOffset;

        private void Awake()
        {

            _originalShadowDistance = QualitySettings.shadowDistance;
            _originalShadowCascades = QualitySettings.shadowCascades;
            _originalShadowProjection = QualitySettings.shadowProjection;
            _originalNearPlaneOffset = QualitySettings.shadowNearPlaneOffset;

            var settings = HighLogic.CurrentGame?.Parameters?.CustomParams<KortexSettings>();
            if (settings != null && !settings.enableDynamicShadows)
            {
                enabled = false;
                return;
            }
        }

        private void Start()
        {
            GameEvents.onVesselSituationChange.Add(OnVesselSituationChange);

            if (FlightGlobals.ActiveVessel != null)
            {
                EvaluateAndUpdateShadows(FlightGlobals.ActiveVessel.situation);
            }
        }

        private void OnDestroy()
        {
            GameEvents.onVesselSituationChange.Remove(OnVesselSituationChange);


            QualitySettings.shadowDistance = _originalShadowDistance;
            QualitySettings.shadowCascades = _originalShadowCascades;
            QualitySettings.shadowProjection = _originalShadowProjection;
            QualitySettings.shadowNearPlaneOffset = _originalNearPlaneOffset;
        }

        private void OnVesselSituationChange(GameEvents.HostedFromToAction<Vessel, Vessel.Situations> data)
        {
            if (data.host != FlightGlobals.ActiveVessel) return;
            EvaluateAndUpdateShadows(data.to);
        }


        private void EvaluateAndUpdateShadows(Vessel.Situations situation)
        {
            if (situation == Vessel.Situations.ORBITING || 
                situation == Vessel.Situations.ESCAPING || 
                situation == Vessel.Situations.SUB_ORBITAL)
            {

                QualitySettings.shadowDistance = 50f; 
                QualitySettings.shadowCascades = 1;
                QualitySettings.shadowProjection = ShadowProjection.StableFit;
            }
            else
            {

                QualitySettings.shadowDistance = Mathf.Min(_originalShadowDistance, 2500f);

                QualitySettings.shadowCascades = 2;

                QualitySettings.shadowProjection = ShadowProjection.StableFit;

                QualitySettings.shadowNearPlaneOffset = 3.0f;
            }

            Debug.Log($"[Kortex:GPU] Shadow Gov → Dist: {QualitySettings.shadowDistance}m | Cascades: {QualitySettings.shadowCascades} | Proj: {QualitySettings.shadowProjection} | Sit: {situation}");
        }
    }
}