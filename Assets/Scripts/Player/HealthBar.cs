using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Slider slider;

    [SerializeField] private Gradient gradient;

    [SerializeField] private Image fill;

    public void SetMaxHealth(int health)
    {
        slider.maxValue = health;
        slider.value = health;

        //Set healthbar to max heatlh
        fill.color = gradient.Evaluate(1f);
    }

    public void SetHealth(int health)

    {
        slider.value = health;

        //Update the health bar
        fill.color = gradient.Evaluate(slider.normalizedValue);
    }
}
