using Audio;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
public class XUiC_EfficientBaseRepairStats : XUiController
{
	private XUiController btnRefresh;

	private XUiV_Button btnRefresh_Background;

	private XUiController btnOn;

	private XUiV_Button btnOn_Background;

	private XUiV_Label lblOnOff;

	private XUiV_Sprite sprOnOff;

	private Color32 onColor = new Color32((byte)250, byte.MaxValue, (byte)163, byte.MaxValue);

	private Color32 offColor = (Color32)Color.white;

	private string turnOff => Localization.Get("xuiTurnOff");

	private string turnOn => Localization.Get("xuiTurnOn");

	private TileEntityEfficientBaseRepair tileEntity;

	private bool lastOn;

	public TileEntityEfficientBaseRepair TileEntity
	{
		get
		{
			return tileEntity;
		}
		set
		{
			tileEntity = value;
		}
	}

	public override void Init()
	{
		base.Init();

		btnRefresh = GetChildById("btnRefresh");
		btnRefresh_Background = (XUiV_Button)btnRefresh.GetChildById("clickable").ViewComponent;
		btnRefresh_Background.Controller.OnPress += BtnRefresh_OnPress;

		btnOn = GetChildById("btnOn");
		btnOn_Background = (XUiV_Button)btnOn.GetChildById("clickable").ViewComponent;
		btnOn_Background.Controller.OnPress += BtnOn_OnPress;

		lblOnOff = (XUiV_Label)GetChildById("lblOnOff").ViewComponent;
		sprOnOff = (XUiV_Sprite)GetChildById("sprOnOff").ViewComponent;

		((XUiV_Label)GetChildById("lblRefresh").ViewComponent).Text = Localization.Get("xuiServerBrowserRefreshList");
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

	private void BtnRefresh_OnPress(XUiController _sender, int _mouseButton)
	{
		tileEntity.Refresh();
		Manager.PlayInsidePlayerHead("UseActions/chest_tier4_open");
	}

	private void BtnOn_OnPress(XUiController _sender, int _mouseButton)
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
	private XUiV_Label GetLabel(string labelName)
	{
		return (XUiV_Label)GetChildById(labelName).ViewComponent;
	}

	private void RefreshStats()
	{
		GetLabel("lblBlocksToRepair").Text = $"{tileEntity.damagedBlockCount:N0} damaged blocks found.";
		GetLabel("lblTotalDamages").Text = $"{tileEntity.totalDamagesCount:N0} damages points to repair.";
		GetLabel("lblVisitedBlocks").Text = $"{tileEntity.visitedBlocksCount:N0} blocks visited.";
		GetLabel("lblIterations").Text = $"{tileEntity.bfsIterationsCount} iterations done.";
		GetLabel("lblTimer").Text = $"Repair time {tileEntity.RepairTime()}";
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
		RefreshIsOn(tileEntity.IsOn);
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
