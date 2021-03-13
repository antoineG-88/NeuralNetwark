using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

public class Manager : MonoBehaviour
{
    [Header("Global settings")]
    public int trainingDuration = 30;
    public float mutationRate = 5;
    [Range(0f, 0.99f)] public float mutationRatio;
    public Text generationtext;

    [Header("Behavior Comparison")]
    public float negativeMultiplier;
    public float[] axonLayerMultiplier;
    public float axonDiffAmplifier;
    [Header("Colony Options")]
    public int colonySize;
    public int maxLeader;
    public int minGenerationToCreateLeader;
    [Range(0f, 1f)] public float leaderFitnessSelectRatio;
    [Range(0.1f, 3f)] public float leaderMinDiffSelectRatio;
    public bool isDiffSelectRatioOnMaxDiff;
    public bool isDiffSelectRatioOnAvgColonies;
    public Color[] colonyColors;
    public Color mutantColor;
    [Header("Colony Refs")]
    public Material[] colonyMaterials;
    public Material[] mutantMaterials;
    public GameObject colonyPanel;
    public Text colonyTitleText;
    public Text averageColonyDiffText;
    public Text maxColonyDiffText;
    public Text bestColonyFitText;
    public Text[] diffInterColonies;
    public Text minDiffSelectText;
    public Text avgDiffText;
    public Text minDiffText;
    public Text maxDiffText;
    [Space]
    public Agent agentPrefab;
    public Transform agentGroup;

    private float maxDiff;
    private float minDiff;
    private float avgDiff;

    private Agent agent;
    private List<AgentColony> agentColonies = new List<AgentColony>();
    private List<Agent> agents = new List<Agent>();
    private float averageDiff;
    private int currentGeneration;
    private float currentMinDiffLeaderSelect;

