using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using LiteNetLib;
using LiteNetLib.Utils;
using Code.Shared;
using Code.Client;
using HotFix;

public class UI_Lobby : UIBase
{
    [SerializeField] Button m_ArcadeBtn;
    [SerializeField] Button m_MatchBtn;
    [SerializeField] Button m_TrainingBtn;
    [SerializeField] Button m_ReplayBtn;
    [SerializeField] Button m_SettingsBtn;
    [SerializeField] Button m_ExitBtn;
    private ClientPlayer localPlayer;

    void Awake()
    {
        m_ArcadeBtn = transform.Find("Menu/Arcade").GetComponent<Button>();
        m_MatchBtn = transform.Find("Menu/Match").GetComponent<Button>();
        m_TrainingBtn = transform.Find("Menu/Training").GetComponent<Button>();
        m_ReplayBtn = transform.Find("Menu/Replay").GetComponent<Button>();
        m_SettingsBtn = transform.Find("Menu/Settings").GetComponent<Button>();
        m_ExitBtn = transform.Find("Menu/Exit").GetComponent<Button>();

        m_ArcadeBtn.onClick.AddListener(OnArcadeButtonClick);
        m_MatchBtn.onClick.AddListener(RequestMatch);
        m_TrainingBtn.onClick.AddListener(OnTrainingButtonClick);
        m_ReplayBtn.onClick.AddListener(OnReplayButtonClick);
        m_SettingsBtn.onClick.AddListener(OnSettingsButtonClick);
        m_ExitBtn.onClick.AddListener(OnExitButtonClick);
    }

    void OnEnable()
    {
        EventManager.RegisterEvent(OnNetCallback);

        localPlayer = ClientNet.Get.m_PlayerManager.LocalPlayer;

        AudioManager.Get().PlayMusic(AudioManager.Paradise, true);
    }

    void OnDisable()
    {
        EventManager.UnRegisterEvent(OnNetCallback);

        AudioManager.Get()?.StopAll();
    }

    #region 网络消息

    public override void OnNetCallback(PacketType eventID, INetSerializable reader, NetPeer peer)
    {
        switch (eventID)
        {
            case PacketType.S2C_LogoutResult:
                OnLogoutResult(reader);
                break;
        }
    }

    private void OnLogoutResult(INetSerializable reader)
    {
        Debug.Log($"[UI] 收到登出消息");

        //UI跳转到登录，关闭本页面
        UIManager.Get().PopAll();
        UIManager.Get().Push<UI_Login>();
        //this.Pop();
    }

    #endregion

    #region 按钮事件

    void OnArcadeButtonClick()
    {
        var ui = UIManager.Get().Push<UI_Toast>();
        ui.Show("敬请期待");
    }

    void RequestMatch()
    {
        //if (localPlayer.Status != PlayerStatus.AtLobby)
        //{
        //    Debug.LogError($"此时不允匹配：{localPlayer.Status}");
        //    return;
        //}
        //Client.GetInstance().SendMatchRequest();
        //UIManager.Get().Push<UI_Matching>(2);
        //localPlayer.SetStatus(PlayerStatus.Matching);
    }

    void OnTrainingButtonClick()
    {
        var ui = UIManager.Get().Push<UI_Toast>();
        ui.Show("敬请期待");
    }

    void OnReplayButtonClick()
    {
        //UIManager.Get().Push<UI_Replay>();
    }

    void OnSettingsButtonClick()
    {
        //UIManager.Get().Push<UI_Settings>();
    }

    void OnExitButtonClick()
    {
        //Client.GetInstance().SendLogout();
        return;

        Debug.Log("Exit");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        //UnityEditor.EditorApplication.isPaused = true; //编辑器暂停
#else
        Application.Quit();
#endif
    }

    #endregion
}