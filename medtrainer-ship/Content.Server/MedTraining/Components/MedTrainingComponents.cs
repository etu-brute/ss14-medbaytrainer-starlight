using System.Linq;
using Content.Shared.MedTraining;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.MedTraining.Components;

[RegisterComponent]
public sealed partial class MedTrainingScenarioComponent : Component
{
    [DataField("lobbySpawnPoint")]
    public EntityUid LobbySpawnPoint;

    [DataField("paramedicSpawnPoint")]
    public EntityUid ParamedicSpawnPoint;

    [DataField("receptionMarker")]
    public EntityUid ReceptionMarker;

    [DataField("stasisBeds")]
    public List<EntityUid> StasisBeds = new();
}

[RegisterComponent]
public sealed partial class MedTrainingButtonComponent : Component
{
    [DataField("lastActivated")]
    public TimeSpan LastActivated = TimeSpan.MinValue;
}

[RegisterComponent]
public sealed partial class MedTrainingPatientComponent : Component
{
    [DataField("condition")]
    public string Condition = string.Empty;

    [DataField("isCritical")]
    public bool IsCritical;

    [DataField("isTreated")]
    public bool IsTreated;

    [DataField("spawnCoords")]
    public EntityCoordinates SpawnCoords;

    public bool ThankedDoctor = false;

    // if this passes before the patient reaches the reception marker, they despawn
    public TimeSpan ReceptionDeadline = TimeSpan.MaxValue;

    public EntityUid ReceptionMarker = EntityUid.Invalid;

    public TimeSpan WalkHomeStartTime = TimeSpan.MaxValue;

    public TimeSpan NextReplanTime = TimeSpan.MaxValue;

    public MapCoordinates LastStuckCheckPos;

    public TimeSpan LastMovedTime = TimeSpan.MaxValue;
}

public enum ParamedicState
{
    WalkingToPatient,
    CarryingToStasis,
    WalkingToExit,
    Done
}

[RegisterComponent]
public sealed partial class MedTrainingParamedicComponent : Component
{
    [DataField("scenario")]
    public EntityUid Scenario = EntityUid.Invalid;

    [DataField("patient")]
    public EntityUid Patient;

    [DataField("rollerBed")]
    public EntityUid RollerBed;

    [DataField("targetStasisBed")]
    public EntityUid TargetStasisBed = EntityUid.Invalid;

    [DataField("stasisBedCoords")]
    public EntityCoordinates StasisBedCoords;

    [DataField("exitCoords")]
    public EntityCoordinates ExitCoords;

    public ParamedicState State = ParamedicState.WalkingToPatient;
    public TimeSpan NextActionTime = TimeSpan.Zero;
    public int TicksInState = 0;
    // arrival at the stasis bed. p1 stops pulling the roller bed p2 unbuckles onto the bed p3 rebuckles into it
    public int ArrivalPhase = 0;

}

[RegisterComponent]
public sealed partial class MedTrainingStasisBedComponent : Component
{
    [DataField("isOccupied")]
    public bool IsOccupied;

    [DataField("currentPatient")]
    public EntityUid CurrentPatient = EntityUid.Invalid;
}

[RegisterComponent]
public sealed partial class MedTrainingSpawnSettingsComponent : Component
{
    [DataField("damageTypeSeverity")]
    public Dictionary<string, int> DamageTypeSeverity = new(MedTrainingDamageTypes.DefaultSeverity);

    [DataField("speciesEnabled")]
    public Dictionary<string, bool> SpeciesEnabled = MedTrainingSpecies.All.ToDictionary(s => s, _ => true);

    [DataField("ultraMode")]
    public Dictionary<string, bool> UltraMode = MedTrainingDamageTypes.All.ToDictionary(t => t, _ => false);
}

[RegisterComponent]
public sealed partial class MedTrainingSettingsButtonComponent : Component { }

[RegisterComponent]
public sealed partial class MedTrainingLobbySpawnComponent : Component { }

[RegisterComponent]
public sealed partial class MedTrainingParamedicSpawnComponent : Component { }

[RegisterComponent]
public sealed partial class MedTrainingReceptionMarkerComponent : Component { }
