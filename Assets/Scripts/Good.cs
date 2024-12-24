using UnityEngine;

[System.Serializable]
public class Good
{
    public int SlotId { get; set; }
    public int ItemId { get; set; }
    public string UIPath { get; set;}
    public string Name { get; set; }
    public int Quantity { get; set; }
    public int MaxStack { get; set; }

    public bool IsEmpty => ItemId == 0 || Quantity == 0;

    public Good(int itemId, int quantity)
    {
        ItemId = itemId;
        Quantity = quantity;
    }
    
    public Good() { }

    // 用于配方结果的构造函数
    public Good(int itemId, string uiPath, string name, int maxStack)
    {
        ItemId = itemId;
        UIPath = uiPath;
        Name = name;
        MaxStack = maxStack;
        Quantity = 0;
    }

    // 用于配方材料的构造函数
    public Good(int itemId, string uiPath, string name, int maxStack, int quantity)
        : this(itemId, uiPath, name, maxStack)
    {
        Quantity = quantity;
    }
}
