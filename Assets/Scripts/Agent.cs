using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Agent : MonoBehaviour, IComparable<Agent>
{
    public NeuralNetwork net;
    public CarController carController;
    public Transform nextCheckPoint;
    public CheckPoint nextCheckPointScript;

    public float fitness;
    public float distanceTraveled;
    public Rigidbody rb;

    private float[] inputs;

    public float nextCheckPointDist;

    public void ResetAgent()
    {
        fitness = 0;
        distanceTraveled = 0;
        transform.position = Vector3.zero;
        transform.rotation = Quaternion.identity;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        inputs = new float[net.layers[0]];
        carController.Reset();

        nextCheckPoint = CheckPointManager.instance.firstCheckPoint.transform;
        nextCheckPointScript = CheckPointManager.instance.firstCheckPoint;
        nextCheckPointDist = Vector3.Distance(nextCheckPoint.position, transform.position);
    }

    private void FixedUpdate()
    {
        InputUpdate();
        OutPutUpdate();
        FitnessUpdate();
    }

    public void InputUpdate()
    {
        // 12 Input
        inputs[0] = RaySensor(transform.position + Vector3.up * 0.2f, transform.forward, 3f);
        inputs[1] = RaySensor(transform.position + Vector3.up * 0.2f, -transform.right, 2f);
        inputs[2] = RaySensor(transform.position + Vector3.up * 0.2f, transform.right, 2f);
        inputs[3] = RaySensor(transform.position + Vector3.up * 0.2f, transform.forward + transform.right, 2f);
        inputs[4] = RaySensor(transform.position + Vector3.up * 0.2f, transform.forward - transform.right, 2f);

        inputs[5] = (float)Math.Tanh(rb.velocity.magnitude * 0.05f);
        inputs[6] = (float)Math.Tanh(rb.angularVelocity.y * 0.1f);


        inputs[7] = rb.velocity.magnitude > 5 ? (float)Math.Tanh(Vector2.SignedAngle(rb.velocity, transform.forward) * 0.05f) : 0;

        inputs[8] = RaySensor(transform.position + Vector3.up * 0.2f, transform.forward, 10f);
        inputs[9] = RaySensor(transform.position + Vector3.up * 0.2f, transform.forward + transform.right, 10f);
        inputs[10] = RaySensor(transform.position + Vector3.up * 0.2f, transform.forward - transform.right, 10f);

        inputs[11] = 1f;
    }


    private RaycastHit hit;
    private float range = 4;
    public LayerMask mask;

    private float RaySensor(Vector3 pos, Vector3 direction, float length)
    {
        if (Physics.Raycast(pos, direction, out hit, length * range, mask))
        {
            Debug.DrawLine(pos, hit.point, Color.Lerp(Color.green, Color.red, (length * range - hit.distance) / (length * range)));
            return (length * range - hit.distance) / (length * range);
        }
        else
        {
            Debug.DrawRay(pos, direction * length * range, Color.green);
            return 0;
        }
    }

    private void OutPutUpdate()
    {
        net.FeedForward(inputs);

        //2 output
        carController.horizontalInput = net.neurons[net.neurons.Length - 1][0];
        carController.verticalInput = net.neurons[net.neurons.Length - 1][1];
    }

    float currentDistance;
    private void FitnessUpdate()
    {
        currentDistance = distanceTraveled + (nextCheckPointDist - (transform.position - nextCheckPoint.position).magnitude);

        if (fitness < currentDistance)
        {
            fitness = currentDistance;
        }
    }

    public void CheckPointReached(CheckPoint checkPoint)
    {
        distanceTraveled += nextCheckPointDist;
        nextCheckPoint = checkPoint.transform;
        nextCheckPointScript = checkPoint;
        nextCheckPointDist = (transform.position - checkPoint.transform.position).magnitude;
    }

    public Renderer render;
    public Material firstMaterial;
    public Material mutantMaterial;
    public Material defaultMaterial;
    public Material followerMaterial;

    public void SetFirstMaterial()
    {
        if (render != null)
            render.material = firstMaterial;
    }
    public void SetMutatedMaterial(Material mat, Color color, Color mutantColor)
    {
        transform.GetChild(0).gameObject.SetActive(false);
        if (render != null)
        {
            if (mat != null)
            {
                render.material = mat;
                mat.color = Color.Lerp(color, mutantColor, 0.2f);
            }
            else
            {
                render.material = mutantMaterial;
            }
        }
    }
    public void SetDefaultMaterial(Material mat, Color color)
    {
        transform.GetChild(0).gameObject.SetActive(false);
        if (render != null)
        {
            if (mat != null)
            {
                render.material = mat;
                mat.color = color;
            }
            else
            {
                render.material = defaultMaterial;
            }
        }
    }
    public void SetFollowerMaterial()
    {
        transform.GetChild(0).gameObject.SetActive(false);
        if (render != null)
        {
            render.material = followerMaterial;
        }
    }

    public int CompareTo(Agent other)
    {
        if(fitness < other.fitness)
        {
            return 1;
        }
        if (fitness > other.fitness)
        {
            return -1;
        }
        return 0;
    }

    float diff = 0;
    float axonDiff = 0;
    int axonNb;

    public float CompareBehavior(Agent ag, float negativeMultiplier, float[] axonLayerMultiplier, float axonDiffAmplifier)
    {
        diff = 0;
        axonNb = 0;
        for (int x = 0; x < net.axons.Length; x++)
        {
            for (int y = 0; y < net.axons[x].Length; y++)
            {
                for (int z = 0; z < net.axons[x][y].Length; z++)
                {
                    axonNb++;
                    axonDiff = Mathf.Abs((float)Math.Tanh(net.axons[x][y][z]) - (float)Math.Tanh(ag.net.axons[x][y][z]));
                    axonDiff = Mathf.Clamp(axonDiff, 0.0f, 1f);
                    axonDiff = Mathf.Pow(axonDiff, axonDiffAmplifier);
                    axonDiff *= Mathf.Sign(net.axons[x][y][z]) == Mathf.Sign(ag.net.axons[x][y][z]) ? 1 : negativeMultiplier;
                    diff += axonDiff * axonLayerMultiplier[x];
                }
            }
        }
        return diff;
    }
}
