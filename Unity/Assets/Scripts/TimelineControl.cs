using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class TimelineControl : MonoBehaviour
{
    public Animator honoka;

    const string CinemachineTrackName = "Cinemachine Track";
    const string AnimationTrackName = "Animation Track";

    public List<PlayableDirector> timelines;
    public int currLine = 0;

    void Awake()
    {
        honoka = GetComponent<Animator>();

        LoadTimeline("Opening");
        LoadTimeline("HADOUKEN");
        LoadTimeline("SHORYUKEN");
    }

    void LoadTimeline(string clip)
    {
        PlayableAsset asset = ResManager.LoadTimeline($"Timeline/{clip}") as PlayableAsset;
        GameObject vcam = ResManager.LoadPrefab($"Timeline/vcam_{clip}");
        GameObject obj = Instantiate(vcam);
        obj.name = $"vcam_{clip}";
        PlayableDirector director = obj.GetComponent<PlayableDirector>();
        timelines.Add(director);
        director.playableAsset = asset;
        foreach (var output in director.playableAsset.outputs)
        {
            //Debug.Log($"{output.streamName}");
            if (output.streamName == AnimationTrackName)
            {
                director.SetGenericBinding(output.sourceObject, honoka.gameObject);
            }
            else if (output.streamName == CinemachineTrackName)
            {
                director.SetGenericBinding(output.sourceObject, Camera.main.gameObject);
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            PlayableDirector current = timelines[0];
            current.Play();
        }
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            PlayableDirector current = timelines[1];
            current.Play();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            PlayableDirector current = timelines[2];
            current.Play();
        }
    }
}