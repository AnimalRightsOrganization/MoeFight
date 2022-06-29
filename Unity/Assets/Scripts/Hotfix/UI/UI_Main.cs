using LiteNetLib;
using UnityEngine;
using UnityEngine.UI;
using Code.Shared;

namespace Code.Client
{
    public class UI_Main : MonoBehaviour
    {
        private Button m_ConnectBtn;
        private Button m_TestX1Btn;
        private Button m_TestX2Btn;
        private Text m_InfoText;

        void Awake()
        {
            m_ConnectBtn = transform.Find("ConnectBtn").GetComponent<Button>();
            m_TestX1Btn = transform.Find("TestX1Btn").GetComponent<Button>();
            m_TestX2Btn = transform.Find("TestX2Btn").GetComponent<Button>();
            m_InfoText = transform.Find("PingText").GetComponent<Text>();

            m_ConnectBtn.onClick.AddListener(OnConnectClick);
            m_TestX1Btn.onClick.AddListener(OnTestX1Click);
            m_TestX2Btn.onClick.AddListener(OnTestX2Click);
        }

        void OnDisconnected(DisconnectInfo info)
        {
            m_InfoText.text = info.Reason.ToString();
            gameObject.SetActive(true);
        }

        void OnConnectClick()
        {
            ClientNet.Get.Connect(OnDisconnected);
        }

        void OnTestX1Click()
        {
            ClientNet.Get.SendTestPVE();

            gameObject.SetActive(false);
        }

        void OnTestX2Click()
        {
            ClientNet.Get.SendTestPVP();

            gameObject.SetActive(false);
        }

        public void Ping(int latency)
        {
            m_InfoText.text = $"Ping: {latency}ms";
        }
    }
}