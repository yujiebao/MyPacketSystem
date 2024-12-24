using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class Slot : MonoBehaviour
{
    public Image iconImage;         // 物品图标
    public Image backgroundImage; // 背景图片
    public TextMeshProUGUI quantityText;    // 数量文本
    public TextMeshProUGUI nameText;        // 名称文本
    public Good currentItem { get; private set; }

    public Boolean UpdateSlot(Good item, Sprite backgroundImage)
    {
        string goodSpritePath = $"UI/{item.UIPath}";
        Sprite itemSprite = Resources.Load<Sprite>(goodSpritePath);
        
        if(itemSprite != null)
        {
            currentItem = item;
            
            this.backgroundImage.sprite = backgroundImage;
            iconImage.enabled = true;

            iconImage.sprite = itemSprite;

            
            quantityText.text = item.Quantity.ToString();
            quantityText.enabled = item.Quantity >= 1;
            
            nameText.text = item.Name;
            nameText.enabled = true;

            return true;
        }
        
            Debug.LogWarning($"找不到物品图片: {item.UIPath}");
            return false;
    }

    public void UpdateSlot(Sprite sprite, string name)
    {
        string slotSpritePath = "UI/ui_zhuangbeikuang";  // 有物品的背景框
        Sprite slotSprite = Resources.Load<Sprite>(slotSpritePath);
        backgroundImage.sprite = slotSprite;
        iconImage.sprite = sprite;
        nameText.text = name;
    }

    public void ClearSlot()
    {
        currentItem = null;
        iconImage.enabled = false;
        quantityText.enabled = false;
        nameText.enabled = false;
    }
}