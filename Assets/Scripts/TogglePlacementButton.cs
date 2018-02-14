using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class TogglePlacementButton : Button {
    public Sprite lockedImage;
    public Sprite unlockedImage;
    private bool _isPlacing = true;

    public override void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
    {
        // TODO: Get this value from ARKitMovableObject, but without tight coupling
        _isPlacing = !_isPlacing;
        GetComponent<Image>().sprite = _isPlacing ? unlockedImage : lockedImage;
        base.OnPointerClick(eventData);
    }
}
