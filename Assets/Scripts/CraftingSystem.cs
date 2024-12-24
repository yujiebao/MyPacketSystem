using UnityEngine;
using System;
using Mono.Data.Sqlite;
using System.Collections.Generic;

public class CraftingSystem
{
    private static CraftingSystem instance;

    public static CraftingSystem Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new CraftingSystem();
            }
            return instance;
        }
    }

    private CraftingSystem()
    {
        RecipeDataLoader.Instance.LoadRecipeData();
    }

    public bool Craft(Recipe recipe, int amount = 1)
    {
        if (!CanCraft(recipe, amount))
        {
            return false;
        }

        try 
        {
            bool success = false;
            DatabaseHelper.Instance.BeginTransaction(connection =>
            {
                try
                {
                    // 扣除材料
                    foreach (var ingredient in recipe.Ingredients)
                    {
                        var requiredAmount = ingredient.RequiredQuantity * amount;
                        var remainingToDeduct = requiredAmount;
                        
                        // 获取所有包含该物品的格子
                        var slotsQuery = @"
                            SELECT SlotId, Quantity 
                            FROM Inventory 
                            WHERE ItemId = @ItemId 
                            AND Quantity > 0 
                            ORDER BY Quantity DESC";
                            
                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = slotsQuery;
                            command.Parameters.AddWithValue("@ItemId", ingredient.ItemId);
                            
                            using (var reader = command.ExecuteReader())
                            {
                                var slots = new List<(int slotId, int quantity)>();
                                while (reader.Read())
                                {
                                    slots.Add((reader.GetInt32(0), reader.GetInt32(1)));
                                }
                                reader.Close();
                                
                                // 从每个格子中扣除材料
                                foreach (var (slotId, quantity) in slots)
                                {
                                    if (remainingToDeduct <= 0) break;
                                    
                                    var deductFromThisSlot = Math.Min(quantity, remainingToDeduct);
                                    var updateQuery = @"
                                        UPDATE Inventory 
                                        SET Quantity = CASE
                                            WHEN Quantity - @Amount <= 0 THEN 0
                                            ELSE Quantity - @Amount
                                        END,
                                        ItemId = CASE
                                            WHEN Quantity - @Amount <= 0 THEN NULL
                                            ELSE ItemId
                                        END
                                        WHERE SlotId = @SlotId";
                                        
                                    using (var updateCommand = connection.CreateCommand())
                                    {
                                        updateCommand.CommandText = updateQuery;
                                        updateCommand.Parameters.AddWithValue("@Amount", deductFromThisSlot);
                                        updateCommand.Parameters.AddWithValue("@SlotId", slotId);
                                        updateCommand.ExecuteNonQuery();
                                    }
                                    
                                    remainingToDeduct -= deductFromThisSlot;
                                }
                                
                                if (remainingToDeduct > 0)
                                {
                                    throw new Exception($"材料不足：物品ID {ingredient.ItemId}");
                                }
                            }
                        }
                    }

                    // 添加制作结果到背包的逻辑
                    var remainingAmount = amount;
                    var maxStack = InventoryManager.Instance.GetItemMaxStack(recipe.ResultItemId);

                    // 先尝试堆叠到现有的同类物品上
                    var existingSlotsQuery = @"
                        SELECT SlotId, Quantity 
                        FROM Inventory 
                        WHERE ItemId = @ItemId 
                        AND Quantity > 0 
                        ORDER BY Quantity DESC";

                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = existingSlotsQuery;
                        command.Parameters.AddWithValue("@ItemId", recipe.ResultItemId);
                        
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read() && remainingAmount > 0)
                            {
                                var slotId = reader.GetInt32(0);
                                var currentQuantity = reader.GetInt32(1);
                                var spaceLeft = maxStack - currentQuantity;
                                
                                if (spaceLeft > 0)
                                {
                                    var amountToAdd = Math.Min(spaceLeft, remainingAmount);
                                    
                                    using (var updateCmd = connection.CreateCommand())
                                    {
                                        updateCmd.CommandText = "UPDATE Inventory SET Quantity = Quantity + @Amount WHERE SlotId = @SlotId";
                                        updateCmd.Parameters.AddWithValue("@Amount", amountToAdd);
                                        updateCmd.Parameters.AddWithValue("@SlotId", slotId);
                                        updateCmd.ExecuteNonQuery();
                                    }
                                    
                                    remainingAmount -= amountToAdd;
                                }
                            }
                        }
                    }

                    // 如果还有剩余物品，放入空格子
                    while (remainingAmount > 0)
                    {
                        var amountForThisSlot = Math.Min(maxStack, remainingAmount);
                        
                        var addToEmptySlotQuery = @"
                            UPDATE Inventory 
                            SET ItemId = @ItemId, 
                                Quantity = @Quantity 
                            WHERE ItemId IS NULL 
                            AND rowid = (
                                SELECT rowid 
                                FROM Inventory 
                                WHERE ItemId IS NULL 
                                LIMIT 1
                            )";

                        using (var command = connection.CreateCommand())
                        {
                            command.CommandText = addToEmptySlotQuery;
                            command.Parameters.AddWithValue("@ItemId", recipe.ResultItemId);
                            command.Parameters.AddWithValue("@Quantity", amountForThisSlot);
                            
                            int rowsAffected = command.ExecuteNonQuery();
                            if (rowsAffected == 0)
                            {
                                throw new Exception("添加物品失败：背包已满");
                            }
                            
                            remainingAmount -= amountForThisSlot;
                        }
                    }

                    success = true;
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogError($"制作失败: {e.Message}");
                    throw;
                }
            });
            
            return success;
        }
        catch (Exception e)
        {
            Debug.LogError($"制作失败: {e.Message}");
            return false;
        }
    }

    private int GetResultQuantity(int itemId)
    {
        var query = "SELECT MaxStack FROM Items WHERE ItemId = @ItemId";
        var parameters = new[] { new SqliteParameter("@ItemId", itemId) };
        var result = DatabaseHelper.Instance.ExecuteQuery(query, parameters);

        if (result.Rows.Count > 0)
        {
            return Convert.ToInt32(result.Rows[0]["MaxStack"]);
        }
        return 1;
    }

    public bool CanCraft(Recipe recipe, int amount = 1)
    {
        if (recipe == null) return false;

        foreach (var ingredient in recipe.Ingredients)
        {
            var requiredAmount = ingredient.RequiredQuantity * amount;
            var availableAmount = InventoryManager.Instance.GetTotalQuantity(ingredient.ItemId);

            if (availableAmount < requiredAmount)
            {
                return false;
            }
        }

        var resultQuantity = GetResultQuantity(recipe.ResultItemId);
        var totalAmount = resultQuantity * amount;
        var maxStackSize = InventoryManager.Instance.GetItemMaxStack(recipe.ResultItemId);

        var existingAmount = InventoryManager.Instance.GetTotalQuantity(recipe.ResultItemId);
        var existingStacks = Mathf.CeilToInt((float)existingAmount / maxStackSize);
        var totalStacks = Mathf.CeilToInt((float)(existingAmount + totalAmount) / maxStackSize);
        var neededNewSlots = totalStacks - existingStacks;

        return InventoryManager.Instance.GetEmptySlotCount() >= neededNewSlots;
    }
}