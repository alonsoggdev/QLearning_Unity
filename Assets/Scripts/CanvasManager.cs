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
    public Button     startButton;
    public Button     resetButton;

    [Header("Dropdowns")]
    public Dropdown algorithmDropdown;

    [Header("Range Configuration")]
    public float minLearningRate = 0.01f;
    public float maxLearningRate = 1.0f;

    public float minDiscountFactor = 0.01f;
    public float maxDiscountFactor = 1.0f;

    public float minGoalAward = 0;
    public float maxGoalAward = 0;

    public float minGiftAward = 0;
    public float maxGiftAward = 0;

    [Header("Colors")]
    public Color validColor = Color.white;
    public Color invalidColor = new Color(1f, 0.6f, 0.6f);

    [Header("Texts")]
    public Text episodeText;
    public Text stepText;
    public Text successfulPathsText;

    float defaultLearningRate = 0.2f;
    float defaultDiscountFactor = 0.99f;
    float defaultGoalAward = 1000f;
    float defaultGiftAward = 150f;

    void Start()
    {
        gameManager = GameManager.Instance;

        if (gameManager == null)
        {
            Debug.LogError("GameManager.Instance no encontrado desde CanvasManager");
        }

        set_default_values();
        resetButton.gameObject.SetActive(false);
    }

    void set_default_values()
    {
        learningRate.text = defaultLearningRate.ToString();
        discountFactor.text = defaultDiscountFactor.ToString();
        goalAward.text = defaultGoalAward.ToString();
        giftAward.text = defaultGiftAward.ToString();

        algorithmDropdown.value = 0;
    }

    public bool CheckInputs()
    {
        bool isValid1 = ValidateAndColor(learningRate, minLearningRate, maxLearningRate);
        bool isValid2 = ValidateAndColor(discountFactor, minDiscountFactor, maxDiscountFactor);
        bool isValid3 = ValidateAndColor(goalAward, minGoalAward, maxGoalAward);
        bool isValid5 = ValidateAndColor(giftAward, minGiftAward, maxGiftAward);

        return isValid1 && isValid2 && isValid3 && isValid5;
    }

    private bool ValidateAndColor(InputField inputField, float min, float max)
    {
        bool isValid = IsValid(inputField.text, min, max);
        Image bg = inputField.GetComponent<Image>();
        if (bg != null)
        {
            bg.color = isValid ? validColor : invalidColor;
        }
        return isValid;
    }

    private bool IsValid(string inputText, float min, float max)
    {
        if (float.TryParse(inputText, out float value))
        {
            bool validMin = (min == 0) || (value >= min);
            bool validMax = (max == 0) || (value <= max);
            return validMin && validMax;
        }
        return false;
    }

    public void ResetButton_Handler()
    {
        gameManager.ResetGame();
        resetButton.gameObject.SetActive(false);
        startButton.interactable = true;
    }

    public void StartButton_Hanlder()
    {
        if (CheckInputs())
        {
            startButton.interactable = false;
            resetButton.gameObject.SetActive(true);

            gameManager.SetAlgorithmValues(
                algorithmDropdown.value,
                float.Parse(learningRate.text),
                float.Parse(discountFactor.text),
                float.Parse(goalAward.text),
                float.Parse(giftAward.text)
            );
            gameManager.ExecuteAI(algorithmDropdown.value);
        }
        else
        {
            Debug.Log("Invalid inputs. Please correct them.");
        }
    }

    public void set_episode_text(int episode, int maxEpisodes)
    {
        episodeText.text = $"{episode} / {maxEpisodes}";
    }

    public void set_step_text(int step, int maxSteps)
    {
        stepText.text = $"{step} / {maxSteps}";
    }

    public void set_successful_paths_text(int successfulPaths)
    {
        successfulPathsText.text = $"{successfulPaths}";
    }
}