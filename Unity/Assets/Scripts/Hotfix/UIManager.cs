using System.Collections.Generic;
using UnityEngine;

namespace HotFix
{
    public delegate void ShowSkillText(int pid, string content);
    public delegate void SetTimeText(string second);
    public delegate void SetCurrentHp(int pid, int hp);
    public delegate void SetGameEnd(int winner);
    public class UIManager : MonoBehaviour
    {
        static UIManager _instance;
        public static UIManager Get()
        {
            return _instance;
        }

        public Transform Parent;
        //public Transform Top;

        // 给UI的委托
        public static ShowSkillText doShowSkillText;
        public static SetTimeText doSetTimeText;
        public static SetCurrentHp doSetCurrentHp;
        public static SetGameEnd doSetGameEnd;

        // UI存储栈
        public Dictionary<string, UIBase> stack;
        public Dictionary<string, UIBase> recyclePool;

        void Awake()
        {
            _instance = this;
            Parent = GameObject.Find("Canvas").transform;
            stack = new Dictionary<string, UIBase>();
            recyclePool = new Dictionary<string, UIBase>();
        }

        public UIBase GetActiveUI()
        {
            var child = Parent.GetChild(Parent.childCount - 1);
            //Debug.Log($"GetActive: {child.name}");
            string scriptName = child.name;

            UIBase ui = null;
            if (stack.TryGetValue(scriptName, out ui) == false)
            {
                Debug.LogError($"还没有创建：{scriptName}");
                return null;
            }
            return ui.GetComponent<UIBase>();
        }

        public T GetUI<T>() where T : UIBase
        {
            string scriptName = typeof(T).ToString().Replace("HotFix.", "");
            //Debug.Log($"GetUI: {scriptName}");
            UIBase ui = null;
            if (stack.TryGetValue(scriptName, out ui) == false)
            {
                Debug.LogError($"还没有创建：{scriptName}");
                return null;
            }
            return ui.GetComponent<T>();
        }

        public T Push<T>(int layer = 1) where T : UIBase
        {
            string fullName = typeof(T).ToString();
            string scriptName = string.Empty;
            if (fullName.Contains("."))
            {
                scriptName = fullName.Split('.')[1];
            }
            else
            {
                scriptName = fullName;
            }
            //Debug.Log($"Push<{scriptName}>");
            UIBase ui = null;
            if (stack.TryGetValue(scriptName, out ui))
            {
                return ui.GetComponent<T>();
            }
            if (recyclePool.TryGetValue(scriptName, out ui))
            {
                recyclePool.Remove(scriptName);
                stack.Add(scriptName, ui);
                //Debug.Log($"<color=yellow>ReUse{stack.Count}/{recyclePool.Count}</color>");
                ui.gameObject.SetActive(true);
                ui.transform.SetAsLastSibling(); //排列在最下面，即渲染的最高层
                return ui.GetComponent<T>();
            }
            else
            {
                GameObject prefab = ResManager.LoadPrefab($"UI/{scriptName}"); //iOS区分大小写？
                GameObject obj = Instantiate(prefab, Parent);
                obj.transform.localPosition = Vector3.zero;
                obj.name = scriptName;

                if (obj.GetComponent<T>() == false)
                    obj.AddComponent<T>();
                var script = obj.GetComponent<T>();
                stack.Add(scriptName, script);
                //Debug.Log($"<color=yellow>New{stack.Count}/{recyclePool.Count}</color>");
                return script;
            }
        }

        public void Pop(UIBase ui)
        {
            string scriptName = ui.name;
            if (ui == null)
            {
                Debug.LogError("没有需要销毁的UI");
                return;
            }
            stack.Remove(scriptName);
            recyclePool.Add(scriptName, ui);
            //ui.transform.SetAsFirstSibling(); //有性能开销
            ui.gameObject.SetActive(false);
        }
        public void PopAll()
        {
            foreach (var item in stack)
            {
                //Debug.Log($"{item.Key}---{item.Value.gameObject}");
                recyclePool.Add(item.Key, item.Value);
                item.Value.gameObject.SetActive(false);
            }
            stack.Clear();
        }
    }
}