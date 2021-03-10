using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInut : MonoBehaviour
{
    public CarController[] carControllers;

    void Update()
    {
        foreach(CarController car in carControllers)
        {
            car.horizontalInput = Input.GetAxis("Horizontal");
            car.verticalInput = Input.GetAxis("Gachette");
        }
    }

    public void ResetPlayerCar()
    {
        Rigidbody rb = carControllers[0].GetComponent<Rigidbody>();
        carControllers[0].transform.position = Vector3.zero;
        carControllers[0].transform.rotation = Quaternion.identity;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
}
