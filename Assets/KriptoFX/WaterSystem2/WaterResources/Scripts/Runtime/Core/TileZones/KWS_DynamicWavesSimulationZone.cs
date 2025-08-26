using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

#if UNITY_EDITOR
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("KriptoFX.KWS2.Editor")]
#endif

namespace KWS
{
    [ExecuteInEditMode]
    public class KWS_DynamicWavesSimulationZone : MonoBehaviour, KWS_TileZoneManager.IWaterZone 
    {
        public SimulationZoneTypeMode ZoneType = SimulationZoneTypeMode.StaticZone;
        public GameObject             FollowObject;
        public LayerMask              IntersectionLayerMask        = ~(1 << KWS_Settings.Water.WaterLayer);
        public float                  SimulationResolutionPerMeter = 2.5f;
        public float                  FlowSpeedMultiplier          = 1.0f;
        public float                  FoamStrengthRiver            = 0.01f;
        public float                  FoamStrengthShoreline        = 0.2f;
        public float                  FoamDissappearSpeed           = 1f;

        public bool                      UseFoamParticles             = true;
        public FoamParticlesMaxLimitEnum MaxFoamParticlesBudget       = FoamParticlesMaxLimitEnum._500k;
        public float                     FoamParticlesScale           = 0.7f;
        public float                     FoamParticlesAlphaMultiplier = 0.4f;
        public float                     RiverEmissionRateFoam        = 0.5f;
        public float                     ShorelineEmissionRateFoam    = 0.5f;
        public bool                      UsePhytoplanktonEmission     = false;

        public bool                        UseSplashParticles             = true;
        public SplashParticlesMaxLimitEnum MaxSplashParticlesBudget       = SplashParticlesMaxLimitEnum._15k;
        public float                       SplashParticlesScale           = 0.8f;
        public float                       SplashParticlesAlphaMultiplier = 0.6f;
        public float                       RiverEmissionRateSplash        = 0.25f;
        public float                       ShorelineEmissionRateSplash    = 0.5f;
        public float                       WaterfallEmissionRateSplash    = 0.5f;


        public SplashCasticShadowModeEnum  CastShadowMode    = SplashCasticShadowModeEnum.LowQuality;
        public SplashReceiveShadowModeEnum ReceiveShadowMode = SplashReceiveShadowModeEnum.DirectionalHighQuality;


        public Vector2Int TextureSize             => BakedTextureSize;
        public Vector3    Position                => BakedAreaPos;
        public Vector3    Size                    => BakedAreaSize;
        public Quaternion Rotation                => BakedAreaRotation;
        public Vector4    RotationMatrix          => BakedRotationMatrix;
        public Bounds     Bounds                  => bakedBounds;
        public float      ClosetsDistanceToCamera { get; set; }
        public bool       IsZoneVisible           => _IsZoneVisible;
        
        
        
        /// <summary>
        /// Returns the current simulation data texture (RGBA16 format unsigned normalized [0-1]).
        /// Each channel stores the following encoded values:
        /// - R: Velocity X, encoded from [-10, 10] range
        /// - G: Velocity Z, encoded from [-10, 10] range
        /// - B: Water Height Offset relative to terrain, encoded from [-2, 20] range
        /// - A: Terrain World Height, normalized by distance from water to zone top
        /// 
        /// Use the following logic to decode:
        /// Velocity.xy = (RG - 0.5) * 20.0
        /// Height      = B * 22.0 - 2.0
        /// Depth       = A * abs(WaterPosY - (ZonePosY + ZoneSizeY * 0.5))
        /// </summary>
        public RenderTexture GetSimulationDataTexture           => _simulationData.GetTarget.rt;
        
        
        /// <summary>
        /// Returns the additional simulation data texture in RGBA8 format (unorm).
        /// Each channel contains the following data:
        ///
        /// - R: Wetmap - surface wetness mask (used for darkening wet surfaces over time)
        /// - G: Shoreline Mask Fade - Signed Distance Field (SDF) mask for shore fading
        /// - B: Foam Mask - intensity of generated foam in the water
        /// - A: Wetmap Depth - height of wetting effect (used to discard objects above water level)
        /// </summary>
        public RenderTexture GetSimulationAdditionalDataTexture => _simulationData.GetAdditionalTarget.rt;
        
        
        public RenderTexture GetSimulationNormal                => _simulationData.GetNormals.rt;
        public RenderTexture GetSimulationColor                 => _simulationData.GetColorTarget.rt;
        public RenderTexture GetSimulationDynamicObjectsData    => _simulationData.DynamicWavesMask;
        public RenderTexture GetSimulationDynamicObjectsDepth   => _simulationData.DynamicWavesMaskDepth;
        public Texture       GetSimulationIntersectionDepth     => _simulationData.Depth;
        public Texture       GetSimulationSdfDepth              => _simulationData.DepthSDF;
        
        
        [SerializeField] internal bool ShowSimulationSettings = true;
        [SerializeField] internal bool ShowFoamParticlesSettings = true;
        [SerializeField] internal bool ShowSplashSettings        = true;
       
