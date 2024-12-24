using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Bagmanager : MonoBehaviour
{

    public static Bagmanager Instance;
    public GameObject slotGrids;        // 背包格子父物体
    public Slot slotPrefab;            // 格子预制体
    private List<Slot> slots = new List<Slot>();
    public TextMeshProUGUI capacityText;  // 在Unity Inspector中设置

    private void Awake()
    {
        if (Instance != null)
            Destroy(this);
        Instance = this;
    }

    private void Start()
    {
        InitializeInventory();
    }

    private void OnEnable()
    {
        RefreshInventory();
    }

    // 初始化背包格子
    private void InitializeInventory()
    {
        // 先获取所有格子确保数据库初始化完成
        var allSlots = InventoryManager.Instance.GetAllItems();
        if (allSlots.Count <30)
        {
            Debug.LogError($"背包格子初始化不完整，当前只有 {allSlots.Count} 个格子");
            return;
        }

        // 创建所有格子的UI
        foreach (var slot in allSlots)
        {
            CreateSlot(slot);
        }

        // 添加初始物品
        // InventoryManager.Instance.AddItem(new Good(101, "icon_wuqi_tiepigong", "铁弓", 1, 2));
        // InventoryManager.Instance.AddItem(new Good(102, "icon_wuqi_dao", "铁刀", 1, 2));
        // InventoryManager.Instance.AddItem(new Good(150, "icon_danyao_tiejian_yumao", "羽毛箭", 15, 45));
        // InventoryManager.Instance.AddItem(new Good(202, "icon_cailiao_shengtie", "生铁", 10, 20));
        // InventoryManager.Instance.AddItem(new Good(203, "icon_cailiao_mutou", "木头", 10, 40));
        // InventoryManager.Instance.AddItem(new Good(204, "icon_cailiao_jiaodai", "胶带", 10, 20));
        // InventoryManager.Instance.AddItem(new Good(205, "icon_cailiao_masheng", "麻绳", 10, 20));
        // InventoryManager.Instance.AddItem(new Good(206, "icon_cailiao_yumao", "羽毛", 30, 60));

        // 刷新显示
        RefreshInventory();
    }


    // 创建单个格子
    private void CreateSlot(Good item)
    {
        Slot newSlot = Instantiate(slotPrefab, slotGrids.transform);
        slots.Add(newSlot);
        UpdateSlotUI(newSlot, item);
    }

    // 刷新背包显示
    public void RefreshInventory()
    {
        var items = InventoryManager.Instance.GetAllItems();
        for (int i = 0; i < items.Count; i++)
        {
            if (i < slots.Count)
            {
                UpdateSlotUI(slots[i], items[i]);
            }
        }
        UpdateCapacityDisplay();
    }

    // 更新格子UI
    private void UpdateSlotUI(Slot slot, Good item)
    {
        if (item != null && !item.IsEmpty)
        {
            // 有物品时使用装备框背景
            string slotSpritePath = "UI/ui_zhuangbeikuang";  // 有物品的背景框
            Sprite slotSprite = Resources.Load<Sprite>(slotSpritePath);
            // slot.GetComponent<Image>().sprite = slotSprite;  // 设置格子背景

            // 加载物品图标
            // Sprite itemSprite = Resources.Load<Sprite>(item.UIPath);
            
            // if (itemSprite != null)
            // {
                // slot.iconImage.sprite = itemSprite;
                // slot.iconImage.enabled = true;
                // slot.quantityText.text = item.Quantity.ToString();
                // slot.quantityText.enabled = !(item.Quantity < 1);
                // slot.nameText.text = item.Name;
                // slot.nameText.enabled = true;
               if(!slot.UpdateSlot(item,slotSprite)) 
               {
                Debug.LogWarning($"背包格子更新异常");
                slot.ClearSlot();
               }
            // }
            // else
            // {
            //     Debug.LogWarning($"找不到物品图片: {item.UIPath}");
            //     slot.ClearSlot();
            // }
        }
        else
        {
            // 无物品时使用空白框背景
            string slotSpritePath = "UI/ui_youbiaoqian";  // 空格子的背景框
            Sprite slotSprite = Resources.Load<Sprite>(slotSpritePath);
            slot.GetComponent<Image>().sprite = slotSprite;  // 设置格子背景
            slot.ClearSlot();
        }
    }

    private void UpdateCapacityDisplay()
    {
        int currentItems = InventoryManager.Instance.GetCurrentItemCount();
        int totalCapacity = InventoryManager.Instance.GetTotalCapacity();
        capacityText.text = $"{currentItems}/{totalCapacity}";
    }
}