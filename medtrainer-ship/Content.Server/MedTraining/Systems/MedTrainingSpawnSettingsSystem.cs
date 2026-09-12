using Content.Server.MedTraining.Components;
using Content.Shared.MedTraining;
using Content.Shared.MedTraining.UI;
using Robust.Shared.GameObjects;
using Robust.Shared.Random;

namespace Content.Server.MedTraining.Systems;

public sealed partial class MedTrainingSpawnSettingsSystem : EntitySystem
{
    [Dependency] private SharedUserInterfaceSystem _ui = default!;
    [Dependency] private MedTrainingSystem _medTraining = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MedTrainingSettingsButtonComponent, BoundUIOpenedEvent>(OnUiOpened);
        SubscribeLocalEvent<MedTrainingSettingsButtonComponent, MedTrainingSetDamageTypeSeverityMessage>(OnSetDamageTypeSeverity);
        SubscribeLocalEvent<MedTrainingSettingsButtonComponent, MedTrainingSetSpeciesEnabledMessage>(OnSetSpeciesEnabled);
        SubscribeLocalEvent<MedTrainingSettingsButtonComponent, MedTrainingSetUltraModeMessage>(OnSetUltraMode);
        SubscribeLocalEvent<MedTrainingSettingsButtonComponent, MedTrainingDespawnPatientsMessage>(OnDespawnPatients);
        SubscribeLocalEvent<MedTrainingSettingsButtonComponent, MedTrainingRandomizeDamageSlidersMessage>(OnRandomizeDamageSliders);
    }

    private void OnUiOpened(EntityUid uid, MedTrainingSettingsButtonComponent button, BoundUIOpenedEvent args)
    {
        PushState(uid);
    }

    private void OnSetDamageTypeSeverity(EntityUid uid, MedTrainingSettingsButtonComponent button, MedTrainingSetDamageTypeSeverityMessage args)
    {
        if (!TryFindSettings(uid, out var settings))
            return;

        if (!settings.DamageTypeSeverity.ContainsKey(args.DamageType))
            return;

        settings.DamageTypeSeverity[args.DamageType] = Math.Clamp(args.Value, 0, 10);
        PushState(uid);
    }

    private void OnSetSpeciesEnabled(EntityUid uid, MedTrainingSettingsButtonComponent button, MedTrainingSetSpeciesEnabledMessage args)
    {
        if (!TryFindSettings(uid, out var settings))
            return;

        if (!settings.SpeciesEnabled.ContainsKey(args.Species))
            return;

        settings.SpeciesEnabled[args.Species] = args.Enabled;
        PushState(uid);
    }

    private void OnSetUltraMode(EntityUid uid, MedTrainingSettingsButtonComponent button, MedTrainingSetUltraModeMessage args)
    {
        if (!TryFindSettings(uid, out var settings))
            return;

        if (!settings.UltraMode.ContainsKey(args.DamageType))
            return;

        settings.UltraMode[args.DamageType] = args.Enabled;
        PushState(uid);
    }

    private void OnDespawnPatients(EntityUid uid, MedTrainingSettingsButtonComponent button, MedTrainingDespawnPatientsMessage args)
    {
        var gridUid = Transform(uid).GridUid;
        _medTraining.ForceDespawnPatients(gridUid);
    }

    private void OnRandomizeDamageSliders(EntityUid uid, MedTrainingSettingsButtonComponent button, MedTrainingRandomizeDamageSlidersMessage args)
    {
        if (!TryFindSettings(uid, out var settings))
            return;

        foreach (var type in MedTrainingDamageTypes.All)
        {
            settings.DamageTypeSeverity[type] = _random.Next(0, 11);
        }

        PushState(uid);
    }

    // rebuilds and sends the full settings UI state to the client
    private void PushState(EntityUid buttonUid)
    {
        if (!TryFindSettings(buttonUid, out var settings))
            return;

        _ui.SetUiState(buttonUid, MedTrainingSpawnSettingsUiKey.Key,
            new MedTrainingSpawnSettingsBoundUserInterfaceState(
                new Dictionary<string, int>(settings.DamageTypeSeverity),
                new Dictionary<string, bool>(settings.SpeciesEnabled),
                new Dictionary<string, bool>(settings.UltraMode)));
    }

    // finds the spawn-settings component on the same grid as this button
    public bool TryFindSettings(EntityUid buttonUid, out MedTrainingSpawnSettingsComponent settings)
    {
        settings = default!;
        var gridUid = Transform(buttonUid).GridUid;
        var query = EntityQueryEnumerator<MedTrainingSpawnSettingsComponent, TransformComponent>();
        while (query.MoveNext(out _, out var comp, out var xform))
        {
            if (xform.GridUid == gridUid)
            {
                settings = comp;
                return true;
            }
        }

        return false;
    }
}
