#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static KWS.KWS_EditorUtils;
using Description = KWS.KWS_EditorTextDescription;
using link = KWS.KWS_EditorUrlLinks;

namespace KWS
{
    [CustomEditor(typeof(KWS_DynamicWavesSimulationZone))]
    public class KWS_EditorDynamicWavesSimulationZones : Editor
    {
        private KWS_DynamicWavesSimulationZone _target;

        private static  string _usedSelectedCachePath;
        
        public override void OnInspectorGUI()
        {
            _target      = (KWS_DynamicWavesSimulationZone)target;
            
            Undo.RecordObject(_target, "Changed Dynamic Waves Simulation Zone");
            
            EditorGUI.BeginChangeCheck();
            EditorGUIUtility.labelWidth = 220;
          
            bool defaultVal       = false;
            EditorGUILayout.Space(20);  
          
            KWS2_Tab(ref _target.ShowSimulationSettings, false, false, ref defaultVal, "Simulation Settings", SimulationSettings, WaterSystem.WaterSettingsCategory.SimulationZone, foldoutSpace: 14);
            KWS2_TabWithEnabledToogle(ref _target.UseFoamParticles, ref _target.ShowFoamParticlesSettings, useExpertButton: false, ref defaultVal, "Foam Particles", FoamParticlesSettings, WaterSystem.WaterSettingsCategory.SimulationZone, foldoutSpace: 14);
            KWS2_TabWithEnabledToogle(ref _target.UseSplashParticles, ref _target.ShowFoamParticlesSettings, useExpertButton: false, ref defaultVal, "Splash Particles", SplashParticlesSettings, WaterSystem.WaterSettingsCategory.SimulationZone, foldoutSpace: 14);

            if(_target.ZoneType != KWS_DynamicWavesSimulationZone.SimulationZoneTypeMode.MovableZone) BakeSettings();

            if (EditorGUI.EndChangeCheck())
            {
                _target.ValueChanged();
                EditorUtility.SetDirty(_target);
            }

        }

        private void BakeSettings()
        {  
            EditorGUILayout.Space(20);

            var isHorizontalUsed = false;
            EditorGUILayout.BeginHorizontal();
            isHorizontalUsed = true;
            
            EditorGUI.BeginChangeCheck();
            
                GUI.enabled = _target.IsBakeMode == false;
                    var isStartBakingPressed = GUILayout.Toggle(false, "Start Cache", "Button");
                GUI.enabled = true;
                
            if (EditorGUI.EndChangeCheck())
            {
                if (isStartBakingPressed)
                {
                    if (EditorUtility.DisplayDialog("Start Precomputation?", "This will overwrite the existing simulation cache. Do you want to continue?",
                                                    "Start", "Cancel"))
                    {
                        ClearSimulationCache();
                        StartBaking(_target);
                    }
                }
            }
            
            EditorGUI.BeginChangeCheck();
            
                GUI.enabled = _target.IsBakeMode == true;
                    var isEndBakingPresset  = GUILayout.Toggle(false, "Stop & Save", "Button");
                GUI.enabled = true;
                
            if (EditorGUI.EndChangeCheck())
            {
                if (isEndBakingPresset)
                {
                    StopBaking(_target);
                }
            }
            
            
            EditorGUI.BeginChangeCheck();
            var isClearCache = GUILayout.Toggle(false, "Clear Cache", "Button");
            if (EditorGUI.EndChangeCheck())
            {
                if (isClearCache && EditorUtility.DisplayDialog("Confirm Deletion",
                                                            "Are you sure you want to delete the precomputed cache textures?",
                                                            "Yes",
                                                            "Cancel"))
                {
                    
                    ClearSimulationCache();
                    _target.ForceUpdateZone();
                }
                
            }
            
            WaterSystem.Instance.AutoUpdateIntersections = GUILayout.Toggle(WaterSystem.Instance.AutoUpdateIntersections, "Auto Update Intersections", "Button");
            
            if(isHorizontalUsed) EditorGUILayout.EndHorizontal();
           
            EditorGUILayout.LabelField("Cache Path:  " + _usedSelectedCachePath, KWS_EditorUtils.NotesLabelStyleFade);
            
        }

