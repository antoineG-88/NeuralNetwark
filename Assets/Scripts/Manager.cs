using System.Collections;
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
    public GameObject[] colonyPanels;
    public Text[] averageDiffText;
    public Text[] bestFitText;
    public Text[] diffInterColonies;
    public Text avgDiffText;
    public Text minDiffText;
    public Text maxDiffText;

    [Header("Behavior Comparison")]
    public float negativeMultiplier;
    public float[] axonLayerMultiplier;
    public float axonDiffAmplifier;
    [Space]
    [Range(0f, 1f)] public float leaderFitnessSelectRatio;
    public float leaderMinDiffSelect;
    public int colonySize;
    public int maxLeader;
    public bool createLeaderInEachColony;
    public Color[] colonyColors;
    public Color mutantColor;
    public Material[] colonyMaterials;
    public Material[] mutantMaterials;

    public Agent agentPrefab;
    public Transform agentGroup;

    Agent agent;
    public List<AgentColony> agentColonies = new List<AgentColony>();
    private bool leaderCreatedThisGeneration;
    private List<Agent> agents = new List<Agent>();
    private float averageDiff;
    private bool colonyWasDestroyed;

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
        if (!isFirst && !colonyWasDestroyed)
        {
            DetectNewBehavior();
        }
        UpdateAgentNumber();
        Mutate();
        UpdateColonyInfo();
        ResetAgents();
        SetMaterials();
        /*Debug.Log("Colony 1 count : " + agentColonies[0].follower.Count);
        if(agentColonies.Count > 1)
            Debug.Log("Colony 2 count : " + agentColonies[1].follower.Count);
        if (agentColonies.Count > 2)
            Debug.Log("Colony 3 count : " + agentColonies[2].follower.Count);*/
    }

    bool shouldBeDestroyed = false;
    void SortColonies()
    {
        colonyWasDestroyed = false;
        agents.Sort();
        for (int c = 0; c < agentColonies.Count; c++)
        {
            agentColonies[c].follower.Sort();
            if(agentColonies[c].initialised && agentColonies[c].follower.Count > 0)
                agentColonies[c].leader = agentColonies[c].follower[0];
        }


        for (int c = 0; c < agentColonies.Count; c++)
        {
            if(agentColonies[c].initialised)
            {
                shouldBeDestroyed = false;
                for (int cc = 0; cc < agentColonies.Count; cc++)
                {
                    if (cc != c)
                    {
                        if (agentColonies[c].follower[0].fitness < agentColonies[cc].follower[agentColonies[cc].follower.Count - 1].fitness) // Check if the best fitness of a colony is worse that the worst of any other
                        {
                            Debug.Log("Colony " + c + " is bad, the best is worse the worst ^^");
                            shouldBeDestroyed = true;
                        }

                        if (agentColonies[c].follower[0].CompareBehavior(agentColonies[cc].follower[0], negativeMultiplier, axonLayerMultiplier, axonDiffAmplifier) < leaderMinDiffSelect)
                        {
                            Debug.Log("Colony " + c + " is really close to other colonies, it must be destroyed");
                            shouldBeDestroyed = true;
                        }
                    }

                }
                if (shouldBeDestroyed && c != 0)
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
                    if(createLeaderInEachColony)
                    {
                        float diff = agentColonies[c].follower[0].CompareBehavior(agentColonies[c].follower[i], negativeMultiplier, axonLayerMultiplier, axonDiffAmplifier);
                        if (agentColonies[c].follower[i].fitness > agentColonies[c].follower[0].fitness * leaderFitnessSelectRatio && diff > leaderMinDiffSelect)
                        {
                            Debug.Log("The agent " + i + "in colony " + c + " has been chosen as a new leader with a difference of " + Mathf.RoundToInt(diff) + " and a fitness of " + agents[i].fitness + " / " + agents[0].fitness + " (" + agents[i].fitness / agents[0].fitness + ")");
                            leaderCreatedThisGeneration = true;
                            AgentColony colony = new AgentColony
                            {
                                leader = agentColonies[c].follower[i],
                                follower = new List<Agent>()
                            };
                            agentColonies.Add(colony);
                        }
                    }
                    else
                    {

                        float diff = agents[0].CompareBehavior(agents[i], negativeMultiplier, axonLayerMultiplier, axonDiffAmplifier);
                        if (agents[i].fitness > agents[0].fitness * leaderFitnessSelectRatio && diff > leaderMinDiffSelect)
                        {
                            Debug.Log("The agent " + i + " has been chosen as a new leader with a difference of " + Mathf.RoundToInt(diff) + " and a fitness of " + agents[i].fitness + " / " + agents[0].fitness + " (" + agents[i].fitness/ agents[0].fitness + ")");
                            leaderCreatedThisGeneration = true;
                            AgentColony colony = new AgentColony
                            {
                                leader = agents[i],
                                follower = new List<Agent>()
                            };
                            agentColonies.Add(colony);
                        }
                    }
                    i++;
                } while (i < agents.Count && !leaderCreatedThisGeneration);
            }
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
        Destroy(agentColonies[colonyIndex].follower[agentColonies[colonyIndex].follower.Count - 1].gameObject);
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
        int index = 0;
        int baseCount = agentColonies[colonyIndex].follower.Count;
        for (int i = 0; i < baseCount; i++)
        {
            index = agents.IndexOf(agentColonies[colonyIndex].follower[agentColonies[colonyIndex].follower.Count - 1]);
            agents.RemoveAt(index);
            Destroy(agentColonies[colonyIndex].follower[agentColonies[colonyIndex].follower.Count - 1].gameObject);
            agentColonies[colonyIndex].follower.RemoveAt(agentColonies[colonyIndex].follower.Count - 1);
        }
        agentColonies.RemoveAt(colonyIndex);
        Focus();
        colonyWasDestroyed = true;
    }

    void Mutate()
    {
        for (int c = 0; c < agentColonies.Count; c++)
        {
            if (!agentColonies[c].initialised)
            {
                agentColonies[c].follower[0] = agentColonies[c].leader;
                for (int i = 1; i < agentColonies[c].follower.Count; i++)
                {
                    agentColonies[c].follower[i].net.CopyNet(agentColonies[c].leader.net);
                    agentColonies[c].follower[i].net.Mutate(mutationRate);
                    agentColonies[c].follower[i].SetMutatedMaterial(mutantMaterials[c], colonyColors[c], mutantColor);
                }
                agentColonies[c].initialised = true;
            }
            else
            {
                for (int i = (int)(agentColonies[c].follower.Count * mutationRatio); i < agentColonies[c].follower.Count; i++)
                {
                    agentColonies[c].follower[i].net.CopyNet(agentColonies[c].follower[i - (int)(agentColonies[c].follower.Count * mutationRatio)].net);
                    agentColonies[c].follower[i].net.Mutate(mutationRate);
                    agentColonies[c].follower[i].SetMutatedMaterial(mutantMaterials[c], colonyColors[c], mutantColor);
                }
            }
        }
    }

    private void UpdateColonyInfo()
    {
        float min = 50000;
        float max = 0;
        float diff = 0;
        float avgDiff = 0;
        for (int i = 1; i < agents.Count; i++)
        {
            diff = agents[i].CompareBehavior(agents[0], negativeMultiplier, axonLayerMultiplier, axonDiffAmplifier);
            if (diff > max)
            {
                max = diff;
            }
            if(diff < min)
            {
                min = diff;
            }
            avgDiff += diff;
        }
        avgDiff /= agents.Count - 1;
        avgDiffText.text = "Avg diff : " + ((int)avgDiff).ToString();
        minDiffText.text = "Min diff : " + ((int)min).ToString();
        maxDiffText.text = "Max diff : " + ((int)max).ToString();

        for (int c = 0; c < agentColonies.Count; c++)
        {
            averageDiff = 0;
            for (int y = 0; y < agentColonies[c].follower.Count; y++)
            {
                diff = agentColonies[c].follower[0].CompareBehavior(agentColonies[c].follower[y], negativeMultiplier, axonLayerMultiplier, axonDiffAmplifier);
                averageDiff += diff;
            }
            averageDiff /= agents.Count;
            averageDiffText[c].text = "Avg. diff : " + ((int)averageDiff).ToString();
            bestFitText[c].text = "Best fitness : " + ((int)agentColonies[c].follower[0].fitness).ToString();
        }

        for (int c = 0; c < colonyPanels.Length; c++)
        {
            if(c < agentColonies.Count)
            {
                colonyPanels[c].SetActive(true);
            }
            else
            {
                colonyPanels[c].SetActive(false);
            }
        }

        if (agentColonies.Count > 1)
        {
            diffInterColonies[0].text = "Diff C1/C2 : " + (int)agentColonies[0].follower[0].CompareBehavior(agentColonies[1].follower[0], negativeMultiplier, axonLayerMultiplier, axonDiffAmplifier);

            if (agentColonies.Count > 2)
            {
                diffInterColonies[1].text = "Diff C2/C3 : " + (int)agentColonies[1].follower[0].CompareBehavior(agentColonies[2].follower[0], negativeMultiplier, axonLayerMultiplier, axonDiffAmplifier);
                diffInterColonies[2].text = "Diff C1/C3 : " + (int)agentColonies[0].follower[0].CompareBehavior(agentColonies[2].follower[0], negativeMultiplier, axonLayerMultiplier, axonDiffAmplifier);
            }
            else
            {
                diffInterColonies[1].text = "";
                diffInterColonies[2].text = "";
            }
        }
        else
        {
            diffInterColonies[0].text = "";
            diffInterColonies[1].text = "";
            diffInterColonies[2].text = "";
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

    [System.Serializable]
    public class AgentColony
    {
        public Agent leader;
        public List<Agent> follower;

        public AgentColony()
        { }

        public bool initialised;
    }
}
