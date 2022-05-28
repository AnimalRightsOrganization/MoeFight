using UnityEngine;

public class TestUpdate : MonoBehaviour
{
    public HitstunRunner runner;

    void FixedUpdate()
    {
        runner.SaveOldBuffer(); //要放在读取input前

        uint[] inputs = LocalSession.RunFrame();
        Debug.Log($"FixedUpdate: <color=yellow>{LocalSession.gs.frameNumber}</color>");
        runner.OnFixedUpdate(inputs);
    }
}