﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

public class Manager : MonoBehaviour
{
    public bool loadOnlyBest;
    public int trainingDuration = 30;
    public float mutationRate = 5;
    [Range(0f, 0.99f)] public float mutationRatio;
    public int currentGeneration;
    public Text generationtext;
    public Text[] averageDiffText;

    [Header("Behavior Comparison")]
    public float negativeMultiplier;
    public float[] axonLayerMultiplier;
    public float axonDiffAmplifier;
    [Space]
    [Range(0f, 1f)] public float leaderFitnessSelectRatio;
    public float leaderMinDiffSelect;
    public int colonySize;
    public int maxLeader;
    public Color[] colonyColors;
    public Color mutantColor;
    public Material[] colonyMaterials;
    public Material[] mutantMaterials;

    public Agent agentPrefab;
    public Transform agentGroup;

    Agent agent;
    private List<AgentColony> agentColonies = new List<AgentColony>();
    private bool leaderCreatedThisGeneration;
    private List<Agent> agents = new List<Agent>();
    private float averageDiff;

    public CameraController cameraController;

    void Start()
    {
        StartCoroutine(InitCoroutine(true));
    }

    private void Update()
    {
        generationtext.text = currentGeneration.ToString();
    }

    IEnumerator InitCoroutine(bool firstTime)
    {
        if(firstTime)
        {
            Debug.Log("FirstColony Created");
            agentColonies.Add(new AgentColony() { follower = new List<Agent>() , initialised = true});
        }
        NewGeneration(true);
        InitNeuralNetworkViewer();
        Focus();
        yield return new WaitForSeconds(trainingDuration);

        StartCoroutine(Loop());
    }

    IEnumerator Loop()
    {
        Focus();
        NewGeneration(false);
        yield return new WaitForSeconds(trainingDuration);

        StartCoroutine(Loop());
    }

    private void NewGeneration(bool isFirst)
    {
        currentGeneration++;
        SortColonies();
        if (!isFirst)
        {
            DetectNewBehavior();
        }
        UpdateAgentNumber();
        Mutate();
        ResetAgents();
        SetMaterials();
    }

    bool isBestWorst = false;
    void SortColonies()
    {
        for (int c = 0; c < agentColonies.Count; c++)
        {
            agentColonies[c].follower.Sort();
        }
        for (int c = 0; c < agentColonies.Count; c++)
        {
            if(agentColonies[c].initialised)
            {
                isBestWorst = false;
                for (int cc = 0; cc < agentColonies.Count; cc++)
                {
                    if (cc != c)
                    {
                        if (agentColonies[c].follower[0].fitness < agentColonies[cc].follower[agentColonies[cc].follower.Count - 1].fitness)
                        {
                            isBestWorst = true;
                        }
                    }
                }
                if (isBestWorst)
                {
                    DestroyColony(c);
                }
            }
        }
    }

    void DetectNewBehavior()
    {
        leaderCreatedThisGeneration = false;
        int previousColonyNumber = agentColonies.Count;
        for (int c = 0; c < previousColonyNumber; c++)
        {
            if (agentColonies.Count < maxLeader)
            {
                int i = 0;
                do
                {
                    float diff = agentColonies[c].follower[0].CompareBehavior(agentColonies[c].follower[i], negativeMultiplier, axonLayerMultiplier, axonDiffAmplifier);
                    if (agentColonies[c].follower[i].fitness > agentColonies[c].follower[0].fitness * leaderFitnessSelectRatio && diff > leaderMinDiffSelect)
                    {
                        Debug.Log("The agent " + i + " has been chosen as a new leader with a difference of " + Mathf.RoundToInt(diff) + " and a fitness of " + agentColonies[c].follower[i].fitness);
                        leaderCreatedThisGeneration = true;
                        AgentColony colony = new AgentColony
                        {
                            leader = agentColonies[c].follower[i],
                            follower = new List<Agent>()
                        };
                        agentColonies.Add(colony);
                    }
                    i++;
                } while (i < agentColonies[c].follower.Count && !leaderCreatedThisGeneration);
            }
            averageDiff = 0;
            for (int y = 0; y < agentColonies[c].follower.Count; y++)
            {
                float diff = agentColonies[c].follower[0].CompareBehavior(agentColonies[c].follower[y], negativeMultiplier, axonLayerMultiplier, axonDiffAmplifier);
                averageDiff += diff;
            }
            averageDiff /= agents.Count;
            averageDiffText[c].text = averageDiff.ToString();
        }

        /*for (int y = 0; y < agents.Count; y++)
        {
            Debug.Log("Agent " + y + " : " + diff);
        }*/
    }

