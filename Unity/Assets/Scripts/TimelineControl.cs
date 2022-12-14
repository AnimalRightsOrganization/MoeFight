using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class TimelineControl : MonoBehaviour
{
	public List<PlayableDirector> timelines;
    public int currLine = 0;

    public GameObject vcam_HADOUKEN;
    public GameObject vcam_SHORYUKEN;
    public PlayableAsset HADOUKEN;
    public PlayableAsset SHORYUKEN;

    void Awake()
    {
        vcam_HADOUKEN = ResManager.LoadPrefab("Timeline/vcam_HADOUKEN");
        vcam_SHORYUKEN = ResManager.LoadPrefab("Timeline/vcam_SHORYUKEN");
    }

    void Start()
    {
        HADOUKEN = ResManager.LoadTimeline("Timeline/HADOUKEN") as PlayableAsset;
        SHORYUKEN = ResManager.LoadTimeline("Timeline/SHORYUKEN") as PlayableAsset;
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