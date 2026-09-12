using Content.Server.MedTraining.Components;
using Content.Shared.Buckle.Components;
using Robust.Shared.GameObjects;

namespace Content.Server.MedTraining.Systems;


public sealed class MedTrainingStasisBedSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MedTrainingStasisBedComponent, StrappedEvent>(OnStrapped);
        SubscribeLocalEvent<MedTrainingStasisBedComponent, UnstrappedEvent>(OnUnstrapped);
    }

    // marks the bed occupied by whoever gets buckled into it
    private void OnStrapped(EntityUid uid, MedTrainingStasisBedComponent bed, ref StrappedEvent args)
    {
        bed.IsOccupied     = true;
        bed.CurrentPatient = args.Buckle.Owner;
    }

    // frees the bed once its occupant is unbuckled
    private void OnUnstrapped(EntityUid uid, MedTrainingStasisBedComponent bed, ref UnstrappedEvent args)
    {
        bed.IsOccupied     = false;
        bed.CurrentPatient = EntityUid.Invalid;
    }
}
