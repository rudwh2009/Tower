namespace Tower.Core.Engine.UI;

public interface IUiSink
{
 void ShowHudText(string text);
 void Clear();
 void AddText(string id, string text, float x, float y, string? fontId = null);
 void SetText(string id, string text);
 void Remove(string id);
 void AddButton(string id, string text, float x, float y, System.Action onClick);
}
