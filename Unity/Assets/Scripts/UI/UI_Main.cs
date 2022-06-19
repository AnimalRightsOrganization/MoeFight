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
        private Button m_ConnectBtn;
        private Button m_TestX1Btn;
        private Button m_TestX2Btn;
        private Button m_ReadyBtn;
        private Text m_InfoText;

        void Awake()
        {
            Instance = this;

            m_ConnectBtn = transform.Find("ConnectBtn").GetComponent<Button>();
            m_TestX1Btn = transform.Find("TestX1Btn").GetComponent<Button>();
            m_TestX2Btn = transform.Find("TestX2Btn").GetComponent<Button>();
            m_ReadyBtn = transform.Find("ReadyBtn").GetComponent<Button>();
            m_InfoText = transform.Find("PingText").GetComponent<Text>();

            m_ConnectBtn.onClick.AddListener(OnConnectClick);
            m_TestX1Btn.onClick.AddListener(OnTestX1Click);
            m_TestX2Btn.onClick.AddListener(OnTestX2Click);
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

        void OnTestX1Click()
        {
            System.Random rd = new System.Random();
            string _userName = System.Environment.MachineName + " " + rd.Next(100000);
            var cmd = new C2S_JoinPacket { UserName = _userName };
            _clientLogic.SendTestPVE(cmd);

            gameObject.SetActive(false);
        }

        void OnTestX2Click()
        {
            System.Random rd = new System.Random();
            string _userName = System.Environment.MachineName + " " + rd.Next(100000);
            var cmd = new C2S_JoinPacket { UserName = _userName };
            _clientLogic.SendTestPVP(cmd);

            gameObject.SetActive(false);
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