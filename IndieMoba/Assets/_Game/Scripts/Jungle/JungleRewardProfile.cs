using UnityEngine;
using IndieMoba.Combat;

namespace IndieMoba.Jungle
{
    // Rewards granted by a camp. Phase 5B only grants the carrier buff to the living hero who lands
    // the killing blow on the carrier. Gold/XP are out of scope; future economy systems should listen
    // to JungleDirector.MonsterKilled / CampCleared instead of extending this asset ad hoc.
    [CreateAssetMenu(menuName = "IndieMoba/Jungle/Reward Profile", fileName = "JungleRewardProfile")]
    public sealed class JungleRewardProfile : ScriptableObject
    {
        [SerializeField] private StatusEffectDefinition carrierBuff;

        public StatusEffectDefinition CarrierBuff => carrierBuff;
    }
}
