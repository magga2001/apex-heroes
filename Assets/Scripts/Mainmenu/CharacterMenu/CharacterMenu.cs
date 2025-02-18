using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Drawing;

public class CharacterMenu : MonoBehaviour
{
    [SerializeField] private Transform content; // Parent container for all rows
    [SerializeField] private GameObject characterPrefab; // Prefab for each character
    [SerializeField] private GameObject dividerPrefab;   // Prefab for the divider
    [SerializeField] private TextMeshProUGUI unlockedCountText; // UI for unlocked/total characters
    [SerializeField] private int charactersPerRow = 2; // Number of characters per row

    private List<Character> characters;
    private int unlockedCount;

    private void Start()
    {
        InitializeCharacters(); // Load characters dynamically
        UpdateUnlockedCount(); // Update the UI with unlocked count
        PopulateCharacterMenu(); // Populate the character menu
    }

    private void InitializeCharacters()
    {
        // Predefined static character data with `isUnlocked` as true or false
        characters = new List<Character>
        {
            new Character(1, "Warrior", 1, 500, false), // Locked
            new Character(2, "Mage", 1, 750, true),    // Unlocked
            new Character(3, "Archer", 1, 1000, false), // Locked
            new Character(4, "Rogue", 1, 1200, true),   // Unlocked
            new Character(5, "Paladin", 1, 1500, false), // Locked
            new Character(6, "Cleric", 1, 700, true)   // Unlocked
        };
    }

    private void PopulateCharacterMenu()
    {
        // Separate unlocked and locked characters
        List<Character> unlockedCharacters = characters.FindAll(c => c.isUnlocked);
        List<Character> lockedCharacters = characters.FindAll(c => !c.isUnlocked);

        // Add title and rows for unlocked characters
        AddSectionTitle("Unlocked Characters");
        CreateRows(unlockedCharacters);

        // Add a divider between unlocked and locked sections
        AddDivider();

        // Add title and rows for locked characters
        AddSectionTitle("Locked Characters");
        CreateRows(lockedCharacters);
    }

    private void CreateRows(List<Character> characterList)
    {
        GameObject currentRow = null;
        int countInRow = 0;

        foreach (Character character in characterList)
        {
            // Create a new row when necessary
            if (countInRow == 0)
            {
                currentRow = new GameObject("Row");
                currentRow.transform.SetParent(content, false);

                // Add Horizontal Layout Group to the row
                var layoutGroup = currentRow.AddComponent<HorizontalLayoutGroup>();
                layoutGroup.childAlignment = TextAnchor.MiddleLeft; // Align children to the center
                layoutGroup.childForceExpandWidth = true; // Allow children to expand horizontally
                layoutGroup.childForceExpandHeight = true; // Allow children to expand vertically
                layoutGroup.childControlWidth = false; // Do not control child width
                layoutGroup.childControlHeight = false; // Do not control child height
                layoutGroup.spacing = 10f; // Add spacing between items in the row

                // Ensure the row has a RectTransform
                RectTransform rowRect = currentRow.GetComponent<RectTransform>();
                if (rowRect == null)
                {
                    rowRect = currentRow.AddComponent<RectTransform>();
                }

                // Set up the RectTransform properties
                //FIX THIS TO BE DYNAMIC
                rowRect.sizeDelta = new Vector2(700, 300); // Minimum height (match prefab size)
                rowRect.anchorMin = new Vector2(0, 1); // Anchor to top-left
                rowRect.anchorMax = new Vector2(1, 1); // Anchor to top-right
                rowRect.pivot = new Vector2(0.5f, 1);  // Pivot at the top center

                countInRow = charactersPerRow; // Reset the count for the new row
            }

            // Instantiate the character prefab
            GameObject newEntry = Instantiate(characterPrefab, currentRow.transform);

            // Get components from the prefab
            TextMeshProUGUI nameText = newEntry.GetComponentInChildren<TextMeshProUGUI>();
            Image characterImage = newEntry.GetComponentInChildren<UnityEngine.UI.Image>();

            // Ensure prefab structure is correct
            if (nameText == null || characterImage == null)
            {
                Debug.LogError("Character prefab is missing required components (Name or Image).");
                continue;
            }

            // Assign values to the prefab's UI components
            nameText.text = character.name;

            // Handle unlocked/locked character logic
            if (character.isUnlocked)
            {
                Button button = newEntry.GetComponentInChildren<UnityEngine.UI.Button>();
                if (button != null)
                {
                    button.interactable = false; // Hide button for unlocked characters
                }
            }
            else
            {
                // Add a debug log for the locked character button
                Button button = newEntry.GetComponentInChildren<UnityEngine.UI.Button>();
                if (button != null)
                {
                    button.onClick.AddListener(() =>
                    {
                        Debug.Log($"{character.name} is locked! Add unlocking logic here.");
                    });
                }
            }

            countInRow--; // Decrement the count for the current row
        }
    }

    private void AddSectionTitle(string title)
    {
        // Create a GameObject for the title
        GameObject titleObject = new GameObject(title);
        TextMeshProUGUI titleText = titleObject.AddComponent<TextMeshProUGUI>();
        titleText.text = title;
        titleText.fontSize = 24;
        titleText.alignment = TextAlignmentOptions.Center;

        // Set the title as a child of the content container
        RectTransform rectTransform = titleObject.GetComponent<RectTransform>();
        rectTransform.SetParent(content, false);

        // Adjust sizeDelta for width and height to give the text room
        rectTransform.sizeDelta = new Vector2(500, 40); // Set a reasonable width and height
        rectTransform.anchorMin = new Vector2(0.5f, 1); // Anchor to the center-top
        rectTransform.anchorMax = new Vector2(0.5f, 1); // Anchor to the center-top
        rectTransform.pivot = new Vector2(0.5f, 1);     // Pivot at the top center
        rectTransform.anchoredPosition = Vector2.zero;  // Reset anchored position
    }


    private void AddDivider()
    {
        // Instantiate a divider prefab (e.g., a horizontal line)
        GameObject divider = Instantiate(dividerPrefab, content);
    }

    private void UpdateUnlockedCount()
    {
        // Count unlocked characters
        unlockedCount = characters.FindAll(c => c.isUnlocked).Count;

        // Update the UI
        unlockedCountText.text = $"{unlockedCount}/{characters.Count} Characters Unlocked";
    }
}
