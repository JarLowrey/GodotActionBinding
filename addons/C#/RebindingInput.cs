using Godot;
using System;

public class RebindingInput : Control {
	RichTextLabel textLabel;
	public Button button;
	int index;
	ActionBindingMenu ActionBindingMenu;
	public override void _Ready () {
		textLabel = GetNode<RichTextLabel> ("RichTextLabel");
		button = GetNode<Button> ("Button");
		ActionBindingMenu = GetNode<ActionBindingMenu>("../../..");
		
		var args = new Godot.Collections.Array();
		args.Add(this);
		Connect("pressed",this,nameof(ActionBindingMenu.awaitRebind));
	}

	public void setup(string action, int index){
		this.index = index;
		setup(action);
	}
	public void setup (string action) {
		Name = action;
		textLabel.Text = ActionBindingMenu.ReassignableActions[action];

		button.Pressed = (false);
		var bindings = InputMap.GetActionList (action);
        var bindingText = "No bindings!";
		if(bindings.Count > 0) {
			var e = (InputEvent)bindings[0];
			bindingText = ActionBindingMenu.EventSimpleText(e);
        }
        button.Text = bindingText;
		button.RectSize = new Vector2(300,30);

		var args = new Godot.Collections.Array();
		args.Add(this);
	}

	public void awaitRebind(){
		button.Text = "... Key";
	}

	public void rebindOccurred(){ 
		setup(Name);  
	}
}