    private int currentFocusedColony;

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
            agentColonies.Add(new AgentColony() { follower = new List<Agent>() , leaderNet = new NeuralNetwork(agentPrefab.net.layers) ,initialised = true});
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
        if (!isFirst && currentGeneration > minGenerationToCreateLeader)
        {
            DetectNewBehavior();
        }
        UpdateAgentNumber();
        Mutate();
        UpdateColonyInfo();
        UpdateColonyInfoDisplay();
        ResetAgents();
        SetMaterials();
    }

    bool shouldBeDestroyed = false;
    void SortColonies()
    {
        UpdateMinSelect();
        agents.Sort();
        for (int c = 0; c < agentColonies.Count; c++)
        {
            agentColonies[c].follower.Sort();
            if(agentColonies[c].initialised && agentColonies[c].follower.Count > 0)
            {
                agentColonies[c].leader = agentColonies[c].follower[0];
                agentColonies[c].leaderNet.CopyNet(agentColonies[c].leader.net);
            }
        }


        for (int c = 0; c < agentColonies.Count; c++)
        {
            if (agentColonies[c].initialised)
            {
                shouldBeDestroyed = false;
                for (int cc = 0; cc < agentColonies.Count; cc++)
                {
                    if (cc != c)
                    {
                        if (agentColonies[c].leader.fitness < agentColonies[cc].follower[agentColonies[cc].follower.Count - 1].fitness) // Check if the best fitness of a colony is worse that the worst of any other
                        {
                            Debug.Log("Colony " + c + " is bad, the best is worse the worst ^^");
                            shouldBeDestroyed = true;
                        }

                        if (agentColonies[c].leader.CompareBehavior(agentColonies[cc].leader, negativeMultiplier, axonLayerMultiplier, axonDiffAmplifier) < currentMinDiffLeaderSelect)
                        {
                            if (c != 0)
                            {
                                Debug.Log(currentMinDiffLeaderSelect);
                                Debug.Log("Colony " + c + " is really close to other colonies, it must be destroyed. Diff : " + (int)agentColonies[c].leader.CompareBehavior(agentColonies[cc].leader, negativeMultiplier, axonLayerMultiplier, axonDiffAmplifier));
                                shouldBeDestroyed = true;
                            }
                        }
                    }

                }
                if (shouldBeDestroyed)
                {
                    DestroyColony(c);
                }
            }
        }
    }

    float diff = 0;
    float bestDiff = 0;
    void DetectNewBehavior()
    {
        if (agentColonies.Count < maxLeader)
        {
            bestDiff = 0;
            agent = null;
            bool newLeaderFound = false;
            for (int i = 0; i < agents.Count; i++)
            {
                diff = agents[0].CompareBehavior(agents[i], negativeMultiplier, axonLayerMultiplier, axonDiffAmplifier);

                if (agents[i].fitness > agents[0].fitness * leaderFitnessSelectRatio && diff > currentMinDiffLeaderSelect && diff > bestDiff)
                {
                    bool leaderAlreaydExists = false;
                    //bool isSameAsPreviousNewLeader = false;
                    for (int c = 0; c < agentColonies.Count; c++)
                    {
                        //Debug.Log("Leader diif with colony : " + c + " : " + agents[i].CompareBehavior(agentColonies[c].leader, negativeMultiplier, axonLayerMultiplier, axonDiffAmplifier));
                        if (agents[i].CompareBehavior(agentColonies[c].leader, negativeMultiplier, axonLayerMultiplier, axonDiffAmplifier) < currentMinDiffLeaderSelect)
                        {
                            leaderAlreaydExists = true;
                        }
                    }

                    if (!leaderAlreaydExists)
                    {
                        bestDiff = diff;
                        agent = agents[i];
                        newLeaderFound = true;
                        
                    }
                }
            }

            if (newLeaderFound)
            {
                CreateNewColony(agent);
            }
        }
    }

    void CreateNewColony(Agent leaderReference)
    {
        currentFocusedColony = 0;
        Debug.Log("New colony ! A new leader with a difference of " + Mathf.RoundToInt(diff) + " and a fitness of " + leaderReference.fitness + " / " + agents[0].fitness + " (" + leaderReference.fitness / agents[0].fitness + ")");
        AgentColony colony = new AgentColony
        {
            leaderNet = new NeuralNetwork(agentPrefab.net.layers),
            follower = new List<Agent>(),
            initialised = false
        };
        colony.leaderNet.CopyNet(leaderReference.net);
        agentColonies.Add(colony);
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
        currentFocusedColony = 0;
        Debug.Log("Colony number : " + colonyIndex + " has been destroyed");
        int index = 0;
        int baseCount = agentColonies[colonyIndex].follower.Count;
        for (int i = 0; i < baseCount; i++)
        {
            index = agents.IndexOf(agentColonies[colonyIndex].follower[agentColonies[colonyIndex].follower.Count - 1]);
            if(index != -1)
                agents.RemoveAt(index);
            Destroy(agentColonies[colonyIndex].follower[agentColonies[colonyIndex].follower.Count - 1].gameObject);
            agentColonies[colonyIndex].follower.RemoveAt(agentColonies[colonyIndex].follower.Count - 1);
        }
        agentColonies.RemoveAt(colonyIndex);
        Focus();
    }

    void Mutate()
    {
        for (int c = 0; c < agentColonies.Count; c++)
        {
            if (!agentColonies[c].initialised)
            {
                InitialiseColonyMutations(c);
            }
            else
            {
                int copyCount = 0;
                int podiumSelectionToCopy = (int)(agentColonies[c].follower.Count * mutationRatio) - 1;
                for (int i = (int)(agentColonies[c].follower.Count * mutationRatio); i < agentColonies[c].follower.Count; i++)
                {
                    agentColonies[c].follower[i].net.CopyNet(agentColonies[c].follower[copyCount].net);
                    agentColonies[c].follower[i].net.Mutate(mutationRate);
                    agentColonies[c].follower[i].SetMutatedMaterial(mutantMaterials[c], colonyColors[c], mutantColor);
                    copyCount++;
                    if(copyCount == podiumSelectionToCopy)
                    {
                        copyCount = 0;
                    }
                }
            }
        }
    }

    void InitialiseColonyMutations(int colonyIndex)
    {
        agentColonies[colonyIndex].follower[0].net.CopyNet(agentColonies[colonyIndex].leaderNet);
        agentColonies[colonyIndex].leader = agentColonies[colonyIndex].follower[0];

        for (int i = 1; i < agentColonies[colonyIndex].follower.Count; i++)
        {
            agentColonies[colonyIndex].follower[i].net.CopyNet(agentColonies[colonyIndex].leader.net);
            agentColonies[colonyIndex].follower[i].net.Mutate(mutationRate);
            agentColonies[colonyIndex].follower[i].SetMutatedMaterial(mutantMaterials[colonyIndex], colonyColors[colonyIndex], mutantColor);
        }
        agentColonies[colonyIndex].initialised = true;
        
    }

    private void UpdateColonyInfoDisplay()
    {
        avgDiffText.text = "Avg diff : " + ((int)avgDiff).ToString();
        minDiffText.text = "Min diff : " + ((int)minDiff).ToString();
        maxDiffText.text = "Max diff : " + ((int)maxDiff).ToString();

        colonyTitleText.color = colonyColors[currentFocusedColony];
        colonyTitleText.text = "Colony " + (currentFocusedColony + 1);

        averageColonyDiffText.color = colonyColors[currentFocusedColony];
        averageColonyDiffText.text = "Avg. diff : " + ((int)agentColonies[currentFocusedColony].avgDiff).ToString();
        maxColonyDiffText.color = colonyColors[currentFocusedColony];
        maxColonyDiffText.text = "Max diff : " + ((int)agentColonies[currentFocusedColony].maxDiff).ToString();
        bestColonyFitText.color = colonyColors[currentFocusedColony];
        bestColonyFitText.text = "Best fitness : " + ((int)agentColonies[currentFocusedColony].bestFitness).ToString();
        int minus = 0;
        for (int cc = 0; cc < diffInterColonies.Length; cc++)
        {
            if (currentFocusedColony == cc)
            {
                minus = 1;
            }

            if ((cc + minus) < agentColonies.Count)
            {
                diffInterColonies[cc].text = "Diff C" + (cc + minus + 1) + " : " + (int)agentColonies[currentFocusedColony].follower[0].CompareBehavior(agentColonies[cc + minus].follower[0], negativeMultiplier, axonLayerMultiplier, axonDiffAmplifier);
            }
            else
            {
                diffInterColonies[cc].text = "";
            }
        }

        minDiffSelectText.text = "Min diff select : " + (int)currentMinDiffLeaderSelect;
    }

    private void UpdateColonyInfo()
    {
        minDiff = 50000;
        maxDiff = 0;
        diff = 0;
        avgDiff = 0;
        for (int i = 1; i < agents.Count; i++)
        {
            diff = agents[i].CompareBehavior(agents[0], negativeMultiplier, axonLayerMultiplier, axonDiffAmplifier);
            if (diff > maxDiff)
            {
                maxDiff = diff;
            }
            if(diff < minDiff)
            {
                minDiff = diff;
            }
            avgDiff += diff;
        }
        avgDiff /= agents.Count - 1;


        for (int c = 0; c < agentColonies.Count; c++)
        {
            agentColonies[c].maxDiff = 0;
            averageDiff = 0;
            for (int y = 0; y < agentColonies[c].follower.Count; y++)
            {
                diff = agentColonies[c].follower[0].CompareBehavior(agentColonies[c].follower[y], negativeMultiplier, axonLayerMultiplier, axonDiffAmplifier);
                averageDiff += diff;

                if(diff > agentColonies[c].maxDiff)
                {
                    agentColonies[c].maxDiff = diff;
                }
            }
            averageDiff /= agents.Count;
            agentColonies[c].avgDiff = averageDiff;
            if(agentColonies[c].bestFitness < agentColonies[c].follower[0].fitness)
                agentColonies[c].bestFitness = agentColonies[c].follower[0].fitness;

        }
        UpdateMinSelect();
    }

    void UpdateMinSelect()
    {
        if (isDiffSelectRatioOnAvgColonies)
        {
            float avgMaxDiffs = 0;
            float avgAvgDiffs = 0;
            int minus = 0;
            for (int c = 0; c < agentColonies.Count; c++)
            {
                if (agentColonies[c].avgDiff > 0)
                {
                    avgAvgDiffs += agentColonies[c].avgDiff;
                }
                else
                {
                    minus++;
                }
                if (agentColonies[c].maxDiff > avgMaxDiffs)
                {
                    avgMaxDiffs = agentColonies[c].maxDiff;
                }
            }
            avgAvgDiffs /= agentColonies.Count - minus;

            currentMinDiffLeaderSelect = (isDiffSelectRatioOnMaxDiff ? avgMaxDiffs * leaderMinDiffSelectRatio : avgAvgDiffs * leaderMinDiffSelectRatio);
        }
        else
        {
            currentMinDiffLeaderSelect = (isDiffSelectRatioOnMaxDiff ? maxDiff * leaderMinDiffSelectRatio : avgDiff * leaderMinDiffSelectRatio);
        }
    }

    public void ChangeColonyFocus(string s)
    {
        int colonyFocusInput;
        if (int.TryParse(s, out colonyFocusInput) && colonyFocusInput > 0)
        {
            if(colonyFocusInput <= agentColonies.Count)
            {
                currentFocusedColony = colonyFocusInput - 1;
                //Debug.Log("Change focus on colony " + currentFocusedColony + 1);
                UpdateColonyInfo();
                UpdateColonyInfoDisplay();
            }
        }
        else
        {
            //Debug.LogWarning("Invalid input. It must be a number above 0");
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
            agentColonies[c].follower[0].SetDefaultMaterial(colonyMaterials[c], colonyColors[c]);
            agentColonies[c].follower[0].transform.GetChild(0).gameObject.SetActive(true);
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
                if (i > data.nets.Count)
                {
                    agents[i].net = data.nets[0];
                }
                else
                {
                    agents[i].net = data.nets[i];
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
        public NeuralNetwork leaderNet;
        public float avgDiff;
        public float maxDiff;
        public float bestFitness;

        public AgentColony()
        { }

        public bool initialised;
    }
}
