using FishNet.Managing.Timing;
using UnityEngine;

namespace Fury.Weapons {

    public struct ThrownInstantiateEventArgs {
        public ThrownInstantiateEventArgs(Weapon weapon, PreciseTick pt, Vector3 position, Vector3 direction, float force, bool serverOnly, GameObject prefab, UserData thrownBy) {
            Weapon = weapon;
            PreciseTick = pt;
            Position = position;
            Direction = direction;
            Force = force;
            ServerOnly = serverOnly;
            Prefab = prefab;
            ThrownBy = thrownBy;
        }

        public readonly Weapon Weapon;
        public readonly PreciseTick PreciseTick;
        public readonly Vector3 Position;
        public readonly Vector3 Direction;
        public readonly float Force;
        public readonly bool ServerOnly;
        public readonly UserData ThrownBy;
        public GameObject Prefab;
    }

}