        void SimulationSettings()
        {
            var isBakedSim = _target.SavedDepth != null;

            if (isBakedSim)
            {
                GUI.enabled = false;
                EditorGUILayout.HelpBox("You can't change some parameters of a precomputed simulation. " + Environment.NewLine +
                                        "Clear the simulation or recompute it again with new parameters", MessageType.Info);
            }
            _target.ZoneType = (KWS_DynamicWavesSimulationZone.SimulationZoneTypeMode)EnumPopup("Zone Type", "", _target.ZoneType, "");
            if (_target.ZoneType == KWS_DynamicWavesSimulationZone.SimulationZoneTypeMode.MovableZone)
            {
                _target.FollowObject = (GameObject)EditorGUILayout.ObjectField(_target.FollowObject, typeof(GameObject), true);
            }
            
            var layerNames = new List<string>();
            for (int i = 0; i <= 31; i++)
            {
                var maskName = LayerMask.LayerToName(i);
                if(maskName != String.Empty) layerNames.Add(maskName);
            }
            _target.IntersectionLayerMask        = MaskField("Intersection Layer Mask", "", _target.IntersectionLayerMask, layerNames.ToArray(), "");
            _target.SimulationResolutionPerMeter = Slider("Simulation Resolution Per Meter", "", _target.SimulationResolutionPerMeter, 2, 3, "", false);
            
            if (isBakedSim)
            {
                GUI.enabled = true;
            }
            
            _target.FlowSpeedMultiplier = Slider("Flow Speed Multiplier", "", _target.FlowSpeedMultiplier, 0.5f, 1.5f, "", false);
            
            Line();
            _target.FoamStrengthRiver     = Slider("Foam Strength River",     "", _target.FoamStrengthRiver,     0.001f, 1.0f, "", false);
            _target.FoamStrengthShoreline = Slider("Foam Strength Shoreline", "", _target.FoamStrengthShoreline, 0.001f, 1.0f, "", false);
            _target.FoamDissappearSpeed   = Slider("Foam Dissappear Speed",   "", _target.FoamDissappearSpeed, 0.1f, 1.0f, "", false);
        }
        
        void FoamParticlesSettings()
        {
           _target.MaxFoamParticlesBudget = (KWS_DynamicWavesSimulationZone.FoamParticlesMaxLimitEnum)EnumPopup("Max Particles Budget", "", _target.MaxFoamParticlesBudget, "");
           _target.FoamParticlesScale = Slider("Particles Scale", "", _target.FoamParticlesScale, 0f, 1f, "", false);
           _target.FoamParticlesAlphaMultiplier = Slider("Particles Alpha Multiplier", "", _target.FoamParticlesAlphaMultiplier, 0f, 1f, "", false);
           _target.RiverEmissionRateFoam = Slider("River Emission Rate", "", _target.RiverEmissionRateFoam, 0f, 1f, "", false);
           _target.ShorelineEmissionRateFoam = Slider("Shoreline Emission Rate", "", _target.ShorelineEmissionRateFoam, 0f, 1f, "", false);
           _target.UsePhytoplanktonEmission = Toggle("Use Phytoplankton Emission", "", _target.UsePhytoplanktonEmission, "", false);
        }
        
        void SplashParticlesSettings()
        {
            _target.MaxSplashParticlesBudget       = (KWS_DynamicWavesSimulationZone.SplashParticlesMaxLimitEnum)EnumPopup("Max Particles Budget", "", _target.MaxSplashParticlesBudget, "");
            _target.SplashParticlesScale           = Slider("Particles Scale",            "", _target.SplashParticlesScale,           0f, 1f, "", false);
            _target.SplashParticlesAlphaMultiplier = Slider("Particles Alpha Multiplier", "", _target.SplashParticlesAlphaMultiplier, 0f, 1f, "", false);
            _target.RiverEmissionRateSplash        = Slider("River Emission Rate",        "", _target.RiverEmissionRateSplash,        0f, 1f, "", false);
            _target.ShorelineEmissionRateSplash    = Slider("Shoreline Emission Rate",    "", _target.ShorelineEmissionRateSplash,    0f, 1f, "", false);
            _target.WaterfallEmissionRateSplash    = Slider("Waterfall Emission Rate",    "", _target.WaterfallEmissionRateSplash,    0f, 1f, "", false);
            
            Line();
            _target.ReceiveShadowMode = (KWS_DynamicWavesSimulationZone.SplashReceiveShadowModeEnum)EnumPopup("Receive Shadow Mode", "", _target.ReceiveShadowMode, "");
            _target.CastShadowMode    = (KWS_DynamicWavesSimulationZone.SplashCasticShadowModeEnum)EnumPopup("Cast Shadow Mode",     "", _target.CastShadowMode, "");
        }

     
        public static void StartBaking(KWS_DynamicWavesSimulationZone zone)
        {
            if (!zone.transform) return;
            //save textures to disk

            zone.IsBakeMode = true;
            
            zone.ForceUpdateZone();
        }

        public static void StopBaking(KWS_DynamicWavesSimulationZone zone)
        {
            zone.IsBakeMode = false;
            
            if (zone._bakeDepthRT)
            {
                SaveBakedTextures(zone);
            }
        }
        
        public static void SetSaveFolderPath(string assetRelativePath)
        {
            _usedSelectedCachePath = assetRelativePath;
        }
        
        public void ClearSimulationCache()
        {
            TryDelete(_target.SavedDepth);
            TryDelete(_target.SavedDistanceField);
            TryDelete(_target.SavedDynamicWavesSimulation);

            _target.SavedDepth                  = null;
            _target.SavedDistanceField          = null;
            _target.SavedDynamicWavesSimulation = null;

            UnityEditor.AssetDatabase.Refresh();
            
        }  
            
