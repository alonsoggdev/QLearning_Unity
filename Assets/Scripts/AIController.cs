using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIController : MonoBehaviour
{

    Matrix matrix;

    public void Start()
    {
        matrix = new Matrix();

        GameManager.Algorithm algorithm = GameManager.Algorithm.SARSA;
    }

    public void SARSA()
    {
        Debug.Log("Ejecutando SARSA");
        
    }

    public void QLearning()
    {
        
    }
}