        int KWS_TileZoneManager.IWaterZone.                                           ID                 { get; set; }
        Bounds KWS_TileZoneManager.IWaterZone.                                        OrientedBounds     => bakedOrientedBounds;
        KWS_TileZoneManager.PrecomputedOBBZone KWS_TileZoneManager.IWaterZone.PrecomputedObbZone => _precomputedObbZone;

        // bool 

        [Space]
        public Texture2D SavedDepth;
        public Texture2D SavedDistanceField;
        public Texture2D SavedDynamicWavesSimulation;

        [SerializeField] internal Vector3    BakedAreaPos;
        [SerializeField] internal Vector3    BakedAreaSize;
        [SerializeField] internal Quaternion BakedAreaRotation;
        [SerializeField] internal Vector4    BakedRotationMatrix;
        [SerializeField] internal Vector4    BakedNearFarSizeXZ;
        [SerializeField] internal Vector2Int BakedTextureSize;
        [SerializeField] internal Bounds     bakedBounds;
        [SerializeField] internal Bounds     bakedOrientedBounds;
        [SerializeField] internal float      bakedSimSize;
        [SerializeField] internal float      bakedWaterLevel;
       
        [SerializeField] internal bool IsBakeMode;
       

        internal Vector3 _lastRenderedDynamicPosition;
        KWS_TileZoneManager.PrecomputedOBBZone _precomputedObbZone;


        internal DynamicWavesPass.SimulationData SimulationData
        {
            get
            {
                if (!_isInitialized) Initialize();

                return _simulationData;
            }
        }

        internal DynamicWavesPass.SimulationData _simulationData = new DynamicWavesPass.SimulationData();

        private  Material      _sdfBakeMaterial;
        private  CommandBuffer _bakeCmd;
        private  Camera        _bakeDepthCamera;
        private  GameObject    _bakeDepthCameraGO;
        internal RenderTexture _bakeDepthRT;
        internal RenderTexture _bakeDepthSdfRT;

        private const float sdfScaleResolution = 0.25f;

        float _lastResolutionPerMeter;
        bool _lastUseFoamParticles;
        bool _lastUseSplashParticles;
        private int _lastLayerMask;
        FoamParticlesMaxLimitEnum _lastFoamParticlesMaxLimit;
        SplashParticlesMaxLimitEnum _lastSplashParticlesMaxLimit;

        internal int CurrentSkippedFrames;
        internal int MaxSkippedFrames;
        bool         _isInitialized;
        private bool _IsZoneVisible;

        public enum FoamParticlesMaxLimitEnum
        {
            _1Million = 1000000,
            _500k = 500000,
            _250k = 250000,
            _100k = 100000
        }

        public enum SplashParticlesMaxLimitEnum
        {
            _50k = 50000,
            _25k = 25000,
            _15k = 15000,
            _5k = 5000,
        }

        public enum SplashCasticShadowModeEnum
        {
            Disabled,
            LowQuality,
            HighQuality
        }
        
