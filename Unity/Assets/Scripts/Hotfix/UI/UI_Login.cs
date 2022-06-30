using UnityEngine;
using UnityEngine.UI;
using LiteNetLib;
using LiteNetLib.Utils;
using Code.Shared;
using Code.Client;
using DG.Tweening;

namespace HotFix
{
    public class UI_Login : UIBase
    {
        public CanvasGroup m_LoginPanel;
        public InputField m_UserNameField;
        public InputField m_PasswordField;
        public Button m_LoginBtn;
        public Button m_ToRegisterBtn;
        public Text m_UserNamePlaceholder; //输入用户名
        public Text m_PasswordPlaceholder; //输入密码
        public Text m_LoginBtnText; //+登录
        public Text m_ToRegisterBtnText; //+去注册

        public CanvasGroup m_RegisterPanel;
        public InputField m_regUserNameField;
        public InputField m_regPasswordField;
        public InputField m_regPassword2Field;
        public Button m_RegisterBtn;
        public Button m_ToLoginBtn;
        public Text m_RegUserNamePlaceholder; //输入用户名
        public Text m_RegPasswordPlaceholder; //输入密码
        public Text m_RegPassword2Placeholder; //确认密码
        public Text m_RegisterBtnText; //+注册
        public Text m_ToLoginBtnText; //+去登录

        void Awake()
        {
            m_LoginPanel = transform.Find("LoginPanel").GetComponent<CanvasGroup>();
            m_UserNameField = transform.Find("LoginPanel/UserName").GetComponent<InputField>();
            m_PasswordField = transform.Find("LoginPanel/Password").GetComponent<InputField>();
            m_LoginBtn = transform.Find("LoginPanel/LoginBtn").GetComponent<Button>();
            m_LoginBtn.onClick.AddListener(SendLogin);
            m_ToRegisterBtn = transform.Find("LoginPanel/ToRegisterBtn").GetComponent<Button>();
            m_ToRegisterBtn.onClick.AddListener(ToRegister);

            m_RegisterPanel = transform.Find("RegisterPanel").GetComponent<CanvasGroup>();
            m_regUserNameField = transform.Find("RegisterPanel/UserName").GetComponent<InputField>();
            m_regPasswordField = transform.Find("RegisterPanel/Password").GetComponent<InputField>();
            m_regPassword2Field = transform.Find("RegisterPanel/Password2").GetComponent<InputField>();
            m_RegisterBtn = transform.Find("RegisterPanel/RegisterBtn").GetComponent<Button>();
            m_RegisterBtn.onClick.AddListener(SendRegister);
            m_ToLoginBtn = transform.Find("RegisterPanel/ToLoginBtn").GetComponent<Button>();
            m_ToLoginBtn.onClick.AddListener(ToLogin);
        }

        void OnEnable()
        {
            ApplyLanguage();

            EventManager.RegisterEvent(OnNetCallback);

            ToLogin();

            ClientNet.Get.Connect(null);
        }

        void OnDisable()
        {
            EventManager.UnRegisterEvent(OnNetCallback);
        }

        public override void ApplyLanguage()
        {
            m_UserNamePlaceholder.text = "输入用户名";
            m_PasswordPlaceholder.text = "输入密码";
            m_LoginBtnText.text = "+登录";
            m_ToRegisterBtnText.text = "+去注册";

            m_RegUserNamePlaceholder.text = "输入用户名";
            m_RegPasswordPlaceholder.text = "输入密码";
            m_RegPassword2Placeholder.text = "确认密码";
            m_RegisterBtnText.text = "+注册";
            m_ToLoginBtnText.text = "+去登录";
        }

        #region 网络消息

        public override void OnNetCallback(PacketType eventID, INetSerializable reader, NetPeer peer)
        {
            switch (eventID)
            {
                case PacketType.S2C_LoginResult:
                    OnLoginResult(reader);
                    break;
            }
        }

        public void OnLoginResult(INetSerializable reader)
        {
            var packet = (S2C_LoginResultPacket)reader;
            switch (packet.Code)
            {
                case 0:
                    {
                        // 创建用户对象
                        var clientPlayer = new ClientPlayer(packet.UserName, packet.PeerId);
                        ClientNet.Get.m_PlayerManager.AddClientPlayer(clientPlayer, true);

                        // 弹出大厅页，关闭本页面
                        UIManager.Get().Push<UI_Lobby>();
                        this.Pop();
                    }
                    break;
                default:
                    {
                        var toast = UIManager.Get().Push<UI_Toast>();
                        toast.Show("登录失败");
                    }
                    break;
            }
        }

        #endregion

        private void SendLogin()
        {
            string UserName = m_UserNameField.text;
            string Password = m_PasswordField.text;
            try
            {
                ClientNet.Get.SendLogin(UserName, Password);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"抛出的错误：{e}");
            }
        }
        private void SendRegister()
        {
            if (string.IsNullOrEmpty(m_regUserNameField.text))
            {
                var ui = UIManager.Get().Push<UI_Toast>();
                ui.Show("请填写用户名");
                return;
            }
            if (string.IsNullOrEmpty(m_regPasswordField.text))
            {
                var ui = UIManager.Get().Push<UI_Toast>();
                ui.Show("请填写密码");
                return;
            }
            if (!m_regPassword2Field.text.Equals(m_regPasswordField.text))
            {
                var ui = UIManager.Get().Push<UI_Toast>();
                ui.Show("两次密码不一致，请重新输入");
                return;
            }

            string UserName = m_regUserNameField.text;
            string Password = m_regPasswordField.text;
            try
            {
                //ClientNet.Get.SendRegister(UserName, Password);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"抛出的错误：{e}");
            }
        }

        private void ToRegister()
        {
            Tweener tw1 = m_LoginPanel.DOFade(0, 0.3f);
            tw1.Play();
            tw1.OnComplete(() =>
            {
                m_LoginPanel.interactable = false;
                m_LoginPanel.blocksRaycasts = false;

                Tweener tw2 = m_RegisterPanel.DOFade(1, 0.3f);
                tw2.Play();
                tw2.OnComplete(() =>
                {
                    m_RegisterPanel.interactable = true;
                    m_RegisterPanel.blocksRaycasts = true;
                });
            });
        }
        private void ToLogin()
        {
            Tweener tw1 = m_RegisterPanel.DOFade(0, 0.3f);
            tw1.Play();
            tw1.OnComplete(() =>
            {
                m_RegisterPanel.interactable = false;
                m_RegisterPanel.blocksRaycasts = false;

                Tweener tw2 = m_LoginPanel.DOFade(1, 0.3f);
                tw2.Play();
                tw2.OnComplete(() =>
                {
                    m_LoginPanel.interactable = true;
                    m_LoginPanel.blocksRaycasts = true;
                });
            });
        }
    }
}