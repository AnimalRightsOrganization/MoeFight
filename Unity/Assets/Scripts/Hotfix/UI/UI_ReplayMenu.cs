using UnityEngine;
using UnityEngine.UI;
using Code.Client;

namespace HotFix
{
    public class UI_ReplayMenu : UIBase
    {
        public Toggle m_PlayTog;
        public Slider m_ProgressBar;
        public Text m_TickText;
        private EventTriggerNotice notice;

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
            m_ProgressBar.value = 1;
            m_ProgressBar.maxValue = info.inputs.Count;
            Debug.Log($"bar: {m_ProgressBar.value}~{m_ProgressBar.maxValue}");

            ClientLogic.Get.InitReplay();

            //m_PlayTog.isOn = true;
            m_PlayTog.isOn = false;
            OnSliderChanged(1);
            //SetProgressValue(1);
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
            uint frameID = (uint)value;
            //Debug.Log($"<color=green>进度条改变: {frameID}/{m_ProgressBar.maxValue}</color>");
            m_TickText.text = $"{frameID} / {m_ProgressBar.maxValue}";
            if (frameID >= m_ProgressBar.maxValue)
            {
                //Debug.Log($"End...{frameID}");
                //m_PlayTog.isOn = false;
            }
        }
        void SetProgressValue(uint frameID)
        {
            //Debug.Log($"<color=yellow>回放进度条: {frameID}/{BattleManager.Instance.replayBuffer.Count}</color>");
            m_ProgressBar.value = frameID; //会导致执行OnDragSlider()
        }
        void OnDrag()
        {
            //Debug.Log($"OnDrag: {m_ProgressBar.value}");
            uint frameID = (uint)m_ProgressBar.value;
            ClientLogic.Get.RollbackReplay(frameID);
        }
        // 指定帧
        void OnEndDrag()
        {
            uint frameID = (uint)m_ProgressBar.value;
            SnapToFrame(frameID);
        }
        // 下一帧
        void NextFrame()
        {
            uint frameID = (uint)(m_ProgressBar.value + 1);
            SnapToFrame(frameID);
        }
        // 上一帧
        void PrevFrame()
        {
            uint frameID = (uint)(m_ProgressBar.value - 1);
            SnapToFrame(frameID);
        }
        void SnapToFrame(uint frameID)
        {
            Debug.Log($"<color=red>OnEndDrag: {frameID}</color>");
            ClientLogic.Get.PauseReplay();
            ClientLogic.Get.RollbackReplay(frameID);

            // 血条
            int hp1 = LocalSession.gs.characters[0].health;
            int hp2 = LocalSession.gs.characters[1].health;
            UIManager.doSetCurrentHp?.Invoke(1, hp1);
            UIManager.doSetCurrentHp?.Invoke(2, hp2);
        }
    }
}