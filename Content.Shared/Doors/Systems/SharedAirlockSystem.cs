using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Emag.Components;
using Content.Shared.Emag.Systems;
using Content.Shared.Popups;
using Content.Shared.MachineLinking.Events;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;

namespace Content.Shared.Doors.Systems;

public abstract class SharedAirlockSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReaderSystem = default!;
    [Dependency] protected readonly SharedAppearanceSystem Appearance = default!;
    [Dependency] protected readonly SharedContainerSystem _container = default!;
    [Dependency] protected readonly SharedAudioSystem Audio = default!;
    [Dependency] protected readonly SharedDoorSystem DoorSystem = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AirlockComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AirlockComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<AirlockComponent, ComponentHandleState>(OnHandleState);
        SubscribeLocalEvent<AirlockComponent, BeforeDoorClosedEvent>(OnBeforeDoorClosed);
        SubscribeLocalEvent<AirlockComponent, LinkAttemptEvent>(OnLinkAttempt);
    }

    private void OnStartup(EntityUid uid, AirlockComponent component, ComponentStartup args)
    {
        component.BoardContainer = _container.EnsureContainer<Container>(uid, component.BoardContainerId);
    }

    private void OnGetState(EntityUid uid, AirlockComponent airlock, ref ComponentGetState args)
    {
        // Need to network airlock safety state to avoid mis-predicts when a door auto-closes as the client walks through the door.
        args.State = new AirlockComponentState(airlock.Safety);
    }

    private void OnHandleState(EntityUid uid, AirlockComponent airlock, ref ComponentHandleState args)
    {
        if (args.Current is not AirlockComponentState state)
            return;

        airlock.Safety = state.Safety;
    }

    protected virtual void OnBeforeDoorClosed(EntityUid uid, AirlockComponent airlock, BeforeDoorClosedEvent args)
    {
        if (!airlock.Safety)
            args.PerformCollisionCheck = false;
    }
    
    private void OnLinkAttempt(EntityUid uid, AirlockComponent component, LinkAttemptEvent args)
    {
        if (args.User == null) // AutoLink (and presumably future external linkers) have no user.
            return;
        
        if(!HasComp<EmaggedComponent>(component.BoardContainer.ContainedEntities[0]) && TryComp<AccessReaderComponent>(component.BoardContainer.ContainedEntities[0], out var access))
        {
        	if(!_accessReaderSystem.IsAllowed(args.User.Value, access))
        	    args.Cancel();
        }
    }

    public void UpdateEmergencyLightStatus(EntityUid uid, AirlockComponent component)
    {
        Appearance.SetData(uid, DoorVisuals.EmergencyLights, component.EmergencyAccess);
    }

    public void ToggleEmergencyAccess(EntityUid uid, AirlockComponent component)
    {
        component.EmergencyAccess = !component.EmergencyAccess;
        UpdateEmergencyLightStatus(uid, component);
    }

    public void SetAutoCloseDelayModifier(AirlockComponent component, float value)
    {
        if (component.AutoCloseDelayModifier.Equals(value))
            return;

        component.AutoCloseDelayModifier = value;
    }

    public void SetSafety(AirlockComponent component, bool value)
    {
        component.Safety = value;
    }

    public void SetBoltWireCut(AirlockComponent component, bool value)
    {
        component.BoltWireCut = value;
    }
}
