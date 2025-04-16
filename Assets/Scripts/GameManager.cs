using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    bool isExecuting = true;
    AIController AIController;

    void Awake()
    {
        AIController = new AIController();
    }
    
    void Start()
    {

    }
}
