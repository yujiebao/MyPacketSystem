using UnityEngine;
using System;
using System.Collections.Generic;
using System.Data;
using Mono.Data.Sqlite;

/// <summary>
/// 背包管理系统，负责管理玩家背包中的物品
/// </summary>
public class InventoryManager
{
    private static InventoryManager instance;
    private const int MAX_SLOTS = 30;  // 背包最大格子数

    /// <summary>
    /// 单例模式访问器
    /// </summary>
    public static InventoryManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new InventoryManager();
            }
            return instance;
        }
    }

    private InventoryManager()
    {
        // 确保所有格子都被正确初始化
        DatabaseHelper.Instance.BeginTransaction(connection =>
        {
            try
            {
                // 先检查现有格子数量
                var countQuery = "SELECT COUNT(*) FROM Inventory";
                int currentSlots = 0;
                
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = countQuery;
                    currentSlots = Convert.ToInt32(command.ExecuteScalar());
                }

                // 如果格子数量不正确，重新初始化所有格子
                if (currentSlots != MAX_SLOTS)
                {
                    // 清空现有格子
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "DELETE FROM Inventory";
                        command.ExecuteNonQuery();
                    }

                    // 创建新的格子
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "INSERT INTO Inventory (SlotId, ItemId, Quantity) VALUES (@SlotId, NULL, 0)";
                        var slotParam = command.CreateParameter();
                        slotParam.ParameterName = "@SlotId";
                        command.Parameters.Add(slotParam);

                        for (int i = 0; i < MAX_SLOTS; i++)
                        {
                            slotParam.Value = i;
                            command.ExecuteNonQuery();
                        }
                    }
                    
                    Debug.Log($"背包已初始化 {MAX_SLOTS} 个格子");
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"背包初始化失败: {e.Message}");
                throw;
            }
        });
    }

    /// <summary>
    /// 添加物品到背包
    /// </summary>
    /// <param name="good">要添加的物品</param>
    /// <returns>是否添加成功</returns>
    public bool AddItem(Good good)
    {
        if (good.Quantity <= 0) return false;

        return DatabaseHelper.Instance.BeginTransaction(connection =>
        {
            try
            {
                // 先尝试堆叠到现有的同类物品上
                var existingSlots = GetSlotsWithItem(good.ItemId);
                foreach (var slotId in existingSlots)
                {
                    var currentQuantity = GetSlotQuantity(slotId);
                    var maxStack = GetItemMaxStack(good.ItemId);
                    var spaceLeft = maxStack - currentQuantity;
                    
                    if (spaceLeft > 0)
                    {
                        var amountToAdd = Math.Min(spaceLeft, good.Quantity);
                        UpdateSlotQuantity(connection, slotId, currentQuantity + amountToAdd);
                        good.Quantity -= amountToAdd;
                        
                        if (good.Quantity <= 0) return true;
                    }
                }

                // 如果还有剩余物品，尝试放入空格子
                var emptySlots = GetEmptySlots();
                foreach (var slotId in emptySlots)
                {
                    var maxStack = GetItemMaxStack(good.ItemId);
                    var amountToAdd = Math.Min(maxStack, good.Quantity);
                    AddItemToSlot(connection, slotId, good.ItemId, amountToAdd);
                    good.Quantity -= amountToAdd;
                    
                    if (good.Quantity <= 0) return true;
                }

                if (good.Quantity > 0)
                {
                    Debug.LogError("背包已满");
                    return false;
                }

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"添加物品失败: {e.Message}");
                return false;
            }
        });
    }

    private void InitializeSlots(SqliteConnection connection)
    {
        var initQuery = @"
            DELETE FROM Inventory;
            INSERT INTO Inventory (SlotId, ItemId, Quantity)
            SELECT number, NULL, 0
            FROM (
                WITH RECURSIVE numbers(number) AS (
                    SELECT 1
                    UNION ALL
                    SELECT number + 1 FROM numbers WHERE number < @MaxSlots
                )
                SELECT number FROM numbers
            );";

        using (var command = connection.CreateCommand())
        {
            command.CommandText = initQuery;
            command.Parameters.AddWithValue("@MaxSlots", MAX_SLOTS);
            command.ExecuteNonQuery();
        }
    }

    private Good GetSlotInfo(int slotId)
    {
        var query = @"
            SELECT i.ItemId, i.Quantity, t.MaxStack, t.UIPath, t.Name
            FROM Inventory i
            LEFT JOIN Items t ON i.ItemId = t.ItemId
            WHERE i.SlotId = @SlotId";

        var parameters = new[] { new SqliteParameter("@SlotId", slotId) };
        var result = DatabaseHelper.Instance.ExecuteQuery(query, parameters);

        if (result.Rows.Count > 0 && !result.Rows[0].IsNull("ItemId"))
        {
            return new Good(
                Convert.ToInt32(result.Rows[0]["ItemId"]),
                result.Rows[0]["UIPath"].ToString(),
                result.Rows[0]["Name"].ToString(),
                Convert.ToInt32(result.Rows[0]["MaxStack"]),
                Convert.ToInt32(result.Rows[0]["Quantity"])
            );
        }
        return null;
    }

    public int GetItemMaxStack(int itemId)
    {
        var query = "SELECT MaxStack FROM Items WHERE ItemId = @ItemId";
        var parameters = new[] { new SqliteParameter("@ItemId", itemId) };
        var result = DatabaseHelper.Instance.ExecuteQuery(query, parameters);

        if (result.Rows.Count > 0)
        {
            return Convert.ToInt32(result.Rows[0]["MaxStack"]);
        }
        return 1; // 默认堆叠数量为1
    }

    /// <summary>
    /// 从背包中移除物品
    /// </summary>
    /// <param name="itemId">要移除的物品ID</param>
    /// <param name="quantity">移除数量</param>
    /// <returns>是否移除成功</returns>
    public bool RemoveItem(int itemId, int quantity)
    {
        if (quantity <= 0) return false;
        if (GetTotalQuantity(itemId) < quantity) return false;

        return DatabaseHelper.Instance.BeginTransaction(connection =>
        {
            var slots = GetSlotsWithItem(itemId);
            int remainingToRemove = quantity;

            foreach (var slotId in slots)
            {
                var currentQuantity = GetSlotQuantity(slotId);
                if (currentQuantity <= remainingToRemove)
                {
                    ClearSlot(connection, slotId);
                    remainingToRemove -= currentQuantity;
                }
                else
                {
                    UpdateSlotQuantity(connection, slotId, currentQuantity - remainingToRemove);
                    remainingToRemove = 0;
                }

                if (remainingToRemove <= 0) break;
            }
            return true;
        });
    }

    /// <summary>
    /// 获取所有背包格子中的物品
    /// </summary>
    /// <returns>背包中的所有物品列表，包括空格子</returns>
    public List<Good> GetAllItems()
    {
        var query = @"
            SELECT 
                i.SlotId, 
                i.ItemId, 
                i.Quantity,
                COALESCE(t.UIPath, '') as UIPath,
                COALESCE(t.Name, '') as Name,
                COALESCE(t.MaxStack, 99) as MaxStack
            FROM Inventory i
            LEFT JOIN Items t ON i.ItemId = t.ItemId
            ORDER BY i.SlotId";

        var result = DatabaseHelper.Instance.ExecuteQuery(query);
        var items = new List<Good>();

        foreach (DataRow row in result.Rows)
        {
            var good = new Good();
            good.SlotId = Convert.ToInt32(row["SlotId"]);

            if (!row.IsNull("ItemId"))
            {
                good.ItemId = Convert.ToInt32(row["ItemId"]);
                good.Name = row["Name"].ToString();
                good.UIPath = row["UIPath"].ToString();
                good.MaxStack = Convert.ToInt32(row["MaxStack"]);
                good.Quantity = Convert.ToInt32(row["Quantity"]);
            }
            else
            {
                good.ItemId = 0;
                good.Quantity = 0;
                good.MaxStack = 99;
            }

            items.Add(good);
        }

        return items;
    }

    /// <summary>
    /// 获取指定格子中的物品
    /// </summary>
    /// <param name="slotId">格子ID</param>
    /// <returns>子中的物品信息</returns>
    public Good GetItem(int slotId)
    {
        var query = @"
            SELECT 
                i.SlotId, 
                i.ItemId, 
                i.Quantity,
                COALESCE(t.UIPath, '') as UIPath,
                COALESCE(t.Name, '') as Name,
                COALESCE(t.MaxStack, 99) as MaxStack
            FROM Inventory i
            LEFT JOIN Items t ON i.ItemId = t.ItemId
            WHERE i.SlotId = @SlotId";

        var parameters = new[] { new SqliteParameter("@SlotId", slotId) };
        var result = DatabaseHelper.Instance.ExecuteQuery(query, parameters);

        if (result.Rows.Count > 0)
        {
            var row = result.Rows[0];
            var good = new Good();
            good.SlotId = slotId;

            if (!row.IsNull("ItemId"))
            {
                good.ItemId = Convert.ToInt32(row["ItemId"]);
                good.Name = row["Name"].ToString();
                good.UIPath = row["UIPath"].ToString();
                good.MaxStack = Convert.ToInt32(row["MaxStack"]);
                good.Quantity = Convert.ToInt32(row["Quantity"]);
            }
            else
            {
                good.ItemId = 0;
                good.Quantity = 0;
                good.MaxStack = 99;
            }

            return good;
        }

        throw new Exception($"Slot {slotId} not found");
    }

    /// <summary>
    /// 获取指定物品在背包中的总数量
    /// </summary>
    /// <param name="itemId">物品ID</param>
    /// <returns>物品总数量</returns>
    public int GetTotalQuantity(int itemId)
    {
        var query = "SELECT SUM(Quantity) FROM Inventory WHERE ItemId = @ItemId";
        var parameters = new[] { new SqliteParameter("@ItemId", itemId) };
        
        var result = DatabaseHelper.Instance.ExecuteQuery(query, parameters);
        if (result.Rows.Count > 0 && !result.Rows[0].IsNull(0))
        {
            return Convert.ToInt32(result.Rows[0][0]);
        }
        return 0;
    }

    /// <summary>
    /// 获取所有的背包格子
    /// </summary>
    /// <returns>空格子ID列表</returns>
    private List<int> GetEmptySlots()
    {
        var query = @"
            SELECT SlotId 
            FROM Inventory 
            WHERE ItemId IS NULL OR Quantity <= 0 
            ORDER BY SlotId";

        var result = DatabaseHelper.Instance.ExecuteQuery(query);
        var emptySlots = new List<int>();
        
        foreach (DataRow row in result.Rows)
        {
            emptySlots.Add(Convert.ToInt32(row["SlotId"]));
        }
        
        return emptySlots;
    }

    /// <summary>
    /// 获取包含指定物品的所有格子
    /// </summary>
    /// <param name="itemId">物品ID</param>
    /// <returns>包含该物品的格子ID列表</returns>
    private List<int> GetSlotsWithItem(int itemId)
    {
        var query = "SELECT SlotId FROM Inventory WHERE ItemId = @ItemId AND Quantity > 0";
        var parameters = new[] { new SqliteParameter("@ItemId", itemId) };
        var result = DatabaseHelper.Instance.ExecuteQuery(query, parameters);
        
        var slots = new List<int>();
        foreach (DataRow row in result.Rows)
        {
            slots.Add(Convert.ToInt32(row["SlotId"]));
        }
        return slots;
    }

    /// <summary>
    /// 获取指定格子中的物品数量
    /// </summary>
    /// <param name="slotId">格子ID</param>
    /// <returns>物品数量</returns>
    private int GetSlotQuantity(int slotId)
    {
        var query = "SELECT Quantity FROM Inventory WHERE SlotId = @SlotId";
        var parameters = new[] { new SqliteParameter("@SlotId", slotId) };
        var result = DatabaseHelper.Instance.ExecuteQuery(query, parameters);
        
        if (result.Rows.Count > 0)
        {
            return Convert.ToInt32(result.Rows[0]["Quantity"]);
        }
        return 0;
    }

    /// <summary>
    /// 更新格子中的物品数量
    /// </summary>
    /// <param name="slotId">格子ID</param>
    /// <param name="quantity">新的数量</param>
    private void UpdateSlotQuantity(SqliteConnection connection, int slotId, int quantity)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "UPDATE Inventory SET Quantity = @Quantity WHERE SlotId = @SlotId";
            command.Parameters.AddWithValue("@Quantity", quantity);
            command.Parameters.AddWithValue("@SlotId", slotId);
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// 向空格子添加物品
    /// </summary>
    /// <param name="slotId">格子ID</param>
    /// <param name="itemId">物品ID</param>
    /// <param name="quantity">数量</param>
    private void AddItemToSlot(SqliteConnection connection, int slotId, int itemId, int quantity)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "UPDATE Inventory SET ItemId = @ItemId, Quantity = @Quantity WHERE SlotId = @SlotId";
            command.Parameters.AddWithValue("@ItemId", itemId);
            command.Parameters.AddWithValue("@Quantity", quantity);
            command.Parameters.AddWithValue("@SlotId", slotId);
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// 清空指定格子
    /// </summary>
    /// <param name="slotId">要清空的格子ID</param>
    private void ClearSlot(SqliteConnection connection, int slotId)
    {
        using (var command = connection.CreateCommand())
        {
            command.CommandText = "UPDATE Inventory SET ItemId = NULL, Quantity = 0 WHERE SlotId = @SlotId";
            command.Parameters.AddWithValue("@SlotId", slotId);
            command.ExecuteNonQuery();
        }
    }

    public int GetEmptySlotCount()
    {
        var query = "SELECT COUNT(*) FROM Inventory WHERE ItemId IS NULL OR Quantity = 0";
        var result = DatabaseHelper.Instance.ExecuteQuery(query);
        return Convert.ToInt32(result.Rows[0][0]);
    }

    public int GetCurrentItemCount()
    {
        var query = "SELECT COUNT(*) FROM Inventory WHERE ItemId IS NOT NULL AND Quantity > 0";
        var result = DatabaseHelper.Instance.ExecuteQuery(query);
        return Convert.ToInt32(result.Rows[0][0]);
    }

    public int GetTotalCapacity()
    {
        return MAX_SLOTS;  // 直接返回最大格子数
    }
} 