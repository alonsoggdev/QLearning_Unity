using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    public enum Algorithm
    {
        SARSA,
        QLEARNING
    }

    public static GameManager Instance { get; private set; }

    private bool isExecuting = false;
    [SerializeField] private AIController AIController;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        isExecuting = true;
    }

    public void ExecuteAI(int algorithm)
    {
        Algorithm selectedAlgorithm = (Algorithm)algorithm;
        if (isExecuting)
        {
            switch (selectedAlgorithm)
            {
                case Algorithm.SARSA:
                    AIController.SARSA();
                    break;
                case Algorithm.QLEARNING:
                    AIController.QLearning();
                    break;
                default:
                    Debug.LogError("Algoritmo no soportado");
                    break;
            }
        }
    }

}
