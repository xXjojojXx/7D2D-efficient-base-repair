using System;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
public class XUiC_EfficientBaseRepairStats : XUiController
{
	private bool RefuelButtonHovered;

	private XUiController windowIcon;

	private XUiController btnRefresh;

	private XUiV_Button btnRefresh_Background;

	private XUiController btnOn;

	private XUiV_Button btnOn_Background;

	private XUiV_Label lblOnOff;

	private XUiV_Label lblBlocksToRepair;

	private XUiV_Label lblVisitedBlocks;

	private XUiV_Label lblIterations;

	private XUiV_Sprite sprOnOff;

	private Color32 onColor = new Color32((byte)250, byte.MaxValue, (byte)163, byte.MaxValue);

	private Color32 offColor = (Color32)Color.white;

	private string turnOff;

	private string turnOn;

	public static FastTags tag = FastTags.Parse("gasoline");

	private TileEntityEfficientBaseRepair tileEntity;

	private bool lastOn;

	private bool isDirty;

	private readonly CachedStringFormatter<ushort> fuelFormatter = new CachedStringFormatter<ushort>((ushort _i) => _i.ToString());

	private readonly CachedStringFormatter<ushort> maxfuelFormatter = new CachedStringFormatter<ushort>((ushort _i) => _i.ToString());

	private readonly CachedStringFormatter<ushort> maxoutputFormatter = new CachedStringFormatter<ushort>((ushort _i) => _i.ToString());

	private readonly CachedStringFormatter<ushort> powerFormatter = new CachedStringFormatter<ushort>((ushort _i) => _i.ToString());

	private readonly CachedStringFormatterFloat potentialFuelFillFormatter = new CachedStringFormatterFloat();

	private readonly CachedStringFormatterFloat powerFillFormatter = new CachedStringFormatterFloat();

	private readonly CachedStringFormatterFloat fuelFillFormatter = new CachedStringFormatterFloat();

	public TileEntityEfficientBaseRepair TileEntity
	{
		get
		{
			return tileEntity;
		}
		set
		{
			tileEntity = value;
			// if (SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
			// {
			// 	PowerSource = tileEntity.GetPowerItem() as PowerSource;
			// }
		}
	}

	public override void Init()
	{
		base.Init();
		windowIcon = GetChildById("windowIcon");

		isDirty = false;
		_ = isDirty;

		btnRefresh = GetChildById("btnRefresh");
		btnRefresh_Background = (XUiV_Button)btnRefresh.GetChildById("clickable").ViewComponent;
		btnRefresh_Background.Controller.OnPress += btnRefresh_OnPress;
		btnRefresh_Background.Controller.OnHover += btnRefresh_OnHover;

		btnOn = GetChildById("btnOn");
		btnOn_Background = (XUiV_Button)btnOn.GetChildById("clickable").ViewComponent;
		btnOn_Background.Controller.OnPress += btnOn_OnPress;

		lblOnOff = (XUiV_Label)GetChildById("lblOnOff").ViewComponent;
		sprOnOff = (XUiV_Sprite)GetChildById("sprOnOff").ViewComponent;

		lblBlocksToRepair = (XUiV_Label)GetChildById("lblBlocksToRepair").ViewComponent;
		lblVisitedBlocks = (XUiV_Label)GetChildById("lblVisitedBlocks").ViewComponent;
		lblIterations = (XUiV_Label)GetChildById("lblIterations").ViewComponent;

		isDirty = true;
		turnOff = Localization.Get("xuiTurnOff");
		turnOn = Localization.Get("xuiTurnOn");
	}