        void TryDelete(Texture2D texture)
        {
            if (texture == null) return;

            var path = UnityEditor.AssetDatabase.GetAssetPath(texture);
            if (!string.IsNullOrEmpty(path))
            {
                UnityEditor.AssetDatabase.DeleteAsset(path);
            }
        }

   

        static void UpdateSaveFolderPath(KWS_DynamicWavesSimulationZone zone, bool requireSaveFolderPanel)
        {
            if (zone && zone.SavedDepth)
            {
                if (String.IsNullOrEmpty(_usedSelectedCachePath))
                {
                    _usedSelectedCachePath = UnityEditor.AssetDatabase.GetAssetPath(zone.SavedDepth);
                    if (!String.IsNullOrEmpty(_usedSelectedCachePath))
                    {
                        _usedSelectedCachePath = Path.GetDirectoryName(Path.GetRelativePath("Assets", Path.Combine("Assets", _usedSelectedCachePath)));
                    }
                }
                
            }

            if (requireSaveFolderPanel && String.IsNullOrEmpty(_usedSelectedCachePath))
            {
                _usedSelectedCachePath = UnityEditor.EditorUtility.SaveFolderPanel("Save texture location", _usedSelectedCachePath, "");
            }

        }


        private static void SaveBakedTextures(KWS_DynamicWavesSimulationZone zone)
        {
            UpdateSaveFolderPath(zone, requireSaveFolderPanel: true);
            
            if (String.IsNullOrEmpty(_usedSelectedCachePath))
            {
                return;
            }

            var randFileName = Path.GetRandomFileName().Substring(0, 5).ToUpper();

            var depthPath    = Path.Combine(_usedSelectedCachePath, "DepthTexture_"           + randFileName);
            var sdfDepthPath = Path.Combine(_usedSelectedCachePath, "DistanceFieldTexture_"   + randFileName);
            var simDataPath  = Path.Combine(_usedSelectedCachePath, "DynamicWavesSimulation_" + randFileName);

            zone._bakeDepthRT.SaveRenderTextureDepth32(depthPath);
            zone._bakeDepthSdfRT.SaveRenderTexture(sdfDepthPath);
            zone._simulationData.GetTarget.rt.SaveRenderTexture(simDataPath);

            zone.SavedDepth                  = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(depthPath.GetRelativeToAssetsPath()    + ".kwsTexture");
            zone.SavedDistanceField          = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(sdfDepthPath.GetRelativeToAssetsPath() + ".kwsTexture");
            zone.SavedDynamicWavesSimulation = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(simDataPath.GetRelativeToAssetsPath()  + ".kwsTexture");
        }



        void OnEnable()
        {
            foreach (var iZone in KWS_TileZoneManager.DynamicWavesZones)
            { 
                var zone = (KWS_DynamicWavesSimulationZone)iZone;
                if(zone.SavedDepth) UpdateSaveFolderPath(zone, false);
            }
            
        }

    }
    
    
    
    [InitializeOnLoad]
    static class MoveDetector
    { 
        static HashSet<KWS_TileZoneManager.IWaterZone> pendingZones = new();
        static double                                  lastUpdateTime;
        
        static MoveDetector()
        {
            Undo.postprocessModifications -= OnPostprocessModifications;
            Undo.postprocessModifications += OnPostprocessModifications;
            
            EditorApplication.update -= EditorUpdate;
            EditorApplication.update += EditorUpdate;
        }

        private static UndoPropertyModification[] OnPostprocessModifications(UndoPropertyModification[] modifications)
        {
            if (!WaterSystem.Instance || !WaterSystem.Instance.AutoUpdateIntersections) return modifications;
            
            foreach (var mod in modifications)
            {
                var prop = mod.currentValue;
                if (prop == null || prop.target == null) continue;

                if (prop.target is not Transform tr) continue;
                
                foreach (var dynamicWavesZone in KWS_TileZoneManager.DynamicWavesZones)
                {
                    if (dynamicWavesZone.Bounds.Contains(tr.position))
                    {
                        pendingZones.Add(dynamicWavesZone);
                    }
                }
            }

            
            return modifications;
        } 
        
        private static void EditorUpdate()
        {
            if (pendingZones.Count == 0) return;
           
            double time = EditorApplication.timeSinceStartup;
            if (time - lastUpdateTime < 0.5) return;

            foreach (var iZone in pendingZones)
            {
                var zone = (KWS_DynamicWavesSimulationZone)iZone;
                if (zone && zone.ZoneType != KWS_DynamicWavesSimulationZone.SimulationZoneTypeMode.MovableZone)
                {
                    zone.ForceUpdateZone(false);
                }
            }

            pendingZones.Clear();
            lastUpdateTime = time;
        }
    }
}

#endif