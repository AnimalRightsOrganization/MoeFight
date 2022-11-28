using System.Threading.Tasks;
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

        private S2C_LoadScenePacket m_Packet;

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
                case PacketType.S2C_Settings:
                    ApplyLanguage();
                    break;
                case PacketType.S2C_BattleReconnect:
                    OnBattleReconnect(reader);
                    break;
                case PacketType.S2C_BattleInputs:
                    OnBattleInputs(reader);
                    break;
                case PacketType.S2C_BattleEnd:
                    OnBattleEnd(reader);
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
            Debug.Log($"[UI.Lobby] 提示重连");

            var packet = (S2C_LoadScenePacket)reader;
            //ClientNet.Get.m_ClientRoom.DoInit(packet); //还没有房间

            int seatId = packet.Host.UserName == localPlayer.UserName ? 0 : 1;
            localPlayer.SetRoomID(packet.RoomId).SetSeatID(seatId).SetStatus(PlayerStatus.AtBattle);

            var dialog = UIManager.Get().Push<UI_Dialog>();
            dialog.Show("你有一个正在进行的比赛，是否立即返回？",
                () =>
                {
                    Debug.Log("放弃比赛");
                    ClientNet.Get.SendBattleQuit(); //认输
                    dialog.Pop();
                }, "No",
                () =>
                {
                    this.m_Packet = packet;
                    Debug.Log("回到比赛");
                    ClientNet.Get.SendLackInput(); //请求帧数据
                    //dialog.Pop(); //这里无法执行
                }, "Yes");
        }

        private async void OnBattleInputs(INetSerializable reader)
        {
            var packet = (S2C_LackInputPacket)reader;
            var size = (packet.inputs.Length * 12 + 4) / 1024;
            Debug.Log($"跳转Loading页：{packet.frameNumber}条，{size}KB");

            // 跳转Loading页
            UIManager.Get().PopAll();
            var ui = UIManager.Get().Push<UI_Versus>();
            ui.FadeIn(0, 0);


            // 创建用户管理
            bool localIsHost = ClientNet.Get.m_PlayerManager.LocalPlayer.PeerId == m_Packet.Host.PeerId;
            string rivalName = localIsHost ? m_Packet.Guest.UserName : m_Packet.Host.UserName;
            short rivalPeer = localIsHost ? m_Packet.Guest.PeerId : m_Packet.Host.PeerId;
            ClientPlayer rivalPlayer = new ClientPlayer(rivalName, rivalPeer);
            ClientNet.Get.m_PlayerManager.AddClientPlayer(rivalPlayer, false);
            // 创建房间管理
            ClientPlayer host = localIsHost ? ClientNet.Get.m_PlayerManager.LocalPlayer : ClientNet.Get.m_PlayerManager.RivalPlayer;
            ClientPlayer guest = localIsHost ? ClientNet.Get.m_PlayerManager.RivalPlayer : ClientNet.Get.m_PlayerManager.LocalPlayer;
            ClientNet.Get.m_ClientRoom = new ClientRoom(m_Packet.RoomId, host, guest);
            ClientNet.Get.m_ClientRoom.BattleMode = BattleMode.Matching;
            ClientNet.Get.m_ClientRoom.DoInit(m_Packet);

            await Task.Delay(1000);
            //ui.FadeOut();

            // 跳转场景
            System.Action action = () =>
            {
                UIManager.Get().PopAll();
                UIManager.Get().Push<UI_GameMenu>();

                Debug.Log("追帧模拟");
                ClientLogic.Get.IsStart = false; //重连
                for (int i = 1; i < packet.frameNumber; i++)
                {
                    S2C_InputPacket inputs = packet.inputs[i];
                    ClientLogic.Get.Process(inputs.frameNumber, inputs.inputs);
                }
                //ClientNet.Get.SendBattleStart(0);
                ClientNet.Get.SendBattleStart(2); //重连后恢复
            };
            GameManager.Get.LoadBattleAsync(action);
        }

        private void OnBattleEnd(INetSerializable reader)
        {
            Debug.Log("大厅接收游戏结束消息，测试正常结束时，是否多余执行");
            //ClientNet.Get.m_PlayerManager.LocalPlayer.ResetToLobby();
            //Debug.Log("断线重连进入大厅时，放弃重连，在这里接收分数变化");
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
            ClientNet.Get.m_ClientRoom.BattleMode = (BattleMode)packet.BattleMode;


            UIManager.Get().PopAll();
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