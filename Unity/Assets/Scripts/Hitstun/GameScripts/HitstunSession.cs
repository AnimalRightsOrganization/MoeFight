using UnityEngine;
using HitstunConstants;
using System.Linq;

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

    public static uint[] RunFrame(uint[] inputs)
    {
        gs.Update(inputs, 0); //按输入更新
        return inputs;
    }

    public static uint ReadInputs()
    {
        uint input = 0;
        string str = "";

        if (Input.GetKey(KeyCode.W))
        {
            input |= (uint)KeyPress.KEY_UP;
            str += "W+";
        }
        if (Input.GetKey(KeyCode.S))
        {
            input |= (uint)KeyPress.KEY_DOWN;
            str += "S+";
        }
        if (Input.GetKey(KeyCode.A))
        {
            input |= (uint)KeyPress.KEY_LEFT;
            str += "A+";
        }
        if (Input.GetKey(KeyCode.D))
        {
            input |= (uint)KeyPress.KEY_RIGHT;
            str += "D+";
        }
        if (Input.GetKey(KeyCode.U))
        {
            input |= (uint)KeyPress.KEY_LP;
            str += "U+";
        }
        if (Input.GetKey(KeyCode.I))
        {
            input |= (uint)KeyPress.KEY_MP;
            str += "I+";
        }
        if (Input.GetKey(KeyCode.O))
        {
            input |= (uint)KeyPress.KEY_HP;
            str += "O+";
        }
        if (Input.GetKey(KeyCode.J))
        {
            input |= (uint)KeyPress.KEY_LK;
            str += "J+";
        }
        if (Input.GetKey(KeyCode.K))
        {
            input |= (uint)KeyPress.KEY_MK;
            str += "K+";
        }
        if (Input.GetKey(KeyCode.L))
        {
            input |= (uint)KeyPress.KEY_HK;
            str += "L+";
        }
        //if (string.IsNullOrEmpty(str) == false)
        //    Debug.Log(str + $"[{input}]");
        return input;
    }

    public static uint ConvertInputs(KeyCode[] keys)
    {
        uint input = 0;

        if (keys.Contains(KeyCode.W))
        {
            input |= (uint)KeyPress.KEY_UP;
        }
        if (keys.Contains(KeyCode.S))
        {
            input |= (uint)KeyPress.KEY_DOWN;
        }
        if (keys.Contains(KeyCode.A))
        {
            input |= (uint)KeyPress.KEY_LEFT;
        }
        if (keys.Contains(KeyCode.D))
        {
            input |= (uint)KeyPress.KEY_RIGHT;
        }
        if (keys.Contains(KeyCode.U))
        {
            input |= (uint)KeyPress.KEY_LP;
        }
        if (keys.Contains(KeyCode.I))
        {
            input |= (uint)KeyPress.KEY_MP;
        }
        if (keys.Contains(KeyCode.O))
        {
            input |= (uint)KeyPress.KEY_HP;
        }
        if (keys.Contains(KeyCode.J))
        {
            input |= (uint)KeyPress.KEY_LK;
        }
        if (keys.Contains(KeyCode.K))
        {
            input |= (uint)KeyPress.KEY_MK;
        }
        if (keys.Contains(KeyCode.L))
        {
            input |= (uint)KeyPress.KEY_HK;
        }
        return input;
    }
}