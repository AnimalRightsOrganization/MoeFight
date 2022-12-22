using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using Code.Client;
using Code.Server;
using Code.Shared;
using HitstunConstants;

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
            case "Client": ClientGUI(); break;
            case "Server": ServerGUI(); break;
            case "Photo": PhotoGUI(); break;
            default: GUILayout.Label("空"); break;
        }
    }

    void ClientGUI()
    {
        if (GUILayout.Button("登录.自动填写.test1", GUILayout.Height(25)))
        {
            var login = HotFix.UIManager.Get().GetUI<HotFix.UI_Login>();
            login.m_UserNameField.text = "test1";
            login.m_PasswordField.text = "123456";
        }
        if (GUILayout.Button("登录.自动填写.test2", GUILayout.Height(25)))
        {
            var login = HotFix.UIManager.Get().GetUI<HotFix.UI_Login>();
            login.m_UserNameField.text = "test2";
            login.m_PasswordField.text = "123456";
        }
        if (GUILayout.Button("打印房间", GUILayout.Height(25)))
        {
            if (ClientNet.Get.m_ClientRoom == null) return;
            string room = $"{ClientNet.Get.m_ClientRoom.ToString()}\n{ClientLogic.Get.IsStart}";
            Debug.Log(room);
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("单机调试", GUILayout.Width(200), GUILayout.Height(25)))
        {
            ClientNet.Get.Disconnect();
            //await Task.Delay(1);

            ClientPlayer host = new ClientPlayer("test1", 0);
            ClientPlayer guest = new ClientPlayer("bot", 1);
            ClientNet.Get.m_PlayerManager.AddClientPlayer(host, true);
            ClientNet.Get.m_PlayerManager.AddClientPlayer(guest, false);
            ClientNet.Get.m_ClientRoom = new ClientRoom(0, host, guest);
            var room = ClientNet.Get.m_ClientRoom;
            room.BattleMode = BattleMode.Training;
            var packet = new S2C_LoadScenePacket
            {
                RoomId = (short)room.RoomID,
                BattleId = room.BattleID,
                MapId = room.MapId,
                Host = new PlayerLoadPacket { UserName = host.UserName, PeerId = host.PeerId, RoleIndex = (int)CharacterName.HONOKA },
                Guest = new PlayerLoadPacket { UserName = guest.UserName, PeerId = guest.PeerId, RoleIndex = (int)CharacterName.AOI },
            };
            Debug.Log($"调试模式:\n{packet}");

            GameManager.Get.OnLoadScene(packet, UseOpening);
        }
        GUILayout.Space(20);
        UseOpening = GUILayout.Toggle(UseOpening, "使用过场", GUILayout.Height(25));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("AI动作", GUILayout.Width(200), GUILayout.Height(25)))
        {
            // SHORYUKEN
            KeyCode front = faceRight ? KeyCode.D : KeyCode.A;
            KeyCode back = faceRight ? KeyCode.A : KeyCode.D;
            List<KeyCode[]> keys = new List<KeyCode[]>
            {
                new KeyCode[] { },
                new KeyCode[] { KeyCode.S },
                new KeyCode[] { KeyCode.S },
                new KeyCode[] { KeyCode.S },
                new KeyCode[] { KeyCode.S },
                new KeyCode[] { KeyCode.S },
                new KeyCode[] { KeyCode.S },
                new KeyCode[] { KeyCode.S },
                new KeyCode[] { KeyCode.S, back },
                new KeyCode[] { KeyCode.S, back },
                new KeyCode[] { KeyCode.S, back },
                new KeyCode[] { KeyCode.S, back },
                new KeyCode[] { back },
                new KeyCode[] { back },
                new KeyCode[] { back },
                new KeyCode[] { back, KeyCode.U },
                new KeyCode[] { KeyCode.U },
                new KeyCode[] { KeyCode.U },
                new KeyCode[] { KeyCode.U },
                new KeyCode[] { KeyCode.U },
                new KeyCode[] { KeyCode.U },
                new KeyCode[] { },
            };
            for (int i = 0; i < keys.Count; i++)
            {
                uint input = LocalSession.ConvertInputs(keys[i]);
                ClientLogic.Get.custom.Enqueue(input);
            }
        }
        GUILayout.Space(20);
        faceRight = GUILayout.Toggle(faceRight, "屏幕左边", GUILayout.Height(25));
        GUILayout.EndHorizontal();

        if (GUILayout.Button("重载配置", GUILayout.Width(200), GUILayout.Height(25)))
        {
            ClientLogic.Get.runner.Reload();
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