using System.Collections.Generic;
using UnityEngine;

public class PanelManager : MonoBehaviour
{
    public GameObject mainMenuPanel;
   
    private Stack<GameObject> panelHistory = new Stack<GameObject>();

    void Start()
    {
        // Initialize by showing only the main menu panel
        ShowPanel(mainMenuPanel);
    }

    void Update()
    {
        // Check for Android's back button
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            GoBack();
        }
    }

    public void ShowPanel(GameObject panelToShow)
    {
        // Hide the currently active panel if any
        if (panelHistory.Count > 0)
        {
            panelHistory.Peek().SetActive(false);
        }

        // Show the new panel
        panelToShow.SetActive(true);

        // Add the new panel to the stack
        panelHistory.Push(panelToShow);
    }

    public void GoBack()
    {
        // If there's more than one panel in the history, go back
        if (panelHistory.Count > 1)
        {
            // Hide the current panel
            panelHistory.Pop().SetActive(false);

            // Show the previous panel
            panelHistory.Peek().SetActive(true);
        }
        else
        {
            // If we're at the first panel, exit the application (optional)
            Application.Quit();
        }
    }
}
