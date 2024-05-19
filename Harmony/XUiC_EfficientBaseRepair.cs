using Audio;
using Platform;

public class XUiC_EfficientBaseRepair : XUiController
{
	public static string ID = "KeypadEdit";

	private XUiC_TextInput txtPassword;

	private ILockable LockedItem;

	public override void Init()
	{
		ID = windowGroup.ID;
		base.Init();
		txtPassword = (XUiC_TextInput)GetChildById("txtPassword");
		txtPassword.OnSubmitHandler += TxtPassword_OnSubmitHandler;
		txtPassword.OnInputAbortedHandler += TextInput_OnInputAbortedHandler;
		((XUiC_SimpleButton)GetChildById("btnCancel")).OnPressed += BtnCancel_OnPressed;
		((XUiC_SimpleButton)GetChildById("btnOk")).OnPressed += BtnOk_OnPressed;
	}

	private void TextInput_OnInputAbortedHandler(XUiController _sender)
	{
		base.xui.playerUI.windowManager.Close(base.WindowGroup.ID);
	}

	private void TxtPassword_OnSubmitHandler(XUiController _sender, string _text)
	{
		BtnOk_OnPressed(_sender, -1);
	}

	private void BtnOk_OnPressed(XUiController _sender, int _mouseButton)
	{
		string text = txtPassword.Text;
		if (LockedItem.CheckPassword(text, PlatformManager.InternalLocalUserIdentifier, out var changed))
		{
			if (LockedItem.LocalPlayerIsOwner())
			{
				if (changed)
				{
					if (text.Length == 0)
					{
						GameManager.ShowTooltip(base.xui.playerUI.entityPlayer, "passcodeRemoved");
					}
					else
					{
						GameManager.ShowTooltip(base.xui.playerUI.entityPlayer, "passcodeSet");
					}
				}
				Manager.PlayInsidePlayerHead("Misc/password_set");
			}
			else
			{
				GameManager.ShowTooltip(base.xui.playerUI.entityPlayer, "passcodeAccepted");
				Manager.PlayInsidePlayerHead("Misc/password_pass");
			}
			base.xui.playerUI.windowManager.Close(base.WindowGroup.ID);
		}
		else
		{
			Manager.PlayInsidePlayerHead("Misc/password_fail");
			GameManager.ShowTooltip(base.xui.playerUI.entityPlayer, "passcodeRejected");
		}
	}

	private void BtnCancel_OnPressed(XUiController _sender, int _mouseButton)
	{
		base.xui.playerUI.windowManager.Close(base.WindowGroup.ID);
	}

	public override void OnOpen()
	{
		base.OnOpen();
		base.xui.playerUI.entityPlayer.PlayOneShot("open_sign");
	}

	public override void OnClose()
	{
		base.OnClose();
		base.xui.playerUI.entityPlayer.PlayOneShot("close_sign");
		LockedItem = null;
	}

	public static void Open(LocalPlayerUI _playerUi, ILockable _lockedItem)
	{
		// _playerUi.xui.FindWindowGroupByName(ID).GetChildByType<XUiC_KeypadWindow>().LockedItem = _lockedItem;
		_playerUi.windowManager.Open(ID, _bModal: true);
	}
}