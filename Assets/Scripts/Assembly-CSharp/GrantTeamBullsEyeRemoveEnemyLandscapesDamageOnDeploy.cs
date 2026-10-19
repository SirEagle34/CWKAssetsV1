public class GrantTeamBullsEyeRemoveEnemyLandscapesDamageOnDeploy : OnDeploy
{
	public override bool OnEnable()
	{
		bool result = false;
		
		
		foreach (LaneState lane in base.Owner.Owner.Lanes)
		{
			if (lane.Creature != null)
			{
				ApplyStatus(lane.Creature, StatusEnum.Pinpoint, base.Val1);
				result = true;
			}
		}
		
	
		foreach (LaneState lane in base.Owner.Owner.Opponent.Lanes)
		{
			if (lane.Creature != null)
			{
				// Check and remove landscapes
				StatusState statusState = lane.Creature.StatusEffects.Find((StatusState s) => s is LandCorn);
				StatusState statusState2 = lane.Creature.StatusEffects.Find((StatusState s) => s is LandPlains);
				StatusState statusState3 = lane.Creature.StatusEffects.Find((StatusState s) => s is LandSand);
				StatusState statusState4 = lane.Creature.StatusEffects.Find((StatusState s) => s is LandNice);
				StatusState statusState5 = lane.Creature.StatusEffects.Find((StatusState s) => s is LandSwamp);
				
				if (statusState != null)
				{
					CancelStatus(lane.Creature, StatusEnum.LandCorn);
				}
				if (statusState2 != null)
				{
					CancelStatus(lane.Creature, StatusEnum.LandPlains);
				}
				if (statusState3 != null)
				{
					CancelStatus(lane.Creature, StatusEnum.LandSand);
				}
				if (statusState4 != null)
				{
					CancelStatus(lane.Creature, StatusEnum.LandNice);
				}
				if (statusState5 != null)
				{
					CancelStatus(lane.Creature, StatusEnum.LandSwamp);
				}
				
				// Deal damage to ALL enemy creatures regardless of landscape
				ApplyStatus(lane.Creature, StatusEnum.DirectDamage, base.Val2);
				result = true;
			}
		}
		return result;
	}
}