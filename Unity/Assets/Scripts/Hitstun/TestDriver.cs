using UnityEngine;

public class TestDriver : MonoBehaviour
{
    HitstunRunner runner;

    private uint recvTick;
    private ReplayFormat rep;

    private GUIStyle style1;
    private int posX1;
    private int posY;

    void Awake()
    {
        runner = FindObjectOfType<HitstunRunner>();
    }

    async void OnEnable()
    {
        style1 = new GUIStyle();
        style1.fontSize = 25;
        style1.normal.textColor = Color.red;
        posX1 = Screen.width / 4;
        posY = Screen.height - 50;

        //string filePath = $"{ConstValue.REPLAY_FOLDER}/20220715_143520.bytes";
        //rep = await ReplayManager.LoadReplay(filePath);
    }

    void FixedUpdate()
    {
        runner.SaveOldBuffer();

        // 必须备份一个oldBuffer，不然帧数多一
        //uint[] inputs = LocalSession.RunFrame();
        //runner.OnFixedUpdate(inputs);

        if (rep == null || rep.inputs.Count <= recvTick)
            return;

        recvTick++;
        uint[] inputs = rep.inputs[recvTick];
        runner.OnReplayUpdate(inputs);

        //Snapshot(recvTick);
    }

    void OnGUI()
    {
        var char0 = LocalSession.gs.characters[0];
        var data0 = LocalSession.gs.characterDatas[0];
        var currentState0 = char0.state;
        var currentAnimation0 = char0.isAttacking() ? data0.attacks[currentState0.ToString()] : data0.animations[currentState0.ToString()];
        int currentFrame0 = (int)char0.framesInState % currentAnimation0.totalFrames;

        string state1 = $"{currentState0}: {currentFrame0}";
        GUI.Label(new Rect(posX1, posY, 100, 50), state1, style1);
    }
}