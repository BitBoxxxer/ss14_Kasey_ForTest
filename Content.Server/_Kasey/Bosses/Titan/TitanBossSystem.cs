using System.Linq;
using System.Numerics;
using Content.Shared.Bosses.Titan;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Audio.Systems;

namespace Content.Server.Bosses.Titan;

public sealed partial class TitanBossSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    // Внутреннее состояние тиков
    private readonly Dictionary<EntityUid, TimeSpan> _nextAiTick = new();
    private readonly Dictionary<EntityUid, TimeSpan> _nextAttackTime = new();

    // Очередь отложенных действий (вместо Timer.Spawn)
    private readonly List<(TimeSpan When, Action Act)> _delayedActions = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TitanBossComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<TitanBossComponent, InteractHandEvent>(OnInteract);
        SubscribeLocalEvent<TitanBossComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnStartup(Entity<TitanBossComponent> ent, ref ComponentStartup args)
    {
        _nextAiTick[ent] = _timing.CurTime;
        _nextAttackTime[ent] = _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.AttackInterval);
    }

    private void OnInteract(Entity<TitanBossComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled || ent.Comp.Activated)
            return;

        ent.Comp.Activated = true;
        Dirty(ent);

        ActivateArena(ent);

        args.Handled = true;
    }

    private void OnMobStateChanged(Entity<TitanBossComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
        {
            DeactivateArena(ent);
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var now = _timing.CurTime;

        // Обработка отложенных действий
        ProcessDelayedActions(now);

        var query = EntityQueryEnumerator<TitanBossComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var boss, out var xform))
        {
            if (!boss.Activated)
                continue;

            if (!_nextAiTick.TryGetValue(uid, out var aiTime) || now < aiTime)
                continue;

            _nextAiTick[uid] = now + TimeSpan.FromSeconds(0.25);

            var target = FindTarget(uid, boss, xform);
            UpdateAim(uid, ref boss, target, xform);
            UpdatePhase(uid, ref boss);

            if (_nextAttackTime.TryGetValue(uid, out var nextAtk) && now >= nextAtk)
            {
                var next = PickNextAttack(boss);
                boss.CurrentAttack = next;
                Dirty(uid, boss);

                _nextAttackTime[uid] = now + TimeSpan.FromSeconds(GetAttackIntervalForPhase(boss));

                switch (next)
                {
                    case TitanAttack.Spikes:
                        DoSpikesAttack(uid, boss, target);
                        break;
                    case TitanAttack.Laser:
                        DoLaserAttack(uid, boss, target);
                        break;
                    case TitanAttack.HandSlam:
                        DoHandSlam(uid, boss, target);
                        break;
                }
            }
        }
    }

    private void ProcessDelayedActions(TimeSpan now)
    {
        for (int i = _delayedActions.Count - 1; i >= 0; i--)
        {
            if (now >= _delayedActions[i].When)
            {
                try
                {
                    _delayedActions[i].Act.Invoke();
                }
                catch (Exception ex)
                {
                    Log.Error("TitanBoss delayed action error: {ex}");
                }
                _delayedActions.RemoveAt(i);
            }
        }
    }

    private void DelayedAction(TimeSpan delay, Action action)
    {
        _delayedActions.Add((_timing.CurTime + delay, action));
    }

    private float GetAttackIntervalForPhase(TitanBossComponent boss)
    {
        return boss.Phase switch
        {
            TitanPhase.Phase1 => boss.AttackInterval,
            TitanPhase.Phase2 => MathF.Max(1.8f, boss.AttackInterval * 0.75f),
            TitanPhase.Phase3 => MathF.Max(1.2f, boss.AttackInterval * 0.5f),
            _ => boss.AttackInterval
        };
    }

    private TitanAttack PickNextAttack(TitanBossComponent boss)
    {
        var wSpikes = boss.Phase == TitanPhase.Phase1 ? 0.5f : 0.35f;
        var wLaser = boss.Phase == TitanPhase.Phase1 ? 0.3f : 0.4f;

        var roll = _random.NextFloat();
        if (roll < wSpikes) return TitanAttack.Spikes;
        if (roll < wSpikes + wLaser) return TitanAttack.Laser;
        return TitanAttack.HandSlam;
    }

    private EntityUid? FindTarget(EntityUid bossUid, TitanBossComponent boss, TransformComponent bossXform)
    {
        var radius = boss.ArenaRadius + 5f;
        EntityUid? best = null;
        var bestDist2 = float.MaxValue;

        var candidates = EntityQueryEnumerator<MobStateComponent, TransformComponent>();
        while (candidates.MoveNext(out var uid, out var mob, out var xform))
        {
            if (uid == bossUid)
                continue;

            if (mob.CurrentState == MobState.Dead)
                continue;

            if (xform.MapID != bossXform.MapID)
                continue;

            var delta = xform.WorldPosition - bossXform.WorldPosition;
            var dist2 = delta.X * delta.X + delta.Y * delta.Y;

            if (dist2 < bestDist2 && dist2 <= radius * radius)
            {
                best = uid;
                bestDist2 = dist2;
            }
        }

        return best;
    }

    private void UpdateAim(EntityUid uid, ref TitanBossComponent boss, EntityUid? target, TransformComponent bossXform)
    {
        if (target == null)
            return;

        var tx = Transform(target.Value);
        if (tx.MapID != bossXform.MapID)
            return;

        var dir = tx.WorldPosition - bossXform.WorldPosition;
        var lenSq = dir.X * dir.X + dir.Y * dir.Y;

        if (lenSq > 0.001f)
        {
            boss.AimDir = new Angle(MathF.Atan2(dir.Y, dir.X));
            Dirty(uid, boss);
        }
    }

    private void UpdatePhase(EntityUid uid, ref TitanBossComponent boss)
    {
        if (!TryComp(uid, out DamageableComponent? dmg))
            return;

        // Получаем общий урон как float
        var total = (float) dmg.TotalDamage;
        var max = 300f; // Можно вынести в компонент
        var hpLeft = MathF.Max(0f, max - total);
        var hpPct = hpLeft / max;

        var newPhase = boss.Phase;
        if (hpPct <= 0.25f)
            newPhase = TitanPhase.Phase3;
        else if (hpPct <= 0.6f)
            newPhase = TitanPhase.Phase2;
        else
            newPhase = TitanPhase.Phase1;

        if (newPhase != boss.Phase)
        {
            boss.Phase = newPhase;
            boss.Enraged = newPhase == TitanPhase.Phase3;
            Dirty(uid, boss);
        }
    }

    // ========== АРЕНА ==========

    private readonly Dictionary<EntityUid, List<EntityUid>> _arenaSegments = new();

    private void ActivateArena(Entity<TitanBossComponent> ent)
    {
        DeactivateArena(ent);

        var list = new List<EntityUid>();
        var center = Transform(ent).WorldPosition;
        var segs = ent.Comp.ArenaSegments;
        var radius = ent.Comp.ArenaRadius;
        var mapId = Transform(ent).MapID;

        for (int i = 0; i < segs; i++)
        {
            var t = (float)i / segs * MathF.Tau;
            var offset = new Vector2(MathF.Cos(t), MathF.Sin(t)) * radius;
            var pos = center + offset;

            var coords = new MapCoordinates(pos, mapId);
            var seg = Spawn(ent.Comp.ArenaBarrierPrototype, coords);
            list.Add(seg);

            if (TryComp(seg, out TransformComponent? sx))
                _xform.AnchorEntity(seg, sx);
        }

        _arenaSegments[ent] = list;
    }

    private void DeactivateArena(Entity<TitanBossComponent> ent)
    {
        if (!_arenaSegments.TryGetValue(ent, out var list))
            return;

        foreach (var seg in list)
        {
            if (Deleted(seg))
                continue;
            Del(seg);
        }

        _arenaSegments.Remove(ent);
    }

    // ========== АТАКИ ==========

    private void DoSpikesAttack(EntityUid uid, TitanBossComponent boss, EntityUid? target)
    {
        var bossXform = Transform(uid);
        var mapId = bossXform.MapID;
        var points = new List<Vector2>();

        if (target != null && !Deleted(target.Value))
        {
            var tx = Transform(target.Value);
            var tgt = tx.WorldPosition;

            points.Add(tgt);

            var rings = boss.Phase == TitanPhase.Phase1 ? 1 : (boss.Phase == TitanPhase.Phase2 ? 2 : 3);
            for (int r = 1; r <= rings; r++)
            {
                var count = 6 + 2 * r;
                var rad = r * 1.5f;
                for (int i = 0; i < count; i++)
                {
                    var a = (float)i / count * MathF.Tau + _random.NextFloat(-0.2f, 0.2f);
                    var offset = new Vector2(MathF.Cos(a), MathF.Sin(a)) * rad;
                    points.Add(tgt + offset);
                }
            }
        }
        else
        {
            var center = bossXform.WorldPosition;
            for (int i = 0; i < 10; i++)
            {
                var randOffset = new Vector2(
                    _random.NextFloat(-1f, 1f) * (boss.ArenaRadius - 1f),
                    _random.NextFloat(-1f, 1f) * (boss.ArenaRadius - 1f)
                );
                points.Add(center + randOffset);
            }
        }

        // Телеграфы
        foreach (var p in points)
        {
            Spawn("TitanTelegraphCircle", new MapCoordinates(p, mapId));
        }

        // Копируем данные для замыкания
        var pointsCopy = points.ToList();
        var spikeRadius = boss.SpikeRadius;
        var spikeDamage = boss.SpikeDamage;
        var sourceUid = uid;
        var sourceMapId = mapId;

        DelayedAction(TimeSpan.FromSeconds(boss.SpikeDelay), () =>
        {
            if (Deleted(sourceUid))
                return;

            foreach (var p in pointsCopy)
            {
                Spawn("TitanSpikeEffect", new MapCoordinates(p, sourceMapId));
                DoRadialDamage(sourceUid, p, spikeRadius, spikeDamage, "Piercing");
            }
        });
    }

    private void DoLaserAttack(EntityUid uid, TitanBossComponent boss, EntityUid? target)
    {
        var bossXform = Transform(uid);
        var start = bossXform.WorldPosition;
        var mapId = bossXform.MapID;

        // Направление к цели
        var aimAngle = boss.AimDir;
        var dir = new Vector2(MathF.Cos((float)aimAngle.Theta), MathF.Sin((float)aimAngle.Theta));

        // Телеграф
        Spawn("TitanTelegraphLine", bossXform.Coordinates);

        var laserDamage = boss.LaserDamage;
        var arenaRadius = boss.ArenaRadius;
        var sourceUid = uid;

        DelayedAction(TimeSpan.FromSeconds(boss.LaserWindup), () =>
        {
            if (Deleted(sourceUid))
                return;

            var currentXform = Transform(sourceUid);
            Spawn("TitanLaserBeamEffect", currentXform.Coordinates);

            // Простой урон по линии: ищем всех в конусе/полосе
            DoLineDamage(sourceUid, currentXform.WorldPosition, dir, arenaRadius * 2f, 1.0f, laserDamage, "Heat");
        });
    }

    private void DoHandSlam(EntityUid uid, TitanBossComponent boss, EntityUid? target)
    {
        var bossXform = Transform(uid);
        var mapId = bossXform.MapID;
        Vector2 slamPos;

        if (target != null && !Deleted(target.Value))
        {
            var tx = Transform(target.Value);
            var randOffset = new Vector2(
                _random.NextFloat(-0.4f, 0.4f),
                _random.NextFloat(-0.4f, 0.4f)
            );
            slamPos = tx.WorldPosition + randOffset;
        }
        else
        {
            var randOffset = new Vector2(
                _random.NextFloat(-3f, 3f),
                _random.NextFloat(-3f, 3f)
            );
            slamPos = bossXform.WorldPosition + randOffset;
        }

        Spawn("TitanTelegraphCircleStrong", new MapCoordinates(slamPos, mapId));

        var slamRadius = boss.HandSlamRadius;
        var slamDamage = boss.HandSlamDamage;
        var sourceUid = uid;
        var sourceMapId = mapId;

        DelayedAction(TimeSpan.FromSeconds(boss.HandSlamDelay), () =>
        {
            if (Deleted(sourceUid))
                return;

            Spawn("TitanSlamImpact", new MapCoordinates(slamPos, sourceMapId));
            DoRadialDamage(sourceUid, slamPos, slamRadius, slamDamage, "Blunt");
        });
    }

    // ========== УРОН ==========

    private void DoRadialDamage(EntityUid source, Vector2 center, float radius, float damage, string damageType)
    {
        if (Deleted(source))
            return;

        var mapId = Transform(source).MapID;
        var radiusSq = radius * radius;

        var toDamage = new List<EntityUid>();

        var query = EntityQueryEnumerator<DamageableComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var dmg, out var xform))
        {
            if (uid == source)
                continue;

            if (xform.MapID != mapId)
                continue;

            var delta = xform.WorldPosition - center;
            var distSq = delta.X * delta.X + delta.Y * delta.Y;

            if (distSq <= radiusSq)
                toDamage.Add(uid);
        }

        var damageSpec = new DamageSpecifier();
        damageSpec.DamageDict[damageType] = FixedPoint2.New(damage);

        foreach (var uid in toDamage)
        {
            _damageable.TryChangeDamage(uid, damageSpec);
        }
    }

    private void DoLineDamage(EntityUid source, Vector2 start, Vector2 dir, float maxDist, float width, float damage, string damageType)
    {
        if (Deleted(source))
            return;

        var mapId = Transform(source).MapID;
        var halfWidth = width / 2f;

        var toDamage = new HashSet<EntityUid>();

        var query = EntityQueryEnumerator<DamageableComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var dmg, out var xform))
        {
            if (uid == source)
                continue;

            if (xform.MapID != mapId)
                continue;

            var pos = xform.WorldPosition;
            var toTarget = pos - start;

            // Проекция на направление луча
            var projection = toTarget.X * dir.X + toTarget.Y * dir.Y;

            if (projection < 0 || projection > maxDist)
                continue;

            // Расстояние до линии
            var closestPoint = start + dir * projection;
            var delta = pos - closestPoint;
            var distToLine = MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y);

            if (distToLine <= halfWidth)
                toDamage.Add(uid);
        }

        var damageSpec = new DamageSpecifier();
        damageSpec.DamageDict[damageType] = FixedPoint2.New(damage);

        foreach (var uid in toDamage)
        {
            _damageable.TryChangeDamage(uid, damageSpec);
        }
    }
}
