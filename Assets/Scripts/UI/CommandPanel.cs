using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CommandPanelUI : MonoBehaviour
{
    private List<CommandButton> commandButtons = new();

    [SerializeField]
    private Sprite attackMoveSprite = null;

    [SerializeField]
    private Transform context;

    Dictionary<KeyCode, int> KeyCodeToIdMap = new Dictionary<KeyCode, int>() {
        { KeyCode.Q, 0 },
        { KeyCode.W, 1 },
        { KeyCode.E, 2 },
        { KeyCode.R, 3 },
        { KeyCode.T, 4 },
        { KeyCode.A, 5 },
        { KeyCode.S, 6 },
        { KeyCode.D, 7 },
        { KeyCode.F, 8 },
        { KeyCode.G, 9 },
        { KeyCode.Z, 10 },
        { KeyCode.X, 11 },
        { KeyCode.C, 12 },
        { KeyCode.V, 13 },
        { KeyCode.B, 14 },
    };

    public bool AttackMove = false;

    private void Start()
    {
        int counter = 0;
        foreach (Transform child in context)
        {
            child.gameObject.name = $"Command Button: {counter}";
            CommandButton button = child.GetComponent<CommandButton>();
            commandButtons.Add(button);
            counter++;
        }

        // TODO: Figure out command panel type based on selection
        //SetCommands(GetMilitaryUnitCommands());
        SetCommands(GetNoCommands());
    }

    private void Update()
    {
        foreach (var keymap in KeyCodeToIdMap)
        {
            if (Input.GetKeyDown(keymap.Key))
            {
                int id = keymap.Value;
                commandButtons[id].SetVisualStateToPressed();
                //commandButtons[id].onClick.Invoke(); // optional
            }

            if (Input.GetKeyUp(keymap.Key))
            {
                int id = keymap.Value;
                commandButtons[id].SetVisualStateToNormal();
                commandButtons[id].onClick.Invoke(); // optional
            }
        }
    }

    public void ClearPanel()
    {
        foreach (var btn in commandButtons)
        {
            btn.onClick.RemoveAllListeners();
            btn.gameObject.SetActive(false);
        }
    }

    public void SetCommands(List<CommandButton.Command> commands)
    {
        ClearPanel();
        for (int i = 0; i < commands.Count; i++)
        {
            var command = commands[i];
            if (command.SlotId < 0 || command.SlotId >= commandButtons.Count) continue;

            CommandButton btn = commandButtons[command.SlotId];
            if (btn)
            {
                btn.gameObject.SetActive(true);
                btn.Setup(commands[i]);
            }
        }
    }

    public List<CommandButton.Command> GetNoCommands()
    {
        return new List<CommandButton.Command>(); ;
    }

    public List<CommandButton.Command> GetMilitaryUnitCommands()
    {
        // TODO: Add proper commands
        List<CommandButton.Command> commandButtons = new List<CommandButton.Command>();

        CommandButton.Command attackMove = new CommandButton.Command();
        attackMove.SlotId = 0;
        attackMove.Icon = attackMoveSprite;
        attackMove.Name = "Attack Move";
        attackMove.Callback = () => {
            Debug.Log($"Clicked for Attack Move");
            AttackMove = true; 
        }; // Implement Attack Move Toggle when pressed

        //CommandButton.Command patrolMove = new CommandButton.Command();
        //patrolMove.SlotId = 1;
        //patrolMove.Name = "Patrol Move";
        //patrolMove.Callback = () => { }; // Implement Patrol Move Toggle when pressed
        
        //CommandButton.Command normalMove = new CommandButton.Command();
        //normalMove.SlotId = 4;
        //normalMove.Name = "Normal Move";
        //normalMove.Callback = () => { }; // Implement Patrol Move Toggle when pressed

        commandButtons.Add(attackMove);
        //commandButtons.Add(patrolMove);
        //commandButtons.Add(normalMove);

        return commandButtons;
    }

    public List<CommandButton.Command> GetShipCommands()
    {
        List<CommandButton.Command> commandButtons = new List<CommandButton.Command>();

        CommandButton.Command dockOnShore = new CommandButton.Command();
        dockOnShore.SlotId = 0;
        dockOnShore.Icon = attackMoveSprite;
        dockOnShore.Name = "Ship Dock";
        dockOnShore.Callback = () => {
            Debug.Log("$Clicked for docking on shore");
            AttackMove = true;
        };
        commandButtons.Add(dockOnShore);
        return commandButtons;
    }

    public List<CommandButton.Command> GetVillagerUnitCommands()
    {
        // TODO: Add proper commands
        List<CommandButton.Command> commandButtons = new List<CommandButton.Command>();

        CommandButton.Command buildCommand = new CommandButton.Command();
        buildCommand.SlotId = 0;
        buildCommand.Name = "Build";
        buildCommand.Callback = () => { }; // Implement Attack Move Toggle when pressed

        CommandButton.Command repairMove = new CommandButton.Command();
        repairMove.SlotId = 1;
        repairMove.Name = "Repair";
        repairMove.Callback = () => { }; // Implement Patrol Move Toggle when pressed

        commandButtons.Add(buildCommand);
        commandButtons.Add(repairMove);

        return commandButtons;
    }

    internal void FigureoutPanelFromSelection(List<Unit> selectedUnits)
    {
        if (selectedUnits == null || selectedUnits.Count == 0)
        {
            SetCommands(GetNoCommands());
            return;
        }

        // 0 = no command.
        // 1 = military command
        // 2 = ship command
        int useCommandType = 0;

        foreach (Unit unit in selectedUnits)
        {
            if (unit.GetType() == typeof(MovableUnit))
            {
                MovableUnit movableUnit = (MovableUnit)unit;
                if (movableUnit.shipData.isShipMode)
                {
                    useCommandType = 2;
                    return;
                }
                if (movableUnit.unitTypeComponent.GetType() == typeof(CombatComponent))
                {
                    useCommandType = 1;
                }
            }
        }

        switch (useCommandType)
        {
            case 1:
                {
                    SetCommands(GetMilitaryUnitCommands());
                }
                break;
            case 2:
                {
                    SetCommands(GetShipCommands());
                }
                break;
        }
    }
}
