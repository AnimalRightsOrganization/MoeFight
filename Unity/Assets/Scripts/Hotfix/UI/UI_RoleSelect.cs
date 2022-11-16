using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Code.Client;
using Code.Shared;
using LiteNetLib;
using LiteNetLib.Utils;

namespace HotFix
{
    public class UI_RoleSelect : UIBase
    {
        public Button m_BackBtn;
        public Text[] m_Rolename;
        public Text[] m_Username;
        public Button[] m_ConfirmBtn;
        public GameObject[] m_ReadyObj;
        public Transform[] m_Selectors;
        public Image[] m_HeadImages;
        public Button[] m_Charactors;

        public Text m_BackText;
        public Text[] m_ConfirmText;
        public Text[] m_ReadyText;

        ClientPlayer localPlayer;
        ClientPlayer rivalPlayer;

        #region 内置方法
        void Awake()
        {
            m_BackBtn = transform.Find("Background/BackBtn").GetComponent<Button>();
            m_BackBtn.onClick.AddListener(OnBackButtonClick);

            m_Rolename = new Text[2];
            m_Rolename[0] = transform.Find("RoomPanel/Role1_Name").GetComponent<Text>();
            m_Rolename[1] = transform.Find("RoomPanel/Role2_Name").GetComponent<Text>();

            m_Username = new Text[2];
            m_Username[0] = transform.Find("RoomPanel/P1_Name").GetComponent<Text>();
            m_Username[1] = transform.Find("RoomPanel/P2_Name").GetComponent<Text>();

            m_ConfirmBtn = new Button[2];
            m_ConfirmBtn[0] = transform.Find("RoomPanel/P1_Confirm").GetComponent<Button>();
            m_ConfirmBtn[1] = transform.Find("RoomPanel/P2_Confirm").GetComponent<Button>();
            m_ConfirmBtn[0].onClick.AddListener(OnSendReady);
            m_ConfirmBtn[1].onClick.AddListener(OnSendReady);

            m_ReadyObj = new GameObject[2];
            m_ReadyObj[0] = transform.Find("RoomPanel/P1_Ready").gameObject;
            m_ReadyObj[1] = transform.Find("RoomPanel/P2_Ready").gameObject;

            var headPanel = transform.Find("HeadPanel");
            m_HeadImages = new Image[headPanel.childCount];
            for (int i = 0; i < headPanel.childCount; i++)
            {
                m_HeadImages[i] = headPanel.GetChild(i).GetComponent<Image>();
            }

            var selectPanel = transform.Find("SelectPanel");
            var select_1 = transform.Find("SelectPanel/1");
            m_Selectors = new Transform[select_1.childCount];
            for (int i = 0; i < select_1.childCount; i++)
            {
                m_Selectors[i] = select_1.GetChild(i);
            }
            m_Charactors = new Button[selectPanel.childCount];
            for (int i = 0; i < selectPanel.childCount; i++)
            {
                int id = i;
                var charactor = selectPanel.GetChild(id);
                m_Charactors[id] = charactor.GetComponent<Button>();
                m_Charactors[id].onClick.AddListener(() => OnSendSelection(id));
            }

            m_BackText = transform.Find("Background/BackBtn/Text").GetComponent<Text>();
        }

        void OnEnable()
        {
            ApplyLanguage();

            EventManager.RegisterEvent(OnNetCallback);

            InitUI();
        }

        void OnDisable()
        {
            EventManager.UnRegisterEvent(OnNetCallback);
        }

        public override void ApplyLanguage()
        {
            var config = ConfigManager.Get();

            m_BackText.text = config.GetWord(15);
        }
        #endregion

        #region 网络消息
        public override void OnNetCallback(PacketType eventID, INetSerializable reader, NetPeer peer)
        {
            switch (eventID)
            {
                case PacketType.S2C_RoleSelect:
                    OnRoleSelect(reader);
                    break;
                case PacketType.S2C_MatchResult:
                    OnMatchResult(reader);
                    break;
                case PacketType.S2C_GameReady:
                    OnGameReady(reader);
                    break;
                case PacketType.S2C_LoadScene:
                    OnLoadScene(reader);
                    break;
                case PacketType.S2C_TestPVE:
                    OnTestPVE(reader);
                    break;
            }
        }

