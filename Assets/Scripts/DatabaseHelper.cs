using UnityEngine;
using System;
using System.Data;
using System.IO;
using Mono.Data.Sqlite;
using System.Threading;

/// <summary>
/// SQLite数据库帮助类，负责数据库的创建、连接和基本操作
/// </summary>
public class DatabaseHelper : MonoBehaviour
{
    private static DatabaseHelper instance;
    private string dbPath;        // 数据库文件路径
    private string connectionString;  // 数据库连接字符串
    private static readonly object lockObject = new object();

    /// <summary>
    /// 单例模式访问器
    /// </summary>
    public static DatabaseHelper Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("DatabaseHelper");
                instance = go.AddComponent<DatabaseHelper>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDatabaseConnection();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeDatabaseConnection()
    {
        // 设置数据库路径到Unity的持久化数据目录
        dbPath = Path.Combine(Application.streamingAssetsPath, "GameData.db");
        
        connectionString = $"URI=file:{dbPath}";
        
        if (File.Exists(dbPath))
        {
            // Debug.Log("使用现有数据库");
            return;  // 如果数据库存在，直接使用
        }
        
        // 数据库不存在时，创建新数据库并初始化表结构
        // Debug.Log("创建新数据库");
        InitializeDatabase();
    }

    /// <summary>
    /// 初始化数据库，如果不存在则创建
    /// </summary>
    private void InitializeDatabase()
    {
        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();
            using (var command = connection.CreateCommand())
            {
                // 创建物品表
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Items (
                        ItemId INTEGER PRIMARY KEY,
                        UIPath TEXT NOT NULL,
                        Name TEXT NOT NULL,
                        MaxStack INTEGER NOT NULL DEFAULT 99
                    )";
                command.ExecuteNonQuery();

                // 创建背包表
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS Inventory (
                        SlotId INTEGER PRIMARY KEY,
                        ItemId INTEGER NULL,
                        Quantity INTEGER NOT NULL DEFAULT 0,
                        FOREIGN KEY(ItemId) REFERENCES Items(ItemId)
                    )";
                command.ExecuteNonQuery();

                // 先初始化物品数据
                InitializeItems(command);

                // 初始化背包格子
                command.CommandText = @"
                    DELETE FROM Inventory;  -- 先清空现有数据
                    INSERT INTO Inventory (SlotId, ItemId, Quantity)
                    VALUES 
                        (1, NULL, 0), (2, NULL, 0), (3, NULL, 0), (4, NULL, 0), (5, NULL, 0),
                        (6, NULL, 0), (7, NULL, 0), (8, NULL, 0), (9, NULL, 0), (10, NULL, 0),
                        (11, NULL, 0), (12, NULL, 0), (13, NULL, 0), (14, NULL, 0), (15, NULL, 0),
                        (16, NULL, 0), (17, NULL, 0), (18, NULL, 0), (19, NULL, 0), (20, NULL, 0),
                        (21, NULL, 0), (22, NULL, 0), (23, NULL, 0), (24, NULL, 0), (25, NULL, 0),
                        (26, NULL, 0), (27, NULL, 0), (28, NULL, 0), (29, NULL, 0), (30, NULL, 0)";
                command.ExecuteNonQuery();
            }
        }
    }

    private void InitializeItems(SqliteCommand command)
    {
        var items = new[]
        {
            "INSERT OR REPLACE INTO Items (ItemId, UIPath, Name, MaxStack) VALUES (101, 'icon_wuqi_tiepigong', '铁弓', 1)",
            "INSERT OR REPLACE INTO Items (ItemId, UIPath, Name, MaxStack) VALUES (102, 'icon_wuqi_dao', '铁刀', 1)",
            "INSERT OR REPLACE INTO Items (ItemId, UIPath, Name, MaxStack) VALUES (150, 'icon_danyao_tiejian_yumao', '羽毛箭', 30)",
            "INSERT OR REPLACE INTO Items (ItemId, UIPath, Name, MaxStack) VALUES (202, 'icon_cailiao_shengtie', '生铁', 10)",
            "INSERT OR REPLACE INTO Items (ItemId, UIPath, Name, MaxStack) VALUES (203, 'icon_cailiao_mutou', '木头', 20)",
            "INSERT OR REPLACE INTO Items (ItemId, UIPath, Name, MaxStack) VALUES (204, 'icon_cailiao_jiaodai', '胶带', 10)",
            "INSERT OR REPLACE INTO Items (ItemId, UIPath, Name, MaxStack) VALUES (205, 'icon_cailiao_masheng', '麻绳', 10)",
            "INSERT OR REPLACE INTO Items (ItemId, UIPath, Name, MaxStack) VALUES (206, 'icon_cailiao_yumao', '羽毛', 30)"
        };

        foreach (var sql in items)
        {
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    /// 执行非查询SQL语句（如INSERT、UPDATE、DELETE等）
    /// </summary>
    public void ExecuteNonQuery(string query, params SqliteParameter[] parameters)
    {
        lock (lockObject)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = query;
                        if (parameters != null)
                        {
                            command.Parameters.AddRange(parameters);
                        }
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"执行SQL失败: {e.Message}");
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// 执行查询SQL语句并返回结果集
    /// </summary>
    public DataTable ExecuteQuery(string query, params SqliteParameter[] parameters)
    {
        lock (lockObject)
        {
            using (var connection = new SqliteConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = query;
                        if (parameters != null)
                        {
                            command.Parameters.AddRange(parameters);
                        }

                        var dataTable = new DataTable();
                        using (var reader = command.ExecuteReader())
                        {
                            dataTable.Load(reader);
                        }
                        return dataTable;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"执行查询失败: {e.Message}");
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// 开始一个数据库事务
    /// </summary>
    public bool BeginTransaction(Func<SqliteConnection, bool> action)
    {
        using (var connection = new SqliteConnection(connectionString))
        {
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    bool result = action(connection);
                    transaction.Commit();
                    return result;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Debug.LogError($"事务执行失败: {ex.Message}");
                    throw;
                }
            }
        }
    }
}