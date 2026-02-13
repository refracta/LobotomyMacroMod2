using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using Harmony;
using UnityEngine;

namespace refracta_WorkBookmark_MOD
{
public class Harmony_Patch
{
	private class SavedWorkEntry
	{
		public string AgentName;

		public long CreatureId;

		public string CreatureName;

		public long SkillId;

		public string SkillName;
	}

	private const string InsertPrefKey = "Lobotomy.refracta.WorkBookmark.Insert";

	private const int SavedFieldCount = 5;

	private static readonly char[] EntrySeparator = new char[1] { ';' };

	private static readonly char[] FieldSeparator = new char[1] { ',' };

	private static readonly FieldInfo ManageCommandCreatureField = AccessTools.Field(typeof(ManageCreatureAgentCommand), "targetCreature");

	private static readonly FieldInfo ManageCommandSkillField = AccessTools.Field(typeof(ManageCreatureAgentCommand), "skill");

	private static bool _insertHandledByHold;

	private static bool _homeHandledByHold;

	private static bool _endHandledByHold;

	public Harmony_Patch()
	{
		try
		{
			HarmonyInstance harmonyInstance = HarmonyInstance.Create("Lobotomy.refracta.WorkBookmark");
			MethodInfo method = typeof(Harmony_Patch).GetMethod("UnitMouseEventManager_Update_Postfix");
			harmonyInstance.Patch((MethodBase)typeof(UnitMouseEventManager).GetMethod("Update", AccessTools.all), (HarmonyMethod)null, new HarmonyMethod(method), (HarmonyMethod)null);
		}
		catch (Exception ex)
		{
			Debug.Log((object)("[WorkBookmark] init failed: " + ex.Message));
		}
	}

	public static void UnitMouseEventManager_Update_Postfix(UnitMouseEventManager __instance)
	{
		try
		{
			if (ConsumeInsertTrigger())
			{
				if (IsShiftHeld() && !IsCtrlHeld())
				{
					PrintInsertLayout();
				}
				else
				{
					CaptureInsertLayout();
				}
			}
			if (ConsumeHomeTrigger())
			{
				ApplyInsertLayout();
			}
			if (ConsumeEndTrigger())
			{
				CancelMovingSavedAgents();
			}
		}
		catch (Exception ex)
		{
			SendSystemLog(Localize("작업 북마크 처리 중 오류가 발생했습니다.", "An error occurred in Work Bookmark mod."));
			Debug.Log((object)("[WorkBookmark] " + ex));
		}
	}

	private static bool IsShiftHeld()
	{
		return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
	}

	private static bool IsCtrlHeld()
	{
		return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey((KeyCode)306) || Input.GetKey((KeyCode)305);
	}

