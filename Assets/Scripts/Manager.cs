using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

public class Manager : MonoBehaviour
{
    public bool loadOnlyBest;
    public int populationSize;
    public int trainingDuration = 30;
    public float mutationRate = 5;
    [Range(0f, 0.99f)] public float mutationRatio;
    public int currentGeneration;
    public Text generationtext;

    public Agent agentPrefab;
    public Transform agentGroup;

    Agent agent;

    List<Agent> agents = new List<Agent>();
    public CameraController cameraController;

    void Start()
    {
        StartCoroutine(InitCoroutine());
    }

    private void Update()
    {
        generationtext.text = currentGeneration.ToString();
    }

    IEnumerator InitCoroutine()
    {
        NewGeneration();
        InitNeuralNetworkViewer();
        Focus();
        yield return new WaitForSeconds(trainingDuration);

        StartCoroutine(Loop());
    }
    private void NewGeneration()
    {
        currentGeneration++;
        agents.Sort();
        UpdateAgentNumber();
        Mutate();
        RestAgents();
        SetMaterials();
    }

    void UpdateAgentNumber()
    {
        if (agents.Count != populationSize)
        {
            int dif = populationSize - agents.Count;
            if (dif > 0)
            {
                for (int i = 0; i < dif; i++)
                {
                    AddAgent();
                }
            }
            else
            {
                for (int i = 0; i < dif; i++)
                {
                    RemoveAgent();
                }
            }
        }
    }

    void RemoveAgent()
    {
        Destroy(agents[agents.Count - 1].transform);
        agents.RemoveAt(agents.Count - 1);
    }

    void AddAgent()
    {
        agent = Instantiate(agentPrefab, Vector3.zero, Quaternion.identity, agentGroup);
        agent.net = new NeuralNetwork(agentPrefab.net.layers);
        agents.Add(agent);
    }

    void Mutate()
    {
        for (int i = (int)(agents.Count * mutationRatio); i < agents.Count; i++)
        {
            agents[i].net.CopyNet(agents[i - (int)(agents.Count * mutationRatio)].net);
            agents[i].net.Mutate(mutationRate);
            agents[i].SetMutatedMaterial();
        }
    }

    private void RestAgents()
    {
        for (int i = 0; i < agents.Count; i++)
        {
            agents[i].ResetAgent();
        }
    }

    void SetMaterials()
    {
        agents[0].SetFirstMaterial();
        for (int i = 1; i < (int)(agents.Count * mutationRatio); i++)
        {
            agents[i].SetDefaultMaterial();
        }
    }

    void Focus()
    {
        NeuralNetworkViewer.instance.agent = agents[0];
        NeuralNetworkViewer.instance.RefreshAxons();

        cameraController.positionTarget = agents[0].transform;
    }

    public void ReFocus()
    {
        agents.Sort();
        Focus();
    }

    IEnumerator Loop()
    {
        Focus();
        NewGeneration();
        yield return new WaitForSeconds(trainingDuration);

        StartCoroutine(Loop());
    }

    public void End()
    {
        StopAllCoroutines();
        StartCoroutine(InitCoroutine());
    }

    public void ResetNets()
    {
        for (int i = 0; i < agents.Count; i++)
        {
            agents[i].net = new NeuralNetwork(agentPrefab.net.layers);
        }
        currentGeneration = 0;
        End();
    }

    public void Save()
    {
        List<NeuralNetwork> nets = new List<NeuralNetwork>();

        for (int i = 0; i < agents.Count; i++)
        {
            nets.Add(agents[i].net);
        }

        DataManager.instance.Save(nets, currentGeneration);
    }

    public void Load()
    {
        Data data = DataManager.instance.Load();

        if (data != null)
        {
            for (int i = 0; i < agents.Count; i++)
            {
                if(loadOnlyBest)
                {
                    agents[i].net = data.nets[0];
                }
                else
                {
                    if(i >= data.nets.Count)
                    {
                        agents[i].net = data.nets[0];
                    }
                    else
                    {
                        agents[i].net = data.nets[i];
                    }
                }
            }

            currentGeneration = data.generation;
        }

        End();
    }

    private void InitNeuralNetworkViewer()
    {
        NeuralNetworkViewer.instance.Init(agents[0]);
    }
}
