using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.Maths;

namespace Content.Shared.Bosses.Titan;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TitanBossComponent : Component
{
    // Состояние боя
    [DataField, AutoNetworkedField]
    public bool Activated = false;

    [DataField, AutoNetworkedField]
    public TitanPhase Phase = TitanPhase.Phase1;

    [DataField, AutoNetworkedField]
    public bool Enraged = false;

    // Текущая цель (сервер хранит uid, клиенту шлём только направление)
    [DataField, AutoNetworkedField]
    public Angle AimDir = Angle.Zero;

    [DataField, AutoNetworkedField]
    public TitanAttack CurrentAttack = TitanAttack.None;

    // Параметры (настраиваются из YAML при желании)
    [DataField]
    public float ArenaRadius = 12f;

    [DataField]
    public int ArenaSegments = 32;

    [DataField]
    public string ArenaBarrierPrototype = "TitanArenaBarrierSegment";

    [DataField]
    public float SpikeDelay = 0.8f;

    [DataField]
    public float LaserWindup = 0.7f;

    [DataField]
    public float HandSlamDelay = 0.5f;

    [DataField]
    public float SpikeDamage = 20f;

    [DataField]
    public float LaserDamage = 35f;

    [DataField]
    public float HandSlamDamage = 30f;

    [DataField]
    public float SpikeRadius = 1.2f;

    [DataField]
    public float HandSlamRadius = 2.0f;

    [DataField]
    public float AttackInterval = 3.0f; // базовая пауза между атаками, меняется по фазам
}
