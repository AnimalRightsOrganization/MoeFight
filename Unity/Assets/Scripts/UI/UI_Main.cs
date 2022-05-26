using LiteNetLib;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Client
{
    public class UI_Main : MonoBehaviour
    {
        public static UI_Main Instance;

        [SerializeField] private GameObject _uiObject;
        [SerializeField] private ClientNet _clientLogic;
        [SerializeField] private Text m_InfoText;

        void Awake()
        {
            Instance = this;
        }

        private void OnDisconnected(DisconnectInfo info)
        {
            m_InfoText.text = info.Reason.ToString();
            _uiObject.SetActive(true);
        }

        public void OnConnectClick()
        {
            _clientLogic.Connect(OnDisconnected);
        }

        public void OnLoginClick()
        {
            _clientLogic.SendLogin();
        }

        public void Ping(int latency)
        {
            m_InfoText.text = $"Ping: {latency}ms";
        }
    }
}