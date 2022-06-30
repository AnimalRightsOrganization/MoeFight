using UnityEngine;
using Code.Shared;

namespace HotFix
{
    public class UI_Connect : UIBase
    {
        public Transform m_Rotate;

        void Awake()
        {
            m_Rotate = transform.Find("Mask/Rotate");
        }

        void Update()
        {
            m_Rotate.Rotate(Vector3.back, 1);
        }

        void OnEnable()
        {
            NetEventManager.RegisterEvent(NetHandle);
        }

        void OnDisable()
        {
            NetEventManager.UnRegisterEvent(NetHandle);
        }

        void NetHandle(NetStatus status)
        {
            switch (status)
            {
                case NetStatus.Connected:
                    Debug.Log("连接成功");
                    this.Pop();
                    break;
                case NetStatus.Disconnected:
                    Debug.Log("连接失败");
                    this.Pop();
                    break;
            }
        }
    }
}