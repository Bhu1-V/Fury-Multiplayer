namespace Fury.Weapons {

    public enum WeaponNames {
        Unset = 0,
        M4A1 = 1,
        Glock = 2,
        FragGrenade = 4,
        Knife = 8
        //// Primary Weapons
        //Thompson = 1,
        //Shotgun = 2,
        //Sniper = 4,
        //// Secondary Weapon
        //Glock = 8,
        //// Throwable
        //FragGrenade = 16,
        //// Meele
        //Knife = 32,
        //// Special Weapons
        //MachineGun = 64,
        //AWM = 128
    }

    public enum PrimaryWeaponType {
        None = -1,
        Thompson,
        Shotgun,
        Sniper
    }

    public class WeaponNameConstants {
        public const string CURRENT_PRIMARY_WEAPON_TYPE = "current_primary_weapon_type";
    }
}