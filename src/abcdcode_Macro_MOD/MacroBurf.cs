using System;
using System.IO;
using UnityEngine;

namespace abcdcode_Macro_MOD
{
public class MacroBurf : UnitBuf
{
	private GameObject GO;

	private CreatureModel who;

	public MacroBurf(CreatureModel model)
	{
		base.remainTime = 1000000f;
		who = model;
		base.type = (UnitBufType)1;
		base.duplicateType = (BufDuplicateType)1;
	}

	public override void Init(UnitModel model)
	{
		((UnitBuf)this).Init(model);
		try
		{
			Sprite portrait_Mod = Add_On.GetPortrait_Mod((who.metaInfo.modid == null) ? string.Empty : who.metaInfo.modid, who.metaInfo.portraitSrc);
			Sprite.Create(Add_On.duplicateTexture(portrait_Mod.texture), new Rect(0f, 0f, (float)((Texture)portrait_Mod.texture).width, (float)((Texture)portrait_Mod.texture).height), new Vector2((float)(((Texture)portrait_Mod.texture).width / 2), (float)(((Texture)portrait_Mod.texture).width / 2)));
			GameObject val = new GameObject("unname");
			SpriteRenderer val2 = val.AddComponent<SpriteRenderer>();
			val2.sprite = portrait_Mod;
			val.transform.SetParent(((Component)((AgentModel)((model is AgentModel) ? model : null)).GetUnit()).gameObject.transform);
			int width = ((Texture)val2.sprite.texture).width;
			float num = 100f / (float)width;
			val.transform.localScale = new Vector3(num, num);
			val.transform.localPosition = new Vector3(0f, 3.5f);
			val.transform.localRotation = Quaternion.identity;
			GO = val;
			GO.SetActive(true);
		}
		catch (Exception ex)
		{
			Debug.Log((object)(ex.Message + Environment.NewLine + ex.StackTrace));
		}
	}

	public override void OnDestroy()
	{
		((UnitBuf)this).OnDestroy();
		GO.SetActive(false);
	}

	public static Sprite GetPortrait(string portraitSrc)
	{
		string[] array = portraitSrc.Split('/');
		Sprite result = null;
		if (array[0] == "Custom")
		{
			Sprite val = Resources.Load<Sprite>("Sprites/Unit/creature/AuthorNote");
			foreach (DirectoryInfo dir in Add_On.instance.DirList)
			{
				string path = dir.FullName + "/Creature/Portrait/" + array[1] + ".png";
				if (File.Exists(path))
				{
					byte[] array2 = File.ReadAllBytes(path);
					ImageConversion.LoadImage(new Texture2D(2, 2), array2);
					ImageConversion.LoadImage(val.texture, array2);
					result = val;
				}
			}
			return result;
		}
		return Resources.Load<Sprite>(portraitSrc);
	}
}
}
