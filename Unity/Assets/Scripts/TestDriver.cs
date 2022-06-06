using UnityEngine;

public class TestDriver : MonoBehaviour
{
    HitstunRunner runner;

    void Awake()
    {
        runner = FindObjectOfType<HitstunRunner>();
    }

    void FixedUpdate()
    {
        runner.SaveOldBuffer();

        // 必须备份一个oldBuffer，不然帧数多一
        uint[] inputs = LocalSession.RunFrame();
        runner.OnFixedUpdate(inputs);
    }
}