using Content.Server.MedTraining.Components;
using Content.Server.NPC.HTN;

using Content.Shared.MedTraining;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;

using Content.Shared.Interaction;
using Content.Shared.Chat;
using Content.Shared.Inventory;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking.Events;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Gibbing;
using Robust.Server.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Map;

namespace Content.Server.MedTraining.Systems;

public sealed partial class MedTrainingSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _proto = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private HTNSystem _htn = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private MobStateSystem _mobState = default!;
    [Dependency] private GibbingSystem _gibbing = default!;
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private IPlayerManager _playerMan = default!;

    [Dependency] private ChatSystem _chat = default!;

    private bool _patientActive = false;

    private const string KeyPatientTargetCoords = "MedPatientTargetCoords";
    private const string KeyPatientRange        = "MedPatientRange";
    private const string KeyPatientWaitMin      = "MedPatientWaitMin";
    private const string KeyPatientWaitMax      = "MedPatientWaitMax";
    private const string KeyPatientCoords       = "PatientCoords";
    private const string KeyPatientEntity       = "PatientEntity";
    private const string KeyRollerBedEntity     = "RollerBedEntity";
    private const string KeyReceptionCoords     = "ReceptionCoords";
    private const string KeyStasisBedCoords     = "StasisBedCoords";
    private const string KeyStasisBedEntity     = "StasisBedEntity";
    private const string KeyParamedicRange      = "ParamedicArrivalRange";
    private const string KeyExitCoords          = "ExitCoords";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MedTrainingButtonComponent, ActivateInWorldEvent>(OnButtonActivated);
        SubscribeLocalEvent<MedTrainingButtonComponent, InteractHandEvent>(OnButtonInteractHand);
        SubscribeLocalEvent<MedTrainingButtonComponent, InteractUsingEvent>(OnButtonInteractUsing);
        SubscribeLocalEvent<MedTrainingPatientComponent, EntityTerminatingEvent>(OnPatientDespawned);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
    }

    // forces chemical solutions to skip their reaction check on round start
    private void OnRoundStarting(RoundStartingEvent args)
    {
        var query = EntityQueryEnumerator<SolutionComponent>();
        while (query.MoveNext(out var uid, out var solution))
        {
            _solutionContainer.UpdateChemicals((uid, solution), needsReactionsProcessing: false);
        }
    }

    private void OnButtonInteractHand(EntityUid uid, MedTrainingButtonComponent button, InteractHandEvent args)
    {
        if (args.Handled) return;
        TryActivateButton(uid);
        args.Handled = true;
    }

    private void OnButtonInteractUsing(EntityUid uid, MedTrainingButtonComponent button, InteractUsingEvent args)
    {
        if (args.Handled) return;
        TryActivateButton(uid);
        args.Handled = true;
    }

    // ignores the button while a patient is already active otherwise finds this grids scenario and spawns one
    private void TryActivateButton(EntityUid uid)
    {
        if (_patientActive)
            return;
        _patientActive = true;
        var xform = Transform(uid);
        if (!TryFindScenario(xform.GridUid, out var scenario, out var scenarioComp))
        {
            Log.Warning("MedTraining: button pressed but no scenario coordinator found!");
            return;
        }
        RefreshScenarioLinks(xform.GridUid!.Value, scenarioComp!);
        SpawnPatient(scenarioComp!, scenario!.Value);
    }

    // frees the stasis bed this patient was holding and reenables the button once no patients remain
    private void OnPatientDespawned(EntityUid uid, MedTrainingPatientComponent comp, ref EntityTerminatingEvent args)
    {
        var bedQuery = EntityQueryEnumerator<MedTrainingStasisBedComponent>();
        while (bedQuery.MoveNext(out _, out var bed))
        {
            if (bed.CurrentPatient == uid)
            {
                bed.IsOccupied = false;
                bed.CurrentPatient = EntityUid.Invalid;
                break;
            }
        }

        var remaining = 0;
        var query = EntityQueryEnumerator<MedTrainingPatientComponent>();
        while (query.MoveNext(out var otherUid, out _))
        {
            if (otherUid != uid)
                remaining++;
        }

        if (remaining == 0)
        {
            _patientActive = false;
            Log.Info("MedTraining: all patients gone — button re-enabled.");
        }
    }

    private void OnButtonActivated(EntityUid uid, MedTrainingButtonComponent button, ActivateInWorldEvent args)
    {
        if (args.Handled) return;
        TryActivateButton(uid);
        args.Handled = true;
    }

    // rescans this grid for its lobby/paramedic/reception markers and stasis beds
    private void RefreshScenarioLinks(EntityUid gridUid, MedTrainingScenarioComponent scenario)
    {
        var lobbyQuery = EntityQueryEnumerator<MedTrainingLobbySpawnComponent, TransformComponent>();
        while (lobbyQuery.MoveNext(out var ent, out _, out var xform))
        {
            if (xform.GridUid == gridUid) { scenario.LobbySpawnPoint = ent; break; }
        }

        var paramQuery = EntityQueryEnumerator<MedTrainingParamedicSpawnComponent, TransformComponent>();
        while (paramQuery.MoveNext(out var ent, out _, out var xform))
        {
            if (xform.GridUid == gridUid) { scenario.ParamedicSpawnPoint = ent; break; }
        }

        var receptionQuery = EntityQueryEnumerator<MedTrainingReceptionMarkerComponent, TransformComponent>();
        while (receptionQuery.MoveNext(out var ent, out _, out var xform))
        {
            if (xform.GridUid == gridUid) { scenario.ReceptionMarker = ent; break; }
        }

        scenario.StasisBeds.Clear();
        var bedQuery = EntityQueryEnumerator<MedTrainingStasisBedComponent, TransformComponent>();
        while (bedQuery.MoveNext(out var ent, out _, out var xform))
        {
            if (xform.GridUid == gridUid) scenario.StasisBeds.Add(ent);
        }

        Log.Info($"MedTraining: links refreshed - Lobby={scenario.LobbySpawnPoint.IsValid()}, Paramedic={scenario.ParamedicSpawnPoint.IsValid()}, Reception={scenario.ReceptionMarker.IsValid()}, Beds={scenario.StasisBeds.Count}");
    }

    // 1% chance to trigger the narsi easter egg
    private void SpawnPatient(MedTrainingScenarioComponent scenario, EntityUid scenarioEnt)
    {
        if (_random.Prob(0.01f))
        {
            SpawnNarsie(scenario);
            return;
        }

        if (!TryComp<MedTrainingSpawnSettingsComponent>(scenarioEnt, out var settings))
            settings = new MedTrainingSpawnSettingsComponent();

        if (!TryGetSpawnCoords(scenario.LobbySpawnPoint, out var lobbyCoords)) return;

        var patient = Spawn(GetRandomPatientProto(settings), lobbyCoords);
        var appliedTypes = ApplyDamageFromSliders(patient, settings);

        var isCrit = IsPatientCritical(patient);
        Log.Info($"MedTraining: spawned {(isCrit ? "critical" : "ambulatory")} patient {patient}");

        if (isCrit)
            SetupCriticalPatient(scenario, scenarioEnt, patient, lobbyCoords, appliedTypes);
        else
            SetupAmbulatoryPatient(scenario, patient, lobbyCoords, appliedTypes);
    }

    private bool IsPatientCritical(EntityUid patient)
    {
        return TryComp<MobStateComponent>(patient, out var mobState)
               && !_mobState.IsAlive(patient, mobState);
    }

    private void SpawnNarsie(MedTrainingScenarioComponent scenario)
    {
        if (!TryGetSpawnCoords(scenario.LobbySpawnPoint, out var spawnCoords)) return;
        Log.Info("MedTraining: Nar'Sie has risen!");

        _audio.PlayGlobal("/Audio/Misc/narsie_rises.ogg", Filter.Broadcast(), true);

        foreach (var session in _playerMan.Sessions)
        {
            if (session.AttachedEntity is not { } playerEnt)
                continue;

            _gibbing.Gib(playerEnt);
        }

        Spawn("MobNarsie", spawnCoords);
    }

    // send walking patient to the reception marker and has them wait there until healed
    private void SetupAmbulatoryPatient(MedTrainingScenarioComponent scenario, EntityUid patient, EntityCoordinates spawnCoords, List<string> appliedTypes)
    {
        if (!TryGetSpawnCoords(scenario.ReceptionMarker, out var receptionCoords)) return;

        var tag = EnsureComp<MedTrainingPatientComponent>(patient);
        tag.Condition   = appliedTypes.Count > 0 ? string.Join(", ", appliedTypes) : "None";
        tag.IsCritical  = false;
        tag.IsTreated   = appliedTypes.Count == 0;
        tag.SpawnCoords = spawnCoords;

        if (!TryComp<HTNComponent>(patient, out var htn)) return;

        htn.Blackboard.SetValue(KeyPatientTargetCoords, receptionCoords);
        htn.Blackboard.SetValue(KeyPatientRange, 1.5f);
        htn.Blackboard.SetValue(KeyPatientWaitMin, 99999f);
        htn.Blackboard.SetValue(KeyPatientWaitMax, 99999f);
        htn.Blackboard.SetValue("MedPatientIdleTime", 99999f);
        htn.RootTask = new HTNCompoundTask { Task = "MedPatientWalkIn" };
        htn.Plan = null;
        _htn.Replan(htn);

        Log.Info($"MedTraining: ambulatory patient {patient} spawned, walking to reception lobby.");
    }

    // spawns roller bed and paramedic for a critical patient
    private void SetupCriticalPatient(MedTrainingScenarioComponent scenario, EntityUid scenarioEnt, EntityUid patient, EntityCoordinates lobbyCoords, List<string> appliedTypes)
    {
        if (!TryGetSpawnCoords(scenario.ParamedicSpawnPoint, out var paramedicSpawnCoords)) return;

        var tag = EnsureComp<MedTrainingPatientComponent>(patient);
        tag.Condition   = appliedTypes.Count > 0 ? string.Join(", ", appliedTypes) : "None";
        tag.IsCritical  = true;
        tag.SpawnCoords = lobbyCoords;

        var rollerBed = Spawn("EmergencyRollerBed", lobbyCoords);
        var paramedic = Spawn("MobMedTrainingParamedic", paramedicSpawnCoords);
        EquipParamedic(paramedic);

        _chat.TrySendInGameICMessage(paramedic, "Incoming critical patient! All medics to the lobby!", InGameICChatType.Speak, false);

        var bedUid = FindFreeStasisBed(scenario);
        TryGetSpawnCoords(bedUid, out var bedCoords);

        var paramComp = EnsureComp<MedTrainingParamedicComponent>(paramedic);
        paramComp.Scenario        = scenarioEnt;
        paramComp.Patient         = patient;
        paramComp.RollerBed       = rollerBed;
        paramComp.TargetStasisBed = bedUid;
        paramComp.StasisBedCoords = bedCoords;
        paramComp.ExitCoords      = paramedicSpawnCoords;

        if (!TryComp<HTNComponent>(paramedic, out var htn)) return;

        htn.Blackboard.SetValue("ParamedicMoveTarget", lobbyCoords);
        htn.Blackboard.SetValue(KeyParamedicRange, 1.8f);
        htn.RootTask = new HTNCompoundTask { Task = "MedParamedicMove" };
        htn.Plan = null;
        _htn.Replan(htn);

        Log.Info($"MedTraining: critical patient {patient} at lobby, paramedic {paramedic} dispatched. Stasis bed: {bedUid}");
    }

    // strips whatever the parent prototype spawned in these slots and replaces it with proper paramedic gear
    private void EquipParamedic(EntityUid paramedic)
    {
        if (!TryComp<InventoryComponent>(paramedic, out var inv)) return;
        var invSystem = EntityManager.System<InventorySystem>();
        var coords = Transform(paramedic).Coordinates;

        foreach (var slot in new[] { "jumpsuit", "outerClothing", "ears", "gloves", "back", "belt", "neck" })
        {
            if (invSystem.TryGetSlotEntity(paramedic, slot, out var existing))
            {
                invSystem.TryUnequip(paramedic, slot, true, true, inventory: inv);
                if (existing.HasValue && Exists(existing.Value))
                    QueueDel(existing.Value);
            }
        }

        var jumpsuit = Spawn("ClothingUniformJumpsuitParamedic", coords);
        invSystem.TryEquip(paramedic, jumpsuit, "jumpsuit", true, inventory: inv);

        var outer = Spawn("ClothingOuterHardsuitVoidParamed", coords);
        invSystem.TryEquip(paramedic, outer, "outerClothing", true, inventory: inv);

        var headset = Spawn("ClothingHeadsetMedical", coords);
        invSystem.TryEquip(paramedic, headset, "ears", true, inventory: inv);

        var gloves = Spawn("ClothingHandsGlovesLatex", coords);
        invSystem.TryEquip(paramedic, gloves, "gloves", true, inventory: inv);
    }

    // rolls damage per type from its slider severity
    private List<string> ApplyDamageFromSliders(EntityUid ent, MedTrainingSpawnSettingsComponent settings)
    {
        var appliedTypes = new List<string>();
        if (!TryComp<DamageableComponent>(ent, out _)) return appliedTypes;

        foreach (var type in MedTrainingDamageTypes.All)
        {
            if (!_proto.TryIndex<DamageTypePrototype>(type, out var dmgType)) continue;

            var severity = settings.DamageTypeSeverity.GetValueOrDefault(type, 0);
            if (severity <= 0)
                continue;

            float scaled = severity * 20f;
            float variance = scaled * 0.2f;
            float roll = _random.NextFloat(-variance, variance);
            float result = scaled + roll;

            if (roll > 0 && _random.Prob(0.333f))
                result -= variance;
            else if (roll < 0 && _random.Prob(0.333f))
                result += variance;

            if (settings.UltraMode.GetValueOrDefault(type, false))
                result *= 5f;

            result = Math.Max(result, 1f);

            var requested = (float)Math.Round(result);
            _damageable.TryChangeDamage(ent, new DamageSpecifier(dmgType, requested), true);
            appliedTypes.Add(type);
        }

        return appliedTypes;
    }

    // deletes every active patient/paramedic/roller bed and frees their stasis beds
    public int ForceDespawnPatients(EntityUid? gridUid)
    {
        var toDelete = new List<EntityUid>();

        var patientQuery = EntityQueryEnumerator<MedTrainingPatientComponent, TransformComponent>();
        while (patientQuery.MoveNext(out var uid, out _, out var xform))
        {
            if (gridUid != null && xform.GridUid != gridUid)
                continue;
            toDelete.Add(uid);
        }

        var paramedicQuery = EntityQueryEnumerator<MedTrainingParamedicComponent, TransformComponent>();
        while (paramedicQuery.MoveNext(out var uid, out var comp, out var xform))
        {
            if (gridUid != null && xform.GridUid != gridUid)
                continue;
            if (Exists(comp.RollerBed))
                toDelete.Add(comp.RollerBed);
            toDelete.Add(uid);
        }

        foreach (var uid in toDelete)
        {
            if (Exists(uid))
                QueueDel(uid);
        }

        var bedQuery = EntityQueryEnumerator<MedTrainingStasisBedComponent, TransformComponent>();
        while (bedQuery.MoveNext(out _, out var bed, out var bedXform))
        {
            if (gridUid != null && bedXform.GridUid != gridUid)
                continue;
            bed.IsOccupied = false;
            bed.CurrentPatient = EntityUid.Invalid;
        }

        _patientActive = false;
        Log.Info($"MedTraining: force-despawned {toDelete.Count} NPC-related entities via UI request, button re-enabled.");

        return toDelete.Count;
    }

    public EntityUid FindFreeStasisBed(MedTrainingScenarioComponent scenario)
    {
        foreach (var bed in scenario.StasisBeds)
        {
            if (Exists(bed) && TryComp<MedTrainingStasisBedComponent>(bed, out var c) && !c.IsOccupied)
                return bed;
        }
        return EntityUid.Invalid;
    }

    // picks a random enabled species. Falling back to any species if all disabled
    private string GetRandomPatientProto(MedTrainingSpawnSettingsComponent settings)
    {
        var pool = new List<string>();
        foreach (var species in MedTrainingSpecies.All)
        {
            if (settings.SpeciesEnabled.GetValueOrDefault(species, true))
                pool.Add(species);
        }

        if (pool.Count == 0)
            return MedTrainingSpecies.GetPrototypeId(MedTrainingSpecies.All[_random.Next(MedTrainingSpecies.All.Length)]);

        return MedTrainingSpecies.GetPrototypeId(pool[_random.Next(pool.Count)]);
    }


    private bool TryGetSpawnCoords(EntityUid marker, out EntityCoordinates coords)
    {
        coords = default;
        if (!Exists(marker)) return false;
        coords = Transform(marker).Coordinates;
        return true;
    }

    private bool TryFindScenario(EntityUid? gridUid, out EntityUid? scenarioEnt, out MedTrainingScenarioComponent? comp)
    {
        scenarioEnt = null;
        comp = null;
        var query = EntityQueryEnumerator<MedTrainingScenarioComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var scenario, out var xform))
        {
            if (xform.GridUid == gridUid) { scenarioEnt = uid; comp = scenario; return true; }
        }
        return false;
    }
}
