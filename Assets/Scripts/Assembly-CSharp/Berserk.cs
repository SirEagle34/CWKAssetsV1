public class Berserk : StatusState 
{ 
    public override void ProcessMessage(GameMessage Message) 
    { 
        if (base.Target != null && base.Target.HP > 0 && 
            Message.Action == GameEvent.START_TURN && 
            Message.WhichPlayer == base.Target.Owner && 
            base.Count > 0 && 
            base.Target.AttackCost <= base.Target.Owner.ActionPoints) 
        { 
            CreatureState targetCreature = null;
            bool foundBravery = false;
            
       
            foreach (LaneState lane in base.Target.Owner.Opponent.Lanes) 
            { 
                if (lane.Creature != null && lane.Creature.HasBravery) 
                { 
                    if (!foundBravery || lane.Creature.HP > targetCreature.HP)
                    {
                        targetCreature = lane.Creature;
                        foundBravery = true;
                    }
                } 
            }
            
        
            if (!foundBravery)
            {
                foreach (LaneState lane in base.Target.Owner.Opponent.Lanes) 
                { 
                    if (lane.Creature != null && 
                        (targetCreature == null || lane.Creature.HP > targetCreature.HP)) 
                    { 
                        targetCreature = lane.Creature; 
                    } 
                }
            }
            
            if (targetCreature != null) 
            { 
                base.Target.Owner.AttackAndDraw(
                    base.Target.Lane.Index, 
                    targetCreature.Lane.Index, 
                    AttackCause.Berserk
                );
            }
            
            TickCount(); 
        } 
    } 
}