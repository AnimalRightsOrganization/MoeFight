using UnityEngine;
using HitstunConstants;
#if UNITY_EDITOR
using UnityEditor;
[CustomEditor(typeof(Test))]
public class DemoEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); //显示默认所有参数

        Test demo = (Test)target;

        if (GUILayout.Button("TurnNext"))
        {
            demo.OnRecieve();
        }
    }
}
#endif
public class Test : MonoBehaviour
{
    public uint serverTick;
    public uint bufferTick;
    public int Ping = 3;
    public float Delta;
    public int Delay;

    void Start()
    {
        //uint buffer = 2;

        Time.fixedDeltaTime = 1f / Constants.FPS;
        //Debug.Log(Time.fixedDeltaTime); //0.02/ 0.0167

        // t=2 → t=1
        //for (int t = 2; t > 0; t--)
        //{
        //    Debug.Log(t);
        //}
    }

    void Update()
    {
        return;

        Delta = Time.fixedDeltaTime * 1000;
        //Delay = (int)(Ping / Delta);
        Delay = Mathf.CeilToInt(Ping / Delta);


        uint buffer = (bufferTick - serverTick);

        for (int t = (int)buffer; t > 1; t--)
        {
            serverTick++;

            Debug.Log($"{serverTick}");
        }
    }

    public void OnRecieve()
    {
        bufferTick++;
    }
}
