using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[ExecuteInEditMode]
public class TestInput : MonoBehaviour
{
    public Transform tr;
    void Start()
    {
        
    }

    void Update()
    {
        Vector2 forward = new Vector2(transform.forward.x, transform.forward.z);
        Vector2 direction = new Vector2((tr.position - transform.position).x, (tr.position - transform.position).z);

        //Debug.Log(Vector2.SignedAngle(forward, direction));
    }
}