	public void SetWidth(int width)
	{

		int max_width = Mathf.Max(width, 230);

		this.viewComponent.ParseAttribute("width", max_width.ToString(), this);

		this.GetChildById("header").ViewComponent.ParseAttribute("width", max_width.ToString(), this);

		XUiController content = GetChildById("content");

		content
			.GetChildById("backgroundMain")
			.ViewComponent.ParseAttribute("width", max_width.ToString(), this);

		content
			.GetChildById("background")
			.ViewComponent.ParseAttribute("width", max_width.ToString(), this);

		content
			.GetChildById("statGrid")
			.GetChildById("stats")
			.ViewComponent.ParseAttribute("cell_width", (max_width - 5).ToString(), this);

		GetChildById("lblBlocksToRepair").ViewComponent.ParseAttribute("width", (max_width - 5).ToString(), this);
		GetChildById("lblTotalDamages").ViewComponent.ParseAttribute("width", (max_width - 5).ToString(), this);
		GetChildById("lblVisitedBlocks").ViewComponent.ParseAttribute("width", (max_width - 5).ToString(), this);
		GetChildById("lblIterations").ViewComponent.ParseAttribute("width", (max_width - 5).ToString(), this);
		GetChildById("lblTimer").ViewComponent.ParseAttribute("width", (max_width - 5).ToString(), this);

		// button ON
		XUiController btnOn = content.GetChildById("btnOn");

		btnOn
			.GetChildById("backgroundMain")
			.ViewComponent.ParseAttribute("width", max_width.ToString(), this);

		btnOn
			.GetChildById("background")
			.ViewComponent.ParseAttribute("width", (max_width - 4).ToString(), this);

		btnOn
			.GetChildById("buttonRect")
			.ViewComponent.ParseAttribute("width", (max_width - 4).ToString(), this);

		btnOn
			.GetChildById("buttonRect")
			.GetChildById("clickable")
			.ViewComponent.ParseAttribute("width", (max_width - 4).ToString(), this);

		btnOn
			.GetChildById("buttonRect")
			.GetChildById("lblOnOff")
			.ViewComponent.ParseAttribute("width", (max_width - 4).ToString(), this);

		btnOn
			.GetChildById("buttonRect")
			.GetChildById("sprOnOff")
			.ViewComponent.ParseAttribute("pos", $"{max_width / 2 - 70}, -2", this);

		// button REFRESH
		XUiController btnRefresh = content.GetChildById("btnRefresh");

		btnRefresh
			.GetChildById("backgroundMain")
			.ViewComponent.ParseAttribute("width", max_width.ToString(), this);

		btnRefresh
			.GetChildById("background")
			.ViewComponent.ParseAttribute("width", (max_width - 4).ToString(), this);

		btnRefresh
			.GetChildById("buttonRect")
			.ViewComponent.ParseAttribute("width", (max_width - 4).ToString(), this);

		btnRefresh
			.GetChildById("buttonRect")
			.GetChildById("clickable")
			.ViewComponent.ParseAttribute("width", (max_width - 4).ToString(), this);

		btnRefresh
			.GetChildById("buttonRect")
			.GetChildById("lblRefresh")
			.ViewComponent.ParseAttribute("width", (max_width - 4).ToString(), this);

		btnRefresh
			.GetChildById("buttonRect")
			.GetChildById("sprRefresh")
			.ViewComponent.ParseAttribute("pos", $"{max_width / 2 - 70}, -2", this);
	}

	private void btnRefresh_OnHover(XUiController _sender, bool _isOver)
	{
		RefuelButtonHovered = _isOver;
		RefreshBindings();
	}

	private void btnRefresh_OnPress(XUiController _sender, int _mouseButton)
	{
		tileEntity.Refresh();
	}

	private void btnOn_OnPress(XUiController _sender, int _mouseButton)
	{
		tileEntity.Switch();
	}

	private void RefreshIsOn(bool isOn)
	{
		if (isOn)
		{
			lblOnOff.Text = turnOff;
			if (sprOnOff != null)
			{
				sprOnOff.Color = onColor;
			}
		}
		else
		{
			lblOnOff.Text = turnOn;
			if (sprOnOff != null)
			{
				sprOnOff.Color = offColor;
			}
		}
	}

