using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class AIState : MonoBehaviour
{
    static bool faceRight = true;
    static KeyCode front => faceRight ? KeyCode.D : KeyCode.A;
    static KeyCode back => faceRight ? KeyCode.A : KeyCode.D;
    // DEFEND
    static List<KeyCode[]> keys_CROUCH = new List<KeyCode[]>
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

    async void Loop()
    {
        List<int> moveList = new List<int>();
        List<int> attackList = new List<int>();
        int index = Random.Range(0, moveList.Count);

        // ①随机一个动作，播放

        // ②播放完等待一定时间，下个循环
        await Task.Delay(2000);
    }
}
