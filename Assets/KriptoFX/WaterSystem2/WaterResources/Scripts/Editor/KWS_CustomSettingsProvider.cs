using System.IO;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UIElements;


using Description = KWS.KWS_EditorTextDescription;
using link = KWS.KWS_EditorUrlLinks;
using static KWS.KWS_Settings;
using System;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
using static KWS.KWS_EditorUtils;
#endif


namespace KWS
{
    

#if UNITY_EDITOR
    
    
    class KWS_CustomSettingsProvider : SettingsProvider 
    {
       
      
        internal static bool                       IsDecalFeatureUsed;

        private KWS_CustomSettingsProvider(string path, SettingsScope scope = SettingsScope.Project) : base(path, scope) { }
        
        [SettingsProvider]
        public static SettingsProvider CreateMyCustomSettingsProvider()
        {
            return new KWS_CustomSettingsProvider("Project/KWS Water Settings", SettingsScope.Project);
        }
        
        
        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            KWS_WaterSettingsRuntimeLoader.GetSerializedSettings();
            KWS_WaterSettingsRuntimeLoader.SyncWithUnityQualityLevels();
        }

       
        public override void OnGUI(string searchContext)
        {
            bool defaultVal = false;

            var _settingsContainer = KWS_WaterSettingsRuntimeLoader._waterSystemQualitySettings;

            Undo.RecordObject(_settingsContainer, "Changed water quality parameters");

            EditorGUIUtility.labelWidth = 80;
            GUILayout.Space(10);
            UnityQualitySettings();
            GUILayout.Space(15);

            EditorGUI.BeginChangeCheck();
            EditorGUIUtility.labelWidth = 220;

            CheckWarnings();

            var _settings = KWS_WaterSettingsRuntimeLoader._waterQualityLevelSettings;

            KWS2_Tab(ref _settingsContainer.ShowReflectionSettings, useHelpBox: true, useExpertButton: true, ref defaultVal, "Reflection", ReflectionSettings, WaterSystem.WaterSettingsCategory.Reflection);
            KWS2_Tab(ref _settingsContainer.ShowRefractionSettings, useHelpBox: true, useExpertButton: false, ref defaultVal, "Refraction (View Through Water)", RefractionSetting, WaterSystem.WaterSettingsCategory.ColorRefraction);
           
            KWS2_TabWithEnabledToogle(ref _settings.UseDynamicWaves, ref _settingsContainer.ShowDynamicWavesSettings, useExpertButton: false, ref defaultVal, "Dynamic Waves", DynamicWavesSettings, WaterSystem.WaterSettingsCategory.DynamicWaves);
            KWS2_TabWithEnabledToogle(ref _settings.UseOceanFoam, ref _settingsContainer.ShowFoamSettings, useExpertButton: false, ref defaultVal, "Ocean Foam", FoamSetting, WaterSystem.WaterSettingsCategory.Foam);
            KWS2_TabWithEnabledToogle(ref _settings.UseWetEffect, ref _settingsContainer.ShowWetSettings, useExpertButton: false, ref defaultVal, "Wet Effect", WetSetting, WaterSystem.WaterSettingsCategory.WetEffect);
            KWS2_TabWithEnabledToogle(ref _settings.UseVolumetricLight, ref _settingsContainer.ShowVolumetricLightSettings, useExpertButton: false, ref defaultVal, "Volumetric Lighting", VolumetricLightingSettings, WaterSystem.WaterSettingsCategory.VolumetricLighting);
            KWS2_TabWithEnabledToogle(ref _settings.UseCausticEffect, ref _settingsContainer.ShowCausticEffectSettings, useExpertButton: false, ref defaultVal, "Caustic (Light Patterns on Surfaces)", CausticSettings, WaterSystem.WaterSettingsCategory.Caustic);
            KWS2_TabWithEnabledToogle(ref _settings.UseUnderwaterEffect, ref _settingsContainer.ShowUnderwaterEffectSettings, useExpertButton: false, ref defaultVal, "Underwater Effects", UnderwaterSettings, WaterSystem.WaterSettingsCategory.Underwater);
            KWS2_Tab(ref _settingsContainer.ShowMeshSettings, useHelpBox: true, useExpertButton: false, ref defaultVal, "Mesh Settings", MeshSettings, WaterSystem.WaterSettingsCategory.Mesh);
            KWS2_Tab(ref _settingsContainer.ShowRendering, useHelpBox: true, useExpertButton: false, ref defaultVal, "Rendering Settings", RenderingSetting, WaterSystem.WaterSettingsCategory.Rendering);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_settingsContainer);
                // AssetDatabase.SaveAssets();
            }

            var _settingsObject = KWS_WaterSettingsRuntimeLoader._settingsObject;
            if (_settingsObject != null && _settingsObject.targetObject)
            {
                _settingsObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }


        void CheckWarnings()
        {
#if KWS_URP
            //if (!IsDecalFeatureUsed)
            {
                EditorGUILayout.HelpBox("To make the \"Wet Effect\" work, you need to enable the URP Decal Rendering Feature." + Environment.NewLine +
                                        "Check the documentation for how to set it up.", MessageType.Info);
            }
                
#endif

        }
        
        
        void UnityQualitySettings()
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(10);
            GUILayout.Label("Unity Quality Levels", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            GUILayout.Space(3);

            var qualityLevels = QualitySettings.names;
            var currentQualityLevel = QualitySettings.GetQualityLevel();
            var selectedLevel = currentQualityLevel;

            for (int idx = 0; idx < qualityLevels.Length; idx++)
            {
                string level = qualityLevels[idx];
                var newValue = Toggle(level, "", selectedLevel == idx, "", false);
                if (newValue == true) selectedLevel = idx;
            }
            QualitySettings.SetQualityLevel(selectedLevel);
            EditorGUI.indentLevel--;

            var _settingsContainer = KWS_WaterSettingsRuntimeLoader._waterSystemQualitySettings;
            var levelName          = qualityLevels[selectedLevel];
            var levelSettings      = _settingsContainer.qualityLevelSettings.Find(x => x.levelName == levelName);
            if (levelSettings == null)
            {
                levelSettings = new WaterQualityLevelSettings { levelName = levelName };
                _settingsContainer.qualityLevelSettings.Add(levelSettings);

                EditorUtility.SetDirty(_settingsContainer);
                AssetDatabase.SaveAssets();
            }
            KWS_WaterSettingsRuntimeLoader._waterQualityLevelSettings = levelSettings;

            if (currentQualityLevel != selectedLevel) WaterSystem.OnAnyWaterSettingsChanged?.Invoke(WaterSystem.WaterSettingsCategory.All);

        }


        void ReflectionSettings()
        {
            //KWS_EditorProfiles.PerfomanceProfiles.Reflection.ReadDataFromProfile(_waterSystem);
            var _settings          = KWS_WaterSettingsRuntimeLoader._waterQualityLevelSettings;
            var _settingsContainer = KWS_WaterSettingsRuntimeLoader._waterSystemQualitySettings;
            _settings.UseScreenSpaceReflection = Toggle("Use Screen Space Reflection", Description.Reflection.UseScreenSpaceReflection, _settings.UseScreenSpaceReflection, link.UseScreenSpaceReflection);

            if (_settings.UseScreenSpaceReflection)
            {
                _settings.ScreenSpaceReflectionResolutionQuality = (WaterQualityLevelSettings.ScreenSpaceReflectionResolutionQualityEnum)EnumPopup("Screen Space Resolution Quality",
                                                                                                                                                   Description.Reflection.ScreenSpaceReflectionResolutionQuality, _settings.ScreenSpaceReflectionResolutionQuality, link.ScreenSpaceReflectionResolutionQuality);


                {
                    //_settings.UseScreenSpaceReflectionHolesFilling = Toggle("Holes Filling", "", _settings.UseScreenSpaceReflectionHolesFilling, link.UseScreenSpaceReflectionHolesFilling));
                    _settings.UseScreenSpaceReflectionSky = Toggle("Use Screen Space Skybox", "", _settings.UseScreenSpaceReflectionSky, "");
                    _settings.ScreenSpaceBordersStretching = Slider("Borders Stretching", "", _settings.ScreenSpaceBordersStretching, 0f, 0.05f, link.ScreenSpaceBordersStretching);
                }

                Line();
            }



            _settings.UsePlanarReflection = Toggle("Use Planar Reflection", Description.Reflection.UsePlanarReflection, _settings.UsePlanarReflection, link.UsePlanarReflection);
            if (_settings.UsePlanarReflection)
            {
                var layerNames = new List<string>();
                for (int i = 0; i <= 31; i++)
                {
                    layerNames.Add(LayerMask.LayerToName(i));
                }

                EditorGUILayout.HelpBox(Description.Warnings.PlanarReflectionUsed, MessageType.Warning);
                _settings.RenderPlanarShadows = Toggle("Planar Shadows", "", _settings.RenderPlanarShadows, link.RenderPlanarShadows);

                if (Reflection.IsVolumetricsAndFogAvailable)
                    _settings.RenderPlanarVolumetricsAndFog = Toggle("Planar Volumetrics and Fog", "", _settings.RenderPlanarVolumetricsAndFog, link.RenderPlanarVolumetricsAndFog);
                if (Reflection.IsCloudRenderingAvailable) _settings.RenderPlanarClouds = Toggle("Planar Clouds", "", _settings.RenderPlanarClouds, link.RenderPlanarClouds);

                _settings.PlanarReflectionResolutionQuality =
                    (WaterQualityLevelSettings.PlanarReflectionResolutionQualityEnum)EnumPopup("Planar Resolution Quality", Description.Reflection.PlanarReflectionResolutionQuality, _settings.PlanarReflectionResolutionQuality,
                                                                                               link.PlanarReflectionResolutionQuality);

                var planarCullingMask = MaskField("Planar Layers Mask", Description.Reflection.PlanarCullingMask, _settings.PlanarCullingMask, layerNames.ToArray(), link.PlanarCullingMask);
                _settings.PlanarCullingMask = planarCullingMask & ~(1 << Water.WaterLayer);

            }

            if ((_settings.UsePlanarReflection || _settings.UseScreenSpaceReflection))
            {
                _settings.ReflectionClipPlaneOffset = Slider("Clip Plane Offset", Description.Reflection.ReflectionClipPlaneOffset, _settings.ReflectionClipPlaneOffset, 0, 0.07f,
                                                             link.ReflectionClipPlaneOffset);
            }

            if (_settings.UseScreenSpaceReflection || _settings.UsePlanarReflection)
            {
                Line();
                _settings.UseAnisotropicReflections = Toggle("Use Anisotropic Reflections", Description.Reflection.UseAnisotropicReflections, _settings.UseAnisotropicReflections, link.UseAnisotropicReflections);

                if (_settings.UseAnisotropicReflections)
                {
                   
                    _settings.AnisotropicReflectionsHighQuality = Toggle("High Quality Anisotropic", Description.Reflection.AnisotropicReflectionsHighQuality, _settings.AnisotropicReflectionsHighQuality,
                                                                         link.AnisotropicReflectionsHighQuality);
                }

            }


        }

        void RefractionSetting()
        {
            var _settings                                                                  = KWS_WaterSettingsRuntimeLoader._waterQualityLevelSettings;
            if (Refraction.IsRefractionDownsampleAvailable) _settings.RefractionResolution = (WaterQualityLevelSettings.RefractionResolutionEnum)EnumPopup("Resolution", "", _settings.RefractionResolution, link.RefractionResolution);
            _settings.UseRefractionDispersion = Toggle("Use Dispersion", Description.Refraction.UseRefractionDispersion, _settings.UseRefractionDispersion, link.UseRefractionDispersion);
        }


        void DynamicWavesSettings()
        {  
            var _settings = KWS_WaterSettingsRuntimeLoader._waterQualityLevelSettings;
            _settings.UseDetailedMeshAtDistance = Toggle("Use Detailed Mesh At Distance",  "", _settings.UseDetailedMeshAtDistance, "");
        }

        void FoamSetting()
        {  
            var _settings = KWS_WaterSettingsRuntimeLoader._waterQualityLevelSettings;
            if (_settings.UseOceanFoam)
            {
                //if (_waterInstance.Settings.CurrentWindSpeed < 7.1f) EditorGUILayout.HelpBox("Foam appears during strong winds (from ~8 meters and above)", MessageType.Info);
            }

        }

        void WetSetting()
        {
#if KWS_URP
            EditorGUILayout.HelpBox("To make the \"Wet Effect\" work, you need to enable the URP Decal Rendering Feature." + Environment.NewLine +
                                    "Check the documentation for how to set it up.", MessageType.Info);
                
#endif
        }


        void VolumetricLightingSettings()
        {  
            var _settings = KWS_WaterSettingsRuntimeLoader._waterQualityLevelSettings;
            
            _settings.VolumetricLightResolutionQuality =
                (WaterQualityLevelSettings.VolumetricLightResolutionQualityEnum)EnumPopup("Resolution Quality", Description.VolumetricLight.ResolutionQuality, _settings.VolumetricLightResolutionQuality, link.VolumetricLightResolutionQuality);
            _settings.VolumetricLightIteration = IntSlider("Iterations", Description.VolumetricLight.Iterations, _settings.VolumetricLightIteration, 2, KWS_Settings.VolumetricLighting.MaxIterations, link.VolumetricLightIteration);

            if (_settings.VolumetricLightUseAdditionalLightsCaustic) EditorGUILayout.HelpBox("AdditionalLightsCaustic with multiple light sources can cause dramatic performance drop.", MessageType.Warning);
            _settings.VolumetricLightUseAdditionalLightsCaustic = Toggle("Use Additional Lights Caustic", "", _settings.VolumetricLightUseAdditionalLightsCaustic, link.VolumetricLightUseAdditionalLightsCaustic);
        }

        void CausticSettings()
        {
            var   _settings             = KWS_WaterSettingsRuntimeLoader._waterQualityLevelSettings;
            var   size                  = (int)_settings.CausticTextureResolutionQuality;
            float currentRenderedPixels = size * size;
            currentRenderedPixels *= _settings.UseCausticHighQualityFiltering ? 2 : 1;
            currentRenderedPixels = (currentRenderedPixels / 1000000f);
            EditorGUILayout.LabelField("Simulation rendered pixels (less is better): " + currentRenderedPixels.ToString("0.0") + " millions", KWS_EditorUtils.NotesLabelStyleFade);

            _settings.CausticTextureResolutionQuality = (WaterQualityLevelSettings.CausticTextureResolutionQualityEnum)EnumPopup("Caustic Resolution", "", _settings.CausticTextureResolutionQuality, link.CausticTextureSize);
            _settings.UseCausticHighQualityFiltering  = Toggle("Use High Quality Filtering", "",                                       _settings.UseCausticHighQualityFiltering, link.UseCausticBicubicInterpolation);
            _settings.UseCausticDispersion            = Toggle("Use Dispersion",             Description.Caustic.UseCausticDispersion, _settings.UseCausticDispersion,           link.UseCausticDispersion);
        }

        void UnderwaterSettings()
        {
            
        }

        void MeshSettings()
        {
            var _settings = KWS_WaterSettingsRuntimeLoader._waterQualityLevelSettings;
            _settings.WaterMeshDetailing       = (WaterQualityLevelSettings.WaterMeshQualityEnum)EnumPopup("Mesh Detailing", "", _settings.WaterMeshDetailing, link.WaterMeshQualityInfinite);
            _settings.MeshDetailingFarDistance = IntSlider("Mesh Detailing Far Distance", "", _settings.MeshDetailingFarDistance, 500, 5000, link.OceanDetailingFarDistance);

        }

        void RenderingSetting()
        {
            var _settings          = KWS_WaterSettingsRuntimeLoader._waterQualityLevelSettings;
            var _settingsContainer = KWS_WaterSettingsRuntimeLoader._waterSystemQualitySettings;
            
            ReadSelectedThirdPartyFog();
            var selectedThirdPartyFogMethod = WaterSystem.ThirdPartyFogAssetsDescriptions[_settingsContainer.SelectedThirdPartyFogMethod];


            if (selectedThirdPartyFogMethod.CustomQueueOffset != 0)
            {
                EditorGUILayout.LabelField($"Min TransparentSortingPriority overrated by {selectedThirdPartyFogMethod.EditorName}", KWS_EditorUtils.NotesLabelStyleFade);
                _settings.WaterTransparentSortingPriority = IntSlider("Transparent Sorting Priority", "", _settings.WaterTransparentSortingPriority, selectedThirdPartyFogMethod.CustomQueueOffset, 50, link.TransparentSortingPriority);
            }
            else
            {
                _settings.WaterTransparentSortingPriority = IntSlider("Transparent Sorting Priority", "", _settings.WaterTransparentSortingPriority, -50, 50, link.TransparentSortingPriority);
            }

            //_settings.EnabledMeshRendering       = Toggle("Enabled Mesh Rendering", "", _settings.EnabledMeshRendering, link.EnabledMeshRendering), false);

            if (selectedThirdPartyFogMethod.DrawToDepth)
            {
                EditorGUILayout.LabelField($"Draw To Depth override by {selectedThirdPartyFogMethod.EditorName}", KWS_EditorUtils.NotesLabelStyleFade);
                GUI.enabled = false;
                _settings.DrawToPosteffectsDepth = Toggle("Draw To Depth", Description.Rendering.DrawToPosteffectsDepth, true, link.DrawToPosteffectsDepth);
                GUI.enabled = true;
            }
            else
            {
                _settings.DrawToPosteffectsDepth = Toggle("Draw To Depth", Description.Rendering.DrawToPosteffectsDepth, _settings.DrawToPosteffectsDepth, link.DrawToPosteffectsDepth);
            }


            _settings.WideAngleCameraRenderingMode = Toggle("Wide-Angle Camera Mode", "", _settings.WideAngleCameraRenderingMode, link.WideAngleCameraRenderingMode);
            //if (_waterSystem.UseTesselation)
            //{
            //    _waterSystem.WireframeMode = false;
            //    EditorGUILayout.LabelField($"Wireframe mode doesn't work with tesselation (water -> mesh -> use tesselation)", KWS_EditorUtils.NotesLabelStyleFade);
            //    GUI.enabled                           = false;
            //    _waterSystem.WireframeMode = Toggle("Wireframe Mode", "", _waterSystem.WireframeMode, nameof(_waterSystem.WireframeMode));
            //    GUI.enabled = _isActive;
            //}
            //else
            //{
            //    _waterSystem.WireframeMode = Toggle("Wireframe Mode", "", _waterSystem.WireframeMode, nameof(_waterSystem.WireframeMode));
            //}

            var assets = WaterSystem.ThirdPartyFogAssetsDescriptions;
            var fogDisplayedNames = new string[assets.Count + 1];
            for (var i = 0; i < assets.Count; i++)
            {
                fogDisplayedNames[i] = assets[i].EditorName;
            }
            EditorGUI.BeginChangeCheck();
            _settingsContainer.SelectedThirdPartyFogMethod = EditorGUILayout.Popup("Third-Party Fog Support", _settingsContainer.SelectedThirdPartyFogMethod, fogDisplayedNames);
            if (EditorGUI.EndChangeCheck())
            {
                UpdateThirdPartyFog();
            }
            if (_settingsContainer.SelectedThirdPartyFogMethod != 0 && !_settingsContainer.IsThirdPartyFogAvailable)
            {
                EditorGUILayout.HelpBox($"Can't find the asset {WaterSystem.ThirdPartyFogAssetsDescriptions[_settingsContainer.SelectedThirdPartyFogMethod].EditorName}", MessageType.Error);
            }
#if KWS_DEBUG
            Line();

            //if (_settings.WaterMeshType == WaterMeshTypeEnum.InfiniteOcean || _settings.WaterMeshType == WaterMeshTypeEnum.FiniteBox)
            //{
            //    WaterSystem.DebugQuadtree = Toggle("Debug Quadtree", "", WaterSystem.DebugQuadtree, "");
            //}
            //_waterInstance.DebugAABB = Toggle("Debug AABB", "", _waterInstance.DebugAABB, "");
            //_waterInstance.DebugFft = Toggle("Debug Fft", "", _waterInstance.DebugFft, "");
            //_waterInstance.DebugDynamicWaves = Toggle("Debug Dynamic Waves", "", _waterInstance.DebugDynamicWaves, "");
            //_waterInstance.DebugOrthoDepth = Toggle("Debug Ortho Depth", "", _waterInstance.DebugOrthoDepth, "");
            //_waterInstance.DebugBuoyancy = Toggle("Debug Buoyancy", "", _waterInstance.DebugBuoyancy, "");
            //WaterSystem.DebugUpdateManager = Toggle("Debug Update Manager", "", WaterSystem.DebugUpdateManager, "");
            //Line();
#endif

        }

        void ReadSelectedThirdPartyFog()
        { 
            var _settingsContainer = KWS_WaterSettingsRuntimeLoader._waterSystemQualitySettings;
            //load enabled third-party asset for all water instances
            if (_settingsContainer.SelectedThirdPartyFogMethod == -1)
            {
                var defines = WaterSystem.ThirdPartyFogAssetsDescriptions.Select(n => n.ShaderDefine).ToList<string>();
                _settingsContainer.SelectedThirdPartyFogMethod = KWS_EditorUtils.GetEnabledDefineIndex(ShaderPaths.KWS_PlatformSpecificHelpers, defines);
            }

        }

       
        void UpdateThirdPartyFog()
        {  
            var _settings          = KWS_WaterSettingsRuntimeLoader._waterQualityLevelSettings;
            var _settingsContainer = KWS_WaterSettingsRuntimeLoader._waterSystemQualitySettings;
            
            if (_settingsContainer.SelectedThirdPartyFogMethod > 0)
            {
                var selectedMethod = WaterSystem.ThirdPartyFogAssetsDescriptions[_settingsContainer.SelectedThirdPartyFogMethod];
                if (!selectedMethod.IgnoreInclude)
                {
                    var inlcudeFileName = KW_Extensions.GetAssetsRelativePathToFile(selectedMethod.ShaderInclude, selectedMethod.AssetNameSearchPattern);
                    if (String.IsNullOrEmpty(inlcudeFileName))
                    {
                        _settingsContainer.IsThirdPartyFogAvailable = false;
                        Debug.LogError($"Can't find the asset {WaterSystem.ThirdPartyFogAssetsDescriptions[_settingsContainer.SelectedThirdPartyFogMethod].EditorName}");
                        return;
                    }
                    else _settingsContainer.IsThirdPartyFogAvailable = true;
                }
            }

            //replace defines
            for (int i = 1; i < WaterSystem.ThirdPartyFogAssetsDescriptions.Count; i++)
            {
                var selectedMethod = WaterSystem.ThirdPartyFogAssetsDescriptions[i];
                SetShaderTextDefine(ShaderPaths.KWS_PlatformSpecificHelpers, false, selectedMethod.ShaderDefine, _settingsContainer.SelectedThirdPartyFogMethod == i);
            }

            //replace paths to assets
            if (_settingsContainer.SelectedThirdPartyFogMethod > 0)
            {
                var selectedMethod = WaterSystem.ThirdPartyFogAssetsDescriptions[_settingsContainer.SelectedThirdPartyFogMethod];
                if (!selectedMethod.IgnoreInclude)
                {
                    var inlcudeFileName = KW_Extensions.GetAssetsRelativePathToFile(selectedMethod.ShaderInclude, selectedMethod.AssetNameSearchPattern);
                    KWS_EditorUtils.ChangeShaderTextIncludePath(KWS_Settings.ShaderPaths.KWS_PlatformSpecificHelpers, selectedMethod.ShaderDefine, inlcudeFileName);
                }
            }
        
            var thirdPartySelectedFog = WaterSystem.ThirdPartyFogAssetsDescriptions[_settingsContainer.SelectedThirdPartyFogMethod];
            if (thirdPartySelectedFog.DrawToDepth) _settings.DrawToPosteffectsDepth = true;

            AssetDatabase.Refresh();

            var go = WaterSystem.Instance?.GetComponent<GameObject>();
            if (go)
            {
               
                go.SetActive(false);
                go.SetActive(true);
            }
        }


    }
#endif
}