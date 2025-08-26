using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;
using Unity.Mathematics;

public class LeverBase : MonoBehaviour
{
    protected GrabFreeTransformer grabFreeTransformer;
    protected TransformerUtils.PositionConstraints positionConstraints = new TransformerUtils.PositionConstraints();
    [SerializeField] protected EINPUT[] inputs = new EINPUT[2];
    [SerializeField, Range(0,180)] protected float threshold = 10f;

    private void Awake()
    {
        grabFreeTransformer = GetComponent<GrabFreeTransformer>();
        positionConstraints.ConstraintsAreRelative = true;
    }

    private void Update()
    {
        ResetPosition();
        LeverInput();
    }

    protected void ResetPosition()
    {
        var range = new TransformerUtils.FloatRange();

        // x ÁÂÇ¥ ÃÊ±âÈ­
        range.Max = transform.position.x;
        range.Min = transform.position.x;

        positionConstraints.XAxis.ConstrainAxis = true;
        positionConstraints.XAxis.AxisRange = range;

        // y ÁÂÇ¥ ÃÊ±âÈ­
        range.Max = transform.position.y;
        range.Min = transform.position.y;

        positionConstraints.YAxis.ConstrainAxis = true;
        positionConstraints.YAxis.AxisRange = range;

        // z ÁÂÇ¥ ÃÊ±âÈ­
        range.Max = transform.position.z;
        range.Min = transform.position.z;

        positionConstraints.ZAxis.ConstrainAxis = true;
        positionConstraints.ZAxis.AxisRange = range;

        //°íÁ¤ÁÂÇ¥ ÃÖÁ¾¼³Á¤
        grabFreeTransformer.InjectOptionalPositionConstraints(positionConstraints);
    }

    virtual protected void LeverInput()
    {

    }

    public void ResetRotation()
    {
        transform.localRotation = Quaternion.Euler(0, 0, 0);
    }
}
