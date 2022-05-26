using LiteNetLib;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Client
{
    public class UiController : MonoBehaviour
    {
        [SerializeField] private GameObject _uiObject;
        [SerializeField] private ClientNet _clientLogic;
        [SerializeField] private Text _disconnectInfoField;

        private void OnDisconnected(DisconnectInfo info)
        {
            _uiObject.SetActive(true);
            _disconnectInfoField.text = info.Reason.ToString();
        }

        public void OnConnectClick()
        {
            _clientLogic.Connect(OnDisconnected);
        }

        public void OnLoginClick()
        {
            _clientLogic.SendLogin();
        }
    }
}