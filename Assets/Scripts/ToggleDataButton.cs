using UnityEngine;
using UnityEngine.UI;
using Mapbox.Unity.Utilities;

[RequireComponent(typeof(Image))]
public class ToggleDataButton : Button {
	public bool isEnabled;
	public string visualizerName;
	public Sprite enabledImage;
	public Sprite disabledImage;

	public override void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
	{
		base.OnPointerClick(eventData);
		isEnabled = !isEnabled;
		GetComponent<Image>().sprite = isEnabled ? enabledImage : disabledImage;

		MapEvents.SendEvent("CLEARDATA");
		MapEvents.SendEvent(visualizerName + ": " + (isEnabled ? "active" : "inactive"));
		MapEvents.SendEvent("BUILDDATA");

	}

	public void SetInactive()
	{
		isEnabled = false;
		GetComponent<Image>().sprite = disabledImage;
		MapEvents.SendEvent(visualizerName + ": " + (isEnabled ? "active" : "inactive"));
	}
}
