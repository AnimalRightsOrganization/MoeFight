using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HotFix
{
    public class UI_Replay : UIBase
    {
        private GameObject m_ItemPrefab;
        [SerializeField] List<Item_Replay> object_pool;
        [SerializeField] List<Item_Replay> recycle_pool;

        [SerializeField] Text m_TitleText;
        [SerializeField] Button m_BackBtn;
        [SerializeField] Text m_BackText;
        [SerializeField] Transform m_List;

        void Awake()
        {
            m_ItemPrefab = ResManager.LoadPrefab("UI/Item/Item_Replay");
            object_pool = new List<Item_Replay>();
            recycle_pool = new List<Item_Replay>();

            m_TitleText = transform.Find("Top/Title").GetComponent<Text>();
            m_BackBtn = transform.Find("Top/BackBtn").GetComponent<Button>();
            m_BackText = transform.Find("Top/BackBtn/Text").GetComponent<Text>();
            m_List = transform.Find("List");

            m_BackBtn.onClick.AddListener(OnBackButtonClick);
        }

        void OnEnable()
        {
            ReadReplayAsync();

            ApplyLanguage();
        }

        void OnDisable()
        {
            for (int i = recycle_pool.Count - 1; i >= 0; i--)
            {
                var child = recycle_pool[i];
                object_pool.Add(child);
                recycle_pool.Remove(child);
                child.gameObject.SetActive(false);
            }
        }

        public override void ApplyLanguage()
        {
            m_TitleText.text = "Replay";
            m_BackText.text = "BACK";
        }

        void OnBackButtonClick()
        {
            this.Pop();
        }

        // 读取本地文件
        async void ReadReplayAsync()
        {
            var connect = UIManager.Get().Push<UI_Connect>();
            await Task.CompletedTask;

            //string folder = $"{ConstValue.REPLAY_FOLDER}/{Code.Client.ClientNet.Get.m_PlayerManager.LocalPlayer.UserName}";
            string folder = ConstValue.REPLAY_FOLDER;
            if (Directory.Exists(folder) == false)
            {
                Debug.LogError("没有录像文件");
                await Task.Delay(200);
                connect.Pop();
                return;
            }
            var dirInfo = new DirectoryInfo(folder);
            FileInfo[] fileInfo = dirInfo.GetFiles();
            await Task.CompletedTask;
            fileInfo.OrderBy(x => x.CreationTime);
            await Task.Delay(100);

            for (int i = 0; i < fileInfo.Length; i++)
            {
                FileInfo file = fileInfo[i];
                await Task.CompletedTask;
                Debug.Log($"{i}---{file.Name}");

                // 对象池
                Item_Replay script = null;
                if (object_pool.Count > 0)
                {
                    //Debug.Log($"{i}---从对象池取");
                    script = object_pool[0];
                    object_pool.Remove(script);
                    recycle_pool.Add(script);
                    script.gameObject.SetActive(true);
                    await Task.CompletedTask;
                }
                else
                {
                    //Debug.Log($"{i}---重新创建");
                    var obj = Instantiate(m_ItemPrefab, m_List);
                    await Task.CompletedTask;
                    if (obj.GetComponent<Item_Replay>() == false)
                    {
                        obj.AddComponent<Item_Replay>();
                        await Task.CompletedTask;
                    }
                    script = obj.GetComponent<Item_Replay>();
                    recycle_pool.Add(script);
                    await Task.CompletedTask;
                }

                script.transform.SetAsLastSibling(); //迟创建的放下面
                await script.InitData(file);
            }

            await Task.Delay(100);
            connect.Pop();
        }
    }
}