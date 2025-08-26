using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class QuickLoop : MonoBehaviour
{
    public AudioClip clip;
    public bool playOnStart = true;

    void Awake()
    {
        var src = GetComponent<AudioSource>();
        src.clip = clip;
        src.loop = true;          // 핵심
        src.playOnAwake = playOnStart;
        src.spatialBlend = 0f;    // 테스트면 2D 권장
        if (playOnStart && clip) src.Play();
    }
}
