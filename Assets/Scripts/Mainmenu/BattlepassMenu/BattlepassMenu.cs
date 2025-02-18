using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattlePassUI : MonoBehaviour
{
    [Header("References")]
    public RectTransform content;           // The Content object inside the Scroll View
    public GameObject rewardPrefab;         // The prefab for rewards (level panel)
    public GameObject progressBarPrefab;    // The prefab for progress bars
    public TextMeshProUGUI current_trophies;

    [Header("Battle Pass Settings")]
    public float rewardSpacing = 250f;      // Spacing between each reward (horizontal distance)
    public int playerTrophies = 0;          // Player's current trophy count (fetched from database)
    private RewardData[] rewardDataList;    // List of rewards loaded from the database

    void Start()
    {
        // Simulate loading rewards from the database
        LoadRewardsFromDatabase();

        // Populate the battle pass UI
        PopulateRewards();

        current_trophies.text = playerTrophies.ToString();
    }

    void LoadRewardsFromDatabase()
    {
        // Example: Simulating reward data from a database
        rewardDataList = new RewardData[]
        {
            new RewardData(1, "+500 HALO", 0, false),
            new RewardData(2, "+1000 HALO", 100, false),
            new RewardData(3, "+1500 HALO", 200, false),
            new RewardData(4, "+2000 HALO", 300, false),
            new RewardData(5, "+2500 HALO", 400, false),
        };
    }

    void PopulateRewards()
    {
        for (int i = 0; i < rewardDataList.Length; i++)
        {
            // Instantiate the reward prefab
            GameObject reward = Instantiate(rewardPrefab, content);
            reward.name = rewardDataList[i].name;

            // Update the reward's UI based on its data
            UpdateRewardUI(reward, rewardDataList[i]);

            // Add a progress bar between rewards, except after the last reward
            if (i < rewardDataList.Length - 1)
            {
                GameObject progressBar = Instantiate(progressBarPrefab, content);
                progressBar.name = "ProgressBar " + (i + 1);

                // Calculate progress for the current range
                int previousTrophies = rewardDataList[i].requiredTrophies;
                int nextTrophies = rewardDataList[i + 1].requiredTrophies;
                SetProgressBar(progressBar, playerTrophies, previousTrophies, nextTrophies);
            }
        }
    }

    void UpdateRewardUI(GameObject rewardObject, RewardData rewardData)
    {
        // Update the reward's text
        var rewardText = rewardObject.transform.Find("Panel/Button/Text (TMP)").GetComponent<TMPro.TextMeshProUGUI>();
        if (rewardText != null)
        {
            rewardText.text = rewardData.name;
        }

        // Update the required trophies text
        var requiredTrophiesText = rewardObject.transform.Find("Required_trophies").GetComponent<TMPro.TextMeshProUGUI>();
        if (requiredTrophiesText != null)
        {
            requiredTrophiesText.text = $"{rewardData.requiredTrophies} Trophies";
        }

        // Update the reward's interactivity based on player trophies
        var claimButton = rewardObject.transform.Find("Panel/Button").GetComponent<Button>();
        if (claimButton != null)
        {
            bool isClaimable = playerTrophies >= rewardData.requiredTrophies && !rewardData.isClaimed;

            // Enable/disable button based on claimability
            claimButton.interactable = isClaimable;

            // Change button appearance (e.g., grayed out if unclaimable)
            var buttonImage = claimButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = isClaimable ? Color.white : Color.gray;
            }

            // Add the claim action if claimable
            claimButton.onClick.RemoveAllListeners();
            if (isClaimable)
            {
                claimButton.onClick.AddListener(() => ClaimReward(rewardData, rewardObject));
            }
        }
    }


    void SetProgressBar(GameObject progressBarObject, int playerTrophies, int previousTrophies, int nextTrophies)
    {
        Transform fillTransform = progressBarObject.transform.Find("Border/Fill");
        if (fillTransform == null)
        {
            Debug.LogError("ProgressBar prefab is missing a 'Fill' object under 'Border'!");
            return;
        }

        Image progressBarFillImage = fillTransform.GetComponent<Image>();
        if (progressBarFillImage == null)
        {
            Debug.LogError("'Fill' object is missing an Image component!");
            return;
        }

        float progress = 0f;

        if (playerTrophies < previousTrophies)
        {
            progress = 0f;
        }
        else if (playerTrophies >= nextTrophies)
        {
            progress = 1f;
        }
        else
        {
            progress = (float)(playerTrophies - previousTrophies) / (nextTrophies - previousTrophies);
        }

        progressBarFillImage.fillAmount = progress;

        Debug.Log($"Progress calculation: PlayerTrophies={playerTrophies}, Previous={previousTrophies}, Next={nextTrophies}, Progress={progress}");
    }



    void ClaimReward(RewardData rewardData, GameObject rewardObject)
    {
        // Mark the reward as claimed
        rewardData.isClaimed = true;

        // Update the reward UI to reflect claimed status
        var claimButton = rewardObject.transform.Find("Panel/Button").GetComponent<Button>();
        if (claimButton != null)
        {
            claimButton.interactable = false;

            // Optional: Update the button's appearance to indicate claimed status
            var buttonImage = claimButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = Color.green; // Change to green or some other "claimed" color
            }
        }

        Debug.Log($"Reward claimed: {rewardData.name}");
    }
}

[System.Serializable]
public class RewardData
{
    public int id;                 // Unique ID for the reward
    public string name;            // Name of the reward
    public int requiredTrophies;   // Trophies required to claim the reward
    public bool isClaimed;         // Whether the reward has been claimed

    public RewardData(int id, string name, int requiredTrophies, bool isClaimed)
    {
        this.id = id;
        this.name = name;
        this.requiredTrophies = requiredTrophies;
        this.isClaimed = isClaimed;
    }
}
