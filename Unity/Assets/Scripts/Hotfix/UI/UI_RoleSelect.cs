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
        [SerializeField] Button m_BackBtn;
        [SerializeField] Text[] m_Rolename;
        [SerializeField] Text[] m_Username;
        [SerializeField] Button[] m_ConfirmBtn;
        [SerializeField] GameObject[] m_ReadyText;
        [SerializeField] Transform[] m_Selectors;
        [SerializeField] Image[] m_HeadImages;
        [SerializeField] Button[] m_Charactors;

        private ClientPlayer localPlayer;
        private ClientPlayer rivalPlayer;

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

            m_ReadyText = new GameObject[2];
            m_ReadyText[0] = transform.Find("RoomPanel/P1_Ready").gameObject;
            m_ReadyText[1] = transform.Find("RoomPanel/P2_Ready").gameObject;

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
                m_Charactors[id].onClick.AddListener(() =>
                {
                    ClientNet.Get.SendSelection(id);
                });
            }
        }

        void OnEnable()
        {
            EventManager.RegisterEvent(OnNetCallback);

            localPlayer = ClientNet.Get.m_PlayerManager.LocalPlayer;
            rivalPlayer = ClientNet.Get.m_PlayerManager.RivalPlayer;
            bool localIsHost = localPlayer.SeatId == 0;
            m_Username[0].text = localIsHost ? localPlayer.UserName : rivalPlayer.UserName;
            m_Username[1].text = !localIsHost ? localPlayer.UserName : rivalPlayer.UserName;
            //Debug.Log($"我的座位{localPlayer.SeatId}");

            m_ConfirmBtn[0].gameObject.SetActive(localIsHost);
            m_ConfirmBtn[1].gameObject.SetActive(!localIsHost);
            m_ReadyText[0].SetActive(false);
            m_ReadyText[1].SetActive(false);

            for (int i = 0; i < 2; i++)
            {
                m_HeadImages[i].color = Color.white;
                m_HeadImages[i].sprite = m_Charactors[0].image.sprite;
                m_Selectors[i].SetParent(m_Charactors[0].transform);
                m_Selectors[i].localPosition = Vector3.zero;
            }

            //int length = ConfigManager.m_RoleConfig.Roles.Length;
            //int index1 = localPlayer.RoleIndex % length;
            //m_Rolename[0].text = ConfigManager.m_RoleConfig.Roles[index1].Name[0];
            //int index2 = rivalPlayer.RoleIndex % length;
            //m_Rolename[1].text = ConfigManager.m_RoleConfig.Roles[index2].Name[0];
        }

        void OnDisable()
        {
            EventManager.UnRegisterEvent(OnNetCallback);
        }

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

            //int length = ConfigManager.m_RoleConfig.Roles.Length;
            //int index = packet.RoleIndex % length;
            //m_Rolename[packet.SeatId].text = ConfigManager.m_RoleConfig.Roles[index].Name[0];
        }

        private void OnMatchResult(INetSerializable reader)
        {
            var packet = (S2C_MatchResultPacket)reader;
            Debug.Log($"[UI_RoleSelect] {packet.ToString()}");

            if (packet.Code == 2) //匹配后退出
            {
                // 这里理论上只会收到2
                Debug.Log("玩家离开，返回大厅");
                //localPlayer.ResetToLobby();
                this.Pop();
            }
        }

        private void OnGameReady(INetSerializable reader)
        {
            var packet = (S2C_GameReadyPacket)reader;
            Debug.Log($"[C] 准备回调, 座位1:{(PlayerStatus)packet.HostStatus}, 座位2:{(PlayerStatus)packet.GuestStatus}");

            var hostStatus = (PlayerStatus)packet.HostStatus;
            var guestStatus = (PlayerStatus)packet.GuestStatus;

            if (hostStatus == PlayerStatus.AtRoomReady)
            {
                m_ConfirmBtn[0].gameObject.SetActive(false);
                m_ReadyText[0].SetActive(true);
            }
            if (guestStatus == PlayerStatus.AtRoomReady)
            {
                m_ConfirmBtn[1].gameObject.SetActive(false);
                m_ReadyText[1].SetActive(true);
            }
        }

        private void OnLoadScene(INetSerializable reader)
        {
            var packet = (S2C_LoadScenePacket)reader;
            ClientNet.Get.m_ClientRoom.DoInit(packet);
            Debug.Log($"[C] 比赛开始，跳转到比赛场景\n{packet}");

            // 先变化状态，让用户看到。再倒计时切换场景。
            m_ConfirmBtn[0].gameObject.SetActive(false);
            m_ReadyText[0].SetActive(true);
            m_ConfirmBtn[1].gameObject.SetActive(false);
            m_ReadyText[1].SetActive(true);

            System.Action action = () =>
            {
                UIManager.Get().PopAll();
                //UIManager.Get().Push<UI_GameMenu>();
                ClientNet.Get.SendBattleStart(0); //切换场景完成时发
            };
            //ConfigManager.Get().LoadScene("Game", 2, action);
        }

        #endregion

        void OnBackButtonClick()
        {
            ClientNet.Get.SendMatchQuit();
        }

        void OnSendReady()
        {
            ClientNet.Get.SendGameReady();
        }
    }
}