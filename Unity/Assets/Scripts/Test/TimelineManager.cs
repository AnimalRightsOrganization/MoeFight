using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class TimelineManager : MonoBehaviour
{
    static TimelineManager _instance;
    public static TimelineManager Get
    {
        get
        {
            if (_instance == null)
                _instance = FindObjectOfType<TimelineManager>();
            return _instance;
        }
    }

    const string CinemachineTrackName = "Cinemachine Track";
    const string AnimationTrackName = "Animation Track";
    static List<string> TimelineArray = new List<string>
    {
        "Opening",
        "HADOUKEN",
        "SHORYUKEN",
    };
    private Dictionary<string, PlayableDirector> timelines;

    public Animator[] characters;

    void Awake()
    {
        timelines = new Dictionary<string, PlayableDirector>();

        //for (int i = 0; i < TimelineArray.Count; i++)
        //{
        //    string timeline_name = TimelineArray[i];
        //    LoadTimeline(timeline_name);
        //}
    }

    void LoadTimeline(string clip)
    {
        PlayableAsset asset = ResManager.LoadTimeline($"Timeline/{clip}");
        GameObject vcam = ResManager.LoadPrefab($"Timeline/vcam_{clip}");
        GameObject obj = Instantiate(vcam, this.transform);
        obj.name = $"vcam_{clip}";
        PlayableDirector director = obj.GetComponent<PlayableDirector>();
        director.playableAsset = asset;
        timelines.Add(clip, director);
    }

    public PlayableDirector BindTimeline(string clip, GameObject avatar)
    {
        PlayableDirector director = timelines[clip];
        Debug.Log($"{director.time} / {director.duration}");

        foreach (PlayableBinding output in director.playableAsset.outputs)
        {
            //Debug.Log($"{output.streamName}");
            if (output.streamName == AnimationTrackName)
            {
                director.SetGenericBinding(output.sourceObject, avatar);
            }
            else if (output.streamName == CinemachineTrackName)
            {
                director.SetGenericBinding(output.sourceObject, Camera.main.gameObject);
            }
        }
        return director;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            //BindTimeline("Opening", characters[0].gameObject);
            PlayableDirector current = timelines[TimelineArray[0]];
            current.Play();
        }
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            PlayableDirector current = timelines[TimelineArray[1]];
            current.Play();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            PlayableDirector current = timelines[TimelineArray[2]];
            current.Play();
        }
    }
}