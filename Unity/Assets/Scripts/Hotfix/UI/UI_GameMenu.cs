using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Code.Shared;
using Code.Client;
using LiteNetLib;
using LiteNetLib.Utils;

namespace HotFix
{
    public class UI_GameMenu : UIBase
    {
        [Header("比赛准备")]
        [SerializeField] GameObject m_Wallpaper; //场景加载完回调后关闭
        [SerializeField] GameObject m_ReadyPanel;
        [SerializeField] Text m_StartText;
        [Header("比赛信息")]
        [SerializeField] Text m_TimeText; //训练中是无穷∞
        [SerializeField] GameObject m_HpPanel;
        [SerializeField] RectTransform[] LastHp;
        [SerializeField] RectTransform[] CurrentHp;
        [SerializeField] Image[] HeadImages;
        [Header("比赛菜单")]
        [SerializeField] Button m_MenuBtn;
        [SerializeField] GameObject m_MenuPanel;
        [SerializeField] Text m_DescText;
        [SerializeField] Button m_ContinueBtn;
        [SerializeField] Button m_SkillInfoBtn; //技能信息
        [SerializeField] Button m_QuitBtn;
        [Header("技能显示")]
        [SerializeField] GameObject m_SkillPanel;
        [SerializeField] Text[] m_SkillName;
        public Transform[] MoveList;
        [Header("比赛结束")]
        [SerializeField] GameObject m_ResultPanel;
        [SerializeField] Text m_ResultText;
        [SerializeField] Button m_BackBtn;

        [SerializeField] Sprite[] m_HeadArray;

        void Awake()
        {
            m_Wallpaper = transform.Find("Wallpaper").gameObject;
            m_ReadyPanel = transform.Find("ReadyPanel").gameObject;
            m_StartText = transform.Find("ReadyPanel/Text").GetComponent<Text>();

            m_TimeText = transform.Find("HpPanel/TimePanel/Text").GetComponent<Text>();

            m_HpPanel = transform.Find("HpPanel").gameObject;
            LastHp = new RectTransform[]
            {
                m_HpPanel.transform.Find("HP_P1/last").GetComponent<RectTransform>(),
                m_HpPanel.transform.Find("HP_P2/last").GetComponent<RectTransform>(),
            };
            CurrentHp = new RectTransform[]
            {
                m_HpPanel.transform.Find("HP_P1/current").GetComponent<RectTransform>(),
                m_HpPanel.transform.Find("HP_P2/current").GetComponent<RectTransform>(),
            };
            HeadImages = new Image[2]
            {
                m_HpPanel.transform.Find("Head_1/Image").GetComponent<Image>(),
                m_HpPanel.transform.Find("Head_2/Image").GetComponent<Image>(),
            };

            m_MenuBtn = transform.Find("MenuBtn").GetComponent<Button>();
            m_MenuPanel = transform.Find("MenuPanel").gameObject;
            m_DescText = transform.Find("MenuPanel/Panel/DescText").GetComponent<Text>();
            m_ContinueBtn = transform.Find("MenuPanel/Panel/ContinueBtn").GetComponent<Button>();
            m_SkillInfoBtn = transform.Find("MenuPanel/Panel/SkillInfoBtn").GetComponent<Button>();
            m_QuitBtn = transform.Find("MenuPanel/Panel/QuitBtn").GetComponent<Button>();
            m_MenuBtn.onClick.AddListener(OpenMenu);
            m_ContinueBtn.onClick.AddListener(OnResume);
            m_SkillInfoBtn.onClick.AddListener(OnSkillInfo);
            m_QuitBtn.onClick.AddListener(OnQuitBtnClick);

            m_SkillPanel = transform.Find("SkillPanel").gameObject;
            var Panel_P1 = transform.Find("SkillPanel/Panel_P1");
            var Panel_P2 = transform.Find("SkillPanel/Panel_P2");
            m_SkillName = new Text[]
            {
                Panel_P1.Find("MoveView/TextName").GetComponent<Text>(),
                Panel_P2.Find("MoveView/TextName").GetComponent<Text>(),
            };
            MoveList = new Transform[]
            {
                Panel_P1.Find("MoveList"),
                Panel_P2.Find("MoveList")
            };

            m_ResultPanel = transform.Find("ResultPanel").gameObject;
            m_ResultText = transform.Find("ResultPanel/Text").GetComponent<Text>();
            m_BackBtn = transform.Find("ResultPanel/BackBtn").GetComponent<Button>();
            m_BackBtn.onClick.AddListener(OnBackBtnClick);
        }

