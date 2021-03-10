using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using NUnit.Framework.Internal;

public class Requirement : MonoBehaviour, IComparable<Requirement>
{
    public List<int> listOfInt;

    public int[][] jaggedArrayTest;
    public int[][][] arrayJaggedIn3D;

    public string performance;
    public int value;

    void Start()
    {
        Array2D();
        Array3D();
    }

    private void Array3D()
    {
        arrayJaggedIn3D = new int[4][][];

        for (int x = 0; x < arrayJaggedIn3D.Length; x++)
        {
            arrayJaggedIn3D[x] = new int[4][];
            for (int y = 0; y < arrayJaggedIn3D[x].Length; y++)
            {
                arrayJaggedIn3D[x][y] = new int[4];
                for (int z = 0; z < arrayJaggedIn3D[x][y].Length; z++)
                {
                    arrayJaggedIn3D[x][y][z] = 3;
                }
            }
        }
    }

    void Array2D()
    {
        jaggedArrayTest = new int[8][];

        for (int x = 0; x < jaggedArrayTest.Length; x++)
        {
            jaggedArrayTest[x] = new int[x + 1];
        }

        int u = 0;
        for (int x = 0; x < jaggedArrayTest.Length; x++)
        {
            for (int y = 0; y < jaggedArrayTest[x].Length; y++)
            {
                u++;
                jaggedArrayTest[x][y] = u;
            }
        }
    }

    public int CompareTo(Requirement other)
    {
        if(value > other.value)
        {
            return 1;
        }
        else
        {
            return -1;
        }
    }
}
