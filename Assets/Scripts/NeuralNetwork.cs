using System;
using System.Collections.Generic;

[Serializable]
public class NeuralNetwork
{
    public int[] layers;
    public float[][] neurons;
    public float[][][] axons;

    private int x, y, z;
    private float value;

    public void CopyNet(NeuralNetwork netCopy)
    {
        for (int x = 0; x < netCopy.axons.Length; x++)
        {
            for (int y = 0; y < netCopy.axons[x].Length; y++)
            {
                for (int z = 0; z < netCopy.axons[x][y].Length; z++)
                {
                    axons[x][y][z] = netCopy.axons[x][y][z];
                }
            }
        }
    }

    public NeuralNetwork()
    { }

    public NeuralNetwork(int[] _layers)
    {
        layers = new int[_layers.Length];

        for (int x = 0; x < _layers.Length; x++)
        {
            layers[x] = _layers[x];
        }

        InitNeuron();
        InitAxon();
    }

    private void InitNeuron()
    {
        neurons = new float[layers.Length][];

        for (x = 0; x < layers.Length; x++)
        {
            neurons[x] = new float[layers[x]];
        }
    }

    private void InitAxon()
    {
        axons = new float[neurons.Length - 1][][];
        for (x = 0; x < neurons.Length - 1; x++)
        {
            axons[x] = new float[neurons[x + 1].Length][];

            for (y = 0; y < neurons[x + 1].Length; y++)
            {
                axons[x][y] = new float[neurons[x].Length];

                for (int z = 0; z < layers[x]; z++)
                {
                    axons[x][y][z] = UnityEngine.Random.Range(-1f, 1f);
                }

            }
        }
    }

    public void FeedForward(float[] inputs)
    {
        neurons[0] = inputs;
        for (int x = 1; x < layers.Length; x++)
        { 
            for (int y = 0; y < layers[x]; y++)
            {
                value = 0;
                for (int z = 0; z < layers[x - 1]; z++)
                {
                    value += neurons[x - 1][z] * axons[x - 1][y][z];
                }
                neurons[x][y] = (float)Math.Tanh(value);
            }
        }
    }

    float random;
    float random2;
    public void Mutate(float probability)
    {
        for (int x = 0; x < axons.Length; x++)
        {
            for (int y = 0; y < axons[x].Length; y++)
            {
                for (int z = 0; z < axons[x][y].Length; z++)
                {
                    random = UnityEngine.Random.Range(0f, 100f);
                    
                    if (random < 0.06f * probability)
                    {
                        axons[x][y][z] = UnityEngine.Random.Range(-1f, 1f);
                    }
                    else if (random < 0.07f * probability)
                    {
                        axons[x][y][z] *= -1f;
                    }
                    else if (random < 0.50f * probability)
                    {
                        axons[x][y][z] += UnityEngine.Random.Range(-0.1f, 0.1f);
                    }
                    else if (random < 0.75f * probability)
                    {
                        axons[x][y][z] *= UnityEngine.Random.Range(0f, 1f) + 1f;
                    }
                    else if(random < 1f * probability)
                    {
                        axons[x][y][z] *= UnityEngine.Random.Range(0f, 1f);
                    }

                    /*random2 = UnityEngine.Random.Range(0, 2);
                    axons[x][y][z] += random2 == 0 ? (+random * 1f * probability) : (-random * 1f * probability);*/
                }
            }
        }
    }
}
