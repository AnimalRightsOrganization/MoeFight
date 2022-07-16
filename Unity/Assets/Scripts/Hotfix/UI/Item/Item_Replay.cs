using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Code.Shared;
using Code.Client;

namespace HotFix
{
    public class Item_Replay : MonoBehaviour
    {
        [SerializeField] Button m_SelfBtn;
        [SerializeField] Text m_HostText;
        [SerializeField] Text m_GuestText;
        [SerializeField] Text m_MapText;
        [SerializeField] Text m_TimeText;
        [SerializeField] Text m_ResultText;
        private Color[] colors = new Color[] { new Color(1, .503f, 0.586f), new Color(1f, .917f, .5f), new Color(.5f, 1f, .75f) };

        void Awake()
        {
            m_SelfBtn = transform.Find("Button").GetComponent<Button>();
            m_HostText = transform.Find("Button/HostText").GetComponent<Text>();
            m_GuestText = transform.Find("Button/GuestText").GetComponent<Text>();
            m_MapText = transform.Find("Button/MapText").GetComponent<Text>();
            m_TimeText = transform.Find("Button/TimeText").GetComponent<Text>();
            m_ResultText = transform.Find("Button/ResultText").GetComponent<Text>();

            m_SelfBtn.onClick.AddListener(OnLoadScene);
        }

        public async Task<Item_Replay> InitData(FileInfo file)
        {
            var repInfo = await ReplayManager.LoadReplay(file.FullName);
            this.SetHostName(repInfo.scene.Host.UserName);
            this.SetGuestName(repInfo.scene.Guest.UserName);
            this.SetMap($"{repInfo.scene.MapId}");
            this.SetTime($"{file.CreationTime}");

            var mySeatId = repInfo.scene.Host.UserName == ClientNet.Get.m_PlayerManager.LocalPlayer.UserName ? 0 : 1;
            var result = BattleResult.Draw;
            if (repInfo.winnerId == 2)
            {
                result = BattleResult.Draw;
            }
            else if (repInfo.winnerId == mySeatId)
            {
                result = BattleResult.Win;
            }
            else
            {
                result = BattleResult.Lose;
            }
            this.SetResult(result);
            return this;
        }
        private Item_Replay SetHostName(string txt)
        {
            m_HostText.text = txt;
            return this;
        }
        private Item_Replay SetGuestName(string txt)
        {
            m_GuestText.text = txt;
            return this;
        }
        private Item_Replay SetMap(string txt)
        {
            m_MapText.text = txt;
            return this;
        }
        private Item_Replay SetTime(string txt)
        {
            m_TimeText.text = txt;
            return this;
        }
        private Item_Replay SetResult(BattleResult result)
        {
            switch (result)
            {
                case BattleResult.Lose:
                    m_ResultText.text = "负";
                    break;
                case BattleResult.Draw:
                    m_ResultText.text = "平";
                    break;
                case BattleResult.Win:
                    m_ResultText.text = "胜";
                    break;
            }
            m_SelfBtn.image.color = colors[(int)result];
            return this;
        }

        private void OnLoadScene()
        {
            var repInfo = ReplayManager.data;
            ClientPlayer host = new ClientPlayer(repInfo.scene.Host.UserName, 0);
            ClientPlayer guest = new ClientPlayer(repInfo.scene.Guest.UserName, 1);
            ClientRoom clientRoom = new ClientRoom(repInfo.scene.RoomId, host, guest);
            clientRoom.DoInit(repInfo.scene);
            clientRoom.BattleMode = BattleMode.Replay;
            ClientNet.Get.m_ClientRoom = clientRoom;

            System.Action action = () =>
            {
                UIManager.Get().PopAll();
                UIManager.Get().Push<UI_GameMenu>();
                var ui_replay = UIManager.Get().Push<UI_ReplayMenu>();
                ui_replay.InitData(repInfo);
            };
            GameManager.Get.LoadBattleAsync(action);
        }
    }
}