        public enum SplashReceiveShadowModeEnum
        {
            Disabled,
            DirectionalLowQuality,
            DirectionalHighQuality,
            AllShadowsLowQuality,
            AllShadowsHighQuality,
        }
        
        public enum SimulationZoneTypeMode
        {
            StaticZone,
            MovableZone,
            BakedSimulation
        }

        public void ForceUpdateZone(bool resetSimulation = true)
        {
            InitializeNewZone(resetSimulation);
        }
     
        void InitializeNewZone(bool resetSimulation)
        {
            BakedAreaPos = transform.position;
            BakedAreaSize = transform.localScale;
            BakedAreaRotation = transform.rotation;
            BakedTextureSize = new Vector2Int(Mathf.CeilToInt(BakedAreaSize.x * SimulationResolutionPerMeter / 4f) * 4, Mathf.CeilToInt(BakedAreaSize.z * SimulationResolutionPerMeter / 4f) * 4);
            bakedBounds = new Bounds(BakedAreaPos, BakedAreaSize);
            bakedOrientedBounds = KW_Extensions.GetOrientedBounds(BakedAreaPos, BakedAreaSize, BakedAreaRotation);
            CachePrecomputedOBBZone();
            bakedSimSize    = SimulationResolutionPerMeter;
            bakedWaterLevel = WaterSystem.Instance ? WaterSystem.Instance.WaterPivotWorldPosition.y : 0;

            var angleRad = BakedAreaRotation.eulerAngles.y * Mathf.Deg2Rad;
            var cos      = Mathf.Cos(angleRad);
            var sin      = Mathf.Sin(angleRad);
            BakedRotationMatrix = new Vector4(cos, sin, -sin, cos);

            BakeDepth(BakedTextureSize.x, BakedTextureSize.y);

            if (resetSimulation)
            {
                _simulationData.Release();

                _simulationData.InitializeSimTextures(BakedTextureSize.x, BakedTextureSize.y);
                _simulationData.InitializePrebakedDepth(_bakeDepthRT, _bakeDepthSdfRT, BakedAreaPos, BakedAreaSize);
                _simulationData.InitializeWetDecalData();
                _simulationData.InitializeParticlesBuffers(UseFoamParticles, (int)MaxFoamParticlesBudget, UseSplashParticles, (int)MaxSplashParticlesBudget);

            }
            else if (_simulationData.CurrentFrame > 3)
            {
                _simulationData.InitializePrebakedDepth(_bakeDepthRT, _bakeDepthSdfRT, BakedAreaPos, BakedAreaSize);
            }
          
            
            _isInitialized = true;

        }

        private void InitializeSavedZone()
        {
            bakedBounds = new Bounds(BakedAreaPos, BakedAreaSize);
            bakedOrientedBounds = KW_Extensions.GetOrientedBounds(BakedAreaPos, BakedAreaSize, BakedAreaRotation);
            CachePrecomputedOBBZone();
            
            var angleRad = BakedAreaRotation.eulerAngles.y * Mathf.Deg2Rad;
            var cos      = Mathf.Cos(angleRad);
            var sin      = Mathf.Sin(angleRad);
            BakedRotationMatrix = new Vector4(cos, sin, -sin, cos);

            _simulationData.InitializeSimTextures(BakedTextureSize.x, BakedTextureSize.y);
            _simulationData.InitializePrebakedDepth(SavedDepth, SavedDistanceField, BakedAreaPos, BakedAreaSize);
            _simulationData.InitializeWetDecalData();
            _simulationData.InitializeParticlesBuffers(UseFoamParticles, (int)MaxFoamParticlesBudget, UseSplashParticles, (int)MaxSplashParticlesBudget);

            if (SavedDynamicWavesSimulation != null) _simulationData.InitializePrebakedSimData(SavedDynamicWavesSimulation);


            _isInitialized = true;
        }

