using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class TimelineControl : MonoBehaviour
{
    public Animator honoka;

    public List<PlayableDirector> timelines;
    public int currLine = 0;

    public PlayableAsset asset_HADOUKEN;
    public PlayableAsset asset_SHORYUKEN;
    public GameObject vcam_HADOUKEN;
    public GameObject vcam_SHORYUKEN;

    const string CinemachineTrackName = "Cinemachine Track";
    const string AnimationTrackName = "Animation Track";

    void Awake()
    {
        honoka = GetComponent<Animator>();

        asset_HADOUKEN = ResManager.LoadTimeline("Timeline/HADOUKEN") as PlayableAsset;
        asset_SHORYUKEN = ResManager.LoadTimeline("Timeline/SHORYUKEN") as PlayableAsset;

        vcam_HADOUKEN = ResManager.LoadPrefab("Timeline/vcam_HADOUKEN");
        GameObject obj_HADOUKEN = Instantiate(vcam_HADOUKEN);
        obj_HADOUKEN.name = "vcam_HADOUKEN";
        PlayableDirector direct_HADOUKEN = obj_HADOUKEN.GetComponent<PlayableDirector>();
        timelines.Add(direct_HADOUKEN);
        direct_HADOUKEN.playableAsset = asset_HADOUKEN;
        foreach (var playableAssetOutput in direct_HADOUKEN.playableAsset.outputs)
        {
            Debug.Log($"{playableAssetOutput.streamName}");
            if (playableAssetOutput.streamName == AnimationTrackName)
            {
                direct_HADOUKEN.SetGenericBinding(playableAssetOutput.sourceObject, honoka.gameObject);
            }
            else if (playableAssetOutput.streamName == CinemachineTrackName)
            {
                direct_HADOUKEN.SetGenericBinding(playableAssetOutput.sourceObject, Camera.main.gameObject);
            }
        }

        vcam_SHORYUKEN = ResManager.LoadPrefab("Timeline/vcam_SHORYUKEN");
        GameObject obj_SHORYUKEN = Instantiate(vcam_SHORYUKEN);
        obj_SHORYUKEN.name = "vcam_SHORYUKEN";
        PlayableDirector direct_SHORYUKEN = obj_SHORYUKEN.GetComponent<PlayableDirector>();
        timelines.Add(direct_SHORYUKEN);
        direct_SHORYUKEN.playableAsset = asset_SHORYUKEN;
        foreach (var playableAssetOutput in direct_SHORYUKEN.playableAsset.outputs)
        {
            Debug.Log($"{playableAssetOutput.streamName}");
            if (playableAssetOutput.streamName == AnimationTrackName)
            {
                direct_SHORYUKEN.SetGenericBinding(playableAssetOutput.sourceObject, honoka.gameObject);
            }
            else if (playableAssetOutput.streamName == CinemachineTrackName)
            {
                direct_SHORYUKEN.SetGenericBinding(playableAssetOutput.sourceObject, Camera.main.gameObject);
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.J))
        {
            PlayableDirector current = timelines[0];
            current.Play();
        }
        if (Input.GetKeyDown(KeyCode.K))
        {
            PlayableDirector current = timelines[1];
            current.Play();
        }
    }
}