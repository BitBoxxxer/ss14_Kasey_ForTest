using Content.Shared.Bosses.Titan;
using Robust.Client.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Maths;

namespace Content.Client.Bosses.Titan;

public sealed class TitanBossVisualsSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TitanBossComponent, ComponentHandleState>(OnBossState);
        SubscribeLocalEvent<TitanBossComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<TitanBossComponent> ent, ref ComponentStartup args)
    {
        UpdateEyeRotation(ent);
    }

    private void OnBossState(Entity<TitanBossComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not TitanBossComponentState state)
            return;

        // Обновляем данные компонента из состояния сети
        ent.Comp.Activated = state.Activated;
        ent.Comp.Phase = state.Phase;
        ent.Comp.Enraged = state.Enraged;
        ent.Comp.AimDir = state.AimDir;
        ent.Comp.CurrentAttack = state.CurrentAttack;

        UpdateEyeRotation(ent);
        // тут можно дергать эффекты под текущую атаку ent.Comp.CurrentAttack
    }

    private void UpdateEyeRotation(Entity<TitanBossComponent> ent)
    {
        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        // Допустим, у спрайта есть слой "eyes" (map: eyes)
        if (sprite.LayerMapTryGet("eyes", out var layer))
        {
            sprite.LayerSetRotation(layer, ent.Comp.AimDir);
        }
    }
}