        internal void ValueChanged()
        {
            if (Math.Abs(_lastResolutionPerMeter - SimulationResolutionPerMeter) > 0.05f)
            {
                Initialize();
            }

            if (_lastUseFoamParticles != UseFoamParticles || _lastFoamParticlesMaxLimit != MaxFoamParticlesBudget ||
               _lastUseSplashParticles != UseSplashParticles || _lastSplashParticlesMaxLimit != MaxSplashParticlesBudget
               || _lastLayerMask != IntersectionLayerMask)
            {
                _simulationData.InitializeParticlesBuffers(UseFoamParticles, (int)MaxFoamParticlesBudget, UseSplashParticles, (int)MaxSplashParticlesBudget);
            }


            _lastResolutionPerMeter = SimulationResolutionPerMeter;
            _lastUseFoamParticles = UseFoamParticles;
            _lastFoamParticlesMaxLimit = MaxFoamParticlesBudget;
            _lastUseSplashParticles = UseSplashParticles;
            _lastSplashParticlesMaxLimit = MaxSplashParticlesBudget;
            _lastLayerMask = IntersectionLayerMask;
        }

        void KWS_TileZoneManager.IWaterZone.UpdateVisibility(Camera cam)
        {
            _IsZoneVisible = false;

            if (!KWS_UpdateManager.FrustumCaches.TryGetValue(cam, out var cache))
            {
                return;
            }

            var planes = cache.FrustumPlanes;
            //_IsZoneVisible = KW_Extensions.IsBoxVisibleApproximated(ref planes, Bounds.min,              Bounds.max);
            _IsZoneVisible = KW_Extensions.IsBoxVisibleApproximated(ref planes, bakedOrientedBounds.min, bakedOrientedBounds.max);
        }

        internal bool CanRender(Camera cam)
        {
            if (!_IsZoneVisible) return false;

            var distanceToAABB = KW_Extensions.DistanceToAABB(cam.transform.position, Bounds.min, Bounds.max);

            var staticZoneMaxDistanceToDropFPS = 300;
            var movableZoneMaxDistanceToDropFPS = 300;

            if (ZoneType == SimulationZoneTypeMode.StaticZone || ZoneType == SimulationZoneTypeMode.BakedSimulation)
            {
                var normalizedDistance = Mathf.Clamp01(-0.1f + distanceToAABB * (1f / staticZoneMaxDistanceToDropFPS));
                MaxSkippedFrames = Mathf.RoundToInt(Mathf.Lerp(0, 4, normalizedDistance));
            }
            else if (ZoneType == SimulationZoneTypeMode.MovableZone)
            {
                var normalizedDistance = Mathf.Clamp01(-0.25f + distanceToAABB * (1f / movableZoneMaxDistanceToDropFPS));
                MaxSkippedFrames = Mathf.RoundToInt(Mathf.Lerp(0, 3, normalizedDistance));
            }
            else
            {
                Debug.LogError("implement SimulationZoneTypeMode!");
            }

            if (++CurrentSkippedFrames > MaxSkippedFrames) CurrentSkippedFrames = 0;
            return (CurrentSkippedFrames == 0);
        }


        internal void Initialize()
        {
            if (SavedDepth != null && (ZoneType == SimulationZoneTypeMode.StaticZone || ZoneType == SimulationZoneTypeMode.BakedSimulation))
            {
                InitializeSavedZone();
            }
            else
            {
                InitializeNewZone(true);
            }

        }

        internal void UpdateMovableZoneTransform()
        {
            BakedAreaPos  = transform.position;
            BakedAreaSize = transform.localScale;
            bakedBounds   = new Bounds(BakedAreaPos, BakedAreaSize);

            BakedNearFarSizeXZ = new Vector4(BakedAreaPos.y + BakedAreaSize.y * 0.5f, BakedAreaSize.y, BakedAreaSize.x, BakedAreaSize.z);

        }

