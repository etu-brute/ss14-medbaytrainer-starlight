namespace Content.Shared.MedTraining;

public static class MedTrainingSpecies
{
    public static readonly string[] All =
    {
        "Human", "Reptilian", "Moth", "Arachnid", "Vox", "Slime",
        "Vulpkanin", "Diona", "Dwarf", "Avali", "Elf", "Felionoid",
        "Lagomorph", "Resomi", "Rodentia", "Shadekin", "Doll", "Cyclorite",
    };

    public static string GetPrototypeId(string species)
    {
        return $"MobMedTrainingPatient{species}";
    }
}
