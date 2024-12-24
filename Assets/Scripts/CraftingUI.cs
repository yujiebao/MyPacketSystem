using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;

public class CraftingUI : MonoBehaviour
{
    [Header("UI引用")]
    [SerializeField] private Transform weaponGridContent; // 武器网格容器
    [SerializeField] private Transform materialGridContent; // 材料网格容器
    [SerializeField] private Slot weaponButtonPrefab; // 武器按钮预制体
    [SerializeField] private introduce materialItemPrefab; // 材料项预制体
    
    [Header("制作信息")]
    [SerializeField] private Image image;
    [SerializeField] private TextMeshProUGUI weaponNameText; // 武器名称
    [SerializeField] private TextMeshProUGUI weaponDescriptionText; // 武器描述
    [SerializeField] private TextMeshProUGUI weaponOwnedText; // 拥有数量

    [Header("制作控制")]
    [SerializeField] private Button decreaseButton; // 减少按钮
    [SerializeField] private Button increaseButton; // 增加按钮
    [SerializeField] private TextMeshProUGUI craftAmountText; // 制作数量
    [SerializeField] private Button craftButton; // 制作按钮
    
    private Recipe currentRecipe; // 当前选中的配方
    private int craftAmount = 1; // 制作数量
    private List<GameObject> materialItems = new List<GameObject>(); // 材料项列表
    

    private void Start()
    {
        // 初始化按钮事件
        decreaseButton.onClick.AddListener(OnDecreaseAmount);
        increaseButton.onClick.AddListener(OnIncreaseAmount);
        craftButton.onClick.AddListener(OnCraftButtonClicked);

        // // 加载配方列表
        // LoadRecipes();
    }

    void OnEnable()
    {
        LoadRecipes();
    }
    private void LoadRecipes()
    {
        
        // 清除旧的材料项
        foreach (var item in materialItems)
        {
            Destroy(item);
        }
        materialItems.Clear();

            // 清除旧的武器按钮
        foreach (Transform child in weaponGridContent)
        {
            Destroy(child.gameObject);
        }

        string slotSpritePath = "UI/ui_zhuangbeikuang";  // 有物品的背景框
        Sprite slotSprite = Resources.Load<Sprite>(slotSpritePath);
        var recipes = RecipeDataLoader.Instance.GetAllRecipes();
        foreach (var recipe in recipes)
        {
            // 创建武器按钮
            var weaponButton = Instantiate(weaponButtonPrefab, weaponGridContent);
            // var icon = weaponButton.GetComponent<Image>();
            
            // 加载武器图标
            var query = "SELECT UIPath, Name FROM Items WHERE ItemId = @ItemId";
            var parameters = new[] { new Mono.Data.Sqlite.SqliteParameter("@ItemId", recipe.ResultItemId) };
            var result = DatabaseHelper.Instance.ExecuteQuery(query, parameters);

            if (result.Rows.Count > 0)
            {
                string uiPath = result.Rows[0]["UIPath"].ToString();
                string name = result.Rows[0]["Name"].ToString();
    
                string SpritePath = "UI/"+uiPath;  // 有物品的背景框
                // Debug.Log("SpritePath:"+SpritePath);
                var sprite = Resources.Load<Sprite>(SpritePath);
                if (sprite != null)
                {
                    weaponButton.UpdateSlot(sprite, name);
                }
            }
         
          

            // 设置按钮点击事件
            weaponButton.GetComponent<Button>().onClick.AddListener(() => SelectRecipe(recipe));

        }

        // 默认选择第一个配方
        if (recipes.Count > 0)
        {
            SelectRecipe(recipes[0]);
        }
    }

   
    private void SelectRecipe(Recipe recipe)
    {
        currentRecipe = recipe;
        craftAmount = 1;
        craftAmountText.text = "制作x"+craftAmount.ToString();
        UpdateUI();

        // Debug.Log("SelectRecipe:"+recipe.ResultItemId+" "+recipe.RecipeId+" "+recipe.Description);
    }

    private void UpdateUI()
    {
        if (currentRecipe == null)
        { 
            Debug.Log("currentRecipe is null");
            return;
        }

        // 获取武器信息
        var query = "SELECT Name, UIPath FROM Items WHERE ItemId = @ItemId";  // 添加 UIPath 到查询
        var parameters = new[] { new Mono.Data.Sqlite.SqliteParameter("@ItemId", currentRecipe.ResultItemId) };
        var result = DatabaseHelper.Instance.ExecuteQuery(query, parameters);

        if (result.Rows.Count > 0)
        {
            weaponNameText.text = result.Rows[0]["Name"].ToString();
            weaponDescriptionText.text = currentRecipe.Description;
            
            // 加载并设置 Sprite
            string uiPath = result.Rows[0]["UIPath"].ToString();
            string spritePath = "UI/" + uiPath;
            var sprite = Resources.Load<Sprite>(spritePath);
            if (sprite != null)
            {
                image.sprite = sprite;
            }
        }

        // 更新拥有数量
        int ownedAmount = InventoryManager.Instance.GetTotalQuantity(currentRecipe.ResultItemId);
        weaponOwnedText.text = $"拥有: {ownedAmount}";

        // 更新材料需求数量
        UpdateMaterialRequirements();

        // 清除旧的材料项
        foreach (var item in materialItems)
        {
            Destroy(item);
        }
        materialItems.Clear();

        // 显示材料需求
        foreach (var ingredient in currentRecipe.Ingredients)
        {
            var materialItem = Instantiate(materialItemPrefab, materialGridContent);
            materialItems.Add(materialItem.gameObject);

            // 直接使用 introduce 组件的方法
            materialItem.SetItemId(ingredient.ItemId);
            materialItem.UpdateRequiredQuantityForRecipe(currentRecipe, craftAmount);
        }

        // 更新制作按钮状态
        craftButton.interactable = CraftingSystem.Instance.CanCraft(currentRecipe, craftAmount);
    }

    private void OnDecreaseAmount()
    {
        if (craftAmount > 1)
        {
            craftAmount--;
            craftAmountText.text = "制作x" + craftAmount.ToString();
            UpdateMaterialRequirements();
            UpdateUI();
        }
    }

    private void OnIncreaseAmount()
    {
        craftAmount++;
        craftAmountText.text = "制作x" + craftAmount.ToString();
        UpdateMaterialRequirements();
        UpdateUI();
    }

    private void UpdateMaterialRequirements()
{
    foreach (var ingredient in currentRecipe.Ingredients)
    {
        var materialItem = materialItems.Find(item => item.GetComponent<introduce>().itemId == ingredient.ItemId);
        if (materialItem != null)
        {
            var introduceComponent = materialItem.GetComponent<introduce>();
            if (introduceComponent != null)
            {
                introduceComponent.UpdateRequiredQuantityForRecipe(currentRecipe, craftAmount);
            }
        }
    }
}

    private void OnCraftButtonClicked()
    {
        if (currentRecipe != null && CraftingSystem.Instance.CanCraft(currentRecipe, craftAmount))
        {
            bool success = CraftingSystem.Instance.Craft(currentRecipe, craftAmount);
            if (success)
            {
                Debug.Log($"成功制作 {craftAmount} 个物品");
                UpdateUI();
            }
            else
            {
                Debug.LogError("制作失败");
            }
        }
    }
}