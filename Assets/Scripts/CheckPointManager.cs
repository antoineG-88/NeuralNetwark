using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckPointManager : MonoBehaviour
{
    public static CheckPointManager instance;
    public Transform firstCheckPoint;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        firstCheckPoint = transform.GetChild(0);
    }

    [ContextMenu("Initialize")]
    private void Init()
    {
        for(int i = 0; i <  transform.childCount; i++)
        {
            CheckPoint checkPoint = transform.GetChild(i).GetComponent<CheckPoint>();
            if(i == transform.childCount - 1)
            {
                checkPoint.nextCheckPoint = transform.GetChild(0);
            }
            else
            {
                checkPoint.nextCheckPoint = transform.GetChild(i + 1);
            }
        }
    }
}
