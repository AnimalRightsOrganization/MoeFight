using Code.Client;
using System.Collections.Generic;
using UnityEngine;

public delegate void AIMotion();
public class AIState : MonoBehaviour
{
    public static AIMotion doAIMotion;

    static bool faceRight = false;
    static KeyCode front => faceRight ? KeyCode.D : KeyCode.A;
    static KeyCode back => faceRight ? KeyCode.A : KeyCode.D;
    // FORWARD
    static List<KeyCode[]> keys_MOVE_FORWARD = new List<KeyCode[]>
    {
        new KeyCode[] { front },
        new KeyCode[] { front },
        new KeyCode[] { front },
        new KeyCode[] { front },
        new KeyCode[] { front },
        new KeyCode[] { front },
        new KeyCode[] { front },
        new KeyCode[] { front },
        new KeyCode[] { front },
        new KeyCode[] { front },
    };
    // BACKWARD
    static List<KeyCode[]> keys_MOVE_BACKWARD = new List<KeyCode[]>
    {
        new KeyCode[] { back },
        new KeyCode[] { back },
        new KeyCode[] { back },
        new KeyCode[] { back },
        new KeyCode[] { back },
        new KeyCode[] { back },
        new KeyCode[] { back },
        new KeyCode[] { back },
        new KeyCode[] { back },
        new KeyCode[] { back },
    };
    // JUMP NETURAL
    static List<KeyCode[]> keys_JUMP = new List<KeyCode[]>
    {
        new KeyCode[] { KeyCode.W },
    };
    // CROUCH
    static List<KeyCode[]> keys_CROUCH = new List<KeyCode[]>
    {
        new KeyCode[] { KeyCode.S },
        new KeyCode[] { KeyCode.S },
        new KeyCode[] { KeyCode.S },
        new KeyCode[] { KeyCode.S },
        new KeyCode[] { KeyCode.S },
        new KeyCode[] { KeyCode.S },
        new KeyCode[] { KeyCode.S },
        new KeyCode[] { KeyCode.S },
        new KeyCode[] { KeyCode.S },
        new KeyCode[] { KeyCode.S },
    };
    // DEFEND
    static List<KeyCode[]> keys_DEFEND = new List<KeyCode[]>
    {
        new KeyCode[] { KeyCode.S, back },
        new KeyCode[] { KeyCode.S, back },
        new KeyCode[] { KeyCode.S, back },
        new KeyCode[] { KeyCode.S, back },
        new KeyCode[] { KeyCode.S, back },
        new KeyCode[] { KeyCode.S, back },
        new KeyCode[] { KeyCode.S, back },
        new KeyCode[] { KeyCode.S, back },
        new KeyCode[] { KeyCode.S, back },
        new KeyCode[] { KeyCode.S, back },
    };
    // SLP
    static List<KeyCode[]> keys_SLP = new List<KeyCode[]>
    {
        new KeyCode[] { KeyCode.U },
    };
    // HADOUKEN
    static List<KeyCode[]> keys_HADOUKEN = new List<KeyCode[]>
    {
        new KeyCode[] { },
        new KeyCode[] { front },
        new KeyCode[] { front },
        new KeyCode[] { front },
        new KeyCode[] { front },
        new KeyCode[] { front },
        new KeyCode[] { front, KeyCode.S },
        new KeyCode[] { KeyCode.S },
        new KeyCode[] { KeyCode.S },
        new KeyCode[] { KeyCode.S },
        new KeyCode[] { KeyCode.S },
        new KeyCode[] { KeyCode.I },
        new KeyCode[] { KeyCode.I },
        new KeyCode[] { KeyCode.I },
        new KeyCode[] { KeyCode.I },
        new KeyCode[] { KeyCode.I },
        new KeyCode[] { },
    };
    // SHORYUKEN
    static List<KeyCode[]> keys_SHORYUKEN = new List<KeyCode[]>
    {
        new KeyCode[] { },
        new KeyCode[] { KeyCode.S },
        new KeyCode[] { KeyCode.S },
        new KeyCode[] { KeyCode.S },
        new KeyCode[] { KeyCode.S },
        new KeyCode[] { KeyCode.S },
        new KeyCode[] { KeyCode.S },
        new KeyCode[] { KeyCode.S },
        new KeyCode[] { KeyCode.S, back },
        new KeyCode[] { KeyCode.S, back },
        new KeyCode[] { KeyCode.S, back },
        new KeyCode[] { KeyCode.S, back },
        new KeyCode[] { back },
        new KeyCode[] { back },
        new KeyCode[] { back },
        new KeyCode[] { back, KeyCode.U },
        new KeyCode[] { KeyCode.U },
        new KeyCode[] { KeyCode.U },
        new KeyCode[] { KeyCode.U },
        new KeyCode[] { KeyCode.U },
        new KeyCode[] { KeyCode.U },
        new KeyCode[] { },
    };
    // 预设的动作片段
    List<List<KeyCode[]>> moveList = new List<List<KeyCode[]>>
    {
        keys_MOVE_FORWARD, //向前
        keys_MOVE_BACKWARD, //向后
        keys_JUMP, //跳跃
        keys_CROUCH, //下蹲
        keys_DEFEND, //防御
        keys_SLP, //站轻拳
        keys_HADOUKEN, //HADOUKEN
        keys_SHORYUKEN, //SHORYUKEN
    };

    private Character _ai;
    private Character player_ai
    {
        get
        {
            if (_ai == null)
                _ai = LocalSession.gs.characters[1];
            return _ai;
        }
    }
    private bool Is_Free() //是否空闲
    {
        return player_ai.state == HitstunConstants.CharacterState.STAND
            && player_ai.hitStun == 0;
    }
    public int CDTime = 0;

    void OnEnable()
    {
        doAIMotion = OnAction;
    }

    void OnDisable()
    {
        doAIMotion = null;
    }

    void Update()
    {
        if (CDTime > 0)
        {
            CDTime--;
            return;
        }

        // 状态检测，如果可行动（STAND/hitStun），调用一次委托。
        if (Is_Free())
        {
            CDTime = 120; //2秒

            doAIMotion?.Invoke(); //加一组动作
        }
        else
        {
            CDTime = 60; //当前正忙，1秒后再来试
        }
    }

    void OnAction()
    {
        faceRight = player_ai.facingRight;

        // ①随机一个动作（或等待几秒），播放
        int index = Random.Range(0, moveList.Count);
        Debug.Log($"加一组动作: {index}");
        var keys = moveList[index];
        for (int i = 0; i < keys.Count; i++)
        {
            uint input = LocalSession.ConvertInputs(keys[i]);
            ClientLogic.Get.custom.Enqueue(input);
        }
        // ②播放完等待一定时间，下个循环
    }
}
