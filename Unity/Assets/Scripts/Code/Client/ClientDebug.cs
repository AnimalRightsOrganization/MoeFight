using UnityEngine;
using Code.Client;

public class ClientDebug : MonoBehaviour
{
    private HitstunRunner runner;
    private GUIStyle style1;
    private int posX1;
    private int posX2;
    private int posY;

    void Awake()
    {
        runner = FindObjectOfType<HitstunRunner>();
        style1 = new GUIStyle { fontSize = 30, normal = new GUIStyleState { textColor = Color.red } };
        posX1 = Screen.width / 4;
        posX2 = Screen.width / 4 * 3;
        posY = Screen.height - 50;
    }

    void OnGUI()
    {
        var char0 = LocalSession.gs.characters[0];
        var data0 = LocalSession.gs.characterDatas[0];
        var currentState0 = char0.state;
        var currentAnimation0 = char0.IsAttacking() ? data0.attacks[currentState0.ToString()] : data0.animations[currentState0.ToString()];
        int currentFrame0 = (int)char0.framesInState % currentAnimation0.totalFrames;

        var char1 = LocalSession.gs.characters[1];
        var data1 = LocalSession.gs.characterDatas[1];
        var currentState1 = char1.state;
        var currentAnimation1 = char1.IsAttacking() ? data1.attacks[currentState1.ToString()] : data1.animations[currentState1.ToString()];
        int currentFrame1 = (int)char1.framesInState % currentAnimation1.totalFrames;

        string log = $"tick: {LocalSession.gs.frameNumber}\nping: {ClientNet.Get._ping}\nF3:回血\nF10:暂停\nF11:恢复\nF12:步进";
        GUI.Label(new Rect(10, 10, 100, 50), log, style1);
        string state1 = $"{currentState0}: {currentFrame0}";
        GUI.Label(new Rect(posX1, posY, 100, 50), state1, style1);
        string state2 = $"{currentState1}: {currentFrame1}";
        GUI.Label(new Rect(posX2, posY, 100, 50), state2, style1);
    }


#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!ClientLogic.Get.IsStart) return;

        var x0 = LocalSession.gs.characters[0].position.x.ToString();
        var x1 = LocalSession.gs.characters[1].position.x.ToString();
        UnityEditor.Handles.Label(runner.characterViews[0].transform.position, x0, style1);
        UnityEditor.Handles.Label(runner.characterViews[1].transform.position, x1, style1);
    }
#endif
}