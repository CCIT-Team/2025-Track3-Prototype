using System.IO;
using System.Linq;

using UnityEngine;


namespace KWS
{
    public static class KWS_WaterSettingsRuntimeLoader
    {
        internal static WaterQualityLevelSettings  _waterQualityLevelSettings;
        internal static WaterSystemQualitySettings _waterSystemQualitySettings;
        
        #if UNITY_EDITOR
            internal static                             UnityEditor.SerializedObject           _settingsObject;
        #endif
        
        public const    string                    settingsName = "WaterQualitySettings.asset";
        
      
        public static void LoadWaterSettings()
        {
            #if UNITY_EDITOR
                LoadEditorWaterSettings();
            #else
                if (_waterSystemQualitySettings == null || _waterQualityLevelSettings == null)
                {
                    LoadRuntimeSettings(out _waterSystemQualitySettings, out _waterQualityLevelSettings);
                }
            #endif
        }
        
        static void  LoadRuntimeSettings(out WaterSystemQualitySettings settingsContainer, out WaterQualityLevelSettings currentSettings)
        {
            settingsContainer = null;
            currentSettings   = null;
            
            settingsContainer = Resources.Load<WaterSystemQualitySettings>("WaterQualitySettings");
            if (!settingsContainer)
            {
                Debug.LogError("WaterQualitySettings.asset not found in Resources.");
                return;
            }

            var levelName       = QualitySettings.names[QualitySettings.GetQualityLevel()];
            
            foreach (var setting in settingsContainer.qualityLevelSettings)
            {
                if (setting.levelName == levelName)
                {
                    currentSettings = setting;
                    break;
                }
            }

            if (currentSettings == null)
            {
                Debug.LogWarning($"No settings found for quality level '{levelName}'.");
            }

        }

  
        static void LoadEditorWaterSettings()
        {
#if UNITY_EDITOR

            if (_waterSystemQualitySettings == null || _waterQualityLevelSettings == null)
            {
                LoadRuntimeSettings(out _waterSystemQualitySettings, out _waterQualityLevelSettings);
            }

            if (!_waterSystemQualitySettings || _waterSystemQualitySettings.qualityLevelSettings.Count == 0 || _waterSystemQualitySettings.qualityLevelSettings.Count != QualitySettings.names.Length)
            {
                GetSerializedSettings();
                SyncWithUnityQualityLevels();
                LoadRuntimeSettings(out _waterSystemQualitySettings, out KWS_WaterSettingsRuntimeLoader._waterQualityLevelSettings);
            }
#endif
        }


        internal static void GetOrCreateSettings()
        {
#if UNITY_EDITOR     
            var pathToResourcesFolder = KW_Extensions.GetFullPathToResourcesFolder();
            if (string.IsNullOrEmpty(pathToResourcesFolder)) return;

            var pathToSettingsFile = Path.Combine(pathToResourcesFolder.GetRelativeToAssetsPath(), settingsName);
            _waterSystemQualitySettings = Resources.Load<WaterSystemQualitySettings>("WaterQualitySettings");
            if (_waterSystemQualitySettings) return;
            
            _waterSystemQualitySettings = ScriptableObject.CreateInstance<WaterSystemQualitySettings>();
            UnityEditor.AssetDatabase.CreateAsset(_waterSystemQualitySettings, pathToSettingsFile);
            UnityEditor.AssetDatabase.SaveAssets();
              

            foreach (UnityEditor.SceneView sv in UnityEditor.SceneView.sceneViews)
                sv.ShowNotification(new GUIContent($"The water settings file is saved in {pathToSettingsFile}"), 6);

                
            Debug.Log($"The water settings file is saved in {pathToSettingsFile}");

#endif
        }

        public static void GetSerializedSettings()
        {
#if UNITY_EDITOR  
            GetOrCreateSettings();
            _settingsObject = new UnityEditor.SerializedObject(_waterSystemQualitySettings);
#endif
        }


        
        public static void SyncWithUnityQualityLevels()
        {
            var unityLevels = QualitySettings.names;

            foreach (var level in unityLevels)
            {
                if (!_waterSystemQualitySettings.qualityLevelSettings.Exists(x => x.levelName == level))
                {
                    var newQualityLevel = new WaterQualityLevelSettings { levelName = level };
                    SyncDefualtQualitySettings(newQualityLevel, level);
                    _waterSystemQualitySettings.qualityLevelSettings.Add(newQualityLevel);
                }
            }

            _waterSystemQualitySettings.qualityLevelSettings.RemoveAll(x => !unityLevels.Contains(x.levelName));
        }

        public static void SyncDefualtQualitySettings(WaterQualityLevelSettings settings, string level)
        {
            level                                           = level.Replace(" ", "");
            settings.ScreenSpaceReflectionResolutionQuality = KW_Extensions.StringToEnum(level, WaterQualityLevelSettings.ScreenSpaceReflectionResolutionQualityEnum.High);
            settings.VolumetricLightResolutionQuality       = KW_Extensions.StringToEnum(level, WaterQualityLevelSettings.VolumetricLightResolutionQualityEnum.High);
            settings.CausticTextureResolutionQuality        = KW_Extensions.StringToEnum(level, WaterQualityLevelSettings.CausticTextureResolutionQualityEnum.High);
            settings.WaterMeshDetailing                     = KW_Extensions.StringToEnum(level, WaterQualityLevelSettings.WaterMeshQualityEnum.High);
        }

    }
}