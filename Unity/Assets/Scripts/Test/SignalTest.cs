using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Timeline;
using UnityEngine.Playables;

public class IdolEvent : UnityEvent
{
    public void DLog()
    {
        Debug.Log("aa");
    }
}

public class SignalTest : MonoBehaviour
{
    SignalReceiver receiver;
    public SignalAsset asset;
    public IdolEvent reaction;

    void Start()
    {
        reaction = new IdolEvent();
        reaction.AddListener(() => { reaction.DLog(); });

        receiver = GetComponent<SignalReceiver>();
        receiver.AddReaction(asset, reaction);
    }

    public void Print()
    {
        Debug.Log("signal");
    }
}