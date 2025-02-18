using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.Networking;
using TMPro;

public class ShopItem : MonoBehaviour
{
    public TextMeshProUGUI title;         // Assign a Text component for the item title
    public TextMeshProUGUI price;         // Assign a Text component for the item price
    public Image itemImage;    // Assign an Image component for the item image
    public Button purchaseButton; // Assign a Button component for purchase

    // Setup method to initialize the item
    public void Setup(string itemName, int itemPrice, string imageUrl, UnityAction onClick)
    {
        title.text = itemName;
        price.text = $"{itemPrice} HALO";

        // Load image dynamically if URL is provided (optional)
        if (!string.IsNullOrEmpty(imageUrl))
        {
            StartCoroutine(LoadImage(imageUrl));
        }

        // Add the button listener
        if (purchaseButton != null)
        {
            purchaseButton.onClick.AddListener(onClick);
        }
    }

    private IEnumerator LoadImage(string url)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            // Add User-Agent header
            request.SetRequestHeader("User-Agent", "UnityWebRequest");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"Failed to load image from URL: {url}\nError: {request.error}");
            }
            else
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                itemImage.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), Vector2.zero);
            }
        }
    }

}
