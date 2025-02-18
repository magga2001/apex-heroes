using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HealingBox : BuffBox
{
    [SerializeField] private int healingAmount = 20;

    private void OnTriggerEnter(Collider other)
    {
       
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                player.IncreaseHealth(healingAmount);
                gameObject.SetActive(false);
            }
    }
}
