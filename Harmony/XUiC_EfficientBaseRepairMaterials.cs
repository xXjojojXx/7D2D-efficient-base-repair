using System;
using UnityEngine;

public class XUiC_EfficientBaseRepairMaterials : XUiController
{
	public string[] MaterialNames;

	private int[] weights;

    public TileEntityEfficientBaseRepair tileEntity;

	private XUiController[] materialTitles;

	private XUiController[] materialWeights;

	private Color baseTextColor;

	private Color validColor = Color.green;

	private Color invalidColor = Color.red;

	public override void Init()
	{
		base.Init();
		materialTitles = GetChildrenById("material");
		materialWeights = GetChildrenById("weight");
		if (materialWeights[0] != null)
		{
			baseTextColor = ((XUiV_Label)materialWeights[0].ViewComponent).Color;
		}
	}

	public override void OnOpen()
	{
		base.OnOpen();
		MaterialNames = Array.Empty<string>();

		for (int i = 0; i < MaterialNames.Length; i++)
		{
			string text = XUi.UppercaseFirst(MaterialNames[i]);
			if (Localization.Exists("lbl" + MaterialNames[i]))
			{
				text = Localization.Get("lbl" + MaterialNames[i]);
			}
			((XUiV_Label)materialTitles[i].ViewComponent).Text = text + ":";
		}
	}

	// public override void Update(float _dt)
	// {
	// 	if (windowGroup.isShowing)
	// 	{
	// 		if (weights == null)
	// 		{
	// 			weights = new int[MaterialNames.Length];
	// 			// SetMaterialWeights(inputGrid.WorkstationData.GetInputStacks());
	// 		}
	// 		for (int i = 0; i < weights.Length; i++)
	// 		{
	// 			((XUiV_Label)materialWeights[i].ViewComponent).Text = $"{weights[i].ToString()}";
	// 		}
	// 		base.Update(_dt);
	// 	}
	// }

	private void ResetWeightColors()
	{
		for (int i = 0; i < weights.Length; i++)
		{
			((XUiV_Label)materialWeights[i].ViewComponent).Color = baseTextColor;
		}
	}

	public void SetMaterialWeights(ItemStack[] stackList)
	{
		for (int i = 3; i < stackList.Length; i++)
		{
			if (weights != null && stackList[i] != null)
			{
				weights[i - 3] = stackList[i].count;
			}
		}
		// onForgeValuesChanged();
	}

	public override bool ParseAttribute(string name, string value, XUiController _parent)
	{
		bool flag = base.ParseAttribute(name, value, _parent);
		if (!flag)
		{
			switch (name)
			{
			case "materials":
				// if (value.Contains(","))
				// {
				// 	MaterialNames = value.Replace(" ", "").Split(',', StringSplitOptions.None);
				// 	weights = new int[MaterialNames.Length];
				// }
				// else
				// {
				// 	MaterialNames = new string[1] { value };
				// }
                MaterialNames = new string[1] { value };
				return true;
			case "valid_materials_color":
				validColor = StringParsers.ParseColor32(value);
				return true;
			case "invalid_materials_color":
				invalidColor = StringParsers.ParseColor32(value);
				return true;
			default:
				return false;
			}
		}
		return flag;
	}

	private float calculateWeightOunces(int materialIndex)
	{
		return weights[materialIndex];
	}

	private float calculateWeightPounds(int materialIndex)
	{
		return calculateWeightOunces(materialIndex) / 16f;
	}
}
