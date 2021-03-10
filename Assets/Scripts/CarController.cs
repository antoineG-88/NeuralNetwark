using UnityEngine;

public class CarController : MonoBehaviour
{
    public Transform centerOfMass;
    public float thrustForce;
    private Rigidbody rb;
    public bool use4WheelMotion;
    public bool use4WheelTraction;
    public bool useForwardTraction;

    public WheelCollider wheelFrontLeftCollider, wheelFrontRightCollider, wheelRearLeftCollider, wheelRearRightCollider;
    public Transform wheelFrontLeftModel, wheelFrontRightModel, wheelRearLeftModel, wheelRearRightModel;

    public float horizontalInput, verticalInput, maxSteerAngle, motorForce = 500;

    Vector3 pos;
    Quaternion quat;
    public bool isThrusting;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass = centerOfMass.localPosition;
    }

    private void Update()
    {
        UpdateWheelTransform();
        //isThrusting = Input.GetAxisRaw("RT") == 1;
    }

    private void FixedUpdate()
    {
        Steer();
        Accelerate();

        if (isThrusting)
        {
            Thrust(thrustForce);
        }
    }

    void Steer()
    { 
        wheelFrontLeftCollider.steerAngle = horizontalInput * maxSteerAngle;
        wheelFrontRightCollider.steerAngle = horizontalInput * maxSteerAngle;
        if(use4WheelMotion)
        {
            wheelFrontLeftCollider.steerAngle = horizontalInput * maxSteerAngle;
            wheelFrontRightCollider.steerAngle = horizontalInput * maxSteerAngle;
            wheelRearLeftCollider.steerAngle = -horizontalInput * maxSteerAngle;
            wheelRearRightCollider.steerAngle = -horizontalInput * maxSteerAngle;
        }
    }

    void Accelerate()
    {
        if(useForwardTraction)
        {
            wheelFrontLeftCollider.motorTorque = verticalInput * motorForce;
            wheelFrontRightCollider.motorTorque = verticalInput * motorForce;
        }
        else
        {
            wheelRearLeftCollider.motorTorque = verticalInput * motorForce;
            wheelRearRightCollider.motorTorque = verticalInput * motorForce;
        }

        if (use4WheelTraction)
        {
            wheelRearLeftCollider.motorTorque = verticalInput * motorForce;
            wheelRearRightCollider.motorTorque = verticalInput * motorForce;
            wheelFrontLeftCollider.motorTorque = verticalInput * motorForce;
            wheelFrontRightCollider.motorTorque = verticalInput * motorForce;
        }
    }

    void UpdateWheelTransform()
    {
        UpdateWheelPos(wheelFrontLeftCollider, wheelFrontLeftModel);
        UpdateWheelPos(wheelRearLeftCollider, wheelRearLeftModel);
        UpdateWheelPos(wheelFrontRightCollider, wheelFrontRightModel);
        UpdateWheelPos(wheelRearRightCollider, wheelRearRightModel);
    }

    void UpdateWheelPos(WheelCollider collider, Transform wheelTransform)
    {
        collider.GetWorldPose(out pos, out quat);

        wheelTransform.position = pos;
        wheelTransform.rotation = quat;
    }

    public void Reset()
    {
        horizontalInput = 0;
        verticalInput = 0;
    }

    private void Thrust(float force)
    {
        rb.velocity += transform.forward * force;
    }
}
