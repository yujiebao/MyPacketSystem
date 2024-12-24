using System;
using System.Collections.Generic;

/// <summary>
/// 配方类，定义一个可制作物品的配方信息
/// </summary>
[Serializable]
public class Recipe
{
    /// <summary>
    /// 配方ID
    /// </summary>
    public int RecipeId { get; set; }

    /// <summary>
    /// 制作结果物品ID
    /// </summary>
    public int ResultItemId { get; set; }

    /// <summary>
    /// 配方描述
    /// </summary>
    public string Description { get; set; }

    /// <summary>
    /// 制作所需的材料列表
    /// </summary>
    public List<RecipeIngredient> Ingredients { get; set; }

    public Recipe()
    {
        Ingredients = new List<RecipeIngredient>();
    }
} 