using UnityEngine;
using Oculus.Interaction;

public class RLeverControl : MonoBehaviour
{
    public Transform RLever;

    public float threshold = 10f;   

    void Update()
    {
        var grabFreeTransformer = GetComponent<GrabFreeTransformer>();
        var positionConstraints = new TransformerUtils.PositionConstraints();

        var rangeX = new TransformerUtils.FloatRange();
        var rangeY = new TransformerUtils.FloatRange();
        var rangeZ = new TransformerUtils.FloatRange();

        rangeX.Min = RLever.position.x;
        rangeX.Max = RLever.position.x;

        rangeY.Min = RLever.position.y;
        rangeY.Max = RLever.position.y;

        rangeZ.Min = RLever.position.z;
        rangeZ.Max = RLever.position.z;

        var constrainedAxisX = new TransformerUtils.ConstrainedAxis();
        var constrainedAxisY = new TransformerUtils.ConstrainedAxis();
        var constrainedAxisZ = new TransformerUtils.ConstrainedAxis();

        constrainedAxisX.ConstrainAxis = false;
        constrainedAxisY.ConstrainAxis = false;
        constrainedAxisZ.ConstrainAxis = false;

        constrainedAxisX.AxisRange = rangeX;
        constrainedAxisY.AxisRange = rangeY;
        constrainedAxisZ.AxisRange = rangeZ;

        positionConstraints.XAxis = constrainedAxisX;
        positionConstraints.YAxis = constrainedAxisY;
        positionConstraints.ZAxis = constrainedAxisZ;

        grabFreeTransformer.InjectOptionalPositionConstraints(positionConstraints);

        Vector3 rot = RLever.localEulerAngles;
        float x = rot.x > 180f ? rot.x - 360f : rot.x;
        float z = rot.z > 180f ? rot.z - 360f : rot.z;

        if (Mathf.Abs(x) > Mathf.Abs(z))
        {
            // X축(앞뒤)이 더 많이 기울어졌을 때
            VirtualInput.W = x < -threshold;
            VirtualInput.S = x > threshold;

            // Z축 방향은 비활성화
            VirtualInput.R = false;
            VirtualInput.F = false;
        }
        else
        {
            // Z축(좌우)이 더 많이 기울어졌을 때
            VirtualInput.R = z < -threshold;
            VirtualInput.F = z > threshold;

            // X축 방향은 비활성화
            VirtualInput.W = false;
            VirtualInput.S = false;
        }
    }
}
