using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillfeedManager : MonoBehaviour
{
    public static KillfeedManager Instance { get; private set; }

    public GameObject killfeedEntryPrefab;  // Assign the killfeed text prefab
    public Transform killfeedContainer;    // Assign the Vertical Layout Group container

    public float killfeedDuration = 5f;    // Duration before the kill message fades

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
}


//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.UI;
//using TMPro; // Include TextMeshPro namespace

//public class KillfeedManager : MonoBehaviour
//{
//    public static KillfeedManager Instance { get; private set; }

//    public GameObject killfeedEntryPrefab;  // Assign the killfeed text prefab
//    public Transform killfeedContainer;     // Assign the Vertical Layout Group container

//    public float killfeedDuration = 5f;     // Duration before the kill message fades

//    private void Awake()
//    {
//        if (Instance == null)
//        {
//            Instance = this;
//        }
//    }

//    // Function to add a new entry to the killfeed
//    public void AddKillfeedEntry(string killer, string victim)
//    {
//        // Instantiate the prefab in the container
//        GameObject newEntry = Instantiate(killfeedEntryPrefab, killfeedContainer);

//        // Ensure correct scale and position
//        newEntry.transform.localScale = Vector3.one;  // Set scale to (1, 1, 1)
//        newEntry.transform.localPosition = Vector3.zero;  // Reset local position

//        // Find the specific UI elements in the prefab
//        Transform panel = newEntry.transform.Find("Panel");

//        if (panel != null)
//        {
//            // Update to TextMeshProUGUI components instead of Text
//            TextMeshProUGUI killerText = panel.Find("Killer").GetComponent<TextMeshProUGUI>();
//            TextMeshProUGUI victimText = panel.Find("Victim").GetComponent<TextMeshProUGUI>();
//            Image killSymbol = panel.Find("Kill Symbol").GetComponent<Image>();  // Assuming the kill symbol is an image

//            // Update the text and symbol
//            killerText.text = killer;
//            victimText.text = victim;

//            Debug.Log(killer + " killed " + victim);

//            // Optionally, change the kill symbol image if you have multiple symbols for different kinds of kills
//            // killSymbol.sprite = someCustomSprite;
//        }

//        // Optionally, remove the entry after some time
//        StartCoroutine(RemoveAfterDelay(newEntry, killfeedDuration));
//    }

//    // Coroutine to remove the entry after a delay
//    IEnumerator RemoveAfterDelay(GameObject entry, float delay)
//    {
//        yield return new WaitForSeconds(delay);
//        Destroy(entry);
//    }
//}
