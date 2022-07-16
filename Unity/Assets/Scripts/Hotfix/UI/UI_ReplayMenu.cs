using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Code.Shared;
using Code.Client;

namespace HotFix
{
    public class UI_ReplayMenu : UIBase
    {
        [SerializeField] Toggle m_PlayTog;
        [SerializeField] Slider m_ProgressBar;
        [SerializeField] Text m_TickText;
        //[SerializeField] EventTriggerNotice notice;

        void Awake()
        {
            m_PlayTog = transform.Find("ReplayPanel/PlayTog").GetComponent<Toggle>();
            m_PlayTog.onValueChanged.AddListener(OnPlay);
            m_TickText = transform.Find("ReplayPanel/TickText").GetComponent<Text>();
            m_ProgressBar = transform.Find("ReplayPanel/ProgressBar").GetComponent<Slider>();
            m_ProgressBar.onValueChanged.AddListener(OnSliderChanged);

            //if (m_ProgressBar.GetComponent<EventTriggerNotice>() == false)
            //    m_ProgressBar.gameObject.AddComponent<EventTriggerNotice>();
            //notice = m_ProgressBar.GetComponent<EventTriggerNotice>();
            //notice.onDrag = OnDrag;
            //notice.onEndDrag = OnEndDrag;
            //notice.onPointClick = OnEndDrag;
        }

        public void InitData(string path)
        {
            StartCoroutine(WaitForStart(path));

            GameManager.Get.logic.IsStart = true;
        }

        IEnumerator WaitForStart(string path)
        {
            //var curScene = SceneManager.GetActiveScene().name;
            //Debug.Log($"WaitForStart.Before: {curScene}"); //Client
            //yield return new WaitUntil(() => GameManager.Instance != null);
            //curScene = SceneManager.GetActiveScene().name;
            //Debug.Log($"WaitForStart.After: {curScene}"); //Game

            //int frameCount = GameManager.Instance.LoadReplay(path);
            yield return new WaitForEndOfFrame();

            m_ProgressBar.value = 0;
            //m_ProgressBar.maxValue = frameCount;
        }

        void OnPlay(bool value)
        {
            if (value)
            {
                //GameManager.Instance.PlayReplay();
            }
            else
            {
                //GameManager.Instance.PauseReplay();
            }
        }

        void OnSliderChanged(float value)
        {
            int frameID = (int)value;
            //Debug.Log($"<color=green>进度条改变：{frameID}/{m_ProgressBar.maxValue}</color>");
            m_TickText.text = $"{frameID} / {m_ProgressBar.maxValue}";
        }
        public void SetProgressValue(int frameID)
        {
            //Debug.Log($"<color=yellow>回放进度条：{frameID}/{BattleManager.Instance.replayBuffer.Count}</color>");
            m_ProgressBar.value = frameID; //会导致执行OnDragSlider()
        }
        public void OnDrag()
        {
            //Debug.Log($"OnDrag：{m_ProgressBar.value}");

            // 测试
            //int frameID = (int)m_ProgressBar.value;
            //BattleManager.Instance.RollBackTo(frameID);
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
            //GameManager.Instance.PauseReplay();
            //GameManager.Instance.RollBackTo(frameID);
            //GameManager.Instance.LogicUpdate(); //更新一帧
            //GameManager.Instance.GetRole(1).SetCrossFade("Idle");
        }
    }
}