	private static bool IsEnglishLanguage()
	{
		try
		{
			if (GlobalGameManager.instance != null)
			{
				string language = GlobalGameManager.instance.language;
				if (!string.IsNullOrEmpty(language) && string.Equals(language, SupportedLanguage.en, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
				if (GlobalGameManager.instance.Language == SystemLanguage.English)
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static string Localize(string korean, string english)
	{
		if (IsEnglishLanguage())
		{
			return english;
		}
		return korean;
	}

	private static bool IsInsertDown()
	{
		return Input.GetKeyDown(KeyCode.Insert) || Input.GetKeyDown(KeyCode.Keypad0);
	}

	private static bool IsHomeDown()
	{
		return Input.GetKeyDown(KeyCode.Home) || Input.GetKeyDown(KeyCode.Keypad7);
	}

	private static bool IsEndDown()
	{
		return Input.GetKeyDown(KeyCode.End) || Input.GetKeyDown(KeyCode.Keypad1);
	}

	private static bool IsInsertHeld()
	{
		return Input.GetKey(KeyCode.Insert) || Input.GetKey(KeyCode.Keypad0);
	}

	private static bool IsHomeHeld()
	{
		return Input.GetKey(KeyCode.Home) || Input.GetKey(KeyCode.Keypad7);
	}

	private static bool IsEndHeld()
	{
		return Input.GetKey(KeyCode.End) || Input.GetKey(KeyCode.Keypad1);
	}

	private static bool ConsumeInsertTrigger()
	{
		if (IsInsertDown())
		{
			_insertHandledByHold = true;
			return true;
		}
		if (!IsInsertHeld())
		{
			_insertHandledByHold = false;
			return false;
		}
		if (_insertHandledByHold)
		{
			return false;
		}
		_insertHandledByHold = true;
		return true;
	}

	private static bool ConsumeHomeTrigger()
	{
		if (IsHomeDown())
		{
			_homeHandledByHold = true;
			return true;
		}
		if (!IsHomeHeld())
		{
			_homeHandledByHold = false;
			return false;
		}
		if (_homeHandledByHold)
		{
			return false;
		}
		_homeHandledByHold = true;
		return true;
	}

	private static bool ConsumeEndTrigger()
	{
		if (IsEndDown())
		{
			_endHandledByHold = true;
			return true;
		}
		if (!IsEndHeld())
		{
			_endHandledByHold = false;
			return false;
		}
		if (_endHandledByHold)
		{
			return false;
		}
		_endHandledByHold = true;
		return true;
	}

	private static void CaptureInsertLayout()
	{
		List<SavedWorkEntry> currentManagedAgents = GetCurrentManagedAgents();
		SaveInsertEntries(currentManagedAgents);
		PlayerPrefs.Save();
		SendSystemLog(Localize("Insert 저장: ", "Insert saved: ") + FormatEntries(currentManagedAgents));
	}

	private static void PrintInsertLayout()
	{
		List<SavedWorkEntry> entries = LoadInsertEntries();
		SendSystemLog(Localize("Shift + Insert 확인: ", "Shift + Insert: ") + FormatEntries(entries));
	}

	private static void ApplyInsertLayout()
	{
		List<SavedWorkEntry> entries = LoadInsertEntries();
		if (entries.Count == 0)
		{
			SendSystemLog(Localize("Home 실행: Insert 저장 목록이 비어 있음", "Home: Insert save list is empty."));
			return;
		}

		if (AgentManager.instance == null)
		{
			SendSystemLog(Localize("Home 실행: 직원 관리자 접근 실패", "Home: failed to access agent manager."));
			return;
		}

		IList<AgentModel> agentList = AgentManager.instance.GetAgentList();
		Dictionary<string, AgentModel> dictionary = new Dictionary<string, AgentModel>(StringComparer.Ordinal);
		for (int i = 0; i < agentList.Count; i++)
		{
			AgentModel agentModel = agentList[i];
			if (agentModel != null && !string.IsNullOrEmpty(agentModel.name) && !dictionary.ContainsKey(agentModel.name))
			{
				dictionary.Add(agentModel.name, agentModel);
			}
		}

		if (dictionary.Count == 0)
		{
			SendSystemLog(Localize("Home 실행: 동원 가능한 직원이 없음", "Home: no dispatchable agents."));
			return;
		}

		List<string> assigned = new List<string>();
		List<string> skipped = new List<string>();
		HashSet<long> usedCreatureIds = new HashSet<long>();
		for (int i = 0; i < entries.Count; i++)
		{
			SavedWorkEntry entry = entries[i];
			if (entry == null || string.IsNullOrEmpty(entry.AgentName))
			{
				continue;
			}

			AgentModel agent;
			if (!dictionary.TryGetValue(entry.AgentName, out agent))
			{
				skipped.Add(entry.AgentName + ": " + Localize("직원 없음", "agent not found"));
				continue;
			}
			if (!IsDispatchable(agent))
			{
				skipped.Add(entry.AgentName + ": " + Localize("동원 불가", "not dispatchable"));
				continue;
			}

			CreatureModel creature = ResolveCreature(entry);
			SkillTypeInfo skill = ResolveSkill(entry);
			if (creature == null || skill == null || creature.IsEscaped() || creature.script == null || !creature.script.IsWorkable())
			{
				skipped.Add(entry.AgentName + ": " + Localize("대상 해석 실패", "target resolution failed"));
				continue;
			}
			if (usedCreatureIds.Contains(creature.instanceId))
			{
				skipped.Add(entry.AgentName + ": " + GetCreatureName(creature) + " " + Localize("중복 배정", "already assigned"));
				continue;
			}
			if (IsCreatureBusyForWork(creature))
			{
				skipped.Add(entry.AgentName + ": " + GetCreatureName(creature) + " " + Localize("이미 작업중", "already being worked on"));
				continue;
			}

			Sprite workIconSprite = GetWorkIconSprite(skill);
			agent.ManageCreature(creature, skill, workIconSprite);
			agent.counterAttackEnabled = false;
			if (creature.Unit != null && creature.Unit.room != null)
			{
				creature.Unit.room.OnWorkAllocated(agent);
			}
			if (creature.script != null)
			{
				creature.script.OnWorkAllocated(skill, agent);
			}
			if (AngelaConversation.instance != null)
			{
				AngelaConversation.instance.MakeMessage((AngelaMessageState)3, new object[3] { agent, skill, creature });
			}
			usedCreatureIds.Add(creature.instanceId);
			assigned.Add(agent.name + " -> " + GetCreatureName(creature) + " (" + GetSkillName(skill) + ")");
		}

		if (assigned.Count == 0)
		{
			SendSystemLog(Localize("Home 실행: 지정 가능한 작업이 없음 (", "Home: no applicable assignments (") + string.Join(", ", skipped.ToArray()) + ")");
			return;
		}

		SendSystemLog(Localize("Home 실행: ", "Home: ") + string.Join(", ", assigned.ToArray()));
		if (skipped.Count > 0)
		{
			SendSystemLog(Localize("Home 스킵: ", "Home skipped: ") + string.Join(", ", skipped.ToArray()));
		}
	}

	private static void CancelMovingSavedAgents()
	{
		List<SavedWorkEntry> entries = LoadInsertEntries();
		if (entries.Count == 0)
		{
			SendSystemLog(Localize("End 실행: Insert 저장 목록이 비어 있음", "End: Insert save list is empty."));
			return;
		}

		if (AgentManager.instance == null)
		{
			SendSystemLog(Localize("End 실행: 직원 관리자 접근 실패", "End: failed to access agent manager."));
			return;
		}

		HashSet<string> targetNames = new HashSet<string>(StringComparer.Ordinal);
		for (int i = 0; i < entries.Count; i++)
		{
			if (!string.IsNullOrEmpty(entries[i].AgentName))
			{
				targetNames.Add(entries[i].AgentName);
			}
		}

		IList<AgentModel> agentList = AgentManager.instance.GetAgentList();
		List<string> canceled = new List<string>();
		for (int i = 0; i < agentList.Count; i++)
		{
			AgentModel agent = agentList[i];
			if (agent == null || !targetNames.Contains(agent.name))
			{
				continue;
			}

			CreatureModel creature;
			SkillTypeInfo skill;
			if (!TryGetWorkFromAgent(agent, out creature, out skill))
			{
				continue;
			}

			MovableObjectNode movableNode = agent.GetMovableNode();
			bool isMoving = movableNode != null && movableNode.IsMoving();
			bool manageQueued = agent.GetState() == AgentAIState.MANAGE && agent.currentSkill == null;
			if (isMoving || manageQueued)
			{
				agent.StopAction();
				canceled.Add(agent.name);
			}
		}

		if (canceled.Count == 0)
		{
			SendSystemLog(Localize("End 실행: 취소할 이동중 작업이 없음", "End: no en-route assignments to cancel."));
			return;
		}

		SendSystemLog(Localize("End 실행 취소: ", "End canceled: ") + string.Join(", ", canceled.ToArray()));
	}

	private static bool IsDispatchable(AgentModel agent)
	{
		if (agent == null || agent.IsDead() || agent.IsCrazy() || agent.CannotControll())
		{
			return false;
		}
		return agent.GetState() == AgentAIState.IDLE;
	}

	private static List<SavedWorkEntry> GetCurrentManagedAgents()
	{
		List<SavedWorkEntry> list = new List<SavedWorkEntry>();
		HashSet<string> nameSet = new HashSet<string>(StringComparer.Ordinal);
		if (AgentManager.instance == null)
		{
			return list;
		}
		IList<AgentModel> agentList = AgentManager.instance.GetAgentList();
		for (int i = 0; i < agentList.Count; i++)
		{
			AgentModel agent = agentList[i];
			if (agent == null || agent.IsDead() || agent.IsCrazy())
			{
				continue;
			}

			CreatureModel creature;
			SkillTypeInfo skill;
			if (!TryGetWorkFromAgent(agent, out creature, out skill))
			{
				continue;
			}

			if (string.IsNullOrEmpty(agent.name) || nameSet.Contains(agent.name))
			{
				continue;
			}

			nameSet.Add(agent.name);
			SavedWorkEntry entry = new SavedWorkEntry();
			entry.AgentName = agent.name;
			entry.CreatureId = ((creature.metaInfo != null) ? creature.metaInfo.id : creature.metadataId);
			entry.CreatureName = GetCreatureName(creature);
			entry.SkillId = skill.id;
			entry.SkillName = GetSkillName(skill);
			list.Add(entry);
		}
		return list;
	}

	private static bool TryGetWorkFromAgent(AgentModel agent, out CreatureModel creature, out SkillTypeInfo skill)
	{
		creature = null;
		skill = null;
		if (agent == null)
		{
			return false;
		}

		WorkerCommand currentCommand = ((WorkerModel)agent).GetCurrentCommand();
		if (TryGetWorkFromCommand(currentCommand, out creature, out skill))
		{
			return true;
		}

		UseSkill currentSkill = agent.currentSkill;
		if (currentSkill != null && currentSkill.targetCreature != null && currentSkill.skillTypeInfo != null)
		{
			creature = currentSkill.targetCreature;
			skill = currentSkill.skillTypeInfo;
			return true;
		}

		if (agent.GetState() == AgentAIState.MANAGE && agent.target != null)
		{
			SkillTypeInfo recentWork = agent.GetRecentWork();
			if (recentWork != null)
			{
				creature = agent.target;
				skill = recentWork;
				return true;
			}
		}

		return false;
	}

	private static bool TryGetWorkFromCommand(WorkerCommand command, out CreatureModel creature, out SkillTypeInfo skill)
	{
		creature = null;
		skill = null;
		if (!(command is ManageCreatureAgentCommand))
		{
			return false;
		}

		try
		{
			if (!object.ReferenceEquals(ManageCommandCreatureField, null))
			{
				creature = ManageCommandCreatureField.GetValue(command) as CreatureModel;
			}
			if (!object.ReferenceEquals(ManageCommandSkillField, null))
			{
				skill = ManageCommandSkillField.GetValue(command) as SkillTypeInfo;
			}
		}
		catch
		{
			return false;
		}

		return creature != null && skill != null;
	}

	private static string GetCreatureName(CreatureModel creature)
	{
		if (creature == null)
		{
			return string.Empty;
		}
		string text = creature.GetUnitName();
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		if (creature.metaInfo != null && !string.IsNullOrEmpty(creature.metaInfo.name))
		{
			return creature.metaInfo.name;
		}
		return "Unknown Creature";
	}

	private static string GetSkillName(SkillTypeInfo skill)
	{
		if (skill == null)
		{
			return string.Empty;
		}
		SkillTypeInfo data = SkillTypeList.instance.GetData(skill.id);
		if (data != null && !string.IsNullOrEmpty(data.name))
		{
			return data.name.Trim();
		}
		if (!string.IsNullOrEmpty(skill.name))
		{
			return skill.name.Trim();
		}
		if (!string.IsNullOrEmpty(skill.calledName))
		{
			return skill.calledName.Trim();
		}
		return "Work " + skill.id.ToString(CultureInfo.InvariantCulture);
	}

	private static CreatureModel ResolveCreature(SavedWorkEntry entry)
	{
		if (CreatureManager.instance == null)
		{
			return null;
		}
		CreatureModel[] creatureList = CreatureManager.instance.GetCreatureList();
		CreatureModel idMatch = null;
		CreatureModel nameMatch = null;
		for (int i = 0; i < creatureList.Length; i++)
		{
			CreatureModel creature = creatureList[i];
			if (creature == null || creature.metaInfo == null)
			{
				continue;
			}

			string creatureName = GetCreatureName(creature);
			if (creature.metaInfo.id == entry.CreatureId)
			{
				if (string.Equals(creatureName, entry.CreatureName, StringComparison.Ordinal))
				{
					return creature;
				}
				if (idMatch == null)
				{
					idMatch = creature;
				}
			}
			if (nameMatch == null && string.Equals(creatureName, entry.CreatureName, StringComparison.Ordinal))
			{
				nameMatch = creature;
			}
		}
		if (idMatch != null)
		{
			return idMatch;
		}
		return nameMatch;
	}

	private static SkillTypeInfo ResolveSkill(SavedWorkEntry entry)
	{
		SkillTypeInfo data = SkillTypeList.instance.GetData(entry.SkillId);
		if (data != null)
		{
			return data;
		}
		if (!string.IsNullOrEmpty(entry.SkillName))
		{
			return SkillTypeList.instance.GetDataByName(entry.SkillName);
		}
		return null;
	}

	private static bool IsCreatureBusyForWork(CreatureModel creature)
	{
		if (creature == null)
		{
			return true;
		}
		if (creature.currentSkill != null)
		{
			return true;
		}
		if (creature.state == CreatureState.WORKING || creature.state == CreatureState.WORKING_SCENE || creature.state == CreatureState.WORKING_AUTO || creature.state == CreatureState.OBSERVE)
		{
			return true;
		}
		if (creature.script != null && creature.script.isWorkAllocated)
		{
			return true;
		}
		if (AgentManager.instance == null)
		{
			return false;
		}
		IList<AgentModel> agentList = AgentManager.instance.GetAgentList();
		for (int i = 0; i < agentList.Count; i++)
		{
			AgentModel agent = agentList[i];
			if (agent == null || agent.IsDead() || agent.IsCrazy())
			{
				continue;
			}
			CreatureModel assignedCreature;
			SkillTypeInfo _;
			if (TryGetWorkFromAgent(agent, out assignedCreature, out _) && assignedCreature != null && assignedCreature.instanceId == creature.instanceId)
			{
				return true;
			}
		}
		return false;
	}

	private static Sprite GetWorkIconSprite(SkillTypeInfo skill)
	{
		if (skill == null || IconManager.instance == null)
		{
			return null;
		}
		try
		{
			int workIconId = AgentModel.GetWorkIconId(skill);
			object workIcon = IconManager.instance.GetWorkIcon(workIconId);
			if (object.ReferenceEquals(workIcon, null))
			{
				return null;
			}
			MethodInfo method = workIcon.GetType().GetMethod("GetIcon", AccessTools.all);
			if (object.ReferenceEquals(method, null))
			{
				return null;
			}
			object iconData = null;
			ParameterInfo[] parameters = method.GetParameters();
			if (parameters.Length == 1)
			{
				object iconType = (parameters[0].ParameterType.IsEnum ? Enum.ToObject(parameters[0].ParameterType, 0) : ((object)0));
				iconData = method.Invoke(workIcon, new object[1] { iconType });
			}
			else
			{
				iconData = method.Invoke(workIcon, null);
			}
			if (object.ReferenceEquals(iconData, null))
			{
				return null;
			}
			FieldInfo field = iconData.GetType().GetField("icon", AccessTools.all);
			if (!object.ReferenceEquals(field, null))
			{
				return field.GetValue(iconData) as Sprite;
			}
			PropertyInfo property = iconData.GetType().GetProperty("icon", AccessTools.all);
			if (!object.ReferenceEquals(property, null))
			{
				return property.GetValue(iconData, null) as Sprite;
			}
		}
		catch
		{
		}
		return null;
	}

	private static string FormatEntries(List<SavedWorkEntry> entries)
	{
		if (entries.Count == 0)
		{
			return Localize("[비어 있음]", "[Empty]");
		}
		List<string> list = new List<string>();
		for (int i = 0; i < entries.Count; i++)
		{
			SavedWorkEntry entry = entries[i];
			list.Add(entry.AgentName + " -> " + entry.CreatureName + " (" + entry.SkillName + ")");
		}
		return string.Join(", ", list.ToArray());
	}

	private static void SaveInsertEntries(List<SavedWorkEntry> entries)
	{
		List<string> list = new List<string>();
		for (int i = 0; i < entries.Count; i++)
		{
			SavedWorkEntry entry = entries[i];
			string[] value = new string[SavedFieldCount]
			{
				Base64Encode(entry.AgentName),
				Base64Encode(entry.CreatureId.ToString(CultureInfo.InvariantCulture)),
				Base64Encode(entry.CreatureName),
				Base64Encode(entry.SkillId.ToString(CultureInfo.InvariantCulture)),
				Base64Encode(entry.SkillName)
			};
			list.Add(string.Join(",", value));
		}
		PlayerPrefs.SetString(InsertPrefKey, string.Join(";", list.ToArray()));
	}

	private static List<SavedWorkEntry> LoadInsertEntries()
	{
		List<SavedWorkEntry> list = new List<SavedWorkEntry>();
		string text = PlayerPrefs.GetString(InsertPrefKey, string.Empty);
		if (string.IsNullOrEmpty(text))
		{
			return list;
		}
		string[] array = text.Split(EntrySeparator, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < array.Length; i++)
		{
			string[] fields = array[i].Split(FieldSeparator);
			if (fields.Length != SavedFieldCount)
			{
				continue;
			}
			SavedWorkEntry entry = new SavedWorkEntry();
			entry.AgentName = Base64Decode(fields[0]);
			long creatureId = 0L;
			long skillId = 0L;
			long.TryParse(Base64Decode(fields[1]), NumberStyles.Integer, CultureInfo.InvariantCulture, out creatureId);
			long.TryParse(Base64Decode(fields[3]), NumberStyles.Integer, CultureInfo.InvariantCulture, out skillId);
			entry.CreatureId = creatureId;
			entry.CreatureName = Base64Decode(fields[2]);
			entry.SkillId = skillId;
			entry.SkillName = Base64Decode(fields[4]).Trim();
			if (!string.IsNullOrEmpty(entry.AgentName))
			{
				list.Add(entry);
			}
		}
		return list;
	}

	private static string Base64Encode(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return string.Empty;
		}
		return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
	}

	private static string Base64Decode(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return string.Empty;
		}
		try
		{
			return Encoding.UTF8.GetString(Convert.FromBase64String(value));
		}
		catch (FormatException)
		{
			return string.Empty;
		}
	}

	private static void SendSystemLog(string message)
	{
		if (string.IsNullOrEmpty(message))
		{
			return;
		}
		try
		{
			if (Notice.instance != null)
			{
				Notice.instance.Send(NoticeName.AddSystemLog, message);
			}
		}
		catch
		{
		}
	}
}
}
