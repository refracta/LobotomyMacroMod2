using System.Collections.Generic;
using Harmony;
using UnityEngine;

namespace abcdcode_Macro_MOD
{
public class CreatureCheck
{
	public static CreatureCheck creaturecheck;

	private List<MacroInfo> Macros = new List<MacroInfo>();

	public static void SetApAgent(AgentModel agent, CreatureModel creature, SkillTypeInfo info, Sprite sprite)
	{
		if (creaturecheck == null)
		{
			creaturecheck = new CreatureCheck();
		}
		creaturecheck.Macros.Add(new MacroInfo(agent, creature, info, sprite));
	}

	public static void RemoveApAgent(CreatureModel creature)
	{
		if (creaturecheck == null)
		{
			creaturecheck = new CreatureCheck();
			return;
		}
		CreatureCheck creatureCheck = creaturecheck;
		if (creatureCheck.Macros.Count == 0)
		{
			return;
		}
		for (int i = 0; i < creatureCheck.Macros.Count; i++)
		{
			if (creatureCheck.Macros[i].creature.metaInfo.name == creature.metaInfo.name)
			{
				RemovingBuf(creatureCheck.Macros[i].agent);
				creatureCheck.Macros.RemoveAt(i);
				break;
			}
		}
	}

	public static void RemoveApAgent(AgentModel agent)
	{
		if (creaturecheck == null)
		{
			creaturecheck = new CreatureCheck();
			return;
		}
		CreatureCheck creatureCheck = creaturecheck;
		if (creatureCheck.Macros.Count == 0)
		{
			return;
		}
		RemovingBuf(agent);
		for (int i = 0; i < creatureCheck.Macros.Count; i++)
		{
			if (((UnitModel)creatureCheck.Macros[i].agent).instanceId == ((UnitModel)agent).instanceId)
			{
				creatureCheck.Macros.RemoveAt(i);
				break;
			}
		}
	}

	public static AgentModel GetApAgent(CreatureModel creature)
	{
		if (creaturecheck == null)
		{
			creaturecheck = new CreatureCheck();
			return null;
		}
		foreach (MacroInfo macro in creaturecheck.Macros)
		{
			if (macro.creature.metaInfo.LcId == creature.metaInfo.LcId)
			{
				return macro.agent;
			}
		}
		return null;
	}

	public static SkillTypeInfo GetApWorkInfo(CreatureModel creature)
	{
		if (creaturecheck == null)
		{
			creaturecheck = new CreatureCheck();
		}
		foreach (MacroInfo macro in creaturecheck.Macros)
		{
			if (macro.creature.metaInfo.id == creature.metaInfo.id)
			{
				return macro.skillinfo;
			}
		}
		return null;
	}

	public static Sprite GetApWorkSprite(CreatureModel creature)
	{
		if (creaturecheck == null)
		{
			creaturecheck = new CreatureCheck();
		}
		foreach (MacroInfo macro in creaturecheck.Macros)
		{
			if (macro.creature.metaInfo.id == creature.metaInfo.id)
			{
				return macro.skillsprite;
			}
		}
		return null;
	}

	public static void RemovingBuf(AgentModel agent)
	{
		foreach (UnitBuf item in (List<UnitBuf>)((object)agent).GetType().GetField("_bufList", AccessTools.all).GetValue(agent))
		{
			if (item is MacroBurf)
			{
				((UnitModel)agent).RemoveUnitBuf(item);
				break;
			}
		}
	}
}
}
