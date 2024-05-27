using Audio;
using UnityEngine;

public class XUiC_EfficientBaseRepair : XUiController
{
	private XUiC_LootWindow lootWindow;

	private XUiC_EfficientBaseRepairStats statsWindow;

	private XUiC_EfficientBaseRepairMaterials MaterialsWindow;

	private XUiC_WindowNonPagingHeader nonPagingHeaderWindow;

	private TileEntityEfficientBaseRepair te;

	private string lootContainerName;

	private bool isOpening;

	private float openTimeLeft;

	private XUiC_Timer timerWindow;

	private bool isClosingFromDamage;

	private string lootingHeader;

	public static string ID = "EfficientBaseRepair";

	private float totalOpenTime;

	private bool ignoreCloseSound;

	public override void Init()
	{
		base.Init();
		openTimeLeft = 0f;
		lootWindow = GetChildByType<XUiC_LootWindow>();
		timerWindow = base.xui.GetChildByType<XUiC_Timer>();
		nonPagingHeaderWindow = GetChildByType<XUiC_WindowNonPagingHeader>();

		statsWindow = (XUiC_EfficientBaseRepairStats)GetChildById("WindowEfficientBaseRepairStats");
		MaterialsWindow = (XUiC_EfficientBaseRepairMaterials)GetChildById("windowEfficientBaseRepairMaterials");

        // ignore compiler warning for unused variables
        _ = isClosingFromDamage;
	}

	public void SetTileEntityChest(string _lootContainerName, TileEntityEfficientBaseRepair _te)
	{
		te = _te;
		lootContainerName = _lootContainerName;
		lootWindow.SetTileEntityChest(_lootContainerName, _te);
		lootingHeader = Localization.Get("xuiEfficientBaseRepair");
	}

	protected void OpenContainer()
	{
		base.OnOpen();
		base.xui.playerUI.windowManager.OpenIfNotOpen("backpack", _bModal: false);
		lootWindow.ViewComponent.UiTransform.gameObject.SetActive(true);
		lootWindow.OpenContainer();

		if (nonPagingHeaderWindow != null)
		{
			nonPagingHeaderWindow.SetHeader(lootingHeader);
		}
		lootWindow.ViewComponent.IsVisible = true;
		base.xui.playerUI.windowManager.Close("timer");
		lootWindow.ViewComponent.ParseAttribute("position", "0, -315", this);
		isOpening = false;
	}

	public override void Update(float _dt)
	{
		base.Update(_dt);
		if (base.xui.playerUI.entityPlayer != null && base.xui.playerUI.entityPlayer.hasBeenAttackedTime > 0 && isOpening)
		{
			GUIWindowManager windowManager = base.xui.playerUI.windowManager;
			windowManager.Close("timer");
			isOpening = false;
			isClosingFromDamage = true;
			windowManager.Close("looting");
		}
		else
		{
			if (!isOpening)
			{
				return;
			}
			if (te.bWasTouched || openTimeLeft <= 0f)
			{
				if (!te.bWasTouched && !te.bPlayerStorage && !te.bPlayerBackpack)
				{
					base.xui.playerUI.entityPlayer.Progression.AddLevelExp(base.xui.playerUI.entityPlayer.gameStage, "_xpFromLoot", Progression.XPTypes.Looting);
				}
				openTimeLeft = 0f;
				OpenContainer();
			}
			else
			{
				if (timerWindow != null)
				{
					float fillAmount = openTimeLeft / totalOpenTime;
					timerWindow.UpdateTimer(openTimeLeft, fillAmount);
				}
				openTimeLeft -= _dt;
			}
		}
	}

