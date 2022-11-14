using UnityEngine;

namespace HotFix
{
    public class UI_Versus : UIBase
    {
        [SerializeField] RectTransform m_Panel;
        [SerializeField] Animator m_Anim;

        void Awake()
        {
            m_Panel = transform.Find("Panel").GetComponent<RectTransform>();
            m_Anim = m_Panel.GetComponent<Animator>();
        }

        public void Idle()
        {
            m_Anim.Play("Idle");
        }

        public void Play()
        {
            m_Anim.Play("Play");
        }
    }
}