using UnityEngine;
using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class CommandButton : Button
{
    public class Command
    {
        public int SlotId;
        public string Name;
        public Sprite Icon;
        public Action Callback; // Action to execute on click
    }

    public Slider hpBarSlider;

    public void Setup(Command command)
    {
        image.sprite = command.Icon;
        onClick.RemoveAllListeners();
        onClick.AddListener(() => { command.Callback?.Invoke(); });
    }

    public void SetVisualStateToPressed()
    {
        DoStateTransition(SelectionState.Pressed, false);
    }

    public void SetVisualStateToNormal()
    {
        DoStateTransition(SelectionState.Normal, false);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        //Debug.Log("Pointer is down");
        base.OnPointerDown(eventData);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        //Debug.Log("Pointer has entered");
        base.OnPointerEnter(eventData);
    }
}