        void Reset()
        {
            // 恢复到初始状态
            m_Wallpaper.SetActive(false); //TODO: 临时关掉调试（记得删除）
            m_ReadyPanel.SetActive(false);
            m_MenuPanel.SetActive(false);
            //m_SkillPanel.SetActive(GameManager.Instance.IsShowUI);
            m_SkillPanel.SetActive(false);
            m_ResultPanel.SetActive(false);
            LastHp[0].sizeDelta = new Vector2(250, -10);
            LastHp[1].sizeDelta = new Vector2(250, -10);
            CurrentHp[0].sizeDelta = new Vector2(250, -10);
            CurrentHp[1].sizeDelta = new Vector2(250, -10);
        }

        void OnEnable()
        {
            EventManager.RegisterEvent(OnNetCallback);

            BindDelegete();

            // 会在跳转场景前就执行到
            Reset();

            var dic_heads = ResManager.LoadSprites("Sprites/Heads.jpg");
            var p1_name = (HitstunConstants.CharacterName)ClientNet.Get.m_ClientRoom.HostPlayer.RoleIndex;
            var p2_name = (HitstunConstants.CharacterName)ClientNet.Get.m_ClientRoom.GuestPlayer.RoleIndex;
            //Debug.Log($"GameMenu: {p1_name} vs {p2_name}");
            HeadImages[0].sprite = dic_heads[p1_name.ToString()];
            HeadImages[1].sprite = dic_heads[p2_name.ToString()];
        }

        void OnDisable()
        {
            EventManager.UnRegisterEvent(OnNetCallback);

            UnbindDelegete();
        }

        public static Transform GetParent(int index)
        {
            var ui = UIManager.Get().GetUI<UI_GameMenu>();
            return ui.MoveList[index];
        }

        #region 网络消息
        public override void OnNetCallback(PacketType eventID, INetSerializable reader, NetPeer peer)
        {
            switch (eventID)
            {
                case PacketType.S2C_BattleStart:
                    OnBattleStart(reader);
                    break;
                case PacketType.S2C_BattlePause:
                    OnBattlePause(reader);
                    break;
                case PacketType.S2C_BattleLostNet: //对方掉线，[UI]显示倒计时
                    OnRivalLostNet(reader);
                    break;
                case PacketType.S2C_BattleEnd: //断线/主动认输/游戏结果上报
                    OnBattleEnd(reader);
                    break;
            }
        }

        private void OnBattleStart(INetSerializable reader)
        {
            var packet = (S2C_BattleStartPacket)reader;
            Debug.Log($"[GameMenu] 战斗开始, 阶段: {packet.Stage}");

            if (packet.Stage == 0) //场景加载完同步
            {
                OnCountdown();
            }
            else if (packet.Stage == 1) //倒计时完同步
            {
                //ClientLogic开始
            }
            else if (packet.Stage == 2) //暂停恢复
            {
                m_MenuPanel.SetActive(false);
            }
        }

