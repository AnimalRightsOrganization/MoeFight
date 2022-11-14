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

        public void FadeIn()
        {
            m_Anim.Play("FadeIn");
        }

        public void FadeOut()
        {
            m_Anim.Play("FadeOut");
        }
    }
}