using UnityEngine;

namespace Transmogged;

public struct FText
{
	public static implicit operator string(FText e) => e.Text;
	public static implicit operator FText(string e) => new FText(e);
	public static FText operator +(FText lhs, FText rhs) => lhs.Text + rhs.Text;
	public static FText operator +(FText lhs, string rhs) => lhs.Text + rhs;
	public static FText operator +(string lhs, FText rhs) => lhs + rhs.Text;
	public string Text;
	public FText(string text)
	{
		Text = text;
	}
	public override string ToString()
	{
		return Text;
	}

	public FText Cen() => Tag("center");
	public FText It() => Tag("i");
	public FText B() => Tag("b");
	public FText Clr(string clr) => Tag("color", $"#{clr}");
	public FText Clr(Color clr) => Clr(ColorUtility.ToHtmlStringRGBA(clr));
	public FText Tag(string tag) => $"<{tag}>{Text}</{tag}>";
	public FText Tag(string tag, string val) => $"<{tag}={val}>{Text}</{tag}>";
	public FText Size(int percent) => Tag("size", $"{percent}%");
}
