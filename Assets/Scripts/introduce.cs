using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class introduce : MonoBehaviour
{
    public int itemId { get; private set; }
    [SerializeField] private Image GoodImage;
    [SerializeField] private TextMeshProUGUI quantityText;

    private int totalQuantity;      // 当前拥有的数量
    private int requiredQuantity;   // 制作所需的数量
    private string itemName;        // 物品名称
    private Recipe currentRecipe;   // 当前物品对应的配方

    private void Start()
    {
        LoadItemData();
        UpdateQuantityDisplay();
    }

    private void LoadItemData()
    {
        // 从数据库获取当前拥有的数量
        totalQuantity = InventoryManager.Instance.GetTotalQuantity(itemId);

        // 从数据库获取 UIPath 和 Name
        var query = "SELECT UIPath, Name FROM Items WHERE ItemId = @ItemId";
        var parameters = new[] { new Mono.Data.Sqlite.SqliteParameter("@ItemId", itemId) };
        var result = DatabaseHelper.Instance.ExecuteQuery(query, parameters);

        if (result.Rows.Count > 0)
        {
            string uiPath = result.Rows[0]["UIPath"].ToString();
            itemName = result.Rows[0]["Name"].ToString();
            string spritePath = "UI/" + uiPath;
            var sprite = Resources.Load<Sprite>(spritePath);
            if (sprite != null)
            {
                GoodImage.sprite = sprite;
            }
            else
            {
                Debug.LogWarning($"找不到物品图片: {spritePath}");
            }
        }
    }

    private void UpdateQuantityDisplay()
    {
        if (quantityText != null)
        {
            quantityText.text = $"{totalQuantity}/{requiredQuantity} {itemName}";
            quantityText.color = totalQuantity >= requiredQuantity ? Color.white : Color.red;
        }
    }

    // 当物品数量变化时调用此方法更新显示
    public void OnQuantityChanged()
    {
        LoadItemData();
        UpdateQuantityDisplay();
    }

    // 设置物品ID
    public void SetItemId(int id)
    {
        itemId = id;
        LoadItemData();
        UpdateQuantityDisplay();
    }

    // 更新当前配方的需求数量
    public void UpdateRequiredQuantityForRecipe(Recipe recipe, int craftAmount = 1)
    {
        foreach (var ingredient in recipe.Ingredients)
        {
            if (ingredient.ItemId == itemId)
            {
                requiredQuantity = ingredient.RequiredQuantity * craftAmount;
                UpdateQuantityDisplay();
                return;
            }
        }
    }

    // 获取当前物品是否满足制作需求
    public bool HasEnoughQuantity()
    {
        return totalQuantity >= requiredQuantity;
    }

    public void UpdateRequiredQuantity(int newRequiredQuantity)
    {
        requiredQuantity = newRequiredQuantity;
        UpdateQuantityDisplay();
    }
}
