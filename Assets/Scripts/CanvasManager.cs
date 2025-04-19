using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CanvasManager : MonoBehaviour
{
    private GameManager gameManager;

    [Header("Input Fields")]
    public InputField learningRate;
    public InputField discountFactor;
    public InputField goalAward;
    public InputField movementAward;
    public InputField giftAward;

    [Header("Dropdowns")]
    public Dropdown algorithmDropdown;

    [Header("Range Configuration")]
    public int minLearningRate = 1;
    public int maxLearningRate = 10;

    public int minDiscountFactor = 1;
    public int maxDiscountFactor = 10;

    public int minGoalAward = 0;
    public int maxGoalAward = 0;

    public int minMovementAward = 0;
    public int maxMovementAward = 0;

    public int minGiftAward = 0;
    public int maxGiftAward = 0;

    [Header("Colors")]
    public Color validColor = Color.white;
    public Color invalidColor = new Color(1f, 0.6f, 0.6f);

    void Start()
    {
        gameManager = GameManager.Instance;

        if (gameManager == null)
        {
            Debug.LogError("GameManager.Instance no encontrado desde CanvasManager");
        }

        set_default_values();
    }

    void set_default_values()
    {
        learningRate.text = "2";
        discountFactor.text = "9";
        goalAward.text = "100";
        movementAward.text = "-1";
        giftAward.text = "15";

        algorithmDropdown.value = 0;
    }

    public bool CheckInputs()
    {
        bool isValid1 = ValidateAndColor(learningRate, minLearningRate, maxLearningRate);
        bool isValid2 = ValidateAndColor(discountFactor, minDiscountFactor, maxDiscountFactor);
        bool isValid3 = ValidateAndColor(goalAward, minGoalAward, maxGoalAward);
        bool isValid4 = ValidateAndColor(movementAward, minMovementAward, maxMovementAward);
        bool isValid5 = ValidateAndColor(giftAward, minGiftAward, maxGiftAward);

        return isValid1 && isValid2 && isValid3 && isValid4 && isValid5;
    }

    private bool ValidateAndColor(InputField inputField, int min, int max)
    {
        bool isValid = IsValid(inputField.text, min, max);
        Image bg = inputField.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = isValid ? validColor : invalidColor;
        }
        return isValid;
    }

    private bool IsValid(string inputText, int min, int max)
    {
        if (int.TryParse(inputText, out int value))
        {
            bool validMin = (min == 0) || (value >= min);
            bool validMax = (max == 0) || (value <= max);
            return validMin && validMax;
        }
        return false;
    }

    public void StartButton_Hanlder()
    {
        if (CheckInputs())
        {

            gameManager.SetAlgorithmValues(
                algorithmDropdown.value,
                float.Parse(learningRate.text),
                float.Parse(discountFactor.text),
                float.Parse(goalAward.text),
                float.Parse(movementAward.text),
                float.Parse(giftAward.text)
            );
            gameManager.ExecuteAI(algorithmDropdown.value);
        }
        else
        {
            Debug.Log("Invalid inputs. Please correct them.");
        }
    }
}
