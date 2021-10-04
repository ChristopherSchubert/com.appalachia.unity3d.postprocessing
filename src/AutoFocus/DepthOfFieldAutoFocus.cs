using System;
using Appalachia.Editing.Attributes;
using Sirenix.OdinInspector;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.Serialization;

namespace Appalachia.PostProcessing.AutoFocus
{
    // ExecuteAlways needed to run OnDisable() after DoF made us allocate the buffer in edit mode.
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class DepthOfFieldAutoFocus : MonoBehaviour, IDepthOfFieldAutoFocus
    {
        private const string _PRF_PFX = nameof(DepthOfFieldAutoFocus) + ".";
        
        [FormerlySerializedAs("m_Compute"), SmartLabel]
        [HideInInspector] 
        public ComputeShader computeShader;
        [NonSerialized] private ComputeBuffer _autoFocusParametersBuffer;
        [NonSerialized] private ComputeBuffer _autoFocusOutputBuffer;
        
        [ShowInInspector, InlineProperty, HideLabel, BoxGroup("Settings Manager"), HideReferenceObjectPicker]
        private DepthOfFieldActiveSettingsManager _currentSettings;

        [SmartLabel, InlineEditor]
        public DepthOfFieldStateSettingCollection settingsCollection;

        private static readonly ProfilerMarker _PRF_SetUpAutoFocusParams = new ProfilerMarker(_PRF_PFX + nameof(SetUpAutoFocusParams));
        public void SetUpAutoFocusParams(
            CommandBuffer cmd,
            float focalLength /*in meters*/,
            float filmHeight,
            Camera cam,
            bool resetHistory)
        {
            using (_PRF_SetUpAutoFocusParams.Auto())
            {
                if (!CheckManager())
                {
                    return;
                }

                _currentSettings.Update(enabled, computeShader, cmd, focalLength, filmHeight, cam, resetHistory);
            }
        }

        private static readonly ProfilerMarker _PRF_Reset = new ProfilerMarker(_PRF_PFX + nameof(Reset));
        private void Reset()
        {
            using (_PRF_Reset.Auto())
            {
                if (!CheckManager())
                {
                    return;
                }
                
                _currentSettings.Reset();
            }
        }

        private static readonly ProfilerMarker _PRF_OnDisable = new ProfilerMarker(_PRF_PFX + nameof(OnDisable));
        private void OnDisable()
        {
            using (_PRF_OnDisable.Auto())
            {
                _currentSettings?.ReleaseBuffers();
            }
        }


#if UNITY_EDITOR
        private static readonly ProfilerMarker _PRF_Update = new ProfilerMarker(_PRF_PFX + nameof(Update));
        private void Update()
        {
            using (_PRF_Update.Auto())
            {
                if (!CheckManager())
                {
                    return;
                }
                
                _currentSettings.RetrieveDisplayData();
            }
        }
#endif

        private bool CheckManager()
        {
            if (settingsCollection == null)
            {
                return false;
            }
                
            if (_currentSettings == null)
            {
                _currentSettings = new DepthOfFieldActiveSettingsManager(settingsCollection);
            }
                
            if (_currentSettings.settingsCollection == null || _currentSettings.settingsCollection != settingsCollection)
            {
                _currentSettings.settingsCollection = settingsCollection;
            }

            return true;
        }
    }
}
