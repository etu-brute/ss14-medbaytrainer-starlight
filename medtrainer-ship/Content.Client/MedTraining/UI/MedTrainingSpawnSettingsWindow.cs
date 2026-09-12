using System;
using System.Collections.Generic;
using System.Numerics;
using Content.Shared.MedTraining;
using Content.Shared.MedTraining.UI;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Maths;

namespace Content.Client.MedTraining.UI;

public sealed class MedTrainingSpawnSettingsWindow : DefaultWindow
{
    private readonly Dictionary<string, SliderIntInput> _damageSeveritySliders = new();
    private readonly Dictionary<string, CheckBox> _ultraModeCheckBoxes = new();
    private readonly Dictionary<string, CheckBox> _speciesCheckBoxes = new();
    private readonly Label _warningLabel;

    private bool _suppressEvents;

    public event Action<string, int>? OnDamageTypeSeverityChanged;
    public event Action<string, bool>? OnUltraModeChanged;
    public event Action<string, bool>? OnSpeciesEnabledChanged;
    public event Action? OnDespawnPatientsRequested;
    public event Action? OnRandomizeDamageSlidersRequested;

    public MedTrainingSpawnSettingsWindow()
    {
        Title = "Patient Spawn Settings";
        MinSize = new Vector2(380, 460);

        var root = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
            VerticalExpand = true,
        };

        _warningLabel = new Label
        {
            Text = "All damage severities are 0 — patients will spawn with no injuries at all.",
            FontColorOverride = Color.OrangeRed,
            Visible = false,
        };
        root.AddChild(_warningLabel);

        var despawnButton = new Button { Text = "Despawn NPC / Reset Spawner", HorizontalExpand = true };
        despawnButton.OnPressed += _ => OnDespawnPatientsRequested?.Invoke();
        root.AddChild(despawnButton);

        var randomizeButton = new Button { Text = "Randomize Damage Sliders", HorizontalExpand = true };
        randomizeButton.OnPressed += _ => OnRandomizeDamageSlidersRequested?.Invoke();
        root.AddChild(randomizeButton);

        var scrollBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            HorizontalExpand = true,
        };

        scrollBox.AddChild(new Label { Text = "Damage Type Severity - 0-10 severity scale. 0 = no damage" });

        foreach (var damageType in MedTrainingDamageTypes.All)
        {
            scrollBox.AddChild(new Label { Text = damageType });

            var slider = new SliderIntInput { MinValue = 0, MaxValue = 10, HorizontalExpand = true };
            var capturedType = damageType;
            slider.OnValueChanged += value =>
            {
                RefreshWarning();
                if (_suppressEvents)
                    return;
                OnDamageTypeSeverityChanged?.Invoke(capturedType, value);
            };
            scrollBox.AddChild(slider);
            _damageSeveritySliders[damageType] = slider;

            // only blunt multiplier is labeled gib. Rest are ultra mode
            var isGibType = damageType is "Blunt";
            var ultraCheckBox = new CheckBox { Text = isGibType ? "Gib Mode (x5 damage)" : "Ultra Mode (x5 damage)" };
            ultraCheckBox.OnToggled += args =>
            {
                if (_suppressEvents)
                    return;
                OnUltraModeChanged?.Invoke(capturedType, args.Pressed);
            };
            scrollBox.AddChild(ultraCheckBox);
            _ultraModeCheckBoxes[damageType] = ultraCheckBox;
        }

        scrollBox.AddChild(new Label { Text = "Species In Spawn Pool" });

        foreach (var species in MedTrainingSpecies.All)
        {
            var checkBox = new CheckBox { Text = species };
            var capturedSpecies = species;
            checkBox.OnToggled += args =>
            {
                if (_suppressEvents)
                    return;
                OnSpeciesEnabledChanged?.Invoke(capturedSpecies, args.Pressed);
            };
            scrollBox.AddChild(checkBox);
            _speciesCheckBoxes[species] = checkBox;
        }

        var scroll = new ScrollContainer
        {
            HorizontalExpand = true,
            VerticalExpand = true,
        };
        scroll.AddChild(scrollBox);
        root.AddChild(scroll);

        Contents.AddChild(root);
    }

    // syncs every slider/checkbox to the servers current settings without re-firing their change events
    public void UpdateState(MedTrainingSpawnSettingsBoundUserInterfaceState state)
    {
        _suppressEvents = true;

        foreach (var (type, slider) in _damageSeveritySliders)
        {
            if (state.DamageTypeSeverity.TryGetValue(type, out var value))
                slider.Value = value;
        }

        foreach (var (type, checkBox) in _ultraModeCheckBoxes)
        {
            if (state.UltraMode.TryGetValue(type, out var enabled))
                checkBox.Pressed = enabled;
        }

        foreach (var (species, checkBox) in _speciesCheckBoxes)
        {
            if (state.SpeciesEnabled.TryGetValue(species, out var enabled))
                checkBox.Pressed = enabled;
        }

        _suppressEvents = false;

        RefreshWarning();
    }

    // shows the 0 damage warning label when every severity slider is at 0
    private void RefreshWarning()
    {
        var allZero = true;
        foreach (var slider in _damageSeveritySliders.Values)
        {
            if (slider.Value > 0)
            {
                allZero = false;
                break;
            }
        }

        _warningLabel.Visible = allZero;
    }
}
