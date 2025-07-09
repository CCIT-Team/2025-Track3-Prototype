using UnityEngine;
using System.Collections.Generic;

public class ParticleToGroundManager : MonoBehaviour
{
    public static ParticleToGroundManager Instance { get; private set; }

    [Header("그룹 설정")]
    [Tooltip("입자들을 하나의 그룹으로 묶을 최대 거리")]
    public float GroupingRadius = 0.5f;

    private List<ParticleToGround> registrationQueue = new List<ParticleToGround>();
    private Vector3 terrainSize;
    private float particleVolume;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (TerrainManager.Instance != null)
        {
            terrainSize = TerrainManager.Instance.TerrainSize;
            particleVolume = TerrainManager.Instance.VolumePerParticle;
        }
        else
        {
            Debug.LogError("ParticleToGroundManager가 TerrainManager를 찾을 수 없습니다!");
        }
    }

    public void RegisterParticle(ParticleToGround particle)
    {
        if (!registrationQueue.Contains(particle))
        {
            registrationQueue.Add(particle);
        }
    }

    void LateUpdate()
    {
        if (registrationQueue.Count == 0) return;
        ProcessParticleGroups();
    }

    private void ProcessParticleGroups()
    {
        List<List<ParticleToGround>> particleGroups = new List<List<ParticleToGround>>();

        while (registrationQueue.Count > 0)
        {
            List<ParticleToGround> newGroup = new List<ParticleToGround>();
            FindGroup(registrationQueue[0], newGroup);
            particleGroups.Add(newGroup);
        }

        foreach (var group in particleGroups)
        {
            if (group.Count == 0) continue;

            Vector3 averagePosition = Vector3.zero;
            foreach (var particle in group)
            {
                averagePosition += particle.transform.position;
            }
            averagePosition /= group.Count;

            float totalParticleVolume = particleVolume * group.Count;
            float pileRadius = 0.3f * Mathf.Sqrt(group.Count);

            // 원뿔 부피 공식 근사: V = (PI * r^2 * h) / 3  ->  h = (3 * V) / (PI * r^2)
            float pileHeightInMeters = (3 * totalParticleVolume) / (Mathf.PI * pileRadius * pileRadius);

            float pileStrength = pileHeightInMeters / terrainSize.y;

            if (TerrainManager.Instance != null)
            {
                TerrainManager.Instance.Pile(averagePosition, pileRadius, pileStrength);
            }

            foreach (var particle in group)
            {
                if (particle != null)
                {
                    Destroy(particle.gameObject);
                }
            }
        }
    }

    private void FindGroup(ParticleToGround startParticle, List<ParticleToGround> group)
    {
        List<ParticleToGround> toCheck = new List<ParticleToGround> { startParticle };
        registrationQueue.Remove(startParticle);
        group.Add(startParticle);

        while (toCheck.Count > 0)
        {
            ParticleToGround current = toCheck[0];
            toCheck.RemoveAt(0);

            for (int i = registrationQueue.Count - 1; i >= 0; i--)
            {
                ParticleToGround other = registrationQueue[i];
                if (other != null && Vector3.Distance(current.transform.position, other.transform.position) < GroupingRadius)
                {
                    group.Add(other);
                    toCheck.Add(other);
                    registrationQueue.RemoveAt(i);
                }
            }
        }
    }
}