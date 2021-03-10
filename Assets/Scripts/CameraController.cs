using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform positionTarget;

    public Vector3 cameraLocalPos;

    public float lerpPositionRatio;
    public float lerpRotationRatio;

    private Vector3 wantedPos;
    private Quaternion wantedRot;

    private void Update()
    {
        wantedPos = positionTarget.TransformPoint(cameraLocalPos);
        wantedPos.y = cameraLocalPos.y + positionTarget.position.y;

        transform.position = Vector3.Lerp(transform.position, wantedPos, lerpPositionRatio * Time.deltaTime);

        wantedRot = Quaternion.LookRotation(positionTarget.position - transform.position, Vector3.up);

        transform.rotation = Quaternion.Lerp(transform.rotation, wantedRot, lerpRotationRatio * Time.deltaTime);
    }
}