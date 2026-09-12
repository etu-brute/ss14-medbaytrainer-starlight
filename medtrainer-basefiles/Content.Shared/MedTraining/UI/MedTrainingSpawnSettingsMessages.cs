using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.MedTraining.UI;

[Serializable, NetSerializable]
public sealed class MedTrainingSetDamageTypeSeverityMessage : BoundUserInterfaceMessage
{
    public string DamageType;
    public int Value;

    public MedTrainingSetDamageTypeSeverityMessage(string damageType, int value)
    {
        DamageType = damageType;
        Value = value;
    }
}

[Serializable, NetSerializable]
public sealed class MedTrainingSetSpeciesEnabledMessage : BoundUserInterfaceMessage
{
    public string Species;
    public bool Enabled;

    public MedTrainingSetSpeciesEnabledMessage(string species, bool enabled)
    {
        Species = species;
        Enabled = enabled;
    }
}

[Serializable, NetSerializable]
public sealed class MedTrainingSetUltraModeMessage : BoundUserInterfaceMessage
{
    public string DamageType;
    public bool Enabled;

    public MedTrainingSetUltraModeMessage(string damageType, bool enabled)
    {
        DamageType = damageType;
        Enabled = enabled;
    }
}

[Serializable, NetSerializable]
public sealed class MedTrainingDespawnPatientsMessage : BoundUserInterfaceMessage
{
}

[Serializable, NetSerializable]
public sealed class MedTrainingRandomizeDamageSlidersMessage : BoundUserInterfaceMessage
{
}
