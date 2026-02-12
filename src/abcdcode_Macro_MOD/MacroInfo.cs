using UnityEngine;

namespace abcdcode_Macro_MOD
{
public class MacroInfo
{
	public AgentModel agent;

	public CreatureModel creature;

	public SkillTypeInfo skillinfo;

	public Sprite skillsprite;

	public MacroInfo(AgentModel agent, CreatureModel creature, SkillTypeInfo info, Sprite sprite)
	{
		this.agent = agent;
		this.creature = creature;
		skillinfo = info;
		skillsprite = sprite;
	}
}
}
