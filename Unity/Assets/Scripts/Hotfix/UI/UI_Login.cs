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
        public Text m_LoginText; //+登录
        public Text m_ToRegisterText; //+去注册

        public CanvasGroup m_RegisterPanel;
        public InputField m_RegUserNameField;
        public InputField m_RegPasswordField;
        public InputField m_RegPassword2Field;
        public Button m_RegisterBtn;
        public Button m_ToLoginBtn;
        public Text m_RegUserNamePlaceholder; //输入用户名
        public Text m_RegPasswordPlaceholder; //输入密码
        public Text m_RegPassword2Placeholder; //确认密码
        public Text m_RegisterText; //+注册
        public Text m_ToLoginText; //+去登录

        void Awake()
        {
            m_LoginPanel = transform.Find("LoginPanel").GetComponent<CanvasGroup>();
            m_UserNameField = transform.Find("LoginPanel/UserName").GetComponent<InputField>();
            m_PasswordField = transform.Find("LoginPanel/Password").GetComponent<InputField>();
            m_LoginBtn = transform.Find("LoginPanel/LoginBtn").GetComponent<Button>();
            m_ToRegisterBtn = transform.Find("LoginPanel/ToRegisterBtn").GetComponent<Button>();
            m_LoginBtn.onClick.AddListener(SendLogin);
            m_ToRegisterBtn.onClick.AddListener(ToRegister);
            m_UserNamePlaceholder = transform.Find("LoginPanel/UserName/Placeholder").GetComponent<Text>();
            m_PasswordPlaceholder = transform.Find("LoginPanel/Password/Placeholder").GetComponent<Text>();
            m_LoginText = transform.Find("LoginPanel/LoginBtn/Text").GetComponent<Text>();
            m_ToRegisterText = transform.Find("LoginPanel/ToRegisterBtn/Text").GetComponent<Text>();


            m_RegisterPanel = transform.Find("RegisterPanel").GetComponent<CanvasGroup>();
            m_RegUserNameField = transform.Find("RegisterPanel/UserName").GetComponent<InputField>();
            m_RegPasswordField = transform.Find("RegisterPanel/Password").GetComponent<InputField>();
            m_RegPassword2Field = transform.Find("RegisterPanel/Password2").GetComponent<InputField>();
            m_RegisterBtn = transform.Find("RegisterPanel/RegisterBtn").GetComponent<Button>();
            m_ToLoginBtn = transform.Find("RegisterPanel/ToLoginBtn").GetComponent<Button>();
            m_RegisterBtn.onClick.AddListener(SendRegister);
            m_ToLoginBtn.onClick.AddListener(ToLogin);
            m_RegUserNamePlaceholder = transform.Find("RegisterPanel/UserName/Placeholder").GetComponent<Text>();
            m_RegPasswordPlaceholder = transform.Find("RegisterPanel/Password/Placeholder").GetComponent<Text>();
            m_RegPassword2Placeholder = transform.Find("RegisterPanel/Password2/Placeholder").GetComponent<Text>();
            m_RegisterText = transform.Find("RegisterPanel/RegisterBtn/Text").GetComponent<Text>();
            m_ToLoginText = transform.Find("RegisterPanel/ToLoginBtn/Text").GetComponent<Text>();
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
            var config = ConfigManager.Get();

            m_UserNamePlaceholder.text = config.GetWord(0);
            m_PasswordPlaceholder.text = config.GetWord(1);
            m_LoginText.text = config.GetWord(2);
            m_ToRegisterText.text = config.GetWord(3);
            m_RegUserNamePlaceholder.text = config.GetWord(0);
            m_RegPasswordPlaceholder.text = config.GetWord(1);
            m_RegPassword2Placeholder.text = config.GetWord(4);
            m_RegisterText.text = config.GetWord(5);
            m_ToLoginText.text = config.GetWord(6);
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
            if (string.IsNullOrEmpty(m_RegUserNameField.text))
            {
                var ui = UIManager.Get().Push<UI_Toast>();
                ui.Show("请填写用户名");
                return;
            }
            if (string.IsNullOrEmpty(m_RegPasswordField.text))
            {
                var ui = UIManager.Get().Push<UI_Toast>();
                ui.Show("请填写密码");
                return;
            }
            if (!m_RegPassword2Field.text.Equals(m_RegPasswordField.text))
            {
                var ui = UIManager.Get().Push<UI_Toast>();
                ui.Show("两次密码不一致，请重新输入");
                return;
            }

            string UserName = m_RegUserNameField.text;
            string Password = m_RegPasswordField.text;
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