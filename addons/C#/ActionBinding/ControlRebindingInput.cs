using Godot;
using System;
using System.Collections.Generic;

public class ControlRebindingInput : Control {
	public const string RES_PATH = "res://ActionBinding/ControlRebindingInput.tscn";
	RichTextLabel textLabel;
	public Button button;
	string ActionName="";
	public string GetActionName(){return ActionName;}

	public override void _Ready () {
		base._Ready();
		textLabel = GetNode<RichTextLabel> ("RichTextLabel");
		button = GetNode<Button> ("Button");
	}

	public void Setup (string action) {
		ActionName = action;
		textLabel.Text = ActionBindingMenu.ReassignableActions[action];

		button.Pressed = false;
		var bindings = InputMap.GetActionList (action);
        var bindingText = "No bindings!";
		if(bindings.Count > 0) {
			var inputs = new List<string>();
			foreach(var b in bindings)inputs.Add(ActionBindingMenu.EventSimpleText((InputEvent)b));
			bindingText = String.Join(", ", inputs);
        }
        button.Text = bindingText;
		button.HintTooltip = bindingText;
	}
}