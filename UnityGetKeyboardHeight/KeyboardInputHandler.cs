using UnityEngine;

public class KeyboardInputHandler : MonoBehaviour
{
    private TouchScreenKeyboard keyboard;

    private void Update()
    {
        if (TouchScreenKeyboard.visible && keyboard == null)
        {
            // 键盘已弹起
            keyboard = TouchScreenKeyboard.Open("", TouchScreenKeyboardType.Default);
            keyboard.active = true;
        }

        if (keyboard != null && !keyboard.visible)
        {
            // 键盘已关闭
            Debug.Log("Keyboard closed.");
            keyboard = null;
        }
    }
}
