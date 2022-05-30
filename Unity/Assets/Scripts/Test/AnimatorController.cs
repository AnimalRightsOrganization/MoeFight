using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(AnimatorController))]
public class DemoEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); //显示默认所有参数

        AnimatorController demo = (AnimatorController)target;

        if (GUILayout.Button("下一帧"))
        {
            demo.PlayFrame();
        }

        if (GUILayout.Button("切换动画"))
        {
            demo.PlayFrame();
        }
    }
}
public class AnimatorController : MonoBehaviour
{
    public uint updateNumber;
    public uint frameNumber;

    Animator animator;
    public string stateName;
    public int stepNumber;

    void Awake()
    {
        Application.targetFrameRate = 60;
        animator = GetComponent<Animator>();
        animator.speed = 0;
    }

    /*
    void Update()
    {
        updateNumber++;
        float normal = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        int frames = Mathf.RoundToInt(animator.GetCurrentAnimatorStateInfo(0).length * 60);
        Debug.Log($"<color=green>Update: {updateNumber},,, {normal * frames % frames} / {frames}</color>");
    }

    void FixedUpdate()
    {
        frameNumber++;
        Debug.Log($"FixedUpdate: {frameNumber}");
    }
    */

    void FixedUpdate()
    {
        PlayFrame();
    }

    public void PlayFrame()
    {
        animator.speed = 1;
        stepNumber++;
        float percent = ((float)stepNumber / 70f);
        animator.Play(stateName, 0, percent);
        animator.Update(0);
        animator.speed = 0;
    }

    public void PlayState()
    {
        animator.Play(stateName);
    }
}
#endif