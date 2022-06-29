using System;
using System.Collections.Generic;
using UnityEngine;

public class DatabaseTest : MonoBehaviour
{
    void Start()
    {
        //Debug.Log(DatabaseEssential.DatabaseManager.instance.Query("SELECT * FROM tb_user ORDER BY userid DESC")); //打印所有

        //string query = $"SELECT Count(*) FROM tb_user WHERE username='admin'";
        //Debug.Log(DatabaseEssential.DatabaseManager.instance.Count(query)); //符合条件的数量
    }

    void Update()
    {
        //Count
        if (Input.GetKeyDown(KeyCode.C))
        {
            string query = $"SELECT Count(*) FROM userdata";
            Debug.Log(DatabaseEssential.DatabaseManager.instance.Count(query));
        }
        //Select
        if (Input.GetKeyDown(KeyCode.S))
        {
            string columnName = "ID,Username,Password,Email,Phone,Address";
            List<string>[] results = DatabaseEssential.DatabaseManager.instance.SelectAllRecord("userdata", columnName);

            for (int i = 0; i < results.Length; i++)
            {
                List<string> tempList = results[i];
                foreach (var records in tempList)
                    Debug.Log(records);
            }
        }
        //Update
        if (Input.GetKeyDown(KeyCode.U))
        {
            DatabaseEssential.DatabaseManager.instance.UpdateRecord("userdata", "Username='Ashikur Rahman' WHERE Username='Srejon Khan'");
            Debug.Log("Update DONE");
        }
        //Insert
        if (Input.GetKeyDown(KeyCode.I))
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[8];
            var random = new System.Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }
            var randomName = new String(stringChars);

            DatabaseEssential.DatabaseManager.instance.InsertRecord("userdata", "ID,Username,Password,Email,Phone,Address", $"'0', '{randomName}', '123456', '{randomName}@gmail.com', '0123456789', 'Unknown Street, Unknown Street'");
            Debug.Log("Insert Done");
        }
        //Delete
        if (Input.GetKeyDown(KeyCode.D))
        {
            DatabaseEssential.DatabaseManager.instance.DeleteRecord("userdata", "Username='Ashikur Rahman'");
            Debug.Log("Delete Done");
        }
        //Select Certain
        if (Input.GetKeyDown(KeyCode.W))
        {
            Debug.Log(DatabaseEssential.DatabaseManager.instance.Query("SELECT * FROM userdata ORDER BY ID DESC"));
        }
    }
}