        private void OnBattlePause(INetSerializable reader)
        {
            var packet = (S2C_BattlePausePacket)reader;
            Debug.Log($"<color=red>[S] {packet.SeatID}提出暂停: {packet.Duration}s</color>");
            if (packet.Duration > 0)
            {
                m_MenuPanel.SetActive(true);

                if (packet.SeatID == ClientNet.Get.m_PlayerManager.LocalPlayer.SeatId)
                {
                    m_DescText.text = "我方暂停中";
                    m_ContinueBtn.interactable = true;
                }
                else
                {
                    // 不是你申请的暂停，不允许点恢复
                    m_DescText.text = "对方申请暂停，请耐心等待";
                    m_ContinueBtn.interactable = false;
                }
            }
            else
            {
                if (packet.SeatID == ClientNet.Get.m_PlayerManager.LocalPlayer.SeatId)
                {
                    var toast = UIManager.Get().Push<UI_Toast>();
                    toast.Show("暂停次数用完");
                }
            }
        }

        private void OnRivalLostNet(INetSerializable reader)
        {
           //对方掉线。超过时间没有返回，将判对方落败。

            ClientLogic.Get.IsStart = false;
            BattleEvent.doSetAnimeSpeed?.Invoke(0);

            m_MenuPanel.SetActive(true);
            m_DescText.text = "对方重连中，请耐心等待";
            m_ContinueBtn.interactable = false;
        }

        private void OnBattleEnd(INetSerializable reader)
        {
            var packet = (S2C_BattleEndPacket)reader;
            Debug.Log($"[UI] 收到游戏结束，获胜者是座位#{packet.WinnerSeatId}");

            m_MenuPanel.SetActive(false);
            m_ResultPanel.gameObject.SetActive(true);

            if (packet.WinnerSeatId == ClientNet.Get.m_PlayerManager.LocalPlayer.SeatId)
            {
                m_ResultText.text = "YOU WIN";
            }
            else if (packet.WinnerSeatId == ClientNet.Get.m_PlayerManager.RivalPlayer?.SeatId)
            {
                m_ResultText.text = "Game Over";
            }
            else
            {
                m_ResultText.text = "Time Out";
            }
        }
        #endregion

        #region 按钮事件
        public void ShowMenu()
        {
            ClientLogic.Get.IsStart = false;
            BattleEvent.doSetAnimeSpeed?.Invoke(0); //重连回来先暂停
            m_MenuPanel.SetActive(true);
            m_DescText.text = "我方暂停中";
            m_ContinueBtn.interactable = true;
        }

        // 3,2,1,开始比赛
        void OnCountdown()
        {
            // 第1秒
            Tweener tw3 = m_StartText.DOText("Round1", 0); //duration是渐变，一个字一个字变过来
            tw3.Pause();
            tw3.SetDelay(1);
            tw3.OnStart(() =>
            {
                m_Wallpaper.SetActive(false);
                m_ReadyPanel.SetActive(true);

                AudioManager.Get().PlaySound(AudioManager.Round_1);
            });
            tw3.Play();

            // 第3秒
            Tweener tw0 = m_StartText.DOText("Ready", 0);
            tw0.Pause();
            tw0.SetDelay(3);
            tw0.Play();

            // 第4秒
            Tweener tw_start = m_StartText.DOText("Fight", 0);
            tw_start.Pause();
            tw_start.SetDelay(4f);
            tw_start.Play();

            // 第5秒，发送第一帧同步，消失
            Tweener tw_end = m_StartText.DOText("Fight", 0);
            tw_end.Pause();
            tw_end.SetDelay(5f);
            tw_end.OnComplete(() =>
            {
                ClientNet.Get.SendBattleStart(1); //倒计时完发
                m_ReadyPanel.SetActive(false);
            });
            tw_end.Play();
        }

        // 暂停比赛
        void OpenMenu()
        {
            switch (ClientNet.Get.m_ClientRoom.BattleMode)
            {
                case BattleMode.Matching:
                    Debug.Log($"[C] 请求暂停");
                    ClientNet.Get.SendBattlePause();
                    break;
                default: //其他情况不会有UI
                    m_MenuPanel.SetActive(true);
                    m_DescText.text = "暂停";
                    m_ContinueBtn.interactable = true;
                    ClientLogic.Get.IsStart = false;
                    BattleEvent.doSetAnimeSpeed?.Invoke(0);
                    break;
            }
        }

