using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ShopManager : MonoBehaviour
{

    [SerializeField] private TextMeshProUGUI current_halo_text;
    private int current_halo = 1000;

    public GameObject regularItemPrefab;
    public GameObject premiumItemPrefab;
    public GameObject bundleItemPrefab;
    public GameObject limitedItemPrefab;
    public Transform contentParent; // Assign the "Content" of your Scroll View here.

    private Dictionary<string, GameObject> prefabMap;

    private void Start()
    {
        // Map types to prefabs
        prefabMap = new Dictionary<string, GameObject>
        {
            { "regular", regularItemPrefab },
            { "premium", premiumItemPrefab },
            { "bundle", bundleItemPrefab },
            { "limited", limitedItemPrefab }
        };

        // Simulate database load
        LoadShopFromDatabase();

        current_halo_text.text = current_halo.ToString();
    }

    private void LoadShopFromDatabase()
    {
        // Simulate data loading
        var shopItems = new List<ShopItemData>
        {
            new ShopItemData("Character 1", 100, "regular", "https://images.unsplash.com/photo-1503023345310-bd7c1de61c7d?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&ixid=MnwzNjUyOXwwfDF8c2VhcmNofDF8fGV4YW1wbGV8ZW58MHx8fHwxNjgwMjcyMjg1&ixlib=rb-4.0.3&q=80&w=400"),
            new ShopItemData("Premium Item", 500, "premium", "https://images.unsplash.com/photo-1503023345310-bd7c1de61c7d?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&ixid=MnwzNjUyOXwwfDF8c2VhcmNofDF8fGV4YW1wbGV8ZW58MHx8fHwxNjgwMjcyMjg1&ixlib=rb-4.0.3&q=80&w=400"),
            new ShopItemData("Bundle Offer", 1000, "bundle", "https://images.unsplash.com/photo-1503023345310-bd7c1de61c7d?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&ixid=MnwzNjUyOXwwfDF8c2VhcmNofDF8fGV4YW1wbGV8ZW58MHx8fHwxNjgwMjcyMjg1&ixlib=rb-4.0.3&q=80&w=400"),
            new ShopItemData("Limited Item", 2000, "limited", "https://images.unsplash.com/photo-1503023345310-bd7c1de61c7d?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&ixid=MnwzNjUyOXwwfDF8c2VhcmNofDF8fGV4YW1wbGV8ZW58MHx8fHwxNjgwMjcyMjg1&ixlib=rb-4.0.3&q=80&w=400")
        };

        // Populate shop
        foreach (var itemData in shopItems)
        {
            if (prefabMap.TryGetValue(itemData.Type, out var prefab))
            {
                // Instantiate the correct prefab
                GameObject item = Instantiate(prefab, contentParent);

                // Get the ShopItem component and set up the item
                var shopItem = item.GetComponent<ShopItem>();
                if (shopItem != null)
                {
                    shopItem.Setup(
                        itemData.Name,
                        itemData.Price,
                        itemData.ImageUrl,
                        () => BuyItem(itemData.Name, itemData.Price)
                    );
                }
                else
                {
                    Debug.LogError("Prefab does not have a ShopItem component attached.");
                }
            }
            else
            {
                Debug.LogError($"No prefab found for type: {itemData.Type}");
            }
        }
    }

    private void BuyItem(string itemName, int itemPrice)
    {
        // Check if the player can afford the item
        if (current_halo >= itemPrice)
        {
            // Deduct the item's price from the player's currency
            current_halo -= itemPrice;

            // Update the UI to reflect the new amount
            current_halo_text.text = current_halo.ToString();

            // Log the purchase
            Debug.Log($"Successfully bought {itemName} for {itemPrice} halos. Remaining halos: {current_halo}");
        }
        else
        {
            // Notify the player that they cannot afford the item
            Debug.Log($"Cannot afford {itemName}. Price: {itemPrice}, Current halos: {current_halo}");
        }
    }

}

[System.Serializable]
public class ShopItemData
{
    public string Name;
    public int Price;
    public string Type;
    public string ImageUrl;

    public ShopItemData(string name, int price, string type, string imageUrl)
    {
        Name = name;
        Price = price;
        Type = type;
        ImageUrl = imageUrl;
    }
}
