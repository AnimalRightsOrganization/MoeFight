using UnityEngine;

public class JController : MonoBehaviour
{
    public EasyJoystick.Joystick joystick;
    public float threshold = 0.25f;
    public Vector2 output;

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