        // 恢复比赛
        void OnResume()
        {
            switch (ClientNet.Get.m_ClientRoom.BattleMode)
            {
                case BattleMode.Matching:
                    ClientNet.Get.SendBattleStart(2); //解除暂停，继续
                    break;
                default: //其他情况不会有UI
                    m_MenuPanel.SetActive(false);
                    ClientLogic.Get.IsStart = true;
                    BattleEvent.doSetAnimeSpeed?.Invoke(1);
                    break;
            }
        }

        // 设置键位
        void OnSkillInfo()
        {
            Debug.Log("查看搓招信息");
        }

        // 退出比赛？（一级菜单）
        void OnQuitBtnClick()
        {
            m_MenuPanel.SetActive(false);

            string titleStr = string.Empty;
            string noStr = "取消";
            string yesStr = "确定";
            var dialog = UIManager.Get().Push<UI_Dialog>();
            System.Action noAction = dialog.Hide;
            System.Action yesAction = null;
            switch (ClientNet.Get.m_ClientRoom.BattleMode)
            {
                case BattleMode.Matching:
                    titleStr = "退出游戏将判定失败，是否继续？";
                    yesAction = () =>
                    {
                        ClientNet.Get.SendBattleQuit();
                        dialog.Hide();
                    };
                    break;
                default:
                    titleStr = "确定退出？";
                    noAction = () =>
                    {
                        dialog.Hide();
                        m_MenuPanel.SetActive(true);
                    };
                    yesAction = () =>
                    {
                        ClientNet.Get.m_PlayerManager.LocalPlayer.ResetToLobby();
                        ClientNet.Get.m_PlayerManager.RemoveRival();
                        OnBackBtnClick();
                    };
                    break;
            }
            dialog.Show(titleStr, noAction, noStr, yesAction, yesStr);
        }

        // 结算/退出比赛：是（二级菜单）
        void OnBackBtnClick()
        {
            GameManager.Get.CleanBattle();

            UIManager.Get().PopAll();
            UIManager.Get().Push<UI_Lobby>();

            ClientNet.Get.m_ClientRoom.Dispose();
            ClientNet.Get.m_ClientRoom = null;
        }
        #endregion

        #region 委托
        void BindDelegete()
        {
            BattleEvent.doShowSkillText = ShowSkill;
            BattleEvent.doSetTimeText = SetTime;
            BattleEvent.doSetCurrentHp = SetCurrentHp;
            BattleEvent.doSetGameEnd = SetGameEnd;
        }
        void UnbindDelegete()
        {
            BattleEvent.doShowSkillText = null;
            BattleEvent.doSetTimeText = null;
            BattleEvent.doSetCurrentHp = null;
            BattleEvent.doSetGameEnd = null;
            BattleEvent.doReplayUpdate = null;
            BattleEvent.doSetAnimeSpeed = null;
        }
        void ShowSkill(int pid, string content)
        {
            m_SkillName[pid - 1].text = content;
        }
        void SetTime(string second)
        {
            m_TimeText.text = second;
        }
        void SetCurrentHp(int pid, int hp)
        {
            CurrentHp[pid - 1].sizeDelta = new Vector2(hp * 0.25f, -10);

            Tweener tw = LastHp[pid - 1].DOSizeDelta(CurrentHp[pid - 1].sizeDelta, 1); //duration是渐变，一个字一个字变过来
            tw.SetDelay(0.5f);
            tw.Play();
        }
        void SetGameEnd(int winner)
        {
            BattleEvent.doSetGameEnd = null;
            switch (ClientNet.Get.m_ClientRoom.BattleMode)
            {
                case BattleMode.Training:
                case BattleMode.Matching:
                    Debug.Log($"delegate gameEnd: {winner}");
                    ClientNet.Get.SendBattleEnd(winner);
                    break;
            }
        }
        #endregion
    }
}