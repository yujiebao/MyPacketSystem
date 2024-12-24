using System;

/// <summary>
/// 配方材料类，定义制作配方中的材料需求
/// </summary>
[Serializable]
public class RecipeIngredient
{
    /// <summary>
    /// 材料物品ID
    /// </summary>
    public int ItemId { get; set; }

    /// <summary>
    /// 所需数量
    /// </summary>
    public int RequiredQuantity { get; set; }
} 