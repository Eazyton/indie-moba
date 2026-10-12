using System;
using UnityEngine;

namespace IndieMoba.Jungle
{
    [Serializable]
    public sealed class JungleSetupEntry
    {
        [Tooltip("MapZoneMarker id in Map5v5_Gameplay, e.g. Clearing_BlueUpper_Buff.")]
        public string zoneId;
        public JungleCampConfig camp;
    }

    // Which camp goes in which clearing. Read by the editor wiring menu only; clearings that are not
    // listed (the optional clearings) stay empty.
    [CreateAssetMenu(menuName = "IndieMoba/Jungle/Jungle Setup", fileName = "JungleSetup")]
    public sealed class JungleSetup : ScriptableObject
    {
        [SerializeField] private JungleSetupEntry[] entries = Array.Empty<JungleSetupEntry>();

        public JungleSetupEntry[] Entries => entries;
    }
}