	public override bool GetBindingValue(ref string value, string bindingName)
	{
		// switch (bindingName)
		// {
		// case "showfuel":
		// 	value = ((tileEntity == null) ? "false" : (tileEntity.PowerItemType == PowerItem.PowerItemTypes.Generator).ToString());
		// 	return true;
		// case "showsolar":
		// 	value = ((tileEntity == null) ? "false" : (tileEntity.PowerItemType == PowerItem.PowerItemTypes.SolarPanel).ToString());
		// 	return true;
		// case "fuel":
		// 	if (tileEntity == null || tileEntity.PowerItemType != PowerItem.PowerItemTypes.Generator)
		// 	{
		// 		value = "";
		// 	}
		// 	else
		// 	{
		// 		value = fuelFormatter.Format(tileEntity.CurrentFuel);
		// 	}
		// 	return true;
		// case "maxfuel":
		// 	if (tileEntity == null || tileEntity.PowerItemType != PowerItem.PowerItemTypes.Generator)
		// 	{
		// 		value = "";
		// 	}
		// 	else
		// 	{
		// 		value = maxfuelFormatter.Format(tileEntity.MaxFuel);
		// 	}
		// 	return true;
		// case "fueltitle":
		// 	value = Localization.Get("xuiGas");
		// 	return true;
		// case "maxoutput":
		// 	value = ((tileEntity == null) ? "" : maxoutputFormatter.Format(tileEntity.MaxOutput));
		// 	return true;
		// case "maxoutputtitle":
		// 	value = Localization.Get("xuiMaxOutput");
		// 	return true;
		// case "power":
		// 	value = ((tileEntity == null) ? "" : powerFormatter.Format(tileEntity.LastOutput));
		// 	return true;
		// case "powertitle":
		// 	value = Localization.Get("xuiPower");
		// 	return true;
		// case "potentialfuelfill":
		// 	if (!RefuelButtonHovered)
		// 	{
		// 		value = "0";
		// 	}
		// 	else if (tileEntity == null || tileEntity.PowerItemType != PowerItem.PowerItemTypes.Generator)
		// 	{
		// 		value = "0";
		// 	}
		// 	else
		// 	{
		// 		value = potentialFuelFillFormatter.Format((float)(tileEntity.CurrentFuel + 250) / (float)(int)tileEntity.MaxFuel);
		// 	}
		// 	return true;
		// case "powerfill":
		// 	value = ((tileEntity == null) ? "0" : powerFillFormatter.Format((float)(int)tileEntity.LastOutput / (float)(int)tileEntity.MaxOutput));
		// 	return true;
		// case "fuelfill":
		// 	if (tileEntity == null || tileEntity.PowerItemType != PowerItem.PowerItemTypes.Generator)
		// 	{
		// 		value = "0";
		// 	}
		// 	else
		// 	{
		// 		value = fuelFillFormatter.Format((float)(int)tileEntity.CurrentFuel / (float)(int)tileEntity.MaxFuel);
		// 	}
		// 	return true;
		// case "powersourceicon":
		// 	if (tileEntity == null)
		// 	{
		// 		value = "";
		// 	}
		// 	else
		// 	{
		// 		switch (tileEntity.PowerItemType)
		// 		{
		// 		case PowerItem.PowerItemTypes.Generator:
		// 			value = "ui_game_symbol_electric_generator";
		// 			break;
		// 		case PowerItem.PowerItemTypes.BatteryBank:
		// 			value = "ui_game_symbol_battery";
		// 			break;
		// 		case PowerItem.PowerItemTypes.SolarPanel:
		// 			value = "ui_game_symbol_electric_solar";
		// 			break;
		// 		}
		// 	}
		// 	return true;
		// default:
		// 	return false;
		// }
		return false;
	}

	private XUiV_Label GetLabel(string labelName)
	{
		return (XUiV_Label)GetChildById(labelName).ViewComponent;
	}

	private void RefreshStats()
	{
		GetLabel("lblBlocksToRepair").Text = $"{tileEntity.damagedBlockCount:N0} damaged blocks found.";
		GetLabel("lblTotalDamages").Text   = $"{tileEntity.totalDamagesCount:N0} damages points to repair.";
		GetLabel("lblVisitedBlocks").Text  = $"{tileEntity.visitedBlocksCount:N0} blocks visited.";
		GetLabel("lblIterations").Text     = $"tileEntity.isOn={tileEntity.IsOn}";
		GetLabel("lblTimer").Text          = $"Repair time {tileEntity.RepairTime()}";
	}

	public override void Update(float _dt)
	{
		if ((GameManager.Instance != null || GameManager.Instance.World != null) && tileEntity != null)
		{
			base.Update(_dt);
			if (lastOn != tileEntity.IsOn)
			{
				lastOn = tileEntity.IsOn;
				RefreshIsOn(tileEntity.IsOn);
			}
			RefreshStats();
			RefreshBindings();
		}
	}

	public override void OnOpen()
	{
		base.OnOpen();
		tileEntity.SetUserAccessing(_bUserAccessing: true);
		bool isOn = tileEntity.IsOn;
		RefreshIsOn(isOn);
		// Owner.SetOn(isOn);
		RefreshBindings();
		tileEntity.SetModified();
	}

	public override void OnClose()
	{
		GameManager instance = GameManager.Instance;
		Vector3i blockPos = tileEntity.ToWorldPos();
		if (!XUiC_CameraWindow.hackyIsOpeningMaximizedWindow)
		{
			tileEntity.SetUserAccessing(_bUserAccessing: false);
			instance.TEUnlockServer(tileEntity.GetClrIdx(), blockPos, tileEntity.entityId);
			tileEntity.SetModified();
		}
		base.OnClose();
	}
}
