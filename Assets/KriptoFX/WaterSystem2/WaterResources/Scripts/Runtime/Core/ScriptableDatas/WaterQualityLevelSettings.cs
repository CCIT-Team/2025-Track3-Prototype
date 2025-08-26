using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace KWS
{
    [Serializable]
    public class WaterQualityLevelSettings
    {
        public int _version;

        public string levelName;

        //dynamic waves
        public bool UseDynamicWaves           = true;
        public bool UseDynamicWavesWetEffect  = true;
        public bool UseDetailedMeshAtDistance = false;
        
        //Reflection settings
        public bool UseScreenSpaceReflection = true;
        public ScreenSpaceReflectionResolutionQualityEnum ScreenSpaceReflectionResolutionQuality = ScreenSpaceReflectionResolutionQualityEnum.High;
        public bool UseScreenSpaceReflectionSky = true;
        public float ScreenSpaceBordersStretching = 0.025f;

        public bool UsePlanarReflection = false;
        public int PlanarCullingMask = ~0;
        public PlanarReflectionResolutionQualityEnum PlanarReflectionResolutionQuality = PlanarReflectionResolutionQualityEnum.Medium;
        public float ReflectionClipPlaneOffset = 0.0005f;
        public bool RenderPlanarShadows = false;
        public bool RenderPlanarVolumetricsAndFog = false;
        public bool RenderPlanarClouds = false;

        //public bool UseReflectionProbes = false;

        public bool  UseAnisotropicReflections         = true;
        public bool  AnisotropicReflectionsHighQuality = false;
       
     
        //Refraction settings
     
        public RefractionResolutionEnum RefractionResolution    = RefractionResolutionEnum.Half;
        public bool                     UseRefractionDispersion = true;
        //Foam settings
        public bool UseOceanFoam = true;

        //Wet effect settings

        public bool UseWetEffect = true;
        
      
        //Volumetric settings
        public bool                                 UseVolumetricLight                        = true;
        public VolumetricLightResolutionQualityEnum VolumetricLightResolutionQuality          = VolumetricLightResolutionQualityEnum.High;
        public int                                  VolumetricLightIteration                  = 8;
        public bool                                 VolumetricLightUseAdditionalLightsCaustic = false;


        //Caustic settings
        public bool UseCausticEffect = true;
        public bool UseCausticHighQualityFiltering = false;
        public CausticTextureResolutionQualityEnum CausticTextureResolutionQuality = CausticTextureResolutionQualityEnum.High;
        
        public bool UseCausticDispersion = true;
      


        //Underwater settings
        public bool UseUnderwaterEffect = true;
   

        //Mesh settings
        public int MeshDetailingFarDistance = 2000;
        public WaterMeshQualityEnum WaterMeshDetailing = WaterMeshQualityEnum.High;

        //Rendering settings
        public int WaterTransparentSortingPriority = -1;
        public bool DrawToPosteffectsDepth;
        public bool WideAngleCameraRenderingMode;

 
        public enum FftWavesQualityEnum
        {
            //Extreme = 512,
            Ultra   = 256,
            High    = 128,
            Medium  = 64,
            Low     = 32
        }

        public enum PlanarReflectionResolutionQualityEnum
        {
            Extreme = 1024,
            Ultra   = 768,
            High    = 512,
            Medium  = 368,
            Low     = 256,
            VeryLow = 128
        }

        /// <summary>
        /// Resolution quality in percent relative to current screen size. For example Medium quality = 35, it's mean ScreenSize * (35 / 100)
        /// </summary>
        public enum ScreenSpaceReflectionResolutionQualityEnum
        {
            Extreme = 100,
            Ultra   = 75,
            High    = 50,
            Medium  = 35,
            Low     = 25,
            VeryLow = 20,
        }

        public enum RefractionModeEnum
        {
            Simple,
            PhysicalAproximationIOR
        }

        public enum RefractionResolutionEnum
        {
            Full = 100,
            Half = 50,
            Quarter = 25
        }

        public enum VolumetricLightResolutionQualityEnum
        {
            Extreme = 75,
            Ultra   = 50,
            High    = 40,
            Medium  = 30,
            Low     = 20,
            VeryLow = 15,
        }
        public enum CausticTextureResolutionQualityEnum
        {
            Extreme = 1536,
            Ultra   = 1024,
            High    = 768,
            Medium  = 512,
            Low     = 256,
            VeryLow = 128
        }
        public enum UnderwaterReflectionModeEnum
        {
            NoInternalReflection,
            PhysicalAproximatedReflection
        }
        public enum WaterMeshQualityEnum
        {
            Ultra,
            High,
            Medium,
            Low,
            VeryLow,
        }

        public enum QualityOverrideEnum
        {
            UseQualitySettings,
            Off
        }

        public static bool ResolveQualityOverride(QualityOverrideEnum overrideValue, bool qualitySetting)
        {
            return overrideValue switch
            {
                QualityOverrideEnum.UseQualitySettings => qualitySetting,
                //QualityOverrideEnum.On                 => true,
                QualityOverrideEnum.Off                => false,
                _                                      => qualitySetting 
            };
        }
    }
}