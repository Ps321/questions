using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ObjectSwitcher : MonoBehaviour
{
    public GameObject[] objects;
    public Button leftButton;
    public Button rightButton;
    public float moveDuration = 0.5f;
    public Vector3 offScreenOffset = new Vector3(1000f, 0f, 0f);  // Adjust the offset for UI canvas

    private int currentIndex = 0;
    private bool isMoving = false;

    private void Start()
    {
        // Set up button listeners
        leftButton.onClick.AddListener(() => SwitchObject(-1));
        rightButton.onClick.AddListener(() => SwitchObject(1));

        // Ensure only the first object is shown initially
        UpdateObjectVisibility();
    }

    private void SwitchObject(int direction)
    {
        if (isMoving) return; // Avoid multiple clicks while moving

        int nextIndex = (currentIndex + direction + objects.Length) % objects.Length;
        StartCoroutine(MoveToObject(nextIndex, direction));
    }

    private IEnumerator MoveToObject(int nextIndex, int direction)
    {
        isMoving = true;

        // Calculate the start and end local positions for current and next objects
        Vector3 currentStartPos = objects[currentIndex].transform.localPosition;
        Vector3 nextStartPos = direction == 1 ? offScreenOffset : -offScreenOffset; // Position the next object off-screen
        Vector3 endPosition = Vector3.zero; // Center position

        // Set the next object off-screen initially
        objects[nextIndex].transform.localPosition = nextStartPos;
        objects[nextIndex].SetActive(true);

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / moveDuration;

            // Lerp the current object off-screen and the next object to center (localPosition = 0)
            objects[currentIndex].transform.localPosition = Vector3.Lerp(currentStartPos, -nextStartPos, t);
            objects[nextIndex].transform.localPosition = Vector3.Lerp(nextStartPos, endPosition, t);

            yield return null;
        }

        // Hide the old object and update the current index
        objects[currentIndex].SetActive(false);
        currentIndex = nextIndex;
        isMoving = false;
    }

    private void UpdateObjectVisibility()
    {
        // Set only the current object to be active and position it at the center (localPosition = 0)
        for (int i = 0; i < objects.Length; i++)
        {
            objects[i].SetActive(i == currentIndex);
            if (i == currentIndex)
            {
                objects[i].transform.localPosition = Vector3.zero; // Center the active object
            }
        }
    }
}
