namespace Content.Shared.MedTraining;

public static class MedTrainingDamageTypes
{
    public static readonly string[] All =
    {
        "Blunt", "Slash", "Piercing", "Heat", "Caustic",
        "Poison", "Cellular", "Radiation", "Asphyxiation", "Bloodloss",
    };

    public static readonly Dictionary<string, float> BaseAmount = new()
    {
        ["Blunt"] = 50f,
        ["Slash"] = 40f,
        ["Piercing"] = 15f,
        ["Heat"] = 45f,
        ["Caustic"] = 15f,
        ["Poison"] = 45f,
        ["Cellular"] = 20f,
        ["Radiation"] = 40f,
        ["Asphyxiation"] = 15f,
        ["Bloodloss"] = 25f,
    };

    public static readonly Dictionary<string, int> DefaultSeverity = new()
    {
        ["Blunt"] = 5,
        ["Slash"] = 5,
        ["Piercing"] = 5,
        ["Heat"] = 5,
        ["Caustic"] = 5,
        ["Poison"] = 5,
        ["Cellular"] = 5,
        ["Radiation"] = 5,
        ["Asphyxiation"] = 5,
        ["Bloodloss"] = 5,
    };
}