	public override void OnOpen()
	{
		isClosingFromDamage = false;
		if (te.entityId != -1)
		{
			Entity entity = GameManager.Instance.World.GetEntity(te.entityId);
			if (EffectManager.GetValue(PassiveEffects.DisableLoot, null, 0f, base.xui.playerUI.entityPlayer, null, entity.EntityClass.Tags) > 0f)
			{
				Manager.PlayInsidePlayerHead("twitch_no_attack");
				GUIWindowManager windowManager = base.xui.playerUI.windowManager;
				ignoreCloseSound = true;
				windowManager.Close("timer");
				isOpening = false;
				isClosingFromDamage = true;
				windowManager.Close("looting");
				return;
			}
		}
		else if (EffectManager.GetValue(PassiveEffects.DisableLoot, null, 0f, base.xui.playerUI.entityPlayer, null, te.blockValue.Block.Tags) > 0f)
		{
			Manager.PlayInsidePlayerHead("twitch_no_attack");
			GUIWindowManager windowManager2 = base.xui.playerUI.windowManager;
			ignoreCloseSound = true;
			windowManager2.Close("timer");
			isOpening = false;
			isClosingFromDamage = true;
			windowManager2.Close("looting");
			return;
		}
		ignoreCloseSound = false;
		base.xui.playerUI.windowManager.CloseIfOpen("backpack");
		lootWindow.ViewComponent.UiTransform.gameObject.SetActive(false);

		EntityPlayer entityPlayer = base.xui.playerUI.entityPlayer;
		totalOpenTime = 0f; //(openTimeLeft = EffectManager.GetValue(PassiveEffects.ScavengingTime, null, entityPlayer.IsCrouching ? (te.GetOpenTime() * 1.5f) : te.GetOpenTime(), entityPlayer));
		if (nonPagingHeaderWindow != null)
		{
			nonPagingHeaderWindow.SetHeader("LOOTING");
		}
		base.xui.playerUI.windowManager.OpenIfNotOpen("CalloutGroup", _bModal: false);
		base.xui.playerUI.windowManager.Open("timer", _bModal: false);
		timerWindow = base.xui.GetChildByType<XUiC_Timer>();
		timerWindow.currentOpenEventText = Localization.Get("xuiOpeningLoot");
		isOpening = true;
		LootContainer lootContainer = LootContainer.GetLootContainer(te.lootListName);
		if (lootContainer == null || lootContainer.soundClose == null)
		{
			return;
		}
		Vector3 position = te.ToWorldPos().ToVector3() + Vector3.one * 0.5f;
		if (te.entityId != -1 && GameManager.Instance.World != null)
		{
			Entity entity2 = GameManager.Instance.World.GetEntity(te.entityId);
			if (entity2 != null)
			{
				position = entity2.GetPosition();
			}
		}

		statsWindow.TileEntity = te;
		statsWindow.SetWidth(lootWindow.ViewComponent.Size.x);
		statsWindow.OnOpen();

		MaterialsWindow.tileEntity = te;
		MaterialsWindow.OnOpen();

		Manager.BroadcastPlayByLocalPlayer(position, lootContainer.soundOpen);
	}

	public override void OnClose()
	{
		base.OnClose();
		base.xui.playerUI.windowManager.CloseIfOpen("backpack");
		te.ToWorldPos();
		if (isOpening)
		{
			base.xui.playerUI.windowManager.Close("timer");
		}
		if (openTimeLeft > 0f && !te.bWasTouched)
		{
			te.bTouched = false;
			te.SetModified();
		}
		lootWindow.CloseContainer(ignoreCloseSound);
		lootWindow.ViewComponent.IsVisible = false;
		isOpening = false;
	}

    public static void Open(LocalPlayerUI _playerUi, TileEntityEfficientBaseRepair tileEntity, World _world)
	{
        XUiC_EfficientBaseRepair instance = (XUiC_EfficientBaseRepair)_playerUi.xui.FindWindowGroupByName(ID);

        if(instance == null){
            Log.Error("[EfficientBaseRepair] null instance of XUiC_EfficientBaseRepair. aborting...");
            return;
        }

		tileEntity.Refresh();

		instance.SetTileEntityChest("EfficientBaseRepair", tileEntity);

		_playerUi.windowManager.Open(ID, _bModal: true);
	}
}