using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

public class RBottomLever : MonoBehaviour
{
    public Transform RCrawl;
    public float threshold = 10f;

    void Update()
    {
        var grabFreeTransformer = GetComponent<GrabFreeTransformer>();
        var positionConstraints = new TransformerUtils.PositionConstraints();

        var rangeX = new TransformerUtils.FloatRange();
        var rangeY = new TransformerUtils.FloatRange();
        var rangeZ = new TransformerUtils.FloatRange();

        rangeX.Min = RCrawl.position.x;
        rangeX.Max = RCrawl.position.x;

        rangeY.Min = RCrawl.position.y;
        rangeY.Max = RCrawl.position.y;

        rangeZ.Min = RCrawl.position.z;
        rangeZ.Max = RCrawl.position.z;

        var constrainedAxisX = new TransformerUtils.ConstrainedAxis();
        var constrainedAxisY = new TransformerUtils.ConstrainedAxis();
        var constrainedAxisZ = new TransformerUtils.ConstrainedAxis();

        constrainedAxisX.ConstrainAxis = true;
        constrainedAxisY.ConstrainAxis = true;
        constrainedAxisZ.ConstrainAxis = true;

        constrainedAxisX.AxisRange = rangeX;
        constrainedAxisY.AxisRange = rangeY;
        constrainedAxisZ.AxisRange = rangeZ;

        positionConstraints.XAxis = constrainedAxisX;
        positionConstraints.YAxis = constrainedAxisY;
        positionConstraints.ZAxis = constrainedAxisZ;

        grabFreeTransformer.InjectOptionalPositionConstraints(positionConstraints);

        // 로컬 회전값 보정: 0~360 → -180~180
        float x = RCrawl.localEulerAngles.x;
        if (x > 180f) x -= 360f;

        VirtualInput.J = x < -threshold;

        VirtualInput.U = x > threshold;
    }
}