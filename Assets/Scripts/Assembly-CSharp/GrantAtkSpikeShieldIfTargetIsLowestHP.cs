using UnityEngine;
public class GrantAtkSpikeShieldIfTargetIsLowestHP : OnAttack
{
    public override bool OnEnable()
    {
        if (Target == null)
        {
            return false;
        }
        
  
        int targetCurrentHP = Target.HP;
        
   
        bool targetIsLowest = true;
        
        foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
        {
            if (lane.Creature != null && lane.Creature != Target)
            {
                
                if (lane.Creature.HP < targetCurrentHP)
                {
                    targetIsLowest = false;
                    break;
                }
            }
        }
        
       
        if (targetIsLowest)
        {
            ApplyStatus(base.Owner, StatusEnum.AttackSpike, base.Val1);
            ApplyStatus(base.Owner, StatusEnum.Shield, base.Val2);
            return true;
        }
        
        return false;
    }
}