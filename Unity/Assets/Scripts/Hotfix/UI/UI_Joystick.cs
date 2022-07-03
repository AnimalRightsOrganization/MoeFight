using UnityEngine;
using UnityEngine.UI;
using EasyJoystick;

namespace HotFix
{
    public class UI_Joystick : UIBase
    {
        private Joystick joystick;
        private float threshold = 0.25f;
        public Vector2 output;

        public Button m_BtnA;
        public Button m_BtnB;
        public Button m_BtnC;
        public Button m_BtnX;
        public Button m_BtnY;
        public Button m_BtnZ;

        void Awake()
        {
            var joystickTrans = transform.Find("Joystick");
            joystickTrans.gameObject.AddComponent<Joystick>();
            joystick = joystickTrans.GetComponent<Joystick>();

            m_BtnA = transform.Find("Buttons/A").GetComponent<Button>();
            m_BtnB = transform.Find("Buttons/B").GetComponent<Button>();
            m_BtnC = transform.Find("Buttons/C").GetComponent<Button>();
            m_BtnX = transform.Find("Buttons/X").GetComponent<Button>();
            m_BtnY = transform.Find("Buttons/Y").GetComponent<Button>();
            m_BtnZ = transform.Find("Buttons/Z").GetComponent<Button>();
        }

        void Update()
        {
            float xMovement = joystick.Horizontal();
            float zMovement = joystick.Vertical();
            Vector2 input = new Vector2(xMovement, zMovement);

            var x = Mathf.Abs(input.x) <= threshold ? 0 : input.x > 0 ? 1f : -1f;
            var y = Mathf.Abs(input.y) <= threshold ? 0 : input.y > 0 ? 1f : -1f;
            output = new Vector2(x, y);
        }
    }
}