using LiteNetLib;
using UnityEngine;
using UnityEngine.UI;
using Code.Shared;

namespace Code.Client
{
    public class UI_Main : MonoBehaviour
    {
        public static UI_Main Instance;

        public ClientNet _clientLogic;
        public Button m_ConnectBtn;
        public Button m_LoginBtn;
        public Button m_ReadyBtn;
        public Text m_InfoText;

        void Awake()
        {
            Instance = this;

            m_ConnectBtn = transform.Find("ConnectBtn").GetComponent<Button>();
            m_LoginBtn = transform.Find("LoginBtn").GetComponent<Button>();
            m_ReadyBtn = transform.Find("ReadyBtn").GetComponent<Button>();
            m_InfoText = transform.Find("PingText").GetComponent<Text>();

            m_ConnectBtn.onClick.AddListener(OnConnectClick);
            m_LoginBtn.onClick.AddListener(OnLoginClick);
            m_ReadyBtn.onClick.AddListener(OnReadyClick);
        }

        void OnDisconnected(DisconnectInfo info)
        {
            m_InfoText.text = info.Reason.ToString();
            gameObject.SetActive(true);
        }

        void OnConnectClick()
        {
            _clientLogic.Connect(OnDisconnected);
        }

        void OnLoginClick()
        {
            System.Random rd = new System.Random();
            string _userName = System.Environment.MachineName + " " + rd.Next(100000);
            var cmd = new C2S_LoginPacket { UserName = _userName };
            _clientLogic.SendLogin(cmd);
        }

        void OnReadyClick()
        {
            var cmd = new EmptyPacket();
            _clientLogic.SendReady(cmd);
        }

        public void Ping(int latency)
        {
            m_InfoText.text = $"Ping: {latency}ms";
        }
    }
}