        internal void CachePrecomputedOBBZone()
        {
            var bounds = Bounds;
            Vector2 center = new Vector2(bounds.center.x, bounds.center.z);
            Vector2 halfSize = new Vector2(bounds.size.x, bounds.size.z) * 0.5f;

            float angleRad = transform.rotation.eulerAngles.y * Mathf.Deg2Rad;
            float cos = Mathf.Cos(angleRad);
            float sin = Mathf.Sin(angleRad);
            Vector4 rotMatrix = new Vector4(cos, -sin, sin, cos);

            _precomputedObbZone = new KWS_TileZoneManager.PrecomputedOBBZone();
            _precomputedObbZone.Center = center;
            _precomputedObbZone.Axis = new Vector2[2];
            _precomputedObbZone.Axis[0] = new Vector2(rotMatrix.x, rotMatrix.y); // right
            _precomputedObbZone.Axis[1] = new Vector2(rotMatrix.z, rotMatrix.w); // forward
            _precomputedObbZone.HalfSize = halfSize;
            _precomputedObbZone.RotMatrix = rotMatrix;

            Vector2 bX = new Vector2(1, 0);
            Vector2 bY = new Vector2(0, 1);

            _precomputedObbZone.Extents = new float[2];
            _precomputedObbZone.Extents[0] = Mathf.Abs(Vector2.Dot(_precomputedObbZone.Axis[0] * halfSize.x, bX)) + Mathf.Abs(Vector2.Dot(_precomputedObbZone.Axis[1] * halfSize.y, bX));
            _precomputedObbZone.Extents[1] = Mathf.Abs(Vector2.Dot(_precomputedObbZone.Axis[0] * halfSize.x, bY)) + Mathf.Abs(Vector2.Dot(_precomputedObbZone.Axis[1] * halfSize.y, bY));
        }

        void OnEnable()
        {
            transform.hasChanged = false;
            _IsZoneVisible       = false;
            _isInitialized       = false;
            IsBakeMode           = false;
            
            Initialize();

            KWS_TileZoneManager.DynamicWavesZones.Add(this);

            _lastResolutionPerMeter = SimulationResolutionPerMeter;
            _lastUseFoamParticles = UseFoamParticles;
            _lastFoamParticlesMaxLimit = MaxFoamParticlesBudget;
            _lastUseSplashParticles = UseSplashParticles;
            _lastSplashParticlesMaxLimit = MaxSplashParticlesBudget;
            _lastLayerMask = IntersectionLayerMask;
        }


        void OnDisable()
        {
            KWS_TileZoneManager.DynamicWavesZones.Remove(this);

            ReleaseTextures();
            KW_Extensions.SafeDestroy(_sdfBakeMaterial, _bakeDepthCameraGO);
            _isInitialized = false;
            
        }

        void LateUpdate()
        {
            if (ZoneType == SimulationZoneTypeMode.MovableZone && FollowObject != null)
            {
                transform.rotation = Quaternion.identity;
                transform.position = FollowObject.transform.position;
            }
        }

