using UnityEngine;
using UnityEngine.UI;
using Code.Shared;
using Code.Client;
using LiteNetLib;
using LiteNetLib.Utils;
using DG.Tweening;

namespace HotFix
{
    public class UI_Settings : UIBase
    {
        //[SerializeField] Text m_TitleText;
        [SerializeField] Button m_BackBtn;
        [SerializeField] Text m_BackText;
        [SerializeField] Button m_ToCommonBtn;
        [SerializeField] Button m_ToInputBtn;

        [SerializeField] Transform m_CommonPanel;
        [SerializeField] Text m_ScreenSizeName;
        [SerializeField] Dropdown m_ScreenSizeDrog;
        [SerializeField] Text m_ScreenSizeValue;
        [SerializeField] Text m_FullScreenName;
        [SerializeField] Toggle m_FullScreenTog;
        [SerializeField] Text m_FullScreenValue;
        [SerializeField] Text m_MusicName;
        [SerializeField] Slider m_MusicSlider;
        [SerializeField] Text m_MusicValue;
        [SerializeField] Text m_SoundName;
        [SerializeField] Slider m_SoundSlider;
        [SerializeField] Text m_SoundValue;
        [SerializeField] Text m_LanguageName;
        [SerializeField] Dropdown m_LanguageDrog;
        [SerializeField] Text m_LanguageValue;
        [SerializeField] Button m_ResetToDefault;

        [SerializeField] Transform m_InputPanel;

        private Settings lastSettings;

        void Awake()
        {
            //m_TitleText = transform.Find("Top/Title").GetComponent<Text>();
            m_BackBtn = transform.Find("Top/BackBtn").GetComponent<Button>();
            m_BackBtn.onClick.AddListener(OnBackButtonClick);
            m_BackText = transform.Find("Top/BackBtn/Text").GetComponent<Text>();
            m_ToCommonBtn = transform.Find("Top/ToCommonBtn").GetComponent<Button>();
            m_ToCommonBtn.onClick.AddListener(SwitchToCommon);
            m_ToInputBtn = transform.Find("Top/ToInputBtn").GetComponent<Button>();
            m_ToInputBtn.onClick.AddListener(SwitchToInput);

            m_CommonPanel = transform.Find("Common");

            m_ScreenSizeName = transform.Find("Common/ScreenSize/Name").GetComponent<Text>();
            m_ScreenSizeDrog = transform.Find("Common/ScreenSize/Dropdown").GetComponent<Dropdown>();
            m_ScreenSizeDrog.options = new System.Collections.Generic.List<Dropdown.OptionData>();
            for (int i = 0; i < ScreenSizeOptions.Length; i++)
            {
                var option = ScreenSizeOptions[i];
                var data = new Dropdown.OptionData($"{option[0]}x{option[1]}");
                m_ScreenSizeDrog.options.Add(data);
            }
            m_ScreenSizeDrog.onValueChanged.AddListener(OnScreenSizeChanged);
            m_ScreenSizeValue = transform.Find("Common/ScreenSize/Value").GetComponent<Text>();

            m_FullScreenName = transform.Find("Common/FullScreen/Name").GetComponent<Text>();
            m_FullScreenTog = transform.Find("Common/FullScreen/Toggle").GetComponent<Toggle>();
            m_FullScreenTog.onValueChanged.AddListener(OnFullScreenChanged);
            m_FullScreenValue = transform.Find("Common/FullScreen/Value").GetComponent<Text>();

            m_MusicName = transform.Find("Common/MusicVolume/Name").GetComponent<Text>();
            m_MusicSlider = transform.Find("Common/MusicVolume/Slider").GetComponent<Slider>();
            m_MusicSlider.onValueChanged.AddListener(OnMusicChanged);
            m_MusicValue = transform.Find("Common/MusicVolume/Value").GetComponent<Text>();

            m_SoundName = transform.Find("Common/SoundVolume/Name").GetComponent<Text>();
            m_SoundSlider = transform.Find("Common/SoundVolume/Slider").GetComponent<Slider>();
            m_SoundSlider.onValueChanged.AddListener(OnSoundChanged);
            m_SoundValue = transform.Find("Common/SoundVolume/Value").GetComponent<Text>();

            m_LanguageName = transform.Find("Common/Language/Name").GetComponent<Text>();
            m_LanguageDrog = transform.Find("Common/Language/Dropdown").GetComponent<Dropdown>();
            m_LanguageDrog.options = new System.Collections.Generic.List<Dropdown.OptionData>();
            for (int i = 0; i < LanguageOptions.Length; i++)
            {
                string option = LanguageOptions[i];
                var data = new Dropdown.OptionData(option);
                m_LanguageDrog.options.Add(data);
            }
            m_LanguageDrog.onValueChanged.AddListener(OnLanguageChanged);
            m_LanguageValue = transform.Find("Common/Language/Value").GetComponent<Text>();

            //m_ResetToDefault = transform.Find("Common/ResetToDefault/Button").GetComponent<Button>();
            //m_ResetToDefault.onClick.AddListener(OnResetToDefault);

            m_InputPanel = transform.Find("Input");
            for (int i = 0; i < m_InputPanel.childCount; i++)
            {
                int index = i;
                var item = m_InputPanel.GetChild(i).GetComponent<Button>();
                item.onClick.AddListener(() =>
                {
                    Debug.Log(index);
                });
            }

            ApplyLanguage();
        }

