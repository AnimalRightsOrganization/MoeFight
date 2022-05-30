using Unity.Collections;
using UnityEngine;
using Newtonsoft.Json;
using HitstunConstants;

public class HitstunRunner : MonoBehaviour
{
    // Settings
    public bool showHitboxes = true;
    public bool manualStep = false;
    public CharacterName player1Character;
    public CharacterName player2Character;

    // Rendering
    public CharacterView characterView;
    CharacterView[] characterViews;
    public Camera mainCamera;

    // Character Data
    CharacterData[] characterDatas; //技能数据

    // Internal
    NativeArray<byte> buffer;
    NativeArray<byte> oldBuffer; //快照
    private bool running;
    private bool nextStep;

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
        Time.fixedDeltaTime = 1f / (float)Constants.FPS;
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
        // Init GameState
        LocalSession.gs.Init();
        // load character data from JSON
        LoadCharacterData();
        // Init View
        InitView(LocalSession.gs);
        running = !manualStep;
        nextStep = false;
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
    /*
    void FixedUpdate()
    {
        SaveOldBuffer();

        // 必须备份一个oldBuffer，不然帧数多一
        uint[] inputs = LocalSession.RunFrame();
        Debug.Log($"FixedUpdate: <color=yellow>{LocalSession.gs.frameNumber}</color>");
        OnFixedUpdate(inputs);
    }
    */
    public void OnFixedUpdate(uint[] inputs)
    {
        if (Time.deltaTime < 0.016f || Time.deltaTime > 0.017f)
        {
            Debug.Log("Unstable update tick!" + Time.deltaTime.ToString());
        }
        // handles function key debugging inputs
        HandleDevKeys();

        // 推进游戏
        if (running || nextStep)
        {
            nextStep = false;


            //// 保存一个Buffer Temp数据
            //// save old gamestate
            //if (oldBuffer.IsCreated)
            //{
            //    oldBuffer.Dispose();
            //}
            //oldBuffer = GameState.ToBytes(LocalSession.gs); //转到NativeArray

            // 获取键盘输入，执行一帧逻辑运算 //LocalSession.gs.Update()
            // run the frame
            //uint[] inputs = LocalSession.RunFrame();
            //OnFixedUpdate(inputs);


            // save new gamestate //TODO: 意义不明
            if (buffer.IsCreated)
            {
                buffer.Dispose();
            }
            buffer = GameState.ToBytes(LocalSession.gs); //class转NativeArray
            int checksum = CalcFletcher32(buffer);
            //Debug.Log($"OnFixed111: <color=green>{LocalSession.gs.frameNumber}, {LocalSession.gs.hitstop}</color>\n0:{LocalSession.gs.characters[0].ToJson()}, 1:{LocalSession.gs.characters[1].ToJson()}");


            // 赋回旧的值，再计算一次？意义不明
            // 两次传入的参数inputs, flag是一样的
            // oldBuffer是执行输入前一帧的 LocalSession.gs
            // load old gamestate and re-simulate
            GameState.FromBytes(LocalSession.gs, oldBuffer);  //oldBuffer赋值给gs（回档）。再执行一次inputs
            //Debug.Log($"导入inputs验证，input[0]={inputs[0]}");
            LocalSession.gs.Update(inputs, 0);


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
            UpdateGameView(LocalSession.gs);
        }
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

    void InitView(GameState gs)
    {
        characterViews = new CharacterView[Constants.NUM_PLAYERS];

        for (int i = 0; i < Constants.NUM_PLAYERS; ++i)
        {
            characterViews[i] = Instantiate(characterView, transform);
            characterViews[i].LoadResources(characterDatas[i]);
            characterViews[i].showHitboxes = showHitboxes;
        }
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

    void HandleDevKeys()
    {
        // quit
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
        // toggle hitboxes
        if (Input.GetKeyDown(KeyCode.F1))
        {
            showHitboxes = !showHitboxes;
            if (showHitboxes)
            {
                Debug.Log("Hitboxes ON");
            }
            else
            {
                Debug.Log("Hitboxes OFF");
            }
        }
        // manual stepping
        if (Input.GetKeyDown(KeyCode.F2))
        {
            manualStep = !manualStep;
            if (manualStep)
            {
                Debug.Log("Manual mode on: Press F3 to advance a single frame");
                running = false;
                nextStep = false;
            }
            else
            {
                Debug.Log("Manual mode off");
                running = true;
            }
        }
        if (Input.GetKeyDown(KeyCode.F3))
        {
            Debug.Log("Manual step");
            nextStep = true;
        }
        // save and load
        if (Input.GetKeyDown(KeyCode.F5))
        {
            Debug.Log("SAVE");
            TestSave();
        }
        if (Input.GetKeyDown(KeyCode.F6))
        {
            Debug.Log("LOAD");
            TestLoad();
        }
    }

    void LoadCharacterData()
    {
        characterDatas = new CharacterData[Constants.NUM_PLAYERS];

        var ta1 = Resources.Load<TextAsset>($"CharacterData/{player1Character}");
        var ta2 = Resources.Load<TextAsset>($"CharacterData/{player2Character}");
        characterDatas[0] = JsonConvert.DeserializeObject<CharacterData>(ta1.text);
        characterDatas[1] = JsonConvert.DeserializeObject<CharacterData>(ta2.text);

        LocalSession.characterDatas = characterDatas;
        LocalSession.gs.characterDatas = characterDatas;
    }

    public void TestSave()
    {
        if (buffer.IsCreated)
        {
            buffer.Dispose();
        }
        buffer = GameState.ToBytes(LocalSession.gs);
    }

    public void TestLoad()
    {
        GameState.FromBytes(LocalSession.gs, buffer);
    }
}