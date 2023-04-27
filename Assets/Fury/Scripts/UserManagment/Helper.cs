using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class Helper : MonoBehaviour {
    public static Camera Camera;


    private static PointerEventData _eventDataCurrentPosition;
    private static List<RaycastResult> _results;

    public static bool IsOverUI() {
        _eventDataCurrentPosition = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        _results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(_eventDataCurrentPosition, _results);
        return _results.Count > 0;
    }

    public static void LockMouse() {
        // Lock Cursor if it isn't Locked
        if(Cursor.lockState != CursorLockMode.Locked) {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private static readonly string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

    public static string GenerateRandomString(int length) {
        char[] stringChars = new char[length];
        for(int i = 0; i < length; i++) {
            stringChars[i] = chars[Random.Range(0, chars.Length)];
        }
        return new string(stringChars);
    }

    public static Color[] colorList = new Color[] { Color.white, Color.black, Color.gray, Color.red, Color.green, Color.blue, Color.yellow, Color.cyan, Color.magenta };

    public static Color GetRandomColor(List<Color> ignoreColors = null) {
        int index = Random.Range(0, colorList.Length);
        while(ignoreColors != null && ignoreColors.Contains(colorList[index])) {
            index = Random.Range(0, colorList.Length);
        }
        return colorList[index];
    }

}
