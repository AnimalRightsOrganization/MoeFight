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
        public List<Item_Replay> object_pool;
        public List<Item_Replay> recycle_pool;
        public Transform m_List;

        public Button m_BackBtn;
        public Text m_BackText;
        public Text m_TitleText;

        void Awake()
        {
            m_ItemPrefab = ResManager.LoadPrefab("UI/Item/Item_Replay");
            object_pool = new List<Item_Replay>();
            recycle_pool = new List<Item_Replay>();

            m_BackBtn = transform.Find("Top/BackBtn").GetComponent<Button>();
            m_BackText = transform.Find("Top/BackBtn/Text").GetComponent<Text>();
            m_TitleText = transform.Find("Top/Title").GetComponent<Text>();
            m_List = transform.Find("List");

            m_BackBtn.onClick.AddListener(this.Pop);
        }

        void OnEnable()
        {
            ApplyLanguage();

            ReadReplayAsync();
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
            var config = ConfigManager.Get();

            m_BackText.text = config.GetWord(25);
            m_TitleText.text = config.GetWord(26);
        }

        // 读取本地文件
        async void ReadReplayAsync()
        {
            var connect = UIManager.Get().Push<UI_Connect>();
            await Task.CompletedTask;

            string folder = ConstValue.USER_REPLAY_FOLDER;
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
            fileInfo = fileInfo.OrderByDescending(x => x.CreationTime).ToArray(); //时间倒序
            await Task.Delay(100);

            int fileNum = Mathf.Min(6, fileInfo.Length);
            Debug.Log($"录像数={fileInfo.Length}，显示数={fileNum}");
            int index = 0;
            for (int i = 0; i < fileNum; i++)
            {
                index = i;
                FileInfo file = fileInfo[index];
                await Task.CompletedTask;
                //Debug.Log($"{i}---{file.Name}");

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
                    obj.name = file.Name;
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