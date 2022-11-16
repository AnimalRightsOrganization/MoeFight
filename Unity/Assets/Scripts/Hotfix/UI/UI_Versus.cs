using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HitstunConstants;

namespace HotFix
{
    public class UI_Versus : UIBase
    {
        [SerializeField] Animator m_Anim;
        [SerializeField] Image[] m_Heads;
        [SerializeField] Dictionary<string, Sprite> m_Poses;

        void Awake()
        {
            Transform panel = transform.Find("Panel");
            m_Anim = panel.GetComponent<Animator>();
            m_Heads = new Image[2];
            m_Heads[0] = panel.Find("Left").GetComponent<Image>();
            m_Heads[1] = panel.Find("Right").GetComponent<Image>();

            m_Poses = ResManager.LoadSprites("Sprites/Bodies.png");
        }

        public void FadeIn(int left, int right)
        {
            m_Heads[0].sprite = m_Poses[((CharacterName)left).ToString()];
            m_Heads[1].sprite = m_Poses[((CharacterName)right).ToString()];

            m_Anim.Play("FadeIn");
        }

        public void FadeOut()
        {
            m_Anim.Play("FadeOut");
        }
    }
}