        private void OnRoleSelect(INetSerializable reader)
        {
            var packet = (S2C_RoleSelectPacket)reader;
            Debug.Log($"[C] 座位{packet.SeatId}，选择角色{packet.RoleIndex}");

            m_HeadImages[packet.SeatId].color = Color.white;
            m_HeadImages[packet.SeatId].sprite = m_Charactors[packet.RoleIndex].image.sprite;
            m_Selectors[packet.SeatId].SetParent(m_Charactors[packet.RoleIndex].transform);
            m_Selectors[packet.SeatId].localPosition = Vector3.zero;

            var roleArray = ConfigManager.Get().m_CharacterList;
            int length = roleArray.Length;
            int index = packet.RoleIndex % length;
            m_Rolename[packet.SeatId].text = roleArray[index].Name;
        }

        private void OnMatchResult(INetSerializable reader)
        {
            var packet = (S2C_MatchResultPacket)reader;
            //情况0, 1在UI_Matching
            if (packet.Code == 2) //匹配后退出
            {
                this.Pop();
            }
        }

        private void OnGameReady(INetSerializable reader)
        {
            var packet = (S2C_GameReadyPacket)reader;
            Debug.Log($"[C] OnGameReady, 座位1:{(PlayerStatus)packet.HostStatus}, 座位2:{(PlayerStatus)packet.GuestStatus}");

            var hostStatus = (PlayerStatus)packet.HostStatus;
            var guestStatus = (PlayerStatus)packet.GuestStatus;

            if (hostStatus == PlayerStatus.AtRoomReady)
            {
                m_ConfirmBtn[0].gameObject.SetActive(false);
                m_ReadyObj[0].SetActive(true);
            }
            if (guestStatus == PlayerStatus.AtRoomReady)
            {
                m_ConfirmBtn[1].gameObject.SetActive(false);
                m_ReadyObj[1].SetActive(true);
            }
        }

        private async void OnLoadScene(INetSerializable reader)
        {
            var packet = (S2C_LoadScenePacket)reader;
            ClientNet.Get.m_ClientRoom.DoInit(packet);
            Debug.Log($"[C] 比赛开始，跳转到比赛场景\n{packet}");

            // 给足动画时间
            m_ConfirmBtn[0].gameObject.SetActive(false);
            m_ReadyObj[0].SetActive(true);
            m_ConfirmBtn[1].gameObject.SetActive(false);
            m_ReadyObj[1].SetActive(true);
            await Task.Delay(1000);

            var ui_versus = UIManager.Get().Push<UI_Versus>();
            ui_versus.FadeIn();
            await Task.Delay(1000);

            //ui_versus.FadeOut();
            //await Task.Delay(1000);

            // 跳转场景
            System.Action action = () =>
            {
                UIManager.Get().PopAll();
                UIManager.Get().Push<UI_GameMenu>();
                ClientNet.Get.SendBattleStart(0); //切换场景完成时发
            };
            GameManager.Get.LoadBattleAsync(action); //匹配赛
        }

