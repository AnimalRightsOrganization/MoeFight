using UnityEngine;
using UnityEngine.UI;
using Code.Client;
using System.Threading.Tasks;
using Code.Shared;

namespace HotFix
{
    public class UI_ReplayMenu : UIBase
    {
        public Toggle m_PlayTog;
        public Slider m_ProgressBar;
        public Text m_TickText;
        private UIEventSystem notice;
        private uint lastFrame;

        void Awake()
        {
            m_PlayTog = transform.Find("ReplayPanel/PlayTog").GetComponent<Toggle>();
            m_ProgressBar = transform.Find("ReplayPanel/ProgressBar").GetComponent<Slider>();
            m_TickText = transform.Find("ReplayPanel/TickText").GetComponent<Text>();
            if (m_ProgressBar.GetComponent<UIEventSystem>() == false)
                m_ProgressBar.gameObject.AddComponent<UIEventSystem>();
            notice = m_ProgressBar.GetComponent<UIEventSystem>();

            m_PlayTog.onValueChanged.AddListener(OnPlay);
            m_ProgressBar.onValueChanged.AddListener(OnSliderChanged);
            notice.onDrag = OnSnap;
            notice.onEndDrag = OnSnap;
            notice.onPointClick = OnSnap;
        }

        void OnEnable()
        {
            BattleEvent.doReplayUpdate = OnUpdateValue; //replay main loop
        }

        void OnDisable()
        {
            BattleEvent.doReplayUpdate = null;
        }

        public void InitData(ReplayFormat info)
        {
            ClientLogic.Get.InitReplay();

            m_ProgressBar.minValue = 1;
            m_ProgressBar.maxValue = info.inputs.Count;
            m_ProgressBar.value = 1;
            OnSliderChanged(1); //更新文字
            Debug.Log($"bar: {m_ProgressBar.value}~{m_ProgressBar.maxValue}");

            m_PlayTog.isOn = false; //不自动开始
        }

        async void OnPlay(bool value)
        {
            if (value)
            {
                ClientLogic.Get.PlayLoop();
                uint frameID = (uint)m_ProgressBar.value;
                if (frameID >= m_ProgressBar.maxValue)
                {
                    await Task.Delay(500); //等待死亡动画播完
                    m_PlayTog.isOn = false;
                }
            }
            else
            {
                ClientLogic.Get.PauseLoop();
                if (m_ProgressBar.value >= 1)
                {
                    ClientNet.Get.m_ClientRoom.SetStage(BattleStage.End);
                }
            }
        }
        async void OnSliderChanged(float value)
        {
            uint frameID = (uint)value;
            m_TickText.text = $"{frameID} / {m_ProgressBar.maxValue}";
            //Debug.Log($"<color=green>SliderChanged: {frameID}/{m_ProgressBar.maxValue}</color>");

            if (frameID >= m_ProgressBar.maxValue)
            {
                await Task.Delay(500); //等待死亡动画播完
                m_PlayTog.isOn = false;
            }
        }
        void OnSnap()
        {
            uint frameID = (uint)m_ProgressBar.value;
            if (lastFrame == frameID) return; //避免重复执行
            lastFrame = frameID;
            Debug.Log($"<color=red>SnapToFrame: {frameID}</color>");

            //OnSliderChanged()执行完，才能得到鼠标指定的帧
            m_PlayTog.isOn = false; //会触发执行OnPlay()
            ClientLogic.Get.PauseLoop();
            ClientLogic.Get.RollbackReplay(frameID);

            // 血条
            int hp1 = LocalSession.gs.characters[0].health;
            int hp2 = LocalSession.gs.characters[1].health;
            BattleEvent.doSetCurrentHp?.Invoke(0, hp1);
            BattleEvent.doSetCurrentHp?.Invoke(1, hp2);
        }
        void OnUpdateValue(uint frameID)
        {
            m_ProgressBar.value = frameID;
            //Debug.Log($"Slider Update: {m_ProgressBar.value}/{m_ProgressBar.maxValue}");
        }
    }
}