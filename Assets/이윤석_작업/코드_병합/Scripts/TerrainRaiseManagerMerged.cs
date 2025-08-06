using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 입자들이 쌓인 형태를 깊이(bakeRadius)를 가진 구 형태로 Terrain에 베이크하는 매니저.
/// SoilParticle에서 RegisterStop()으로 모은 입자를, autoBakeInterval이 지나면 자동으로 처리합니다.
/// </summary>
public class TerrainRaiseManagerMerged : MonoBehaviour, IPoolable
{
    [Header("Terrain Reference")]
    [SerializeField] private Terrain terrain;

    [Header("Bake Settings")]
    [SerializeField] private bool autoBakeEnabled = true;
    [SerializeField] private float autoBakeInterval = 5f;

    [Header("Particle Bake Defaults")]
    [SerializeField] private float defaultBakeRadius = 0.5f;
    [SerializeField] private float defaultHeightOffset = 0.3f;

    [Header("VFX Settings")]
    [Tooltip("흙이 쌓일 때 생성될 먼지 효과 프리팹")]
    [SerializeField] private GameObject dustVFXPrefab;

    [Header("Slope Relaxation Settings")]
    [SerializeField] private float relaxRadius = 2f;
    [SerializeField] private float maxSlopeAngleDeg = 35f;
    [SerializeField] private float relaxStrength = 0.01f;

    private TerrainData _terrainData;
    private TerrainCollider _terrainCollider;
    private List<GameObject> _stopped = new List<GameObject>();
    private float _bakeTimer = 0f;

    private GameObjectPool _pool;

    void Awake()
    {
        if (terrain == null) terrain = Terrain.activeTerrain;
        _terrainData = terrain.terrainData;
        _terrainCollider = terrain.GetComponent<TerrainCollider>();
    }

    public void RegisterStop(GameObject particle)
    {
        if (particle == null || _stopped.Contains(particle))
            return;

        _stopped.Add(particle);
    }

    void Update()
    {
        if (!autoBakeEnabled) return;
        _bakeTimer += Time.deltaTime;
        if (_bakeTimer >= autoBakeInterval)
        {
            _bakeTimer = 0f;
            BakeAndClearParticles();
        }
    }

    private void BakeAndClearParticles()
    {
        if (_stopped.Count == 0)
            return;

        // 높이 맵 베이크
        BakeTerrainFromParticles();
        // 알파맵 페인팅
        PaintTerrainFromParticles();

        // 파티클 제거
        foreach (var go in _stopped)
        {
            if (go != null)
                Destroy(go);
        }
        _stopped.Clear();
    }

    private void BakeTerrainFromParticles()
    {
        if (dustVFXPrefab != null && _stopped.Count > 0)
        {
            Vector3 avg = Vector3.zero;
            int cnt = 0;
            foreach (var go in _stopped)
                if (go != null) { avg += go.transform.position; cnt++; }
            if (cnt > 0)
            {
                Instantiate(dustVFXPrefab, avg / cnt, Quaternion.identity);
            }
        }

        int res = _terrainData.heightmapResolution;
        float[,] heights = _terrainData.GetHeights(0, 0, res, res);
        Vector3 tPos = terrain.transform.position;
        float sizeX = _terrainData.size.x;
        float sizeZ = _terrainData.size.z;
        float sizeY = _terrainData.size.y;
        float cellX = sizeX / (res - 1);
        float cellZ = sizeZ / (res - 1);

        var centers = new HashSet<Vector2Int>();
        foreach (var go in _stopped)
        {
            if (go == null) continue;
            var sp = go.GetComponent<SoilParticleMerged>();
            float rad = sp?.bakeRadius ?? defaultBakeRadius;
            float centerY = go.transform.position.y + (sp?.heightOffset ?? defaultHeightOffset);
            float normT = Mathf.Clamp01((centerY - tPos.y) / sizeY);

            Vector3 local = go.transform.position - tPos;
            int cx = Mathf.RoundToInt(local.x / sizeX * (res - 1));
            int cz = Mathf.RoundToInt(local.z / sizeZ * (res - 1));
            centers.Add(new Vector2Int(cx, cz));

            int rX = Mathf.CeilToInt(rad / cellX);
            int rZ = Mathf.CeilToInt(rad / cellZ);
            float rr = rad * rad;

            int x0 = Mathf.Clamp(cx - rX, 0, res - 1);
            int x1 = Mathf.Clamp(cx + rX, 0, res - 1);
            int z0 = Mathf.Clamp(cz - rZ, 0, res - 1);
            int z1 = Mathf.Clamp(cz + rZ, 0, res - 1);

            for (int z = z0; z <= z1; z++)
            for (int x = x0; x <= x1; x++)
            {
                float dx = (x - cx) * cellX;
                float dz = (z - cz) * cellZ;
                if (dx*dx + dz*dz > rr) continue;
                float cellY = heights[z, x] * sizeY + tPos.y;
                if ((go.transform.position.y - cellY)*(go.transform.position.y - cellY) + dx*dx + dz*dz > rr) continue;
                if (heights[z, x] < normT) heights[z, x] = normT;
            }
        }
        foreach (var c in centers)
            RelaxSlopeAround(heights, res, c.x, c.y, cellX, cellZ, sizeY);

        _terrainData.SetHeights(0, 0, heights);
    }

