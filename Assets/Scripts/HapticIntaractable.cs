using System.Collections;
using UnityEngine;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using Oculus.Haptics;

public class HapticIntaractable : MonoBehaviour
{
    //[SerializeField] float amplitude = 0.5f;
    //[SerializeField] float frequency = 1.0f;
    //[SerializeField] float duration = 0.1f;
    [SerializeField] HapticClip clip;

    HapticClipPlayer player;

    HandGrabInteractable handgrabInteractable;

    private void Awake()
    {
        handgrabInteractable = GetComponent<HandGrabInteractable>();
    }

    private void Start()
    {
        handgrabInteractable.WhenSelectingInteractorAdded.Action += WhenSelectingInteractorAdd;
        player = new HapticClipPlayer(clip);
    }

    public void WhenSelectingInteractorAdd(HandGrabInteractor obj)
    {
        ControllerRef controllerRef = obj.GetComponent<ControllerRef>();
       
        if (controllerRef)
        {
            Debug.Log(controllerRef.Handedness);
            switch (controllerRef.Handedness)
            {
                case Handedness.Left:
                    TriggerHaptic(Oculus.Haptics.Controller.Left);
                    break;
                case Handedness.Right:
                    TriggerHaptic(Oculus.Haptics.Controller.Right);
                    break;
                default:
                    break;
            }
        }
    }

    public void TriggerHaptic(Oculus.Haptics.Controller controller)
    {
        player.clip = clip;
        player.Play(controller);
        //StartCoroutine(TriggerHapticCorutine(controller));
    }

    //IEnumerator TriggerHapticCorutine(OVRInput.Controller controller)
    //{
    //    OVRInput.SetControllerVibration(frequency, amplitude, controller);
    //    yield return new WaitForSeconds(duration);
    //    OVRInput.SetControllerVibration(0, 0, controller);
    //}

}
