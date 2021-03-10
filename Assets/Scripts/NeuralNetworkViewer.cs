using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public struct NeuronDisplay
{
	public GameObject    neuronObj;
	public RectTransform neuronRectTransform;
	public Image         neuronImage;
	public Text          neuronValue;

	public void Init(float xPos, float yPos)
	{
		neuronRectTransform                  = neuronObj.GetComponent<RectTransform>();
		neuronImage                          = neuronObj.GetComponent<Image>();
		neuronValue                          = neuronObj.GetComponentInChildren<Text>();
		neuronRectTransform.anchoredPosition = new Vector3(xPos, yPos, 0);
	}

	public void Refresh(float value, Gradient colorGradient)
	{
		neuronImage.color = colorGradient.Evaluate((value + 1) * .5f);
		neuronValue.text  = value.ToString("F2");
	}
}

public struct AxonDisplay
{
	public GameObject    axonObj;
	public RectTransform axonRectTransform;
	public Image         axonImage;

	public void Init(RectTransform start, RectTransform end, float thickness, float neuronDiameter)
	{
		axonRectTransform                  = axonObj.GetComponent<RectTransform>();
		axonImage                          = axonObj.GetComponent<Image>();
		axonRectTransform.anchoredPosition = start.anchoredPosition + (end.anchoredPosition                        - start.anchoredPosition) / 2;
		axonRectTransform.sizeDelta        = new Vector2((end.anchoredPosition - start.anchoredPosition).magnitude - neuronDiameter, thickness);
		axonRectTransform.rotation         = Quaternion.FromToRotation(axonRectTransform.right, (end.anchoredPosition - start.anchoredPosition).normalized);
		axonRectTransform.SetAsFirstSibling();
	}

	public void Refresh(float value, Gradient colorGradient)
	{
		axonImage.color = colorGradient.Evaluate((value + 1) * .5f);
	}
}

public class NeuralNetworkViewer : MonoBehaviour
{
	//Display Var

	[Header("Agent Ref")] public Agent agent;

	[Header("Requirement Ref")] [SerializeField]
	GameObject axonPrefab;

	[SerializeField] GameObject    neuronPrefab;
	[SerializeField] GameObject    fitnessPrefab;
	[SerializeField] RectTransform viewerGroup;

	[Header("Display Values")] [SerializeField]
	float layerSpacing;

	[SerializeField] float    neuronSpacing;
	[SerializeField] float    neuronDiameter = 32;
	[SerializeField] Gradient neuronColorGradient;
	[SerializeField] float    axonThickness;
	[SerializeField] Gradient axonColorGradient;

	//Hidden Var

	NeuronDisplay[][] neurons;
	AxonDisplay[][][] axons;

	float neuronsHeight;
	float padding;

	Text fitnessDisplay;

	//Static Var

	public static NeuralNetworkViewer instance;

	private void Awake()
	{
		#region Singleton

		if (instance == null)
		{
			instance = this;
		}
		else
		{
			print("Reworked Network Viewer already exist on " + gameObject.name);
			print("Destroying version on "                    + gameObject.name);
			Destroy(this);
		}

		#endregion
	}

	private void Update()
	{
		for (x = 0; x < neurons.Length; x++)
		{
			for (y = 0; y < neurons[x].Length; y++) neurons[x][y].Refresh(agent.net.neurons[x][y], neuronColorGradient);
		}

		fitnessDisplay.text = agent.fitness.ToString("F1");
	}

	int x = 0;
	int y = 0;
	int z = 0;

	public void Init(Agent _agent)
	{
		NeuralNetwork tempNet = null;

		tempNet = _agent.net;
		agent   = _agent;

		#region Finding biggest layer

		int maxNeurons = 0;

		neuronsHeight = neuronPrefab.GetComponent<RectTransform>().rect.height;

		for (int i = 0; i < tempNet.layers.Length; i++)
		{
			if (tempNet.layers[i] > maxNeurons)
			{
				maxNeurons = tempNet.layers[i];
			}
		}

		#endregion

		#region Neurons Creation 

		neurons = new NeuronDisplay[tempNet.layers.Length][];

		for (x = 0; x < tempNet.layers.Length; x++)
		{
			if (tempNet.layers[x] < maxNeurons)
			{
				padding = ((maxNeurons - tempNet.layers[x]) / 2) * (neuronsHeight + neuronSpacing);

				if (tempNet.layers[x] % 2 != maxNeurons % 2)
				{
					padding += (neuronsHeight + neuronSpacing) / 2;
				}
			}
			else
			{
				padding = 0;
			}

			neurons[x] = new NeuronDisplay[tempNet.layers[x]];

			for (y = 0; y < tempNet.layers[x]; y++)
			{
				neurons[x][y]           = new NeuronDisplay();
				neurons[x][y].neuronObj = Instantiate(neuronPrefab, viewerGroup);
				neurons[x][y].Init(layerSpacing * x, -padding - (neuronsHeight + neuronSpacing) * y);
			}
		}

		#endregion

		#region Axons Creation

		axons = new AxonDisplay[tempNet.layers.Length - 1][][];

		for (x = 0; x < tempNet.layers.Length - 1; x++)
		{
			axons[x] = new AxonDisplay[tempNet.layers[x]][];

			for (y = 0; y < tempNet.layers[x]; y++)
			{
				axons[x][y] = new AxonDisplay[tempNet.layers[x + 1]];

				for (z = 0; z < tempNet.layers[x + 1]; z++)
				{
					axons[x][y][z]         = new AxonDisplay();
					axons[x][y][z].axonObj = Instantiate(axonPrefab, viewerGroup);
					axons[x][y][z].Init(neurons[x][y].neuronRectTransform, neurons[x + 1][z].neuronRectTransform, axonThickness, neuronDiameter);
				}
			}
		}

		#endregion

		#region Fitness

		GameObject fitness = Instantiate(fitnessPrefab, viewerGroup);
		Vector2    pos     = new Vector2(((tempNet.layers.Length) * layerSpacing), -(float) maxNeurons / 2 * (neuronsHeight + neuronSpacing));
		fitness.GetComponent<RectTransform>().anchoredPosition = pos;
		fitnessDisplay                                         = fitness.GetComponent<Text>();

		#endregion
	}

	public void RefreshAxons()
	{
		for (x = 0; x < axons.Length; x++)
		{
			for (y = 0; y < axons[x].Length; y++)
			{
				for (z = 0; z < axons[x][y].Length; z++)
				{
					axons[x][y][z].Refresh(agent.net.axons[x][z][y], axonColorGradient);
				}
			}
		}
	}
}