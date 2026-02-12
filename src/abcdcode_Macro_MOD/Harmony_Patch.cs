using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using Harmony;
using UnityEngine;

namespace abcdcode_Macro_MOD
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

	private const string InsertPrefKey = "Lobotomy.abcdcode.CommandMacro2.Insert";

	private const int SavedFieldCount = 5;

	private static readonly char[] EntrySeparator = new char[1] { ';' };

	private static readonly char[] FieldSeparator = new char[1] { ',' };

	private static readonly FieldInfo ManageCommandCreatureField = AccessTools.Field(typeof(ManageCreatureAgentCommand), "targetCreature");

	private static readonly FieldInfo ManageCommandSkillField = AccessTools.Field(typeof(ManageCreatureAgentCommand), "skill");

	private static bool _insertHandledByHold;

	private static bool _homeHandledByHold;

	private static bool _endHandledByHold;

	public static AgentModel actorCache;

	public static Dictionary<string, object> Do_Macro = new Dictionary<string, object>();

	public Harmony_Patch()
	{
		try
		{
			HarmonyInstance val = HarmonyInstance.Create("Lobotomy.abcdcode.Macro");
			MethodInfo method = typeof(Harmony_Patch).GetMethod("CreatureModel_OnFixedUpdate");
			MethodInfo method2 = typeof(Harmony_Patch).GetMethod("CreatureModel_OnFixedUpdate2");
			val.Patch((MethodBase)typeof(CreatureModel).GetMethod("OnFixedUpdate"), new HarmonyMethod(method), new HarmonyMethod(method2), (HarmonyMethod)null);
			method = typeof(Harmony_Patch).GetMethod("CommandWindow_OnClick");
			val.Patch((MethodBase)typeof(global::CommandWindow.CommandWindow).GetMethod("OnClick"), new HarmonyMethod(method), (HarmonyMethod)null, (HarmonyMethod)null);
			method = typeof(Harmony_Patch).GetMethod("GameManager_EndGame");
			val.Patch((MethodBase)typeof(GameManager).GetMethod("EndGame"), new HarmonyMethod(method), (HarmonyMethod)null, (HarmonyMethod)null);
			method = typeof(Harmony_Patch).GetMethod("UnitMouseEventManager_Update");
			val.Patch((MethodBase)typeof(UnitMouseEventManager).GetMethod("Update", AccessTools.all), (HarmonyMethod)null, new HarmonyMethod(method), (HarmonyMethod)null);
			method = typeof(Harmony_Patch).GetMethod("AgentModel_ManageCreature");
			val.Patch((MethodBase)typeof(AgentModel).GetMethod("ManageCreature", AccessTools.all), (HarmonyMethod)null, new HarmonyMethod(method), (HarmonyMethod)null);
			method = typeof(Harmony_Patch).GetMethod("AgentModel_StopAction");
			val.Patch((MethodBase)typeof(AgentModel).GetMethod("StopAction", AccessTools.all), (HarmonyMethod)null, new HarmonyMethod(method), (HarmonyMethod)null);
		}
		catch (Exception)
		{
			SendSystemLog(Localize("모드 초기화 오류가 발생했습니다.", "Command Macro mod initialization error."));
		}
	}

	public static void AgentModel_ManageCreature(AgentModel __instance, CreatureModel target, SkillTypeInfo skill, Sprite skillSprite)
	{
		bool flag = actorCache == __instance;
		if (!flag && IsShiftHeld() && target != null && skill != null)
		{
			flag = true;
		}
		if (flag)
		{
			CreatureCheck.SetApAgent(__instance, target, skill, skillSprite);
			((UnitModel)__instance).AddUnitBuf((UnitBuf)(object)new MacroBurf(target));
			SendSystemLog(Localize("Shift 반복 등록: ", "Shift repeat registered: ") + __instance.name + " -> " + GetCreatureName(target) + " (" + GetSkillName(skill) + ")");
		}
		actorCache = null;
	}

	public static void AgentModel_StopAction(AgentModel __instance)
	{
		CreatureCheck.RemoveApAgent(__instance);
	}

	public static bool CommandWindow_OnClick(global::CommandWindow.CommandWindow __instance, AgentModel actor)
	{
		if (actor != null)
		{
			CreatureCheck.RemoveApAgent(actor);
			if (Input.GetKey((KeyCode)304) || Input.GetKey((KeyCode)303))
			{
				actorCache = actor;
			}
		}
		return true;
	}

	public static bool GameManager_EndGame()
	{
		CreatureCheck.creaturecheck = null;
		Do_Macro.Clear();
		return true;
	}

	public static bool CreatureModel_OnFixedUpdate(CreatureModel __instance)
	{
		if ((int)__instance.feelingState != 0 && __instance.feelingStateRemainTime <= Time.deltaTime)
		{
			AgentModel apAgent = CreatureCheck.GetApAgent(__instance);
			if (apAgent != null && !((WorkerModel)apAgent).IsDead() && !((WorkerModel)apAgent).IsCrazy() && !((UnitModel)apAgent).HasUnitBuf((UnitBufType)19) && (int)apAgent.GetState() == 0 && (int)__instance.state == 0)
			{
				Do_Macro[((object)__instance.metaInfo.LcId).ToString()] = true;
				return true;
			}
		}
		return true;
	}

	public static void AgentModel_ForcelyCancelWork(AgentModel __instance)
	{
		CreatureCheck.RemoveApAgent(__instance);
	}

	public static void UnitMouseEventManager_Update(UnitMouseEventManager __instance)
	{
		try
		{
			if ((Input.GetKeyDown((KeyCode)304) || Input.GetKeyDown((KeyCode)303)) && __instance.GetSelectedAgents().Count > 0 && ((UnityEngine.Object)(object)global::CommandWindow.CommandWindow.CurrentWindow == (UnityEngine.Object)null || !global::CommandWindow.CommandWindow.CurrentWindow.IsEnabled || (int)global::CommandWindow.CommandWindow.CurrentWindow.CurrentWindowType != 1))
			{
				foreach (AgentModel selectedAgent in __instance.GetSelectedAgents())
				{
					CreatureCheck.RemoveApAgent(selectedAgent);
				}
			}
			if (ConsumeInsertTrigger())
			{
				if (IsShiftHeld() && !IsCtrlHeld())
				{
					List<SavedWorkEntry> list = LoadInsertEntries();
					if (list.Count == 0)
					{
						List<SavedWorkEntry> currentManagedAgents = GetCurrentManagedAgents();
						if (currentManagedAgents.Count > 0)
						{
							SaveInsertEntries(currentManagedAgents);
							PlayerPrefs.Save();
							SendSystemLog(Localize("Shift + Insert: 저장 슬롯이 비어 현재 작업을 자동 저장함", "Shift + Insert: save slot was empty, so current assignments were auto-saved."));
						}
					}
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
		catch (Exception)
		{
			SendSystemLog(Localize("작업 중 오류가 발생했습니다.", "An error occurred while processing command macro."));
		}
	}

	public static void CreatureModel_OnFixedUpdate2(CreatureModel __instance)
	{
		if (Do_Macro.ContainsKey(((object)__instance.metaInfo.LcId).ToString()))
		{
			AgentModel apAgent = CreatureCheck.GetApAgent(__instance);
			if (apAgent != null && !((WorkerModel)apAgent).IsDead() && !((WorkerModel)apAgent).IsCrazy() && !((UnitModel)apAgent).HasUnitBuf((UnitBufType)19) && (int)apAgent.GetState() == 0 && (int)__instance.state == 0)
			{
				SkillTypeInfo apWorkInfo = CreatureCheck.GetApWorkInfo(__instance);
				Sprite apWorkSprite = CreatureCheck.GetApWorkSprite(__instance);
				apAgent.ManageCreature(__instance, apWorkInfo, apWorkSprite);
				apAgent.counterAttackEnabled = false;
				__instance.Unit.room.OnWorkAllocated(apAgent);
				__instance.script.OnWorkAllocated(apWorkInfo, apAgent);
				((UnitModel)apAgent).AddUnitBuf((UnitBuf)(object)new MacroBurf(__instance));
				AngelaConversation.instance.MakeMessage((AngelaMessageState)3, new object[3] { apAgent, apWorkInfo, __instance });
				SendSystemLog(Localize("반복 작업 실행: ", "Repeat work executed: ") + apAgent.name + " -> " + GetCreatureName(__instance) + " (" + GetSkillName(apWorkInfo) + ")");
			}
			Do_Macro.Remove(((object)__instance.metaInfo.LcId).ToString());
		}
	}

	private static bool IsShiftHeld()
	{
		return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
	}

	private static bool IsCtrlHeld()
	{
		return Input.GetKey((KeyCode)306) || Input.GetKey((KeyCode)305) || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
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
		List<SavedWorkEntry> insertEntries = LoadInsertEntries();
		SendSystemLog(Localize("Shift + Insert 확인: ", "Shift + Insert: ") + FormatEntries(insertEntries));
	}

	private static void ApplyInsertLayout()
	{
		List<SavedWorkEntry> insertEntries = LoadInsertEntries();
		if (insertEntries.Count == 0)
		{
			SendSystemLog(Localize("Home 실행: Insert 저장 목록이 비어 있음", "Home: Insert save list is empty."));
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

		List<string> list = new List<string>();
		List<string> list2 = new List<string>();
		HashSet<long> hashSet = new HashSet<long>();
		for (int j = 0; j < insertEntries.Count; j++)
		{
			SavedWorkEntry savedWorkEntry = insertEntries[j];
			if (savedWorkEntry == null || string.IsNullOrEmpty(savedWorkEntry.AgentName))
			{
				continue;
			}

			AgentModel agentModel2;
			if (!dictionary.TryGetValue(savedWorkEntry.AgentName, out agentModel2))
			{
				list2.Add(savedWorkEntry.AgentName + ": " + Localize("직원 없음", "agent not found"));
				continue;
			}
			if (!IsDispatchable(agentModel2))
			{
				list2.Add(savedWorkEntry.AgentName + ": " + Localize("동원 불가", "not dispatchable"));
				continue;
			}

			CreatureModel creatureModel = ResolveCreature(savedWorkEntry);
			SkillTypeInfo skillTypeInfo = ResolveSkill(savedWorkEntry);
			if (creatureModel == null || skillTypeInfo == null || creatureModel.IsEscaped() || creatureModel.script == null || !creatureModel.script.IsWorkable())
			{
				list2.Add(savedWorkEntry.AgentName + ": " + Localize("대상 해석 실패", "target resolution failed"));
				continue;
			}
			if (hashSet.Contains(creatureModel.instanceId))
			{
				list2.Add(savedWorkEntry.AgentName + ": " + GetCreatureName(creatureModel) + " " + Localize("중복 배정", "already assigned"));
				continue;
			}
			if (IsCreatureBusyForWork(creatureModel))
			{
				list2.Add(savedWorkEntry.AgentName + ": " + GetCreatureName(creatureModel) + " " + Localize("이미 작업중", "already being worked on"));
				continue;
			}

			Sprite workIconSprite = GetWorkIconSprite(skillTypeInfo);
			agentModel2.ManageCreature(creatureModel, skillTypeInfo, workIconSprite);
			agentModel2.counterAttackEnabled = false;
			if (creatureModel.Unit != null && creatureModel.Unit.room != null)
			{
				creatureModel.Unit.room.OnWorkAllocated(agentModel2);
			}
			if (creatureModel.script != null)
			{
				creatureModel.script.OnWorkAllocated(skillTypeInfo, agentModel2);
			}
			AngelaConversation.instance.MakeMessage((AngelaMessageState)3, new object[3] { agentModel2, skillTypeInfo, creatureModel });
			hashSet.Add(creatureModel.instanceId);
			list.Add(agentModel2.name + " -> " + GetCreatureName(creatureModel) + " (" + GetSkillName(skillTypeInfo) + ")");
		}

		if (list.Count == 0)
		{
			SendSystemLog(Localize("Home 실행: 지정 가능한 작업이 없음 (", "Home: no applicable assignments (") + string.Join(", ", list2.ToArray()) + ")");
			return;
		}

		SendSystemLog(Localize("Home 실행: ", "Home: ") + string.Join(", ", list.ToArray()));
		if (list2.Count > 0)
		{
			SendSystemLog(Localize("Home 스킵: ", "Home skipped: ") + string.Join(", ", list2.ToArray()));
		}
	}

	private static void CancelMovingSavedAgents()
	{
		List<SavedWorkEntry> insertEntries = LoadInsertEntries();
		if (insertEntries.Count == 0)
		{
			SendSystemLog(Localize("End 실행: Insert 저장 목록이 비어 있음", "End: Insert save list is empty."));
			return;
		}

		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		for (int i = 0; i < insertEntries.Count; i++)
		{
			if (!string.IsNullOrEmpty(insertEntries[i].AgentName))
			{
				hashSet.Add(insertEntries[i].AgentName);
			}
		}

		IList<AgentModel> agentList = AgentManager.instance.GetAgentList();
		List<string> list = new List<string>();
		for (int j = 0; j < agentList.Count; j++)
		{
			AgentModel agentModel = agentList[j];
			if (agentModel == null || !hashSet.Contains(agentModel.name))
			{
				continue;
			}

			CreatureModel creatureModel;
			SkillTypeInfo skillTypeInfo;
			if (!TryGetWorkFromAgent(agentModel, out creatureModel, out skillTypeInfo))
			{
				continue;
			}

			MovableObjectNode movableNode = agentModel.GetMovableNode();
			bool flag = movableNode != null && movableNode.IsMoving();
			bool flag2 = agentModel.GetState() == AgentAIState.MANAGE && agentModel.currentSkill == null;
			if (flag || flag2)
			{
				agentModel.StopAction();
				CreatureCheck.RemoveApAgent(agentModel);
				list.Add(agentModel.name);
			}
		}

		if (list.Count == 0)
		{
			SendSystemLog(Localize("End 실행: 취소할 이동중 작업이 없음", "End: no en-route assignments to cancel."));
			return;
		}

		SendSystemLog(Localize("End 실행 취소: ", "End canceled: ") + string.Join(", ", list.ToArray()));
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
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		IList<AgentModel> agentList = AgentManager.instance.GetAgentList();
		for (int i = 0; i < agentList.Count; i++)
		{
			AgentModel agentModel = agentList[i];
			if (agentModel == null || agentModel.IsDead() || agentModel.IsCrazy())
			{
				continue;
			}

			CreatureModel creatureModel;
			SkillTypeInfo skillTypeInfo;
			if (!TryGetWorkFromAgent(agentModel, out creatureModel, out skillTypeInfo))
			{
				continue;
			}

			if (string.IsNullOrEmpty(agentModel.name) || hashSet.Contains(agentModel.name))
			{
				continue;
			}

			hashSet.Add(agentModel.name);
			SavedWorkEntry savedWorkEntry = new SavedWorkEntry();
			savedWorkEntry.AgentName = agentModel.name;
			savedWorkEntry.CreatureId = ((creatureModel.metaInfo != null) ? creatureModel.metaInfo.id : creatureModel.metadataId);
			savedWorkEntry.CreatureName = GetCreatureName(creatureModel);
			savedWorkEntry.SkillId = skillTypeInfo.id;
			savedWorkEntry.SkillName = GetSkillName(skillTypeInfo);
			list.Add(savedWorkEntry);
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

		UseSkill useSkill = agent.currentSkill;
		if (useSkill != null && useSkill.targetCreature != null && useSkill.skillTypeInfo != null)
		{
			creature = useSkill.targetCreature;
			skill = useSkill.skillTypeInfo;
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
		catch (Exception)
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

	private static SavedWorkEntry FindByAgentName(List<SavedWorkEntry> entries, string agentName)
	{
		if (string.IsNullOrEmpty(agentName))
		{
			return null;
		}
		for (int i = 0; i < entries.Count; i++)
		{
			if (string.Equals(entries[i].AgentName, agentName, StringComparison.Ordinal))
			{
				return entries[i];
			}
		}
		return null;
	}

	private static CreatureModel ResolveCreature(SavedWorkEntry entry)
	{
		CreatureModel[] creatureList = CreatureManager.instance.GetCreatureList();
		CreatureModel creatureModel = null;
		CreatureModel creatureModel2 = null;
		for (int i = 0; i < creatureList.Length; i++)
		{
			CreatureModel creatureModel3 = creatureList[i];
			if (creatureModel3 == null || creatureModel3.metaInfo == null)
			{
				continue;
			}

			string creatureName = GetCreatureName(creatureModel3);
			if (creatureModel3.metaInfo.id == entry.CreatureId)
			{
				if (string.Equals(creatureName, entry.CreatureName, StringComparison.Ordinal))
				{
					return creatureModel3;
				}
				if (creatureModel == null)
				{
					creatureModel = creatureModel3;
				}
			}
			if (creatureModel2 == null && string.Equals(creatureName, entry.CreatureName, StringComparison.Ordinal))
			{
				creatureModel2 = creatureModel3;
			}
		}
		if (creatureModel != null)
		{
			return creatureModel;
		}
		return creatureModel2;
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
		IList<AgentModel> agentList = AgentManager.instance.GetAgentList();
		for (int i = 0; i < agentList.Count; i++)
		{
			AgentModel agentModel = agentList[i];
			if (agentModel == null || agentModel.IsDead() || agentModel.IsCrazy())
			{
				continue;
			}
			CreatureModel creatureModel;
			SkillTypeInfo skillTypeInfo;
			if (TryGetWorkFromAgent(agentModel, out creatureModel, out skillTypeInfo) && creatureModel != null && creatureModel.instanceId == creature.instanceId)
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
			object obj = null;
			ParameterInfo[] parameters = method.GetParameters();
			if (parameters.Length == 1)
			{
				object obj2 = (parameters[0].ParameterType.IsEnum ? Enum.ToObject(parameters[0].ParameterType, 0) : ((object)0));
				obj = method.Invoke(workIcon, new object[1] { obj2 });
			}
			else
			{
				obj = method.Invoke(workIcon, null);
			}
			if (object.ReferenceEquals(obj, null))
			{
				return null;
			}
			FieldInfo field = obj.GetType().GetField("icon", AccessTools.all);
			if (!object.ReferenceEquals(field, null))
			{
				return field.GetValue(obj) as Sprite;
			}
			PropertyInfo property = obj.GetType().GetProperty("icon", AccessTools.all);
			if (!object.ReferenceEquals(property, null))
			{
				return property.GetValue(obj, null) as Sprite;
			}
		}
		catch (Exception)
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
			SavedWorkEntry savedWorkEntry = entries[i];
			list.Add(savedWorkEntry.AgentName + " -> " + savedWorkEntry.CreatureName + " (" + savedWorkEntry.SkillName + ")");
		}
		return string.Join(", ", list.ToArray());
	}

	private static void SaveInsertEntries(List<SavedWorkEntry> entries)
	{
		List<string> list = new List<string>();
		for (int i = 0; i < entries.Count; i++)
		{
			SavedWorkEntry savedWorkEntry = entries[i];
			string[] value = new string[SavedFieldCount]
			{
				Base64Encode(savedWorkEntry.AgentName),
				Base64Encode(savedWorkEntry.CreatureId.ToString(CultureInfo.InvariantCulture)),
				Base64Encode(savedWorkEntry.CreatureName),
				Base64Encode(savedWorkEntry.SkillId.ToString(CultureInfo.InvariantCulture)),
				Base64Encode(savedWorkEntry.SkillName)
			};
			list.Add(string.Join(",", value));
		}
		PlayerPrefs.SetString(InsertPrefKey, string.Join(";", list.ToArray()));
	}

	private static List<SavedWorkEntry> LoadInsertEntries()
	{
		List<SavedWorkEntry> list = new List<SavedWorkEntry>();
		string @string = PlayerPrefs.GetString(InsertPrefKey, string.Empty);
		if (string.IsNullOrEmpty(@string))
		{
			return list;
		}
		string[] array = @string.Split(EntrySeparator, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < array.Length; i++)
		{
			string[] array2 = array[i].Split(FieldSeparator);
			if (array2.Length != SavedFieldCount)
			{
				continue;
			}
			SavedWorkEntry savedWorkEntry = new SavedWorkEntry();
			savedWorkEntry.AgentName = Base64Decode(array2[0]);
			long result = 0L;
			long result2 = 0L;
			long.TryParse(Base64Decode(array2[1]), NumberStyles.Integer, CultureInfo.InvariantCulture, out result);
			long.TryParse(Base64Decode(array2[3]), NumberStyles.Integer, CultureInfo.InvariantCulture, out result2);
			savedWorkEntry.CreatureId = result;
			savedWorkEntry.CreatureName = Base64Decode(array2[2]);
			savedWorkEntry.SkillId = result2;
			savedWorkEntry.SkillName = Base64Decode(array2[4]).Trim();
			if (!string.IsNullOrEmpty(savedWorkEntry.AgentName))
			{
				list.Add(savedWorkEntry);
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
			byte[] bytes = Convert.FromBase64String(value);
			return Encoding.UTF8.GetString(bytes);
		}
		catch (FormatException)
		{
			return string.Empty;
		}
	}

	private static void SendSystemLog(string message)
	{
		if (Notice.instance != null)
		{
			Notice.instance.Send(NoticeName.AddSystemLog, message);
		}
	}
}
}
