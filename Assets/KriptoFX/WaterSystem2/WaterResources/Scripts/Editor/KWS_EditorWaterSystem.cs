#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static KWS.KWS_EditorUtils;
using Debug = UnityEngine.Debug;
using static KWS.WaterSystem;
using static KWS.KWS_Settings;

using Description = KWS.KWS_EditorTextDescription;
using link = KWS.KWS_EditorUrlLinks;
    
namespace KWS
{
    [System.Serializable]
    [CustomEditor(typeof(WaterSystem))]
    internal partial class KWS_EditorWaterSystem : Editor
    {
        private WaterSystem _waterInstance;
        private WaterQualityLevelSettings _qualitySettings;

        private bool _isActive;
        private SceneView.SceneViewState _lastSceneView;


        void OnDestroy()
        {
            KWS_EditorUtils.Release();
        }


        public override void OnInspectorGUI()
        {
            _waterInstance = (WaterSystem)target;

            if (_waterInstance.enabled && _waterInstance.gameObject.activeSelf)
            {
                _isActive = true;
                GUI.enabled = true;
            }
            else
            {
                _isActive = false;
                GUI.enabled = false;
            }

            UpdateWaterGUI();
        }


        void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUICustom;
        }


        void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUICustom;
        }

        void OnSceneGUICustom(SceneView sceneView)
        {
            if (Event.current.type == EventType.Repaint)
            {
                SceneView.RepaintAll();
            }
        }

        void UpdateWaterGUI()
        {
            _qualitySettings = WaterSystem.QualitySettings;
            if (_qualitySettings == null)
            {
                KWS_WaterSettingsRuntimeLoader.LoadWaterSettings();
                _qualitySettings = WaterSystem.QualitySettings;
                if (_qualitySettings == null)
                {
                    Debug.Log("empty settings?");
                    return;
                }
            }

            Undo.RecordObject(_waterInstance, "Changed main water parameters");
#if KWS_DEBUG
            WaterSystem.Test4 = EditorGUILayout.Vector4Field("Test4", WaterSystem.Test4);
            if (KWS_CoreUtils.SinglePassStereoEnabled) VRScale = Slider("VR Scale", "", VRScale, 0.5f, 2.5f, "");
            WaterSystem.TestTexture = (Texture2D)EditorGUILayout.ObjectField(WaterSystem.TestTexture, typeof(Texture2D), true);
            if (WaterSystem.TestTexture != null)
            {
                Shader.SetGlobalTexture("KWS_TestTexture", WaterSystem.TestTexture);
            }
#endif
            //waterSystem.TestObj = (GameObject) EditorGUILayout.ObjectField(waterSystem.TestObj, typeof(GameObject), true);


            EditorGUI.BeginChangeCheck();

            //CheckMessages();

            GUI.enabled = _isActive;
            bool defaultVal = false;

            EditorGUILayout.Space(20);  

            KWS_Tab(ref _waterInstance.ShowColorSettings,            useHelpBox: false, useExpertButton: false, ref defaultVal, "Color Settings",                       ColorSettings,              WaterSettingsCategory.ColorSettings);
            KWS_Tab(ref _waterInstance.ShowWavesSettings,            useHelpBox: false, useExpertButton: false, ref defaultVal, "Waves",                                WavesSettings,              WaterSettingsCategory.Waves);
            KWS_Tab(ref _waterInstance.ShowReflectionSettings,       useHelpBox: false, useExpertButton: false, ref defaultVal, "Reflection",                           ReflectionSettings,         WaterSettingsCategory.Reflection);
            KWS_Tab(ref _waterInstance.ShowRefractionSettings,       useHelpBox: false, useExpertButton: false, ref defaultVal, "Refraction (View Through Water)",      RefractionSetting,          WaterSettingsCategory.ColorRefraction);
            KWS_Tab(ref _waterInstance.ShowFoamSettings,             useHelpBox: false, useExpertButton: false, ref defaultVal, "Ocean Foam",                           FoamSetting,                WaterSettingsCategory.Foam);
            KWS_Tab(ref _waterInstance.ShowWetSettings,              useHelpBox: false, useExpertButton: false, ref defaultVal, "Wet Effect",                           WetSetting,                 WaterSettingsCategory.WetEffect);
            KWS_Tab(ref _waterInstance.ShowVolumetricSettings,       useHelpBox: false, useExpertButton: false, ref defaultVal, "Volumetric Lighting",                  VolumetricLightingSettings, WaterSettingsCategory.VolumetricLighting);
            KWS_Tab(ref _waterInstance.ShowCausticEffectSettings,    useHelpBox: false, useExpertButton: false, ref defaultVal, "Caustic (Light Patterns on Surfaces)", CausticSettings,            WaterSettingsCategory.Caustic);
            KWS_Tab(ref _waterInstance.ShowUnderwaterEffectSettings, useHelpBox: false, useExpertButton: false, ref defaultVal, "Underwater Effect",                    UnderwaterSettings,         WaterSettingsCategory.Underwater);

            
            if (EditorGUI.EndChangeCheck())
            {
                if (!Application.isPlaying)
                {
                    EditorUtility.SetDirty(_waterInstance);
                    EditorSceneManager.MarkSceneDirty(_waterInstance.gameObject.scene);
                }
            }

        }



        void ColorSettings()
        {
            _waterInstance.Transparent = Slider("Transparent (Meters)", Description.Color.Transparent, _waterInstance.Transparent, 1f, 100f, link.Transparent);
            _waterInstance.WaterColor = ColorFieldHUE("Water Color", Description.Color.WaterColor, _waterInstance.WaterColor, false, false, false, link.WaterColor);
            _waterInstance.TurbidityColor = ColorField("Turbidity Color", Description.Color.TurbidityColor, _waterInstance.TurbidityColor, false, false, false, link.TurbidityColor);
        }

        void WavesSettings()
        {

            _waterInstance.WindZone = (WindZone)EditorGUILayout.ObjectField(_waterInstance.WindZone, typeof(WindZone), true);
            if (_waterInstance.WindZone != null)
            {
                _waterInstance.WindZoneSpeedMultiplier = Slider("Wind Speed Multiplier", "", _waterInstance.WindZoneSpeedMultiplier, 0.01f, 10, link.WindZoneSpeedMultiplier);
                _waterInstance.WindZoneTurbulenceMultiplier = Slider("Wind Turbulence Multiplier", "", _waterInstance.WindZoneTurbulenceMultiplier, 0.01f, 10.0f, link.WindZoneTurbulenceMultiplier);
            }
            else
            {
                _waterInstance.WindSpeed = Slider(" Wind Speed", Description.Waves.WindSpeed, _waterInstance.WindSpeed, 0.1f, FFT.MaxWindSpeed, link.WindSpeed);
                _waterInstance.WindRotation = Slider(" Wind Rotation", Description.Waves.WindRotation, _waterInstance.WindRotation, 0.0f, 360.0f, link.WindRotation);
                _waterInstance.WindTurbulence = Slider(" Wind Turbulence", Description.Waves.WindTurbulence, _waterInstance.WindTurbulence, 0.0f, 1.0f, link.WindTurbulence);
            }
            Line();
            _waterInstance.FftWavesQuality = (WaterQualityLevelSettings.FftWavesQualityEnum)EnumPopup(" Waves Quality", Description.Waves.FftWavesQuality, _waterInstance.FftWavesQuality, link.FftWavesQuality);
            _waterInstance.FftWavesCascades = IntSlider(" Simulation Cascades", "", _waterInstance.FftWavesCascades, 1, FFT.MaxLods, link.FftWavesCascades);
            _waterInstance.WavesAreaScale = Slider(" Area Scale", "", _waterInstance.WavesAreaScale, 0.2f, KWS_Settings.FFT.MaxWavesAreaScale, link.WavesAreaScale);
            _waterInstance.WavesTimeScale = Slider(" Time Scale", Description.Waves.TimeScale, _waterInstance.WavesTimeScale, 0.0f, 2.0f, link.TimeScale);

#if KWS_DEBUG
            EditorGUILayout.Space(20);
            _waterInstance.DebugQuadtree = Toggle("Debug Quadtree", "", _waterInstance.DebugQuadtree, "");
            _waterInstance.DebugAABB = Toggle("Debug AABB", "", _waterInstance.DebugAABB, "");
            _waterInstance.DebugDynamicWaves = Toggle("Debug Dynamic Waves", "", _waterInstance.DebugDynamicWaves, "");
            _waterInstance.DebugBuoyancy = Toggle("Debug Buoyancy", "", _waterInstance.DebugBuoyancy, "");
            _waterInstance.DebugUpdateManager = Toggle("Debug Update Manager", "", _waterInstance.DebugUpdateManager, "");
#endif
        }
        
        
        void ReflectionSettings()
        {
            _waterInstance.ScreenSpaceReflection = (WaterQualityLevelSettings.QualityOverrideEnum)EnumPopup("Screen Space Reflection", Description.Reflection.UseScreenSpaceReflection, _waterInstance.ScreenSpaceReflection, link.UseScreenSpaceReflection);
            _waterInstance.PlanarReflection      = (WaterQualityLevelSettings.QualityOverrideEnum)EnumPopup("Planar Reflection",       Description.Reflection.UsePlanarReflection,      _waterInstance.PlanarReflection,      link.UsePlanarReflection);

            if (_waterInstance.ScreenSpaceReflection != WaterQualityLevelSettings.QualityOverrideEnum.Off && WaterSystem.QualitySettings.UseScreenSpaceReflection
             || _waterInstance.PlanarReflection      != WaterQualityLevelSettings.QualityOverrideEnum.Off && WaterSystem.QualitySettings.UsePlanarReflection)
            {
                _waterInstance.AnisotropicReflectionsScale = Slider("Anisotropic Reflections Scale", Description.Reflection.AnisotropicReflectionsScale, _waterInstance.AnisotropicReflectionsScale, 0.1f, 1.0f,
                                                                    link.AnisotropicReflectionsScale);

            }

            
            
            Line();

            _waterInstance.OverrideSkyColor = Toggle("Override Sky Color", "", _waterInstance.OverrideSkyColor, link.OverrideSkyColor);
            if (_waterInstance.OverrideSkyColor)
            {
                _waterInstance.CustomSkyColor = ColorField("Custom Sky Color", "", _waterInstance.CustomSkyColor, false, false, false, link.OverrideSkyColor);
            }

            _waterInstance.ReflectSun = Toggle("Reflect Sunlight", Description.Reflection.ReflectSun, _waterInstance.ReflectSun, link.ReflectSun);
            if (_waterInstance.ReflectSun)
            {
                _waterInstance.ReflectedSunCloudinessStrength = Slider("Sun Cloudiness", Description.Reflection.ReflectedSunCloudinessStrength, _waterInstance.ReflectedSunCloudinessStrength, 0.03f, 0.25f,
                                                                       link.ReflectedSunCloudinessStrength);
                _waterInstance.ReflectedSunStrength = Slider("Sun Strength", Description.Reflection.ReflectedSunStrength, _waterInstance.ReflectedSunStrength, 0f, 1f, link.ReflectedSunStrength);
            }

            CheckPlatformSpecificMessages_Reflection();

            //KWS_EditorProfiles.PerfomanceProfiles.Reflection.CheckDataChangesAnsSetCustomProfile(_waterInstance);
        }
        
        
        void RefractionSetting()
        {
            _waterInstance.RefractionDispersion = (WaterQualityLevelSettings.QualityOverrideEnum)EnumPopup("Dispersion", Description.Refraction.UseRefractionDispersion, _waterInstance.RefractionDispersion, link.UseRefractionDispersion);

            
            if (_waterInstance.RefractionMode == WaterQualityLevelSettings.RefractionModeEnum.PhysicalAproximationIOR)
            {
                _waterInstance.RefractionAproximatedDepth = Slider("Aproximated Depth", Description.Refraction.RefractionAproximatedDepth, _waterInstance.RefractionAproximatedDepth, 0.25f, 10f, link.RefractionAproximatedDepth);
            }

            if (_waterInstance.RefractionMode == WaterQualityLevelSettings.RefractionModeEnum.Simple)
            {
                _waterInstance.RefractionSimpleStrength = Slider("Strength", Description.Refraction.RefractionSimpleStrength, _waterInstance.RefractionSimpleStrength, 0.02f, 1, link.RefractionSimpleStrength);
            }

            if (_waterInstance.RefractionDispersion != WaterQualityLevelSettings.QualityOverrideEnum.Off)
            {
                _waterInstance.RefractionDispersionStrength = Slider("Dispersion Strength", Description.Refraction.RefractionDispersionStrength, _waterInstance.RefractionDispersionStrength, 0.25f, 1,
                                                                     link.RefractionDispersionStrength);
            }

        }


        void FoamSetting()
        {
            _waterInstance.OceanFoam = (WaterQualityLevelSettings.QualityOverrideEnum)EnumPopup("Ocean Foam", "", _waterInstance.OceanFoam, "");

            _waterInstance.OceanFoamStrength                 = Slider("Ocean Foam Strength",        "", _waterInstance.OceanFoamStrength,       0.0f, 1,  link.OceanFoamStrength, false);
            _waterInstance.OceanFoamDisappearSpeedMultiplier = Slider("Ocean Foam Disappear Speed", "", _waterInstance.OceanFoamDisappearSpeedMultiplier, 0.25f,    1f, "",                     false);
            //_waterInstance.OceanFoamTextureSize = Slider("Ocean Foam Texture Size", "", _waterInstance.OceanFoamTextureSize, 5, 50, link.TextureFoamSize, false);
        }

        void WetSetting()
        { 
            _waterInstance.WetEffect = (WaterQualityLevelSettings.QualityOverrideEnum)EnumPopup("Wet Effect", "", _waterInstance.WetEffect, "");

#if KWS_URP
            if (!KWS_CustomSettingsProvider.IsDecalFeatureUsed)
            {
                EditorGUILayout.HelpBox("To make the \"Wet Effect\" work, you need to enable the URP Decal Rendering Feature." + Environment.NewLine +
                                        "Check the documentation for how to set it up.", MessageType.Error);
            }
                
#endif
            _waterInstance.WetStrength             = Slider("Wet Strength",           "", _waterInstance.WetStrength, 0.1f, 1.0f, "");
            _waterInstance.WetnessHeightAboveWater = Slider("Wet Height Above Water", "", _waterInstance.WetnessHeightAboveWater, 0, 3.0f, "");
        }
        
        void VolumetricLightingSettings()
        {  
            CheckPlatformSpecificMessages_VolumeLight();
            _waterInstance.VolumetricLighting = (WaterQualityLevelSettings.QualityOverrideEnum)EnumPopup("Volumetric Lighting", "", _waterInstance.VolumetricLighting, "");
            
            _waterInstance.VolumetricLightTemporalReprojectionAccumulationFactor = Slider("Temporal Accumulation Factor", "", _waterInstance.VolumetricLightTemporalReprojectionAccumulationFactor, 0.1f, 0.75f, link.VolumetricLightTemporalAccumulationFactor);
            _waterInstance.VolumetricLightUseBlur = Toggle("Use Blur", "", _waterInstance.VolumetricLightUseBlur, "");
            if (_waterInstance.VolumetricLightUseBlur) _waterInstance.VolumetricLightBlurRadius = Slider("Blur Radius", "", _waterInstance.VolumetricLightBlurRadius, 1f, 3f, "");
            Line();

            if (_waterInstance.VolumetricLightUseAdditionalLightsCaustic) EditorGUILayout.HelpBox("AdditionalLightsCaustic with multiple light sources can cause dramatic performance drop.", MessageType.Warning);
            _waterInstance.VolumetricLightUseAdditionalLightsCaustic = Toggle("Use Additional Lights Caustic", "", _waterInstance.VolumetricLightUseAdditionalLightsCaustic, link.VolumetricLightUseAdditionalLightsCaustic);
        }


        void CausticSettings()
        {
            _waterInstance.CausticEffect   = (WaterQualityLevelSettings.QualityOverrideEnum)EnumPopup("Caustic Effect", "", _waterInstance.CausticEffect, "");
            _waterInstance.CausticDepth    = Slider("Caustic Depth(m)", Description.Caustic.CausticDepthScale, _waterInstance.CausticDepth,    0.5f,  Caustic.MaxCausticDepth, link.CausticDepthScale);
            _waterInstance.CausticStrength = Slider("Caustic Strength", Description.Caustic.CausticStrength,   _waterInstance.CausticStrength, 0.25f, 5,                       link.CausticStrength);
        }
        
        
        void UnderwaterSettings()
        { 
            _waterInstance.UnderwaterEffect   = (WaterQualityLevelSettings.QualityOverrideEnum)EnumPopup("Underwater Effect", "", _waterInstance.UnderwaterEffect, "");
            
            _waterInstance.UnderwaterReflectionMode      = (WaterQualityLevelSettings.UnderwaterReflectionModeEnum)EnumPopup("Internal Reflection Mode", "", _waterInstance.UnderwaterReflectionMode, link.UnderwaterReflectionMode);
           
            _waterInstance.UseUnderwaterHalfLineTensionEffect = Toggle("Use Half Line Tension Effect", "", _waterInstance.UseUnderwaterHalfLineTensionEffect, link.UnderwaterHalfLineTensionEffect);
            if (_waterInstance.UseUnderwaterHalfLineTensionEffect) _waterInstance.UnderwaterHalfLineTensionScale = Slider("Tension Scale", "", _waterInstance.UnderwaterHalfLineTensionScale, 0.2f, 1f, link.TensionScale);
            _waterInstance.UseWaterDropsEffect = Toggle("Use Water Drops Effect", "", _waterInstance.UseWaterDropsEffect, link.Default);

            
            _waterInstance.OverrideUnderwaterTransparent = Toggle("Override Transparent", "", _waterInstance.OverrideUnderwaterTransparent, link.OverrideUnderwaterTransparent);
            if (_waterInstance.OverrideUnderwaterTransparent)
            {
                _waterInstance.UnderwaterTransparentOffset = Slider("Transparent Offset", Description.Color.Transparent, _waterInstance.UnderwaterTransparentOffset, -100, 100, link.Transparent);
            }

        }

        
              
        void CheckPlatformSpecificMessages_Reflection()
        {  
            //var _settings = KWS_WaterSettingsRuntimeLoader._waterQualityLevelSettings;
            //if (_waterInstance.Settings.ReflectSun)
            //{
            //    if (KWS_WaterLights.Lights.Count == 0 || KWS_WaterLights.Lights.Count(l => l.Light.type == LightType.Directional) == 0)
            //    {
            //        EditorGUILayout.HelpBox("'Water->Reflection->Reflect Sunlight' doesn't work because no directional light has been added for water rendering! Add the script 'AddLightToWaterRendering' to your directional light!", MessageType.Error);
            //    }
            //}

#if KWS_BUILTIN || KWS_URP
            if (ReflectionProbe.defaultTexture.width == 1 && _waterInstance.OverrideSkyColor == false)
            {
                EditorGUILayout.HelpBox("Sky reflection doesn't work in this scene, you need to generate scene lighting! " + Environment.NewLine +
                                        "Open the \"Lighting\" window -> select the Generate Lighting option Reflection Probes", MessageType.Error);
            }
#endif
        }

        void CheckPlatformSpecificMessages_VolumeLight()
        {
            //if (_waterInstance.Settings.UseVolumetricLight && KWS_WaterLights.Lights.Count == 0) EditorGUILayout.HelpBox("Water->'Volumetric lighting' doesn't work because no lights has been added for water rendering! Add the script 'AddLightToWaterRendering' to your light.", MessageType.Error);
        }
    }
}

#endif