using System;
using UnityEngine;
using UnityEngine.UI;

public class UnitVisual : MonoBehaviour
{
    public Unit core;
    public CustomSpriteReader spriteReader;
    public SpriteRenderer selectionCircle;

    // HP Bar
    public Canvas hpBarCanvas;
    public Slider hpBarSlider;

    bool SetupSpriteAnimationPlayer(Unit core)
    {
        if (core.GetType() == typeof(MovableUnit))
        {
            MovableUnit controllableUnit = (MovableUnit)core;
            spriteReader.deterministicVisualUpdater = controllableUnit.GetDeterministicVisualUpdater();
            spriteReader.mainTransform = controllableUnit.transform;
            spriteReader.gameObject.SetActive(true);
            //spriteReader.DeterministicVisualUpdater_OnRefreshEvent();
            return true;
        }
        else if (core.GetType() == typeof(ProjectileUnit))
        {
            ProjectileUnit projectileUnit = (ProjectileUnit)core;
            spriteReader.deterministicVisualUpdater = projectileUnit.GetDeterministicVisualUpdater();
            spriteReader.mainTransform = projectileUnit.transform;
            spriteReader.gameObject.SetActive(true);
            hpBarCanvas.gameObject.SetActive(false);
        }
        return false;
    }

    internal void AttachToCore(Unit core)
    {
        this.core = core;
        if (spriteReader)
        {
            if (!SetupSpriteAnimationPlayer(core))
            {
                spriteReader.gameObject.SetActive(false);
            }
        }
    }

    internal void DetachFromCore()
    {
        if (spriteReader)
        {
            spriteReader.gameObject.SetActive(false);
        }
        core = null;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (core == null || !core.gameObject.activeSelf)
            return;

        transform.position = core.transform.position;
    }
}
