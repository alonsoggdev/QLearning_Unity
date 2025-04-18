using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIController : MonoBehaviour
{

    Matrix matrix;

    enum Algorithm
    {
        SARSA,
        QLEARNING
    }

    public void Start()
    {
        matrix = new Matrix();

        Algorithm algorithm = Algorithm.SARSA;
    }

    public void Execute()
    {
        
    }
}
