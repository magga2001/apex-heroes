using UnityEngine;
using UnityEngine.UI;

public class ScrollView : MonoBehaviour
{
    [SerializeField] private Transform content;
    [SerializeField] private GameObject[] prefabs;

    private void Start()
    {
        PopulateScrollView(); // Automatically populate on start, if desired
    }

    public void PopulateScrollView()
    {
        ClearContent(); // Optional: Clear existing items first
        foreach (GameObject prefab in prefabs)
        {
            Instantiate(prefab, content);
        }
    }

    public void ClearContent()
    {
        foreach (Transform child in content)
        {
            Destroy(child.gameObject);
        }
    }
}
