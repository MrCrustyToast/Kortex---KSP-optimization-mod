using System;
using UnityEngine;
using Kortex.Configuration;

namespace Kortex.PerformanceFlux
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class FrameStabilizer : MonoBehaviour
    {
        private float _originalMaxDeltaTime;
        private float _fpsMeasurementInterval = 0.2f;
        private float _timeCounter = 0f;
        private int _frameCount = 0;
        private float _currentFps = 60f;

        private void Awake()
        {

            _originalMaxDeltaTime = Time.maximumDeltaTime;

            var settings = HighLogic.CurrentGame?.Parameters?.CustomParams<KortexSettings>();
            if (settings != null && !settings.enableFrameStabilizer)
            {
                enabled = false;
                return;
            }
        }

        private void Update()
        {

            _timeCounter += Time.unscaledDeltaTime;
            _frameCount++;

            if (_timeCounter >= _fpsMeasurementInterval)
            {
                _currentFps = _frameCount / _timeCounter;
                _frameCount = 0;
                _timeCounter = 0f;

                EnforceFramePacing();
            }
        }


        private void EnforceFramePacing()
        {
            if (_currentFps < 32f)
            {

                Time.maximumDeltaTime = Time.fixedDeltaTime * 1.15f;
            }
            else if (_currentFps < 48f)
            {

                Time.maximumDeltaTime = Time.fixedDeltaTime * 2.1f;
            }
            else
            {
                Time.maximumDeltaTime = _originalMaxDeltaTime;
            }
        }

        private void OnDestroy()
        {
            Time.maximumDeltaTime = _originalMaxDeltaTime;
        }
    }
}