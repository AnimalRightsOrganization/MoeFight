using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using Code.Server;

public class TestWindow : EditorWindow
{
    static TestWindow window;

    public bool UseOpening;
    public bool faceRight = true;

    public static void ShowWindow()
    {
        window = (TestWindow)GetWindow(typeof(TestWindow));
        window.titleContent = new GUIContent("调试窗口");
        window.Show();
    }

    void OnGUI()
    {
        switch (SceneManager.GetActiveScene().name)
        {
            case "Server": ServerGUI(); break;
            case "Photo": PhotoGUI(); break;
            default: GUILayout.Label("空"); break;
        }
    }

    void ServerGUI()
    {
        if (GUILayout.Button("Server Print"))
        {
            ServerNet.Get.m_PlayerManager.Print();
            ServerNet.Get.m_RoomManager.Print();
        }
        if (GUILayout.Button("Room Print"))
        {
            ServerRoom room = ServerNet.Get.m_RoomManager.GetAll()[0];
            Debug.Log($"room#{room.RoomID}:{room.BattleStage}:{room.hostPlayer.RoleIndex} vs {room.guestPlayer.RoleIndex}" +
                $"\nbufferTick:{room.bufferTick}, serverTick:{room.serverTick}, dic_recv:{room.dic_recv.Count}");
        }
    }
    void PhotoGUI()
    {
        if (GUILayout.Button("SnapShot"))
        {
            string fileName = $"{Application.streamingAssetsPath}/Actor_{DateTime.Now.ToString("yyyyMMddhhmmss")}.png";
            ScreenCapture.CaptureScreenshot(fileName);
            Debug.Log(fileName);
            AssetDatabase.Refresh();
        }
    }
}