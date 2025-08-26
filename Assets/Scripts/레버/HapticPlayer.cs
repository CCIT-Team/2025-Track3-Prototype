using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Haptics;
public class HapticPlayer : MonoBehaviour
{
    [SerializeField] HapticClip clip;
    HapticClipPlayer player;
    private void Awake()
    {
        player = new HapticClipPlayer(clip);
    }

    public void PlayHaptic(Controller controller)
    {
        player.Play(controller);
    }
}
