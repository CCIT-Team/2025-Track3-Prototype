using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

public class LBottomLever : MonoBehaviour
{
    public Transform LCrawl;
    public float threshold = 10f;

    void Update()
    {
        var grabFreeTransformer = GetComponent<GrabFreeTransformer>();
        var positionConstraints = new TransformerUtils.PositionConstraints();

        var rangeX = new TransformerUtils.FloatRange();
        var rangeY = new TransformerUtils.FloatRange();
        var rangeZ = new TransformerUtils.FloatRange();

        rangeX.Min = LCrawl.position.x;
        rangeX.Max = LCrawl.position.x;

        rangeY.Min = LCrawl.position.y;
        rangeY.Max = LCrawl.position.y;

        rangeZ.Min = LCrawl.position.z;
        rangeZ.Max = LCrawl.position.z;

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

        // 로컬 회전값 보정: 0~360 → -180~180
        float x = LCrawl.localEulerAngles.x;

        if (x > 180f) x -= 360f;

        VirtualInput.T = x < -threshold;

        VirtualInput.G = x > threshold;
    }
}