using Unity.Collections;
using UnityEngine;
using Newtonsoft.Json;
using HitstunConstants;
using Code.Client;

public class HitstunRunner : MonoBehaviour
{
    // Settings
    public bool showHitboxes = true;
    public CharacterName player1Character;
    public CharacterName player2Character;

    // Rendering
    private CharacterView characterView; //prefab asset
    public CharacterView[] characterViews;
    //public Camera mainCamera;
    public Transform mainCamera;

    // Character Data
    CharacterData[] characterDatas; //技能数据
    CharacterNode[] characterNodes; //伤害数据

    // Internal
    NativeArray<byte> buffer;
    NativeArray<byte> oldBuffer;

    // Fletcher32校验算法
    static int CalcFletcher32(NativeArray<byte> data)
    {
        uint sum1 = 0;
        uint sum2 = 0;

        int index;
        for (index = 0; index < data.Length; ++index)
        {
            sum1 = (sum1 + data[index]) % 0xffff;
            sum2 = (sum2 + sum1) % 0xffff;
        }
        return unchecked((int)((sum2 << 16) | sum1));
    }

    void Start()
    {
        // Fix the FPS
        Application.targetFrameRate = Constants.FPS;
        Time.fixedDeltaTime = 1f / Constants.FPS;
        // Init LocalSession
        LocalSession.Init(new GameState(), new NonGameState());
        // Init NonGameState
        for (int i = 0; i <= 1; i++)
        {
            LocalSession.ngs.players = new PlayerConnectionInfo[Constants.NUM_PLAYERS];
            LocalSession.ngs.players[i] = new PlayerConnectionInfo
            {
                handle = i,
                type = PlayerType.LOCAL,
                controllerId = i
            };
            LocalSession.ngs.SetConnectState(i, PlayerConnectState.RUNNING);
        }
        // load character node from JSON
        LoadCharacterNode();
        // Init GameState
        LocalSession.gs.Init();
        // load character data from JSON
        LoadCharacterData();
        // Init View
        InitView();
    }

    void OnDestroy()
    {
        if (buffer.IsCreated)
        {
            buffer.Dispose();
        }
        if (oldBuffer.IsCreated)
        {
            oldBuffer.Dispose();
        }
    }

    public void SaveOldBuffer()
    {
        // 保存一个Buffer Temp数据，要放在读取input前
        // save old gamestate
        if (oldBuffer.IsCreated)
        {
            oldBuffer.Dispose();
        }
        oldBuffer = GameState.ToBytes(LocalSession.gs); //转到NativeArray
    }

    public void OnFixedUpdate(uint[] inputs)
    {
        // save new gamestate
        if (buffer.IsCreated)
        {
            buffer.Dispose();
        }
        buffer = GameState.ToBytes(LocalSession.gs); //class转NativeArray
        int checksum = CalcFletcher32(buffer);
        //Debug.Log($"OnFixed111: <color=green>{LocalSession.gs.frameNumber}, {LocalSession.gs.hitstop}</color>\n0:{LocalSession.gs.characters[0].ToJson()}, 1:{LocalSession.gs.characters[1].ToJson()}");


        // oldBuffer是执行输入前一帧的 LocalSession.gs
        // load old gamestate and re-simulate
        GameState.FromBytes(LocalSession.gs, oldBuffer);
        LocalSession.gs.Update(inputs, 0); //按返回更新


        // 这里会产生错误
        // save new gamestate again
        if (buffer.IsCreated)
        {
            buffer.Dispose();
        }
        buffer = GameState.ToBytes(LocalSession.gs);
        int checksum2 = CalcFletcher32(buffer);
        //Debug.Log($"OnFixed222: <color=green>{LocalSession.gs.frameNumber}, {LocalSession.gs.hitstop}</color>\n0:{LocalSession.gs.characters[0].ToJson()}, 1:{LocalSession.gs.characters[1].ToJson()}");

        if (checksum != checksum2)
        {
            Debug.LogError(LocalSession.gs.frameNumber + ": " + checksum.ToString() + " , " + checksum2.ToString()); //state和framesInState不同
        }

        // 运算结束，驱动角色、子弹、相机
        UpdateGameView(LocalSession.gs); //游戏
    }

    public void OnReplayUpdate(uint[] inputs)
    {
        //GameState.FromBytes(LocalSession.gs, oldBuffer);
        LocalSession.gs.Update(inputs, 0); //回放

        // 运算结束，驱动角色、子弹、相机
        UpdateGameView(LocalSession.gs); //回放
    }

    void InitView()
    {
        characterView = ResManager.LoadPrefab("Prefabs/CharacterView").GetComponent<CharacterView>();
        characterViews = new CharacterView[Constants.NUM_PLAYERS];

        for (int i = 0; i < Constants.NUM_PLAYERS; ++i)
        {
            characterViews[i] = Instantiate(characterView, transform);
            characterViews[i].LoadResources(characterDatas[i]);
            characterViews[i].showHitboxes = showHitboxes;
        }
        // setup position
        UpdateGameView(LocalSession.gs); //初始化执行一次

        BattleEvent.doSetLight = (int pid) =>
        {
            //var chara = characterViews[pid];
            //TODO: 关闭灯光，单独打亮该角色，两秒后自动恢复灯光
        };
    }

    void UpdateGameView(GameState gs)
    {
        // update characterView objects
        for (int i = 0; i < Constants.NUM_PLAYERS; ++i)
        {
            characterViews[i].showHitboxes = showHitboxes;
            characterViews[i].UpdateCharacterView(gs.characters[i]);
        }
        // update cameraPosition
        float xMean = (gs.characters[0].position.x + gs.characters[1].position.x) / 2.0f;
        float xMeanTranslated = (xMean - Constants.BOUNDS_WIDTH / 2.0f) / Constants.SCALE;
        float newCamPos = xMeanTranslated;
        if (newCamPos < Constants.CAM_LOWER_BOUND)
        {
            newCamPos = Constants.CAM_LOWER_BOUND;
        }
        if (newCamPos > Constants.CAM_UPPER_BOUND)
        {
            newCamPos = Constants.CAM_UPPER_BOUND;
        }
        mainCamera.transform.position = new Vector3(newCamPos, 1, -3);
    }

    void LoadCharacterNode()
    {
        characterNodes = new CharacterNode[Constants.NUM_PLAYERS];
        characterNodes[0] = ConfigManager.Get().GetCharacter(player1Character);
        characterNodes[1] = ConfigManager.Get().GetCharacter(player2Character);
        LocalSession.gs.characterNodes = characterNodes;
    }

    void LoadCharacterData()
    {
        var ta1 = Resources.Load<TextAsset>($"CharacterData/{player1Character}");
        var ta2 = Resources.Load<TextAsset>($"CharacterData/{player2Character}");
        characterDatas = new CharacterData[Constants.NUM_PLAYERS];
        characterDatas[0] = JsonConvert.DeserializeObject<CharacterData>(ta1.text);
        characterDatas[1] = JsonConvert.DeserializeObject<CharacterData>(ta2.text);
        LocalSession.characterDatas = characterDatas;
        LocalSession.gs.characterDatas = characterDatas;
    }

    public void Reload()
    {
        LoadCharacterData();

        for (int i = 0; i < Constants.NUM_PLAYERS; ++i)
        {
            var data = characterDatas[i];
            characterViews[i].LoadData(data);
        }
    }
}