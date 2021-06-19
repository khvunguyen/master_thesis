using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Based on: http://gyanendushekhar.com/2019/11/11/move-canvas-ui-mouse-drag-unity-3d-drag-drop-ui/ 
public class DragBehaviour : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler
{
    public Image centerCircle;
    public Image bigCircle;
    public Text debugInfo;
    private float centerX;
    private float centerY;
    private float radiusBigCircle;
    private Vector2 lastMousePosition;
    private Vector3 centerCirclePosition;
    private bool inside = false;

    public void OnBeginDrag(PointerEventData eventData)
    {
        lastMousePosition = eventData.position;
        // Radius of the big circle & position of the center circle
        radiusBigCircle = bigCircle.rectTransform.rect.width / 2;
        centerCirclePosition = centerCircle.transform.localPosition;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Set the new position of the element based on mouse position
        Vector2 currentMousePosition = eventData.position;
        Vector2 diff = currentMousePosition - lastMousePosition;
        RectTransform rect = GetComponent<RectTransform>();

        Vector3 newPosition = rect.position
            + new Vector3(diff.x, diff.y, transform.position.z);
        Vector3 oldPos = rect.position;
        rect.position = newPosition;

        lastMousePosition = currentMousePosition;

        SendInfoToSuperCollider(this.gameObject);
    }

    public void OnEndDrag(PointerEventData eventData) { } // not used in this thesis

    // Function to calculate distance and azimuth (if distance is "valid") to send both to SC 
    void SendInfoToSuperCollider(GameObject circleSound)
    {
        Vector3 positionSound = circleSound.transform.localPosition;
        // Distance to the small circle at center (same z-value for both elements so it has no effect)
        float distance = Vector3.Distance(positionSound, centerCirclePosition);
        // Name of UI element is just "1" or "2" so the address would be "/ambisonic1" or "/ambisonic2"
        string addressPattern = "/ambisonic" + circleSound.name;

        // Only when distance is smaller than the radius of the big circle and the OscClient for SC is valid
        if (distance <= radiusBigCircle && ControlBoardScript.superColliderClient != null)
        {
            inside = true;
            // Calculate azimuth using atan2 in (-pi,pi] and relative distance in [0,1]
            float x1 = (positionSound.x - centerCirclePosition.x) / radiusBigCircle;
            float y1 = (positionSound.y - centerCirclePosition.y) / radiusBigCircle;
            float azimuth = (float)System.Math.Atan2(y1, x1);
            float relativeDistance = distance / radiusBigCircle;

            // Message can be considered as an array of 2 values: relative distance and azimuth 
            Vector2 msg = new Vector2(relativeDistance, azimuth);

            // Send message containing distance and azimuth to SuperCollider
            ControlBoardScript.superColliderClient.Send(addressPattern, msg);
            // Show info on the control board
            debugInfo.text = "Info is sent to SC at " + ControlBoardScript.superColliderClient.Destination
            + addressPattern + " - Relative distance: " + System.Math.Round(relativeDistance, 2)
            + "|Azimuth: " + System.Math.Round(azimuth * 180 / Mathf.PI, 2) + "°";
        }
        else if (distance > radiusBigCircle)
        {
            // Only send an empty message to SC once to pause the sound
            if (inside == true && ControlBoardScript.superColliderClient != null)
                ControlBoardScript.superColliderClient.Send(addressPattern);

            inside = false;
            debugInfo.text = "";
        }
    }
}