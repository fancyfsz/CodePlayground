public static int GetRelativeKeyboardHeight(RectTransform rectTransform, bool includeInput)
{
    int keyboardHeight = GetKeyboardHeight(includeInput);
    float screenToRectRatio = Screen.height / rectTransform.rect.height;
    float keyboardHeightRelativeToRect = keyboardHeight / screenToRectRatio;
 
    return (int) keyboardHeightRelativeToRect;
}
 
private static int GetKeyboardHeight(bool includeInput)
{
#if UNITY_EDITOR
        return 0;
#elif UNITY_ANDROID
        using (AndroidJavaClass unityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            AndroidJavaObject unityPlayer = unityClass.GetStatic<AndroidJavaObject>("currentActivity").Get<AndroidJavaObject>("mUnityPlayer");
            AndroidJavaObject view = unityPlayer.Call<AndroidJavaObject>("getView");
            AndroidJavaObject dialog = unityPlayer.Get<AndroidJavaObject>("mSoftInputDialog");
            if (view == null || dialog == null)
                return 0;
            var decorHeight = 0;
            if (includeInput)
            {
                AndroidJavaObject decorView = dialog.Call<AndroidJavaObject>("getWindow").Call<AndroidJavaObject>("getDecorView");
                if (decorView != null)
                    decorHeight = decorView.Call<int>("getHeight");
            }
            using (AndroidJavaObject rect = new AndroidJavaObject("android.graphics.Rect"))
            {
                view.Call("getWindowVisibleDisplayFrame", rect);
                return Screen.height - rect.Call<int>("height") + decorHeight;
            }
        }
#elif UNITY_IOS
        return (int)TouchScreenKeyboard.area.height;
#endif
    return 0;
}