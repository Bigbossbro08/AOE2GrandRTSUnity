using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class CommandPanelUI : MonoBehaviour
{
    private List<CommandButton> commandButtons = new();

    //[SerializeField]
    //private Sprite attackMoveSprite = null;

    public System.Action<RaycastHit> HandleOrder = (hit) => { };

    [SerializeField]
    SelectionPanel selectionPanel;

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

    bool blockUI = false;

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

        if (!blockUI)
        {
            // Update Command Panel here
            FigureoutPanelFromSelection(selectionPanel.GetSelectedUnits());
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
        SetOrderAction(null);
        return new List<CommandButton.Command>(); ;
    }

    void SetOrderAction(System.Action<RaycastHit> newOrderAction)
    {
        if (newOrderAction == null)
        {
            newOrderAction = (position) => { };
        }
        HandleOrder = newOrderAction;
        HandleOrder += (position) =>
        {
            blockUI = false;
        };
    }

    void StartBlockUI()
    {
        blockUI = true;
    }

    public List<CommandButton.Command> GetMilitaryUnitCommands()
    {
        // TODO: Add proper commands
        List<CommandButton.Command> commandButtons = new List<CommandButton.Command>();

        CommandButton.Command attackMove = new CommandButton.Command();
        attackMove.SlotId = 0;

        string attackMoveSpriteName = "data\\ui_buttons\\attack_move";
        CustomSpriteLoader.IconReturnData attackMoveSprite = CustomSpriteLoader.Instance.LoadIconSprite(attackMoveSpriteName);
        if (attackMoveSprite != null && attackMoveSprite.sprite != null)
        {
            attackMove.Icon = attackMoveSprite.sprite;
        }
        attackMove.Name = "Attack Move";
        attackMove.Callback = () => {
            //Debug.Log($"Clicked for Attack Move");
            System.Action OnCancel = () => {
                //SetCommands(GetShipUnDockedCommands());
                FigureoutPanelFromSelection(selectionPanel.GetSelectedUnits());
                Debug.Log("Cancelled attack move and going back to normal");
            };
            SetCommands(GetOrderOrCancelCommands("Attack Move", OnCancel));
            StartBlockUI();
        }; // Implement Attack Move Toggle when pressed

        commandButtons.Add(attackMove);

        SetOrderAction((hit) =>
        {
            List<ulong> ids = new List<ulong>();
            foreach (Unit u in selectionPanel.GetSelectedUnits())
            {
                if (u.GetType() == typeof(MovableUnit))
                {
                    if (u.playerId == 1)
                        ids.Add(u.GetUnitID());
                }
            }

            if (ids.Count > 0)
            {
                MoveUnitsCommand moveUnitsCommand = new MoveUnitsCommand();
                moveUnitsCommand.action = MoveUnitsCommand.commandName;
                moveUnitsCommand.unitIDs = new List<ulong>();
                moveUnitsCommand.unitIDs.AddRange(ids);
                moveUnitsCommand.position = hit.point;
                moveUnitsCommand.IsAttackMove = this.blockUI;
                InputManager.Instance.SendInputCommand(moveUnitsCommand);
            }
        });
        return commandButtons;
    }

    public List<CommandButton.Command> GetOrderOrCancelCommands(string name, System.Action OnCancel)
    {
        blockUI = true;
        List<CommandButton.Command> commandButtons = new List<CommandButton.Command>();
        CommandButton.Command action = new CommandButton.Command();
        action.SlotId = 14;
        string stopSpriteName = "data\\ui_buttons\\cancelIcon";
        CustomSpriteLoader.IconReturnData stopSprite = CustomSpriteLoader.Instance.LoadIconSprite(stopSpriteName);
        if (stopSprite != null && stopSprite.sprite != null)
        {
            action.Icon = stopSprite.sprite;
        }
        action.Name = name;
        if (OnCancel != null)
        {
            OnCancel += () => { blockUI = false; };
        }
        action.Callback = () => OnCancel?.Invoke();
        commandButtons.Add(action);
        return commandButtons;
    }

    public List<CommandButton.Command> GetShipDockedCommands()
    {
        List<CommandButton.Command> commandButtons = new List<CommandButton.Command>();

        CommandButton.Command dockOnShore = new CommandButton.Command();
        SetOrderAction((hit) =>
        {
            List<ulong> ids = new List<ulong>();
            foreach (Unit u in selectionPanel.GetSelectedUnits())
            {
                if (u.GetType() == typeof(MovableUnit))
                {
                    if (u.playerId == 1)
                        ids.Add(u.GetUnitID());
                }
            }

            if (ids.Count > 0)
            {
                if (this.blockUI)
                {
                    if (hit.collider.TryGetComponent(out MovableUnit targetUnit))
                    {
                        if (targetUnit.IsShip())
                        {
                            
                        }
                        Debug.Log($"{hit.collider.name}");
                    }
                    DockShipUnitCommand dockShipUnitCommand = new DockShipUnitCommand();
                    dockShipUnitCommand.action = DockShipUnitCommand.commandName;
                    dockShipUnitCommand.unitIDs = new List<ulong>();
                    dockShipUnitCommand.unitIDs.AddRange(ids);
                    dockShipUnitCommand.position = hit.point;
                    InputManager.Instance.SendInputCommand(dockShipUnitCommand);
                }
                else
                {
                    MoveUnitsCommand moveUnitsCommand = new MoveUnitsCommand();
                    moveUnitsCommand.action = MoveUnitsCommand.commandName;
                    moveUnitsCommand.unitIDs = new List<ulong>();
                    moveUnitsCommand.unitIDs.AddRange(ids);
                    moveUnitsCommand.position = hit.point;
                    moveUnitsCommand.IsAttackMove = this.blockUI;
                    InputManager.Instance.SendInputCommand(moveUnitsCommand);
                }
            }
        });
        return commandButtons;
    }

    public List<CommandButton.Command> GetShipUnDockedCommands()
    {
        List<CommandButton.Command> commandButtons = new List<CommandButton.Command>();

        CommandButton.Command dockOnShore = new CommandButton.Command();
        dockOnShore.SlotId = 0;
        string attackMoveSpriteName = "data\\ui_buttons\\dock_on_shore";
        CustomSpriteLoader.IconReturnData dockOnShoreSprite = CustomSpriteLoader.Instance.LoadIconSprite(attackMoveSpriteName);
        if (dockOnShoreSprite != null && dockOnShoreSprite.sprite != null)
        {
            dockOnShore.Icon = dockOnShoreSprite.sprite;
        }
        dockOnShore.Name = "Ship Dock";
        dockOnShore.Callback = () => {
            System.Action OnCancel = () => {
                SetCommands(GetShipUnDockedCommands());
                Debug.Log("Cancelled command and now getting backed undocked command UI");
            };
            SetCommands(GetOrderOrCancelCommands("Dock ship", OnCancel));
            //AttackMove = true;
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
        // 2 = ship command docked
        // 3 = ship command undocked
        int useCommandType = 0;

        foreach (Unit unit in selectedUnits)
        {
            if (unit.GetType() == typeof(MovableUnit))
            {
                MovableUnit movableUnit = (MovableUnit)unit;
                if (movableUnit.shipData.isShipMode)
                {
                    if (movableUnit.shipData.isDocked) 
                        useCommandType = 2;
                    else 
                        useCommandType = 3;
                    break;
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
                    SetCommands(GetShipDockedCommands());
                }
                break;
            case 3:
                {
                    SetCommands(GetShipUnDockedCommands());
                }
                break;
        }
    }
}
