using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.MedTraining.UI;

[Serializable, NetSerializable]
public sealed class MedTrainingSpawnSettingsBoundUserInterfaceState : BoundUserInterfaceState
{
    public Dictionary<string, int> DamageTypeSeverity;
    public Dictionary<string, bool> SpeciesEnabled;
    public Dictionary<string, bool> UltraMode;

    public MedTrainingSpawnSettingsBoundUserInterfaceState(
        Dictionary<string, int> damageTypeSeverity,
        Dictionary<string, bool> speciesEnabled,
        Dictionary<string, bool> ultraMode)
    {
        DamageTypeSeverity = damageTypeSeverity;
        SpeciesEnabled = speciesEnabled;
        UltraMode = ultraMode;
    }
}