    void UpdateAgentNumber()
    {
        for (int c = 0; c < agentColonies.Count; c++)
        {
            if (agentColonies[c].follower.Count != colonySize)
            {
                int dif = colonySize - agentColonies[c].follower.Count;
                if (dif > 0)
                {
                    for (int i = 0; i < dif; i++)
                    {
                        AddAgentInColony(c);
                    }
                }
                else
                {
                    for (int i = 0; i < dif; i++)
                    {
                        RemoveAgentInColony(c);
                    }
                }
            }
        }
    }

    void RemoveAgentInColony(int colonyIndex)
    {
        int index = agents.IndexOf(agentColonies[colonyIndex].follower[agentColonies[colonyIndex].follower.Count - 1]);
        agents.RemoveAt(index);
        Destroy(agentColonies[colonyIndex].follower[agentColonies[colonyIndex].follower.Count - 1].transform);
        agentColonies[colonyIndex].follower.RemoveAt(agentColonies[colonyIndex].follower.Count - 1);
    }

    void AddAgentInColony(int colonyIndex)
    {
        agent = Instantiate(agentPrefab, Vector3.zero, Quaternion.identity, agentGroup);
        agent.net = new NeuralNetwork(agentPrefab.net.layers);
        agentColonies[colonyIndex].follower.Add(agent);
        agents.Add(agent);
    }

    private void DestroyColony(int colonyIndex)
    {
        Debug.Log("Colony number : " + colonyIndex + " has been destroyed");
        for (int i = 0; i < agentColonies[colonyIndex].follower.Count; i++)
        {
            agents.Remove(agentColonies[colonyIndex].follower[0]);
            Destroy(agentColonies[colonyIndex].follower[0]);
            agentColonies[colonyIndex].follower.RemoveAt(0);
        }
        agentColonies.RemoveAt(colonyIndex);
    }

    void Mutate()
    {
        for (int c = 0; c < agentColonies.Count; c++)
        {
            for (int i = (int)(agentColonies[c].follower.Count * mutationRatio); i < agentColonies[c].follower.Count; i++)
            {
                if(!agentColonies[c].initialised)
                {
                    agentColonies[c].follower[i].net.CopyNet(agentColonies[c].follower[0].net);
                    agentColonies[c].initialised = true;
                }
                else
                {
                    agentColonies[c].follower[i].net.CopyNet(agentColonies[c].follower[i - (int)(agentColonies[c].follower.Count * mutationRatio)].net);
                }
                agentColonies[c].follower[i].net.Mutate(mutationRate);
                agentColonies[c].follower[i].SetMutatedMaterial(mutantMaterials[c], colonyColors[c], mutantColor);
            }
        }
    }

    private void ResetAgents()
    {
        for (int i = 0; i < agents.Count; i++)
        {
            agents[i].ResetAgent();
        }
    }

    void SetMaterials()
    {
        agents[0].SetFirstMaterial();
        for (int c = 0; c < agentColonies.Count; c++)
        {
            for (int i = 1; i < (int)(agentColonies[c].follower.Count * mutationRatio); i++)
            {
                agentColonies[c].follower[i].SetDefaultMaterial(colonyMaterials[c], colonyColors[c]);
            }
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


    public void End()
    {
        StopAllCoroutines();
        StartCoroutine(InitCoroutine(false));
    }

    public void ResetNets()
    {
        for (int i = 0; i < agents.Count; i++)
        {
            agents[i].net = new NeuralNetwork(agentPrefab.net.layers);
        }
        agentColonies.Clear();
        Debug.Log("FirstColony Created");
        agentColonies.Add(new AgentColony() { follower = new List<Agent>() , initialised = true});

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

    public class AgentColony
    {
        public Agent leader;
        public List<Agent> follower;

        public AgentColony()
        { }

        public bool initialised;
    }
}
