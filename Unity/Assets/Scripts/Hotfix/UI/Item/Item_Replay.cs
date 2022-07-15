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
        private string replayPath;
        private Color[] colors = new Color[] { new Color(1, .503f, 0.586f), new Color(1f, .917f, .5f), new Color(.5f, 1f, .75f) };

        void Awake()
        {
            m_SelfBtn = transform.Find("Button").GetComponent<Button>();
            m_SelfBtn.onClick.AddListener(OnLoadScene);
            m_HostText = transform.Find("Button/HostText").GetComponent<Text>();
            m_GuestText = transform.Find("Button/GuestText").GetComponent<Text>();
            m_MapText = transform.Find("Button/MapText").GetComponent<Text>();
            m_TimeText = transform.Find("Button/TimeText").GetComponent<Text>();
            m_ResultText = transform.Find("Button/ResultText").GetComponent<Text>();
        }

        public async Task<Item_Replay> InitData(FileInfo file)
        {
            var repInfo = await ReplayManager.LoadReplay(file.FullName);
            this.SetFilePath(file.FullName);
            this.SetHostName(repInfo.scene.Host.UserName);
            this.SetGuestName(repInfo.scene.Guest.UserName);
            this.SetMap($"{repInfo.scene.MapId}");
            this.SetTime($"{file.CreationTime}");

            var mySeatId = repInfo.scene.Host.UserName == ClientNet.Get.m_PlayerManager.LocalPlayer.UserName ? 0 : 1;
            var result = BattleResult.Draw;
            if (repInfo.winnerId == -1)
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
        private Item_Replay SetFilePath(string path)
        {
            replayPath = path;
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
            var ui_replay = UIManager.Get().GetUI<UI_Replay>();

            string timeStr = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var host = new ClientPlayer("host", 0); //单机时，host都是自己
            var guest = new ClientPlayer("guest", 1);
            var clientRoom = new ClientRoom(0, host, guest);
            ClientNet.Get.m_ClientRoom = clientRoom;
            var packet = new S2C_LoadScenePacket
            {
                RoomId = 0,
                BattleId = $"{timeStr}_replay",
                MapId = 0,
                Host = new PlayerLoadPacket { UserName = host.UserName, PeerId = host.PeerId, RoleIndex = 0 },
                Guest = new PlayerLoadPacket { UserName = guest.UserName, PeerId = guest.PeerId, RoleIndex = 1 },
            };
            clientRoom.DoInit(packet);
            Debug.Log($"加载录像：{clientRoom.BattleID}");
            ClientNet.Get.m_PlayerManager.LocalPlayer.SetStatus(PlayerStatus.AtBattle);

            System.Action action = () =>
            {
                UIManager.Get().PopAll();
                UIManager.Get().Push<UI_GameMenu>();
                var ui = UIManager.Get().Push<UI_ReplayMenu>();
                ui.InitData(replayPath);
            };
            GameManager.Get.LoadBattleAsync(action);
        }
    }
}