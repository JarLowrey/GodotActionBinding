using Godot;
using System.Collections.Generic;
public class ActionBindingMenu : Node2D
{
	private Dictionary<string,List<byte[]>> InitialBindings = new Dictionary<string,List<byte[]>>();

	public readonly static Dictionary<string,string> ReassignableActions = new Dictionary<string,string>(){
		{"ui_up", "Move Up"},
		{"ui_left", "Move Left"},
		{"ui_down", "Move Down"},
		{"ui_right", "Move Right"},
		{"MakePlayerShoot", "Shoot"},
		{"beginAbility", "Ability"},
		{"ui_pause", "Pause"},
	};
    private RebindingInput awaitingRebindForActionInput = null;
	private bool isRebinding = false;
	private List<RebindingInput> KeyRebinders = new List<RebindingInput>();
	public const int InputButtonX = 10;
    DataManager dataManager;
	Button ResetToDefaultControls;

	VBoxContainer scrollContainer;

	public override void _Ready () {
		scrollContainer = GetNode<VBoxContainer>("ScrollContainer/VBoxContainer");
		ResetToDefaultControls = scrollContainer.GetNode<Button>("ResetToDefaultControls");
        ResetToDefaultControls.Connect("pressed",this,nameof(ActionBindingMenu.ResetBinding));

		CopyInitialBindingsToRuntimeMemory();
		CreateInputButtons ();
		SetProcessUnhandledInput(false); 
        dataManager = GetNode<DataManager>(DataManager.Path);
	}

	public static string EventSimpleText(InputEvent e){		
		var bindingText = e.AsText ();
		bindingText = bindingText.ToLower();
        //TODO :::: Improve and expand for different inputs
		if(bindingText.Contains("mousebutton")) {
			if(bindingText.Contains("left")){bindingText = "Left Click";}
			else if(bindingText.Contains("middle")){bindingText = "Mid Click";}
			else if(bindingText.Contains("right")){bindingText = "Right Click";}
		}
		return bindingText;
	}

	private void setAllInputsClickable(RebindingInput notThisOne, bool isEnabled){
		foreach(var binder in this.KeyRebinders){
			if(binder != notThisOne) binder.button.Disabled = (!isEnabled);
		}
	}
	private void CreateInputButtons () {
		var actions = new List<string>(ReassignableActions.Keys);
		KeyRebinders = new List<RebindingInput>();

		for (int i =0;i<actions.Count;i++){
			var action = actions[i];
			var bindingControl = (RebindingInput)((PackedScene)GD.Load("res://addons/C#/RebindingInput.tscn")).Instance();
			scrollContainer.AddChild(bindingControl);
			bindingControl.setup(action,i);
			// PositionBtn(keyboardBinderControl, i);
			KeyRebinders.Add(bindingControl);
		}
		scrollContainer.MoveChild(ResetToDefaultControls,scrollContainer.GetChildCount()-1);

		// PositionBtn(ResetToDefaultControls, actions.Count);
	}
	
	// private void PositionBtn(Control btn, int i){
	// 	// var edges = globs.ScreenEdges();
	// 	btn.RectPosition =new Vector2(-btn.RectSize.x/2,(i * btn.RectSize.y + 5) + 400);
	// }

	public void UpdateInputButtons(){
		var actions = new List<string>(ReassignableActions.Keys);

		for (int i =0;i<actions.Count;i++){
			var action = actions[i];
			var bindingControl = KeyRebinders[i];
			bindingControl.setup(action,i);
		}
	}

	public override void _UnhandledInput(InputEvent inputEvent){
		var isKeyOrMouseInput = inputEvent is InputEventKey || inputEvent is InputEventMouseButton;
		if(isRebinding && isKeyOrMouseInput)
			doRebind(inputEvent);
	}
	
	public void awaitRebind(RebindingInput input){
		input.awaitRebind();
		awaitingRebindForActionInput = input;
		SetProcessUnhandledInput(true);
		setAllInputsClickable(awaitingRebindForActionInput, false);
		isRebinding = true;
	}
	public void doRebind(InputEvent e){
		var action = awaitingRebindForActionInput.Name;
		if(!IsRebindValid(action,e)) {
			return;
		}
		InputMap.ActionEraseEvents(action);
		InputMap.ActionAddEvent(action,e);
		awaitingRebindForActionInput.rebindOccurred();
		setAllInputsClickable(null, true);
		isRebinding = false;

		dataManager.SaveData.Settings.ControlBindings[action] = new List<byte[]>(){ GD.Var2Bytes(e,true)};
        dataManager.Save();
	}
    private bool IsRebindValid(string rebindedAction,InputEvent e){
		foreach(var a in ReassignableActions.Keys){
			if(a != rebindedAction){
				foreach(var b in InputMap.GetActionList (a)){
					if (EventSimpleText((InputEvent)b).Equals(EventSimpleText(e))) return false;
				}
			}
		}
		return true;
	}

	private void ResetBinding(){
		dataManager.LoadControlBindings(InitialBindings);
		UpdateInputButtons();
		
		foreach(var a in InitialBindings.Keys){
			dataManager.SaveData.Settings.ControlBindings[a] = InitialBindings[a];
		}
		dataManager.Save();
	}
	private void CopyInitialBindingsToRuntimeMemory(){
        foreach(var a in ReassignableActions.Keys){
			var gBindings =  InputMap.GetActionList (a);
			var cBindings = new List<byte[]>();
			foreach(var b in gBindings){
				cBindings.Add(GD.Var2Bytes(b,true));
			}
			InitialBindings[a] = cBindings;
        }
	}

}