        void OnEnable()
        {
            EventManager.RegisterEvent(OnNetCallback);

            lastSettings = ClientNet.Get.m_PlayerManager.LocalPlayer.m_Settings;
            //m_MusicSlider.value = AudioManager.musicVolume * 100;
            //m_SoundSlider.value = AudioManager.soundVolume * 100;
            m_MusicSlider.value = lastSettings.MusicVolume;
            m_SoundSlider.value = lastSettings.SoundVolume;
            m_ScreenSizeDrog.value = lastSettings.ScreenSize;
            m_ScreenSizeValue.text = m_ScreenSizeDrog.options[m_ScreenSizeDrog.value].text;
            m_FullScreenTog.isOn = lastSettings.FullScreen == 1;
            m_FullScreenValue.text = m_FullScreenTog.isOn ? "ON" : "OFF";
            m_LanguageDrog.value = lastSettings.Language;
            m_LanguageValue.text = m_LanguageDrog.options[m_LanguageDrog.value].text;
            Debug.Log($"进来时：{lastSettings.ToString()}");

            SwitchToCommon();
        }

        void OnDisable()
        {
            EventManager.UnRegisterEvent(OnNetCallback);
        }

        public override void ApplyLanguage()
        {
            // 多国语言
            //m_TitleText.text = "Settings";
            m_ScreenSizeName.text = "Screen Size";
            m_FullScreenName.text = "Full Screen";
            m_MusicName.text = "Music Volume";
            m_SoundName.text = "Sound Volume";
            m_LanguageName.text = "Language";
            m_BackText.text = "- BACK";
        }

        public override void OnNetCallback(PacketType eventID, INetSerializable reader, NetPeer peer)
        {
            switch (eventID)
            {
                case PacketType.S2C_Settings:
                    var packet = (Settings)reader;
                    ClientNet.Get.m_PlayerManager.LocalPlayer.m_Settings = packet;
                    this.Pop();
                    break;
            }
        }

        void OnBackButtonClick()
        {
            var cmd = new Settings
            {
                ScreenSize = 0,
                FullScreen = 0,
                MusicVolume = (byte)m_MusicSlider.value,
                SoundVolume = (byte)m_SoundSlider.value,
                Language = (byte)m_LanguageDrog.value,
            };
            Debug.Log($"退出时：{cmd.ToString()}");
            if (cmd.Equals(lastSettings))
            {
                Debug.Log("没有改变，直接退出");
                this.Pop(); //没有改变，直接退出
            }
            else
            {
                Debug.Log("设置变了，提交后退出");
                //ClientNet.Get.SendSettins(cmd); //提交后退出
            }
        }
        void SwitchToCommon()
        {
            m_ToCommonBtn.interactable = false;
            m_ToInputBtn.interactable = true;

            Tweener tw1 = m_CommonPanel.DOLocalMoveX(0, 0.5f);
            tw1.Play();
            Tweener tw2 = m_InputPanel.DOLocalMoveX(Screen.width, 0.5f);
            tw2.Play();
        }
        void SwitchToInput()
        {
            m_ToCommonBtn.interactable = true;
            m_ToInputBtn.interactable = false;

            Tweener tw1 = m_CommonPanel.DOLocalMoveX(-Screen.width, 0.5f);
            tw1.Play();
            Tweener tw2 = m_InputPanel.DOLocalMoveX(0, 0.5f);
            tw2.Play();
        }

        static readonly int[][] ScreenSizeOptions = new int[][]
        {
            new int[] { 640, 360 },
            new int[] { 960, 540 },
            new int[] { 1024, 576 },
            new int[] { 1280, 720 },
            new int[] { 1920, 1080 },
        };
        void OnScreenSizeChanged(int index)
        {
            Debug.Log($"选项：{index}---{m_ScreenSizeDrog.options[index]}");
            m_ScreenSizeValue.text = m_ScreenSizeDrog.options[index].text;
            var option = ScreenSizeOptions[index];
            Screen.SetResolution(option[0], option[1], m_FullScreenTog.isOn); //设置分辨率
        }

        void OnFullScreenChanged(bool value)
        {
            m_FullScreenValue.text = value ? "ON" : "OFF";
            Screen.fullScreen = value;
        }

        void OnMusicChanged(float value)
        {
            //Debug.LogError($"音乐变了：{value}");
            AudioManager.musicVolume = value;
            m_MusicValue.text = $"{value}";
        }

        void OnSoundChanged(float value)
        {
            //Debug.LogError($"音效变了：{value}");
            AudioManager.soundVolume = value;
            m_SoundValue.text = $"{value}";
        }

        static readonly string[] LanguageOptions = new string[] { "English", "简体中文", "日本語", "Español" };
        void OnLanguageChanged(int index)
        {
            Debug.Log($"改变语言：{m_LanguageDrog.options[index].text}");
            m_LanguageValue.text = m_LanguageDrog.options[index].text;
        }

        void OnResetToDefault()
        {
            //TODO: 读取配置。
            m_MusicSlider.value = 80;
            m_SoundSlider.value = 80;
        }
    }
}