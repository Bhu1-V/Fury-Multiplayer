using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInfoEntry : MonoBehaviour {
    [SerializeField]
    Color redBgColor;

    [SerializeField]
    Color blueBgColor;

    [SerializeField]
    TextMeshProUGUI userNameText;

    [SerializeField]
    TextMeshProUGUI xpText;

    public UserData.Team _team;
    public UserData.Team Team {
        set {
            Initalize(value == UserData.Team.Red, userName: userNameText.text, xp: uint.Parse(xpText.text));
        }
    }

    public void Initalize(bool isRed, string userName, uint xp) {
        if(isRed) {
            new List<Image>(GetComponentsInChildren<Image>()).ForEach(x => x.color = redBgColor);
        } else {
            new List<Image>(GetComponentsInChildren<Image>()).ForEach(x => x.color = blueBgColor);
        }

        userNameText.text = userName;
        xpText.text = xp.ToString();
    }

    private void Update() {
        RectTransform thisRect = GetComponent<RectTransform>();
        RectTransform parentRect = transform.parent.parent.GetComponent<RectTransform>();

        if(thisRect != null && parentRect != null) {
            thisRect.sizeDelta = new Vector2(thisRect.sizeDelta.x, parentRect.sizeDelta.y / 5f);
        }
    }

    public void SetUserName(string userName) {
        userNameText.text = userName;
    }

    public void SetXp(uint xp) {
        Debug.Log($"setting {userNameText.text} score to {xp}");
        xpText.text = xp.ToString();
    }
}
