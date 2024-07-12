using System;
using UnityEngine;
using Verse;

namespace Transmogged;
#nullable enable

public class TextPrompt : Window
{
	public override Vector2 InitialSize => new(350, 150);

	public string Buffer = string.Empty;
	public string Title;
	public Action<string> AcceptAction;

    public TextPrompt(string title, Action<string> acceptAction)
    {
        Title = title;
        AcceptAction = acceptAction;
		closeOnAccept = true;
    }

	public void Accept()
	{
		AcceptAction(Buffer);
	}

    public override void DoWindowContents(Rect inRect)
    {
		Text.Font = GameFont.Medium;
        
		var list = new Listing_Standard();
		list.Begin(inRect);
		{
			list.Label(Title);
			list.GapLine();
			Buffer = list.TextEntry(Buffer);
			var btnrow = new WidgetRow(list.curX, list.curY, UIDirection.RightThenDown);
			if (btnrow.ButtonText("Transmogged.Cancel".Translate()))
				Close();
			if (btnrow.ButtonText("Transmogged.Accept".Translate()))
				Accept();
		}
		list.End();
    }

    public override void OnAcceptKeyPressed() => Accept();
}
