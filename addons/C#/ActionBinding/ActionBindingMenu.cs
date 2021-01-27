using Godot;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Text;

public class ActionBindingMenu : Node
{
	public readonly static Dictionary<string,string> ReassignableActions = new Dictionary<string,string>(){
		{"ui_up", "Move Up"},
		{"ui_left", "Move Left"},
		{"ui_down", "Move Down"},
		{"ui_right", "Move Right"},
	};
	public const string SAVE_CONTROLS_FILE_PATH = "user://controls.save";


	private Dictionary<string,List<InputEvent>> InitialBindings = new Dictionary<string,List<InputEvent>>();
    private ControlRebindingInput awaitingRebindForActionInput = null;
	private bool isRebinding = false;
	private List<ControlRebindingInput> KeyRebinders = new List<ControlRebindingInput>();
	public const int InputButtonX = 10;
	Button ResetToDefaultControls;
	VBoxContainer vBoxContainer;

	public override void _Ready () {
		base._Ready();

		vBoxContainer = GetNode<VBoxContainer>("ScrollContainer/VBoxContainer");
		ResetToDefaultControls = vBoxContainer.GetNode<Button>("ResetToDefaultControls");
        ResetToDefaultControls.Connect("pressed",this,nameof(ActionBindingMenu.ResetBinding));

		CopyInitialBindingsToRuntimeMemory();
		LoadControlsFromSave();
		CreateInputButtons ();
		SetProcessUnhandledInput(false); 
	}

	public static string EventSimpleText(InputEvent e){		
		var joy = e as InputEventJoypadButton;
		var mouseBtn = e as InputEventMouseButton;
		var key = e as InputEventKey;

		var bindingText = e.AsText ();
        //TODO :::: Improve and expand for different inputs
		if(mouseBtn != null) {
			if(mouseBtn.ButtonIndex == 3) bindingText = "Mid Click";
			else if(mouseBtn.ButtonIndex == 1) bindingText = "Left Click";
			else if(mouseBtn.ButtonIndex == 2) bindingText = "Right Click";
		}else if (joy != null){
			bindingText = "Joy "+joy.ButtonIndex;
		}
		return bindingText;
	}
	
	public void UpdateInputButtons(){
		var actions = new List<string>(ReassignableActions.Keys);

		for (int i =0;i<actions.Count;i++){
			var action = actions[i];
			var bindingControl = KeyRebinders[i];
			bindingControl.Setup(action);
		}
	}

	public override void _UnhandledInput(InputEvent inputEvent){
		var isValidInput = inputEvent is InputEventKey || inputEvent is InputEventMouseButton || inputEvent is InputEventJoypadButton || inputEvent is InputEventScreenTouch;
		if(isRebinding && isValidInput)
			DoRebind(inputEvent);
	}
	
	public void AwaitRebind(ControlRebindingInput input){
		input.button.Text = "... Key";
		awaitingRebindForActionInput = input;
		SetProcessUnhandledInput(true);
		SetAllInputsClickable(awaitingRebindForActionInput, false);
		isRebinding = true;
	}
	public void DoRebind(InputEvent e){
		var action = awaitingRebindForActionInput.GetActionName();
		if(!IsRebindValid(action,e)) {
			return;
		}
		InputMap.ActionEraseEvents(action);
		InputMap.ActionAddEvent(action,e);
		awaitingRebindForActionInput.Setup(action);
		SetAllInputsClickable(null, true);
		isRebinding = false;
	
		SaveControls();
	}
	
	private void SetAllInputsClickable(ControlRebindingInput notThisOne, bool isEnabled){
		foreach(var binder in this.KeyRebinders){
			if(binder != notThisOne) binder.button.Disabled = (!isEnabled);
		}
	}
	private void CreateInputButtons () {
		var actions = new List<string>(ReassignableActions.Keys);
		KeyRebinders = new List<ControlRebindingInput>();

		for(int i =0;i<actions.Count;i++){
			var action = actions[i];
			var bindingControl = (ControlRebindingInput)((PackedScene)GD.Load(ControlRebindingInput.RES_PATH)).Instance();
			vBoxContainer.AddChild(bindingControl);
			bindingControl.Setup(action);
			KeyRebinders.Add(bindingControl);
			
			var args = new Godot.Collections.Array();
			args.Add(bindingControl);
			bindingControl.button.Connect("pressed",this,nameof(ActionBindingMenu.AwaitRebind),args);
		}
		vBoxContainer.MoveChild(ResetToDefaultControls,vBoxContainer.GetChildCount()-1);
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
		isRebinding = false;
		SetProcessUnhandledInput(false);
		SetAllInputsClickable(null,true);

		LoadControls(InitialBindings);
		UpdateInputButtons();
		SaveControls();
	}

	public void SaveControls()
	{
		var SavedControls = new Dictionary<string,List<byte[]>>();
		foreach(var action in ReassignableActions.Keys){
			SavedControls[action] = new List<byte[]>();
			foreach(var binding in InputMap.GetActionList (action)){
				var bytes = GD.Var2Bytes(binding,true);
				SavedControls[action].Add(bytes);
			}
		}
		
		var dir = new Godot.Directory();
		dir.Remove(SAVE_CONTROLS_FILE_PATH);

		var savedControlsFile = new Godot.File();
		savedControlsFile.Open(SAVE_CONTROLS_FILE_PATH, Godot.File.ModeFlags.Write);
		savedControlsFile.StoreLine(JsonConvert.SerializeObject(SavedControls));
		savedControlsFile.Close();
	}
	private void LoadControlsFromSave(){
		var savedControlsFile = new File();
		if (!savedControlsFile.FileExists(SAVE_CONTROLS_FILE_PATH)){
			GD.Print("Saved key bindings/controls file could not be found");
			return;
		}

		savedControlsFile.Open(SAVE_CONTROLS_FILE_PATH, File.ModeFlags.Read);
		var line = savedControlsFile.GetLine();
		var controlData = JsonConvert.DeserializeObject<Dictionary<string, List<byte[]>>>(line);
		var controls = new Dictionary<string, List<InputEvent>>();
		foreach(var action in controlData.Keys){
			var temp = new List<InputEvent>();
			foreach(var binding in controlData[action]) {
				temp.Add((InputEvent)GD.Bytes2Var(binding,true));
			}
			controls[action] = temp;
		}
		savedControlsFile.Close();

		LoadControls(controls);
	}

	private void LoadControls(Dictionary<string,List<InputEvent>> bindings){		
        foreach(var a in bindings.Keys){
            InputMap.ActionEraseEvents(a);
            var inputs = bindings[a];
			foreach(var b in inputs){
				InputMap.ActionAddEvent(a,b);
			}
        }
	}

	private void CopyInitialBindingsToRuntimeMemory(){
        foreach(var a in ReassignableActions.Keys){
			var gBindings =  InputMap.GetActionList (a);
			var cBindings = new List<InputEvent>();
			foreach(var b in gBindings){
				cBindings.Add((InputEvent)b);
			}
			InitialBindings[a] = cBindings;
        }
	}

}
