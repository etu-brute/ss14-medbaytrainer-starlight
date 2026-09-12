using Content.Shared.MedTraining.UI;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;

namespace Content.Client.MedTraining.UI;

public sealed class MedTrainingSpawnSettingsBoundUserInterface : BoundUserInterface
{
    private MedTrainingSpawnSettingsWindow? _window;

    public MedTrainingSpawnSettingsBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<MedTrainingSpawnSettingsWindow>();
        _window.OnDamageTypeSeverityChanged += (type, value) => SendMessage(new MedTrainingSetDamageTypeSeverityMessage(type, value));
        _window.OnUltraModeChanged += (type, enabled) => SendMessage(new MedTrainingSetUltraModeMessage(type, enabled));
        _window.OnSpeciesEnabledChanged += (species, enabled) => SendMessage(new MedTrainingSetSpeciesEnabledMessage(species, enabled));
        _window.OnDespawnPatientsRequested += () => SendMessage(new MedTrainingDespawnPatientsMessage());
        _window.OnRandomizeDamageSlidersRequested += () => SendMessage(new MedTrainingRandomizeDamageSlidersMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not MedTrainingSpawnSettingsBoundUserInterfaceState castState)
            return;

        _window?.UpdateState(castState);
    }
}
