using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Code.Client;

namespace HotFix
{
    public class UI_ReplayMenu : UIBase
    {
        [SerializeField] Toggle m_PlayTog;
        [SerializeField] Slider m_ProgressBar;
        [SerializeField] Text m_TickText;
        private EventTriggerNotice notice;
        private ReplayFormat repInfo;

        void Awake()
        {
            m_PlayTog = transform.Find("ReplayPanel/PlayTog").GetComponent<Toggle>();
            m_ProgressBar = transform.Find("ReplayPanel/ProgressBar").GetComponent<Slider>();
            m_TickText = transform.Find("ReplayPanel/TickText").GetComponent<Text>();

            m_PlayTog.onValueChanged.AddListener(OnPlay);
            m_ProgressBar.onValueChanged.AddListener(OnSliderChanged);

            if (m_ProgressBar.GetComponent<EventTriggerNotice>() == false)
                m_ProgressBar.gameObject.AddComponent<EventTriggerNotice>();
            notice = m_ProgressBar.GetComponent<EventTriggerNotice>();
            notice.onDrag = OnDrag;
            notice.onEndDrag = OnEndDrag;
            notice.onPointClick = OnEndDrag;

            UIManager.doReplayUpdate = SetProgressValue;
        }

        public void InitData(ReplayFormat info)
        {
            repInfo = info;

            m_ProgressBar.value = 0;
            m_ProgressBar.maxValue = info.inputs.Count;

            m_PlayTog.isOn = true;
        }

        void OnPlay(bool value)
        {
            if (value)
            {
                ClientLogic.Get.PlayReplay();
            }
            else
            {
                ClientLogic.Get.PauseReplay();
            }
        }

        void OnSliderChanged(float value)
        {
            int frameID = (int)value;
            //Debug.Log($"<color=green>进度条改变：{frameID}/{m_ProgressBar.maxValue}</color>");
            m_TickText.text = $"{frameID} / {m_ProgressBar.maxValue}";
        }
        public void SetProgressValue(uint frameID)
        {
            //Debug.Log($"<color=yellow>回放进度条：{frameID}/{BattleManager.Instance.replayBuffer.Count}</color>");
            m_ProgressBar.value = frameID; //会导致执行OnDragSlider()
        }
        public void OnDrag()
        {
            //Debug.Log($"OnDrag：{m_ProgressBar.value}");

            // 测试
            uint frameID = (uint)m_ProgressBar.value;
            ClientLogic.Get.Rollback(frameID);
        }
        // 指定帧
        public void OnEndDrag()
        {
            ushort frameID = (ushort)m_ProgressBar.value;
            SnapToFrame(frameID);
        }
        // 下一帧
        public void NextFrame()
        {
            ushort frameID = (ushort)(m_ProgressBar.value + 1);
            SnapToFrame(frameID);
        }
        // 上一帧
        public void PrevFrame()
        {
            ushort frameID = (ushort)(m_ProgressBar.value - 1);
            SnapToFrame(frameID);
        }
        void SnapToFrame(ushort frameID)
        {
            Debug.Log($"<color=red>OnEndDrag：{frameID}</color>");
            ClientLogic.Get.PauseReplay();
            ClientLogic.Get.Rollback(frameID);
            //ClientLogic.Get.LogicUpdate(); //更新一帧
        }
    }
}