    private void PaintTerrainFromParticles()
    {
        int res = _terrainData.alphamapResolution;
        int layers = _terrainData.alphamapLayers;
        float[,,] am = _terrainData.GetAlphamaps(0, 0, res, res);
        Vector3 tPos = terrain.transform.position;
        float sizeX = _terrainData.size.x;
        float sizeZ = _terrainData.size.z;
        float cellX = sizeX / (res - 1);
        float cellZ = sizeZ / (res - 1);

        foreach (var go in _stopped)
        {
            if (go == null) continue;
            var layer = go.GetComponent<SoilParticleMerged>()?.GetLayer();
            if (layer == null) continue;
            int li = GetLayerIndex(layer);
            if (li < 0) continue;
            Vector3 local = go.transform.position - tPos;
            int cx = Mathf.RoundToInt(local.x / sizeX * (res -1));
            int cz = Mathf.RoundToInt(local.z / sizeZ * (res -1));
            float rad = (go.GetComponent<SoilParticleMerged>()?.bakeRadius ?? 0f)*1.5f;
            float rr = rad*rad;
            int rX = Mathf.CeilToInt(rad/cellX);
            int rZ = Mathf.CeilToInt(rad/cellZ);
            int x0 = Mathf.Clamp(cx-rX,0,res-1);
            int x1 = Mathf.Clamp(cx+rX,0,res-1);
            int z0 = Mathf.Clamp(cz-rZ,0,res-1);
            int z1 = Mathf.Clamp(cz+rZ,0,res-1);
            for(int z=z0;z<=z1;z++)
            for(int x=x0;x<=x1;x++)
            {
                float dx=(x-cx)*cellX;
                float dz=(z-cz)*cellZ;
                float d2=dx*dx+dz*dz;
                if(d2>rr) continue;
                float t=(1f-Mathf.Sqrt(d2)/rad)*10f;
                t=Mathf.Clamp01(t);
                float sum=0f;
                for(int l=0; l<layers; l++)
                {
                    if(l==li) am[z,x,l]=Mathf.Lerp(am[z,x,l],1f,t);
                    else am[z,x,l]=Mathf.Lerp(am[z,x,l],0f,t);
                    sum+=am[z,x,l];
                }
                for(int l=0;l<layers;l++) am[z,x,l]/=sum;
            }
        }
        _terrainData.SetAlphamaps(0,0,am);
    }

    private void RelaxSlopeAround(float[,] heights,int res,int cx,int cz,float cellX,float cellZ,float sizeY)
    {
        int rp=Mathf.RoundToInt(relaxRadius/cellX);
        int x0=Mathf.Clamp(cx-rp,1,res-2);
        int x1=Mathf.Clamp(cx+rp,1,res-2);
        int z0=Mathf.Clamp(cz-rp,1,res-2);
        int z1=Mathf.Clamp(cz+rp,1,res-2);
        float maxSl=Mathf.Tan(maxSlopeAngleDeg*Mathf.Deg2Rad);
        for(int z=z0;z<=z1;z++) for(int x=x0;x<=x1;x++)
        {
            float dz=(heights[z+1,x]-heights[z-1,x])/(2*cellZ);
            float dx=(heights[z,x+1]-heights[z,x-1])/(2*cellX);
            float slope=Mathf.Sqrt(dx*dx+dz*dz);
            if(slope>maxSl)
            {
                float ex=slope-maxSl;
                float rd=ex*relaxStrength;
                heights[z,x]-=rd;
                float disp=rd*0.25f;
                heights[z+1,x]+=disp;heights[z-1,x]+=disp;heights[z,x+1]+=disp;heights[z,x-1]+=disp;
            }
        }
        int w=x1-x0+1,h=z1-z0+1;
        float[,] copy=new float[h,w];
        for(int dz=0;dz<h;dz++)for(int dx=0;dx<w;dx++) copy[dz,dx]=heights[z0+dz,x0+dx];
        for(int dz=1;dz<h-1;dz++) for(int dx=1;dx<w-1;dx++)
        {
            float sum=0f;
            for(int oy=-1;oy<=1;oy++)for(int ox=-1;ox<=1;ox++) sum+=copy[dz+oy,dx+ox];
            heights[z0+dz,x0+dx]=sum/9f;
        }
    }

    private int GetLayerIndex(TerrainLayer target)
    {
        var ls=_terrainData.terrainLayers;
        for(int i=0;i<ls.Length;i++) if(ls[i]==target) return i;
        return -1;
    }

    public void SetPoolInstance(GameObjectPool pool) { _pool=pool;}    
    public bool ComparePoolInstance(GameObjectPool pool) { return _pool==pool; }
}
