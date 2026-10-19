using UnityEngine;

public class EnhanceVGR : StatusState
{
    // StatusState içindeki 'Target' (veya yaratığı temsil eden değişken) referansı
    private CreatureState creatureState;

    protected override void OnEnable()
    {
        creatureState = this.Target as CreatureState;

        if (creatureState?.SourceItem != null)
        {
            creatureState.SourceItem.EnhancePassive = true;
        }

        base.OnEnable();
    }

    protected override void OnDisable()
    {
        creatureState ??= this.Target as CreatureState;

        if (creatureState?.SourceItem != null)
        {
            creatureState.SourceItem.EnhancePassive = false;
        }

        base.OnDisable();
    }
}