        void BakeDepth(int simulationWidth, int simulationHeight)
        {
            var zonePos = transform.position;
            var zoneSize = transform.localScale;

            var camPos = new Vector3(zonePos.x, zonePos.y + zoneSize.y * 0.5f, zonePos.z);
            var areaSize = Mathf.Max(zoneSize.x, zoneSize.z);
            var far = zoneSize.y;
            BakedNearFarSizeXZ = new Vector4(camPos.y, far, zoneSize.x, zoneSize.z);

            if (ZoneType == SimulationZoneTypeMode.MovableZone) return;

            if (_sdfBakeMaterial == null) _sdfBakeMaterial = KWS_CoreUtils.CreateDepthSdfMaterial();
            if (_bakeCmd == null) _bakeCmd = new CommandBuffer() { name = "ComputeDepthSDF" };
            
            if (_bakeDepthCameraGO == null)
            {
                _bakeDepthCameraGO = ReflectionUtils.CreateDepthCamera("Bake Ortho Depth Camera", out _bakeDepthCamera);
                _bakeDepthCameraGO.transform.parent = transform;
                _bakeDepthCameraGO.transform.localRotation = Quaternion.Euler(90, 0, 0);
                _bakeDepthCameraGO.transform.localScale = Vector3.one;
                _bakeDepthCameraGO.hideFlags = HideFlags.HideAndDontSave;
            }

            var simulationWidthSDF = Mathf.CeilToInt(sdfScaleResolution * zoneSize.x * SimulationResolutionPerMeter / 4f) * 4;
            var simulationHeightSDF = Mathf.CeilToInt(sdfScaleResolution * zoneSize.z * SimulationResolutionPerMeter / 4f) * 4;

            simulationWidthSDF = Mathf.Min(512, simulationWidthSDF);
            simulationHeightSDF = Mathf.Min(512, simulationHeightSDF);

            if (_bakeDepthRT != null) RenderTexture.ReleaseTemporary(_bakeDepthRT);
            if (_bakeDepthSdfRT != null) RenderTexture.ReleaseTemporary(_bakeDepthSdfRT);

            _bakeDepthRT = RenderTexture.GetTemporary(simulationWidth, simulationHeight, 24, RenderTextureFormat.Depth);
            _bakeDepthSdfRT = RenderTexture.GetTemporary(simulationWidthSDF, simulationHeightSDF, 0, GraphicsFormat.R16_SFloat);

            _bakeDepthCamera.transform.position = camPos;
            _bakeDepthCamera.orthographicSize = areaSize * 0.5f;
            _bakeDepthCamera.nearClipPlane = 0.01f;
            _bakeDepthCamera.farClipPlane = far;
            _bakeDepthCamera.cullingMask = IntersectionLayerMask;
            _bakeDepthCamera.aspect = (float)simulationWidth / simulationHeight;
            if (_bakeDepthCamera.aspect > 1.0) _bakeDepthCamera.orthographicSize /= _bakeDepthCamera.aspect;

            if (bakedWaterLevel == 0f)
            {
                if (WaterSystem.Instance) bakedWaterLevel = WaterSystem.Instance.WaterPivotWorldPosition.y;
            }

            KWS_CoreUtils.RenderDepth(_bakeDepthCamera, _bakeDepthRT);
            KWS_CoreUtils.ComputeSDF(_bakeCmd, _sdfBakeMaterial, areaSize, BakedNearFarSizeXZ, bakedWaterLevel, _bakeDepthRT, _bakeDepthSdfRT);
        }



        void ReleaseTextures()
        {
            if (_bakeDepthRT != null) RenderTexture.ReleaseTemporary(_bakeDepthRT);
            if (_bakeDepthSdfRT != null) RenderTexture.ReleaseTemporary(_bakeDepthSdfRT);

            _bakeDepthRT = _bakeDepthSdfRT = null;

            _simulationData.Release();

            this.WaterLog(string.Empty, KW_Extensions.WaterLogMessageType.ReleaseRT);
        }
        
        void OnDrawGizmosSelected()
        {
            var angles                    = transform.rotation.eulerAngles;
            angles.x           = angles.z = 0;
            transform.rotation = Quaternion.Euler(angles);

            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.color = new Color(0.15f, 0.35f, 1, 0.99f);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

            Gizmos.color = new Color(0.15f, 0.35f, 1, 0.25f);
            Gizmos.DrawCube(Vector3.zero, Vector3.one);

            if ((ZoneType == SimulationZoneTypeMode.StaticZone || ZoneType == SimulationZoneTypeMode.BakedSimulation))
            {
                if (transform.hasChanged)
                {
                    transform.hasChanged = false;

#if KWS_HDRP && !KWS_URP
                    InitializeNewZone(true);
#else
                    #if UNITY_EDITOR
                        if (!Application.isPlaying) UnityEditor.EditorApplication.delayCall += () => ForceUpdateZone(); //avoid Recursive rendering error
                    #endif
#endif


                }
            }
        }



        void OnDrawGizmos()
        {
            Gizmos.matrix = transform.localToWorldMatrix;

            Gizmos.color = new Color(0.15f, 0.35f, 1, 0.99f);
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }


    }
}