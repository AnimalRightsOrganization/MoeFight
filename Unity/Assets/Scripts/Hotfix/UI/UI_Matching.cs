using System;
using System.Timers;
using UnityEngine;
using UnityEngine.UI;
using Code.Client;
using Code.Shared;
using LiteNetLib.Utils;
using LiteNetLib;
using Timer = System.Timers.Timer;
using Debug = UnityEngine.Debug;

namespace HotFix
{
    public class UI_Matching : UIBase
    {
        public RectTransform m_Panel;
        public Image m_Rotate;
        public Button m_CancelBtn;

        public Text m_UsedTime; //已用时间：0:00
        public Text m_CancelText; //取消

        void Awake()
        {
            m_Panel = transform.Find("Panel").GetComponent<RectTransform>();
            m_Rotate = transform.Find("Panel/Rotate").GetComponent<Image>();
            m_CancelBtn = transform.Find("Panel/CancelBtn").GetComponent<Button>();

            m_CancelBtn.onClick.AddListener(ClientNet.Get.SendMatchCancel);

            m_Panel.anchoredPosition = Vector3.zero;


            m_UsedTime = transform.Find("Panel/UsedTime").GetComponent<Text>();
            m_CancelText = transform.Find("Panel/CancelBtn/Text").GetComponent<Text>();
        }

        void OnEnable()
        {
            ApplyLanguage();

            EventManager.RegisterEvent(OnNetCallback);

            SetTimer();
        }

        void OnDisable()
        {
            EventManager.UnRegisterEvent(OnNetCallback);

            aTimer?.Stop();
            aTimer?.Dispose();
        }

        void Update()
        {
            m_Rotate.transform.Rotate(Vector3.back, 5);
            m_UsedTime.text = $"已经用时：{deltaStr}";
        }

        public override void ApplyLanguage()
        {
            var config = ConfigManager.Get();

            m_UsedTime.text = config.GetWord(13);
            m_CancelText.text = config.GetWord(14);
        }

        #region 网络消息
        public override void OnNetCallback(PacketType eventID, INetSerializable reader, NetPeer peer)
        {
            switch (eventID)
            {
                case PacketType.S2C_MatchResult:
                    OnMatchResult(reader);
                    break;
            }
        }
        private void OnMatchResult(INetSerializable reader)
        {
            var packet = (S2C_MatchResultPacket)reader;
            Debug.Log($"[UI_Matching] {packet}");

            if (packet.Code == 0) //匹配成功
            {
                ClientPlayer localPlayer = ClientNet.Get.m_PlayerManager.LocalPlayer;
                bool localIsHost = localPlayer.PeerId == packet.Host.PeerId;

                //创建用户对象
                short rivalPlayerId = localIsHost ? packet.Guest.PeerId : packet.Host.PeerId;
                string rivalPlayerName = localIsHost ? packet.Guest.UserName : packet.Host.UserName;
                ClientPlayer rivalPlayer = new ClientPlayer(rivalPlayerName, rivalPlayerId);
                ClientNet.Get.m_PlayerManager.AddClientPlayer(rivalPlayer, false);

                int localSeatId = localIsHost ? 0 : 1;
                int rivalSeatId = localIsHost ? 1 : 0;
                localPlayer.SetRoomID(packet.RoomId).SetSeatID(localSeatId).SetStatus(PlayerStatus.AtRoomWait);
                rivalPlayer.SetRoomID(packet.RoomId).SetSeatID(rivalSeatId).SetStatus(PlayerStatus.AtRoomWait);

                this.Pop();
                UIManager.Get().Push<UI_RoleSelect>();
            }
            else if (packet.Code == 1) //匹配取消
            {
                //ClientPlayer localPlayer = ClientNet.Get.m_PlayerManager.LocalPlayer;
                //localPlayer.ResetToLobby();
                this.Pop();
            }
        }
        #endregion

        #region 匹配用时
        private Timer aTimer;
        private DateTime startTime;
        private string deltaStr;
        private void SetTimer()
        {
            // Create a timer with a one second interval.
            aTimer = new Timer(1000);
            // Hook up the Elapsed event for the timer. 
            aTimer.Elapsed += ATimer_Elapsed;
            aTimer.AutoReset = true;
            aTimer.Enabled = true;
            startTime = DateTime.Now;
        }
        private void ATimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            TimeSpan delta = e.SignalTime - startTime;
            deltaStr = delta.ToString(@"hh\:mm\:ss");
        }
        #endregion
    }
}