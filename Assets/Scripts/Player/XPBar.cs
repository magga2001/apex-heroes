using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class XPBar : MonoBehaviour
{
    [SerializeField] private List<Slider> subBars;  // List of sliders representing sub-bars
    [SerializeField] private List<Gradient> subBarGradients;  // List of gradients for each sub-bar
    [SerializeField] private List<Image> fillImages;  // List of images to fill each sub-bar with color

    private int maxXPPerSubBar;
    private int totalXP;

    [SerializeField] private float smoothSpeed = 0.1f;  // Speed of smooth transition

    // Initialize the XP bar with max XP divided into sub-bars and always start full
    public void SetMaxXP(int totalXP, int subBarCount)
    {
        this.totalXP = totalXP;
        maxXPPerSubBar = totalXP / subBarCount;

        // Initialize each sub-bar to be full initially
        for (int i = 0; i < subBars.Count; i++)
        {
            subBars[i].maxValue = maxXPPerSubBar;
            subBars[i].value = maxXPPerSubBar;  // Start fully filled
            fillImages[i].color = subBarGradients[i].Evaluate(1f);  // Apply gradient as full
        }
    }

    // Function to update the XP bars based on the current XP with smooth interpolation
    public void SetXP(float xp)
    {
        StartCoroutine(SmoothUpdateXPBars(xp));  // Use Coroutine to smooth the transition
    }

    // Coroutine for smooth UI updates
    public IEnumerator SmoothUpdateXPBars(float targetXP)
    {
        float remainingXP = targetXP;

        for (int i = 0; i < subBars.Count; i++)
        {
            float currentValue = subBars[i].value;  // Get current value

            if (remainingXP >= maxXPPerSubBar)
            {
                remainingXP -= maxXPPerSubBar;
                StartCoroutine(SmoothLerp(subBars[i], currentValue, maxXPPerSubBar));
            }
            else
            {
                StartCoroutine(SmoothLerp(subBars[i], currentValue, remainingXP));
                remainingXP = 0;  // No more XP to distribute
            }
        }

        yield return null;
    }

    // Smoothly interpolate the slider value from current to target over time
    private IEnumerator SmoothLerp(Slider bar, float startValue, float targetValue)
    {
        float elapsedTime = 0f;

        while (elapsedTime < smoothSpeed)
        {
            bar.value = Mathf.Lerp(startValue, targetValue, elapsedTime / smoothSpeed);
            elapsedTime += Time.deltaTime;

            // Apply the individual gradient for this specific sub-bar
            float normalizedFill = bar.value / bar.maxValue;
            fillImages[subBars.IndexOf(bar)].color = subBarGradients[subBars.IndexOf(bar)].Evaluate(normalizedFill);

            yield return null;
        }

        bar.value = targetValue;  // Ensure the value reaches the target exactly
    }
}
