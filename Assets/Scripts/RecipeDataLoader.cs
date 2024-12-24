using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// CSV数据加载器，负责从CSV文件读取游戏数据
/// </summary>
public class RecipeDataLoader
{
    private static RecipeDataLoader instance;
    private Dictionary<int, Recipe> recipeDict = new Dictionary<int, Recipe>();
    private bool isLoaded = false;

    /// <summary>
    /// 单例模式访问器
    /// </summary>
    public static RecipeDataLoader Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new RecipeDataLoader();
            }
            return instance;
        }
    }

    private RecipeDataLoader()
    {
        LoadRecipeData(); // 在构造函数中加载数据
    }

    /// <summary>
    /// 从CSV文件加载配方数据
    /// </summary>
    public void LoadRecipeData()
    {
        if (isLoaded)
        {
            // Debug.Log("配方数据已经加载过了");
            return;
        }

        string recipePath = Path.Combine(Application.streamingAssetsPath, "Recipes.csv");
        string ingredientPath = Path.Combine(Application.streamingAssetsPath, "RecipeIngredients.csv");
        
        if (!File.Exists(recipePath) || !File.Exists(ingredientPath))
        {
            Debug.LogError("配方CSV文件不存在!");
            return;
        }

        try
        {
            // 读取配方主表
            var recipeLines = File.ReadAllLines(recipePath);
            // Debug.Log($"读取到 {recipeLines.Length} 行配方数据");
            LoadRecipes(recipeLines.Skip(1)); // 跳过表头

            // 读取配方材料表
            var ingredientLines = File.ReadAllLines(ingredientPath);
            LoadIngredients(ingredientLines.Skip(1)); // 跳过表头

            isLoaded = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"加载CSV数据失败: {e.Message}\n{e.StackTrace}");
        }
    }

    private void LoadRecipes(IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(line)) continue;

            try
            {
                var values = line.Split(',');

                if (values.Length < 3) // 现在需要至少3个值：ID、结果物品ID和描述
                {
                    Debug.LogError($"配方数据格式错误: {line}");
                    continue;
                }

                var recipe = new Recipe
                {
                    RecipeId = int.Parse(values[0]),
                    ResultItemId = int.Parse(values[1]),
                    Description = values[2]
                };
                
                // Debug.Log($"添加配方: ID={recipe.RecipeId}, 结果物品ID={recipe.ResultItemId}, 描述={recipe.Description}");
                recipeDict[recipe.RecipeId] = recipe;
            }
            catch (Exception e)
            {
                Debug.LogError($"加载配方数据失败，行内容: {line}, 错误: {e.Message}");
            }
        }
    }

    private void LoadIngredients(IEnumerable<string> lines)
    {
        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(line)) continue;

            try
            {
                var values = line.Split(',');
                // Debug.Log($"解析材料行: {line}");

                if (values.Length < 3)
                {
                    Debug.LogError($"材料数据格式错误: {line}");
                    continue;
                }

                var recipeId = int.Parse(values[0]);
                
                if (recipeDict.TryGetValue(recipeId, out Recipe recipe))
                {
                    var ingredient = new RecipeIngredient
                    {
                        ItemId = int.Parse(values[1]),
                        RequiredQuantity = int.Parse(values[2])
                    };
                    
                    // Debug.Log($"添加材料: 配方ID={recipeId}, 材料ID={ingredient.ItemId}, 数量={ingredient.RequiredQuantity}");
                    recipe.Ingredients.Add(ingredient);
                }
                else
                {
                    Debug.LogError($"找不到对应的配方ID: {recipeId}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"加载配方材料数据失败，行内容: {line}, 错误: {e.Message}");
            }
        }
    }

    /// <summary>
    /// 获取所有配方
    /// </summary>
    public List<Recipe> GetAllRecipes()
    {
        if (!isLoaded)
        {
            Debug.LogWarning("配方数据尚未加载，正在重新加载...");
            LoadRecipeData();
        }
        return new List<Recipe>(recipeDict.Values);
    }

    /// <summary>
    /// 获取指定配方
    /// </summary>
    public Recipe GetRecipe(int recipeId)
    {
        if (!isLoaded)
        {
            Debug.LogWarning("配方数据尚未加载，正在重新加载...");
            LoadRecipeData();
        }
        return recipeDict.TryGetValue(recipeId, out Recipe recipe) ? recipe : null;
    }
} 