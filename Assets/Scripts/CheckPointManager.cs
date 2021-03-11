using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckPointManager : MonoBehaviour
{
    public static CheckPointManager instance;
    public CheckPoint firstCheckPoint;
    public CheckPoint[] checkPoints;
    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        firstCheckPoint = transform.GetChild(0).GetComponent<CheckPoint>();
    }

    [ContextMenu("Initialize")]
    private void Init()
    {
        checkPoints = new CheckPoint[transform.childCount];
        for(int i = 0; i <  transform.childCount; i++)
        {
            CheckPoint checkPoint = transform.GetChild(i).GetComponent<CheckPoint>();
            checkPoints[i] = checkPoint;
            if (i == transform.childCount - 1)
            {
                checkPoint.nextCheckPoint = transform.GetChild(0).GetComponent<CheckPoint>();
            }
            else
            {
                checkPoint.nextCheckPoint = transform.GetChild(i + 1).GetComponent<CheckPoint>();
            }
        }
    }
}
