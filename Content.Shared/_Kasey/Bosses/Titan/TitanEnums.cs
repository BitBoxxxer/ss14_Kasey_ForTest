using Robust.Shared.Serialization;

namespace Content.Shared.Bosses.Titan;

[Serializable, NetSerializable]
public enum TitanPhase
{
    Phase1,
    Phase2,
    Phase3
}

[Serializable, NetSerializable]
public enum TitanAttack
{
    None,
    Spikes,
    Laser,
    HandSlam
}
