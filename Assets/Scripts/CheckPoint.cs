using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    public Transform nextCheckPoint;

    private void OnTriggerEnter(Collider other)
    {
        Agent agent = other.transform.parent.GetComponent<Agent>();
        if (agent != null)
        {
            if(agent.nextCheckPoint == transform)
            {
                agent.CheckPointReached(nextCheckPoint);
            }
        }
    }
}
