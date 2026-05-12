using System;
using UnityEngine;

public class GameEventsManager : MonoBehaviour
{
    public static GameEventsManager instance { get; private set; }

    public PlayerEvents playerEvents;
    public InputEvents inputEvents;
    public DialogueEvents dialogueEvents;

    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogError("Found more than one Game Events Manager in the scene.");
        }
        instance = this;

        // initialize all events
        playerEvents = new PlayerEvents();
        dialogueEvents = new DialogueEvents();
        inputEvents = new InputEvents();
    }
}
