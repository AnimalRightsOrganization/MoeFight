using UnityEngine;
using UnityEngine.UI;
using LiteNetLib;
using LiteNetLib.Utils;
using Code.Shared;
using Code.Client;

namespace HotFix
{
    public class UI_Lobby : UIBase
    {
        public Button m_ArcadeBtn;
        public Button m_MatchBtn;
        public Button m_TrainingBtn;
        public Button m_ReplayBtn;
        public Button m_SettingsBtn;
        public Button m_ExitBtn;
        private ClientPlayer localPlayer;

        public Text m_ArcadeText;
        public Text m_MatchText;
        public Text m_TrainingText;
        public Text m_ReplayText;
        public Text m_SettingsText;
        public Text m_ExitText;

        void Awake()
        {
            m_ArcadeBtn = transform.Find("Menu/Arcade").GetComponent<Button>();
            m_MatchBtn = transform.Find("Menu/Match").GetComponent<Button>();
            m_TrainingBtn = transform.Find("Menu/Training").GetComponent<Button>();
            m_ReplayBtn = transform.Find("Menu/Replay").GetComponent<Button>();
            m_SettingsBtn = transform.Find("Menu/Settings").GetComponent<Button>();
            m_ExitBtn = transform.Find("Menu/Exit").GetComponent<Button>();

            m_ArcadeBtn.onClick.AddListener(OnArcadeButtonClick);
            m_MatchBtn.onClick.AddListener(RequestMatch);
            m_TrainingBtn.onClick.AddListener(OnTrainingButtonClick);
            m_ReplayBtn.onClick.AddListener(OnReplayButtonClick);
            m_SettingsBtn.onClick.AddListener(OnSettingsButtonClick);
            m_ExitBtn.onClick.AddListener(OnExitButtonClick);


            m_ArcadeText = transform.Find("Menu/Arcade/Text").GetComponent<Text>();
            m_MatchText = transform.Find("Menu/Match/Text").GetComponent<Text>();
            m_TrainingText = transform.Find("Menu/Training/Text").GetComponent<Text>();
            m_ReplayText = transform.Find("Menu/Replay/Text").GetComponent<Text>();
            m_SettingsText = transform.Find("Menu/Settings/Text").GetComponent<Text>();
            m_ExitText = transform.Find("Menu/Exit/Text").GetComponent<Text>();
        }

        void OnEnable()
        {
            ApplyLanguage();

            EventManager.RegisterEvent(OnNetCallback);

            localPlayer = ClientNet.Get.m_PlayerManager.LocalPlayer;

            AudioManager.Get().PlayMusic(AudioManager.Paradise, true);
        }

        void OnDisable()
        {
            EventManager.UnRegisterEvent(OnNetCallback);

            AudioManager.Get()?.StopAll();
        }

        public override void ApplyLanguage()
        {
            var config = ConfigManager.Get();

            m_ArcadeText.text = config.GetWord(7);
            m_MatchText.text = config.GetWord(8);
            m_TrainingText.text = config.GetWord(9);
            m_ReplayText.text = config.GetWord(10);
            m_SettingsText.text = config.GetWord(11);
            m_ExitText.text = config.GetWord(12);
        }

        #region 网络消息
        public override void OnNetCallback(PacketType eventID, INetSerializable reader, NetPeer peer)
        {
            switch (eventID)
            {
                case PacketType.S2C_LogoutResult:
                    OnLogoutResult(reader);
                    break;
                case PacketType.S2C_BattleReconnect:
                    OnBattleReconnect(reader);
                    break;
                case PacketType.S2C_Settings:
                    ApplyLanguage();
                    break;
            }
        }

        private void OnLogoutResult(INetSerializable reader)
        {
            Debug.Log($"[UI] 收到登出消息");

            //UI跳转到登录，关闭本页面
            UIManager.Get().PopAll();
            UIManager.Get().Push<UI_Login>();
        }

        private void OnBattleReconnect(INetSerializable reader)
        {
            Debug.Log($"[UI.Lobby] 重连");

            var packet = (S2C_LoadScenePacket)reader;

            int seatId = packet.Host.UserName == localPlayer.UserName ? 0 : 1;
            localPlayer.SetRoomID(packet.RoomId).SetSeatID(seatId).SetStatus(PlayerStatus.AtBattle);

            var dialog = UIManager.Get().Push<UI_Dialog>();
            dialog.Show("你有一个正在进行的比赛，是否立即返回？",
                () =>
                {
                    Debug.Log("放弃比赛");
                    ClientNet.Get.SendBattleQuit();
                    dialog.Pop();
                }, "No",
                () =>
                {
                    Debug.Log("返回比赛");

                    //进入Loading
                    System.Action action = () =>
                    {
                        UIManager.Get().PopAll();
                        UIManager.Get().Push<UI_GameMenu>();
                        ClientNet.Get.SendLackInput(); //请求帧数据
                    };
                    GameManager.Get.LoadBattleAsync(action); //跳转比赛

                    //追帧，完成后发送恢复比赛

                }, "Yes");
        }
        #endregion

        #region 按钮事件
        void OnArcadeButtonClick()
        {
            var ui = UIManager.Get().Push<UI_Toast>();
            ui.Show("敬请期待");
        }

        void RequestMatch()
        {
            if (localPlayer.Status != PlayerStatus.AtLobby)
            {
                Debug.LogError($"此时不允匹配：{localPlayer.Status}");
                return;
            }
            ClientNet.Get.SendMatchRequest();
            UIManager.Get().Push<UI_Matching>(2);
            localPlayer.SetStatus(PlayerStatus.Matching);
        }

        void OnTrainingButtonClick()
        {
            // 创建模拟消息
            ClientPlayer localPlayer = ClientNet.Get.m_PlayerManager.LocalPlayer;
            var packet = new S2C_MatchResultPacket
            {
                Code = 0,
                BattleMode = (byte)(BattleMode.Training),
                RoomId = 1,
                Host = new UserInfo { UserName = localPlayer.UserName, PeerId = localPlayer.PeerId },
                Guest = new UserInfo { UserName = "BOT", PeerId = 1 },
            };

            // 创建用户管理
            ClientPlayer rivalPlayer = new ClientPlayer(packet.Guest.UserName, packet.Guest.PeerId);
            ClientNet.Get.m_PlayerManager.AddClientPlayer(rivalPlayer, false);

            // 创建房间管理
            ClientNet.Get.m_ClientRoom = new ClientRoom(packet.RoomId, localPlayer, rivalPlayer);
            ClientNet.Get.m_ClientRoom.BattleMode = BattleMode.Training;


            UIManager.Get().Push<UI_RoleSelect>();
        }

        void OnReplayButtonClick()
        {
            UIManager.Get().Push<UI_Replay>();
        }

        void OnSettingsButtonClick()
        {
            UIManager.Get().Push<UI_Settings>();
        }

        void OnExitButtonClick()
        {
            var dialog = UIManager.Get().Push<UI_Dialog>();
            dialog.Show("确定退出？",
                () =>
                {
                    dialog.Pop();
                }, "否",
                () =>
                {
                    ClientNet.Get.SendLogout(); dialog.Pop();
                }, "是");
            return;

            Debug.Log("Exit");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            //UnityEditor.EditorApplication.isPaused = true; //编辑器暂停
#else
            Application.Quit();
#endif
        }
        #endregion
    }
}