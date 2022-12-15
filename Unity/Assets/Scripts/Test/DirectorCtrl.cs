using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class DirectorCtrl : MonoBehaviour
{
    public PlayableDirector director;
    public KeyCode KeyPlay = KeyCode.F1;
    public KeyCode KeyStop = KeyCode.F2;

    void Play()
    {
        director.Play();
    }

    void Stop()
    {
        director.Stop();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyPlay))
        {
            Play();
        }
        if (Input.GetKeyDown(KeyStop))
        {
            Stop();
        }
    }
}