        private async void OnTestPVE(INetSerializable reader)
        {
            var packet = (S2C_JoinResultPacket)reader;
            Debug.Log($"[S2C] 单人测试: code={packet.Code}, peerid={packet.HostId}, {packet.HostName}");

            var room = ClientNet.Get.m_ClientRoom;
            var pt = new S2C_LoadScenePacket
            {
                RoomId = (short)room.RoomID,
                BattleId = room.BattleID,
                MapId = room.MapId,
                Host = new PlayerLoadPacket { UserName = localPlayer.UserName, PeerId = localPlayer.PeerId, RoleIndex = localPlayer.RoleIndex },
                Guest = new PlayerLoadPacket { UserName = rivalPlayer.UserName, PeerId = rivalPlayer.PeerId, RoleIndex = rivalPlayer.RoleIndex },
            };
            room.DoInit(pt);

            // 给足动画时间
            m_ConfirmBtn[0].gameObject.SetActive(false);
            m_ReadyObj[0].SetActive(true);
            m_ConfirmBtn[1].gameObject.SetActive(false);
            m_ReadyObj[1].SetActive(true);
            await Task.Delay(1000);

            var ui_versus = UIManager.Get().Push<UI_Versus>();
            ui_versus.FadeIn();
            await Task.Delay(1000);

            //ui_versus.FadeOut();
            //await Task.Delay(1000);

            ///*
            // 跳转场景
            System.Action action = () =>
            {
                UIManager.Get().PopAll();
                UIManager.Get().Push<UI_GameMenu>();
                ClientLogic.Get.IsStart = true;
            };
            GameManager.Get.LoadBattleAsync(action); //训练
            //*/
        }
        #endregion

        void InitUI()
        {
            //①通过匹配进入
            //②通过训练进入
            localPlayer = ClientNet.Get.m_PlayerManager.LocalPlayer;
            rivalPlayer = ClientNet.Get.m_PlayerManager.RivalPlayer;
            bool localIsHost = localPlayer.SeatId == 0;
            m_Username[0].text = localIsHost ? localPlayer.UserName : rivalPlayer.UserName;
            m_Username[1].text = !localIsHost ? localPlayer.UserName : rivalPlayer.UserName;
            //Debug.Log($"我的座位{localPlayer.SeatId}");

            m_ConfirmBtn[0].gameObject.SetActive(localIsHost);
            m_ConfirmBtn[1].gameObject.SetActive(!localIsHost);
            m_ReadyObj[0].SetActive(false);
            m_ReadyObj[1].SetActive(false);

            for (int i = 0; i < 2; i++)
            {
                m_HeadImages[i].color = Color.white;
                m_HeadImages[i].sprite = m_Charactors[0].image.sprite;
                m_Selectors[i].SetParent(m_Charactors[0].transform);
                m_Selectors[i].localPosition = Vector3.zero;
            }

            var roleArray = ConfigManager.Get().m_CharacterList;
            int length = roleArray.Length;
            int index1 = localPlayer.RoleIndex % length;
            m_Rolename[0].text = roleArray[index1].Name;
            int index2 = rivalPlayer.RoleIndex % length;
            m_Rolename[1].text = roleArray[index2].Name;
        }

        void OnBackButtonClick()
        {
            switch (ClientNet.Get.m_ClientRoom.BattleMode)
            {
                case BattleMode.Training:
                    this.Pop();
                    localPlayer.ResetToLobby();
                    break;
                case BattleMode.Matching:
                    ClientNet.Get.SendMatchQuit();
                    break;
            }
            UIManager.Get().Push<UI_Lobby>();
        }

        void OnSendSelection(int id)
        {
            switch (ClientNet.Get.m_ClientRoom.BattleMode)
            {
                case BattleMode.Editor:
                case BattleMode.Training:
                    var packet = new S2C_RoleSelectPacket { SeatId = (byte)0, RoleIndex = (byte)id };
                    localPlayer.RoleIndex = packet.RoleIndex;
                    OnRoleSelect(packet);
                    break;
                case BattleMode.Matching:
                    ClientNet.Get.SendSelection(id);
                    break;
                default:
                    Debug.Log($"未实现的模式: {ClientNet.Get.m_ClientRoom.BattleMode}");
                    break;
            }
        }

        void OnSendReady()
        {
            switch (ClientNet.Get.m_ClientRoom.BattleMode)
            {
                case BattleMode.Editor:
                case BattleMode.Training:
                    ClientNet.Get.SendTestPVE();
                    break;
                case BattleMode.Matching:
                    ClientNet.Get.SendGameReady();
                    break;
                default:
                    Debug.Log($"未实现的模式: {ClientNet.Get.m_ClientRoom.BattleMode}");
                    break;
            }
        }
    }
}