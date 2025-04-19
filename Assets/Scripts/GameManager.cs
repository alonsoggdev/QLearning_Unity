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

    public void SetAlgorithmValues(int algorithm, int learningRate, int discountFactor, int goalAward, int movementAward, int giftAward)
    {
        AIController.set_algorithm(algorithm);
        AIController.set_learning_rate(learningRate);
        AIController.set_discount_factor(discountFactor);
        AIController.set_goal_award(goalAward);
        AIController.set_movement_award(movementAward);
        AIController.set_gift_award(giftAward);
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
