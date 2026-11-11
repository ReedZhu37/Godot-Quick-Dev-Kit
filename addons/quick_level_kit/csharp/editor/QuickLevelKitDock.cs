#if TOOLS
using System;
using Godot;
using Godot.Collections;

namespace QuickDev.Level;

[Tool]
public partial class QuickLevelKitDock : Control
{
    private QuickLevelKitCatalog? _catalog;
    private ItemList? _levelList;
    private Label? _errorLabel;
    private Label? _statusLabel;

    public override void _Ready()
    {
        Name = "Quick Level Kit C#";
        CustomMinimumSize = new Vector2(280, 320);
        BuildUi();
        Refresh();
    }

    public void SetCatalog(QuickLevelKitCatalog? catalog)
    {
        _catalog = catalog;
        if (_errorLabel != null) _errorLabel.Text = "";
        Refresh();
    }

    public QuickLevelKitCatalog? GetCatalog() => _catalog;

    public void Refresh()
    {
        if (_levelList == null || _statusLabel == null) return;
        _levelList.Clear();
        if (_catalog == null)
        {
            _statusLabel.Text = "No catalog assigned.";
            return;
        }
        for (var index = 0; index < _catalog.GetLevelCount(); index++)
        {
            var definition = _catalog.GetDefinitionAt(index);
            _levelList.AddItem(definition == null ? "<null>" : $"{index}: {definition.LevelId}");
        }
        _statusLabel.Text = $"Levels: {_catalog.GetLevelCount()}";
    }

    public Array<Dictionary> ValidateCatalog()
    {
        var errors = _catalog == null
            ? new Array<Dictionary> { new() { [new StringName("code")] = new StringName("catalog_missing") } }
            : _catalog.GetValidationErrors();
        if (_errorLabel != null) _errorLabel.Text = errors.Count == 0 ? "OK" : errors.ToString();
        return errors;
    }

    public bool AddLevel(StringName levelId, PackedScene? scene)
    {
        if (_catalog == null || scene == null) return false;
        _catalog.Levels.Add(new QuickLevelKitDefinition { LevelId = levelId, Scene = scene });
        _catalog.EmitChanged();
        Refresh();
        return true;
    }

    public bool RemoveLevel(int index)
    {
        if (!IsValidIndex(index)) return false;
        _catalog!.Levels.RemoveAt(index);
        _catalog.EmitChanged();
        Refresh();
        return true;
    }

    public bool MoveLevel(int index, int delta)
    {
        var target = (long)index + delta;
        if (!IsValidIndex(index) || target < 0 || target >= _catalog!.GetLevelCount()) return false;
        var definition = _catalog.Levels[index];
        _catalog.Levels.RemoveAt(index);
        _catalog.Levels.Insert((int)target, definition);
        _catalog.EmitChanged();
        Refresh();
        return true;
    }

    public QuickLevelKitFlow? CreateDefaultFlowFor(int index)
    {
        if (!IsValidIndex(index)) return null;
        var definition = _catalog!.GetDefinitionAt(index);
        if (definition == null) return null;
        var flow = QuickLevelKitDefaultFlow.Create();
        definition.Flow = flow;
        definition.EmitChanged();
        _catalog.EmitChanged();
        Refresh();
        return flow;
    }

    private bool IsValidIndex(int index) => _catalog != null && index >= 0 && index < _catalog.GetLevelCount();

    private void BuildUi()
    {
        var layout = new VBoxContainer();
        layout.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(layout);
        layout.AddChild(new Label { Text = "Quick Level Kit Catalog (C#)" });
        _levelList = new ItemList { CustomMinimumSize = new Vector2(0, 160) };
        layout.AddChild(_levelList);
        var buttons = new HBoxContainer();
        layout.AddChild(buttons);
        buttons.AddChild(MakeButton("Up", () => WithSelection(index => MoveLevel(index, -1))));
        buttons.AddChild(MakeButton("Down", () => WithSelection(index => MoveLevel(index, 1))));
        buttons.AddChild(MakeButton("Remove", () => WithSelection(index => RemoveLevel(index))));
        buttons.AddChild(MakeButton("Flow", () => WithSelection(index => CreateDefaultFlowFor(index))));
        buttons.AddChild(MakeButton("Validate", () => ValidateCatalog()));
        _statusLabel = new Label();
        layout.AddChild(_statusLabel);
        _errorLabel = new Label { AutowrapMode = TextServer.AutowrapMode.WordSmart };
        layout.AddChild(_errorLabel);
    }

    private void WithSelection(Action<int> action)
    {
        var selected = _levelList?.GetSelectedItems();
        if (selected is { Length: > 0 }) action(selected[0]);
    }

    private static Button MakeButton(string text, Action callback)
    {
        var button = new Button { Text = text };
        button.Pressed += callback;
        return button;
    }
}
#endif
