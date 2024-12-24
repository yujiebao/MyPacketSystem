using System.Collections;
using System.Collections.Generic;
using System.IO;
using Mono.Data.Sqlite;
using SQLite;
using UnityEngine;

public class SQLiteManager : MonoBehaviour
{

    // private SQLiteConnection connection;
    private SqliteConnection connection;

    public void Initialize()
    {
        string dbPath = Path.Combine(Application.persistentDataPath, "myDatabase.db3");
        // connection = new SQLiteConnection("Data Source=" + dbPath);
        connection = new SqliteConnection("Data Source=" + dbPath);
        connection.Open();
    }

    public void Close()
    {
        connection.Close();
    }
}
