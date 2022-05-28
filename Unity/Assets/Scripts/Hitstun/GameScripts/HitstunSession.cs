using UnityEngine;
using HitstunConstants;

public static class LocalSession
{
    public static GameState gs;
    public static NonGameState ngs;
    public static CharacterData[] characterDatas;

    public static void Init(GameState _gs, NonGameState _ngs)
    {
        gs = _gs;
        ngs = _ngs;
    }

    public static uint[] RunFrame()
    {
        uint[] inputs = new uint[ngs.players.Length];
        for (int i = 0; i < inputs.Length; ++i)
        {
            inputs[i] = ReadInputs(ngs.players[i].controllerId);
        }
        gs.Update(inputs, 0);
        return inputs;
    }

    // 键盘输入，左右两边控制
    static uint ReadInputs(int controllerId)
    {
        uint input = 0;

        if (controllerId == 0)
        {
            if (Input.GetKey(KeyCode.W))
            {
                input |= (uint)KeyPress.KEY_UP;
            }
            if (Input.GetKey(KeyCode.S))
            {
                input |= (uint)KeyPress.KEY_DOWN;
            }
            if (Input.GetKey(KeyCode.A))
            {
                input |= (uint)KeyPress.KEY_LEFT;
            }
            if (Input.GetKey(KeyCode.D))
            {
                input |= (uint)KeyPress.KEY_RIGHT;
            }
            if (Input.GetKey(KeyCode.U))
            {
                input |= (uint)KeyPress.KEY_LP;
            }
            if (Input.GetKey(KeyCode.I))
            {
                input |= (uint)KeyPress.KEY_MP;
            }
            if (Input.GetKey(KeyCode.O))
            {
                input |= (uint)KeyPress.KEY_HP;
            }
            if (Input.GetKey(KeyCode.J))
            {
                input |= (uint)KeyPress.KEY_LK;
            }
            if (Input.GetKey(KeyCode.K))
            {
                input |= (uint)KeyPress.KEY_MK;
            }
            if (Input.GetKey(KeyCode.L))
            {
                input |= (uint)KeyPress.KEY_HK;
            }
        }
        else if (controllerId == 1)
        {
            if (Input.GetKey(KeyCode.UpArrow))
            {
                input |= (uint)KeyPress.KEY_UP;
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                input |= (uint)KeyPress.KEY_DOWN;
            }
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                input |= (uint)KeyPress.KEY_LEFT;
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                input |= (uint)KeyPress.KEY_RIGHT;
            }
            if (Input.GetKey(KeyCode.RightControl))
            {
                input |= (uint)KeyPress.KEY_MK;
            }
        }
        return input;
    }

    public static uint[] ReadFrame(uint tick, uint[] inputs)
    {
        // 传进来第五帧数据inputs，先回到第四帧
        gs.frameNumber = tick - 1;
        gs.Update(inputs, 0);
        return inputs;
    }
}