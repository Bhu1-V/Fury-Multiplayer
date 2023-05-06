using Fury.Weapons;
using UnityEngine;
using UnityEngine.UI;

public class WeaponSelectHandler : MonoBehaviour {
    [SerializeField]
    PrimaryWeaponType currentSelectedGunType;

    // Start is called before the first frame update
    void Start() {
        currentSelectedGunType = (PrimaryWeaponType)PlayerPrefs.GetInt(WeaponNameConstants.CURRENT_PRIMARY_WEAPON_TYPE, 0);
        GetComponent<ToggleGroup>().transform.GetChild((int)currentSelectedGunType).GetComponent<Toggle>().isOn = true;
    }

    public void OnGunToggleSelected(int gunType) {
        currentSelectedGunType = (PrimaryWeaponType)gunType;
        PlayerPrefs.SetInt(WeaponNameConstants.CURRENT_PRIMARY_WEAPON_TYPE, (int)currentSelectedGunType);
    }
}
