using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ink.Runtime;
using System;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    [Header("Ink Story")]
    [SerializeField] private TextAsset inkJson;

    [Header("References")]
    [SerializeField] private GameObject _playerRef;

    [SerializeField] private TextMeshProUGUI displayNameText;
    [SerializeField] private Animator portraitAnimator;
    [SerializeField] private Animator layoutAnimator;

    private Story story;
    private int currentChoiceIndex = -1;

    private bool dialoguePlaying = false;
    public bool canContinueToNextLine = false;


    private const string SPEAKER_TAG = "speaker";
    private const string PORTRAIT_TAG = "portrait";
    private const string LAYOUT_TAG = "layout";

    
    private void Awake()
    {
        story = new Story(inkJson.text);
        if (_playerRef == null ) _playerRef = GameObject.FindGameObjectWithTag("Player");
    }


    private void OnEnable()
    {
        GameEventsManager.instance.dialogueEvents.onEnterDialogue += EnterDialogue;
        GameEventsManager.instance.inputEvents.onSubmitPressed += SubmitPressed;
        GameEventsManager.instance.dialogueEvents.onUpdateChoiceIndex += UpdateChoiceIndex;
        //GameEventsManager.instance.questEvents.onQuestStateChange += QuestStateChange;
    }

    private void OnDisable()
    {
        GameEventsManager.instance.dialogueEvents.onEnterDialogue -= EnterDialogue;
        GameEventsManager.instance.inputEvents.onSubmitPressed -= SubmitPressed;
        GameEventsManager.instance.dialogueEvents.onUpdateChoiceIndex -= UpdateChoiceIndex;
        //GameEventsManager.instance.questEvents.onQuestStateChange -= QuestStateChange;
    }

    /// <summary>
    /// In case we need to implement quest system
    /// </summary>
    /*
    private void QuestStateChange(Quest quest)
    {
        GameEventsManager.instance.dialogueEvents.UpdateInkDialogueVariable(
            quest.info.id + "State",
            new StringValue(quest.state.ToString())
        );
    }
    */


    private void UpdateChoiceIndex(int choiceIndex)
    {
        this.currentChoiceIndex = choiceIndex;
    }

    private void SubmitPressed(InputEventContext inputEventContext)
    {
        // if the context isn't dialogue, we never want to register input here
        if (!canContinueToNextLine || !inputEventContext.Equals(InputEventContext.DIALOGUE))
        {
            return;
        }

        ContinueOrExitStory();
    }

    private void EnterDialogue(string knotName)
    {
        //Debug.Log("Dialogue opened");
        // don't enter dialogue if we've already entered
        if (dialoguePlaying)
        {
            return;
        }

        dialoguePlaying = true;

        // inform other parts of our system that we've started dialogue
        GameEventsManager.instance.dialogueEvents.DialogueStarted();

        // freeze player movement
        _playerRef.GetComponent<Player>().PlayerCanMove = false;
        _playerRef.GetComponent<InputBufferCombatSystem>().enabled = false;

        // input event context
        GameEventsManager.instance.inputEvents.ChangeInputEventContext(InputEventContext.DIALOGUE);

        // jump to the dialogue knot
        if (!knotName.Equals(""))
        {
            story.ChoosePathString(knotName);
        }
        else
        {
            Debug.LogWarning("Knot name was the empty string when entering dialogue.");
        }
        //reset portrait, layout, and speaker
        displayNameText.text = "???";
        portraitAnimator.Play("default");
        layoutAnimator.Play("right");


        // kick off the story
        ContinueOrExitStory();
    }

    private void ContinueOrExitStory()
    {
        // make a choice, if applicable
        if (story.currentChoices.Count > 0 && currentChoiceIndex != -1)
        {
            story.ChooseChoiceIndex(currentChoiceIndex);
            // reset choice index for next time
            currentChoiceIndex = -1;
        }

        if (story.canContinue)
        {
            string dialogueLine = story.Continue();

            // handle the case where there's an empty line of dialogue
            // by continuing until we get a line with content
            while (IsLineBlank(dialogueLine) && story.canContinue)
            {
                dialogueLine = story.Continue();
            }
            // Handle tag
            HandleTags(story.currentTags);


            // handle the case where the last line of dialogue is blank
            // (empty choice, external function, etc...)
            if (IsLineBlank(dialogueLine) && !story.canContinue)
            {
                ExitDialogue();
            }
            else
            {
                GameEventsManager.instance.dialogueEvents.DisplayDialogue(dialogueLine, story.currentChoices);
            }
        }
        else if (story.currentChoices.Count == 0)
        {
            ExitDialogue();
        }
    }

    private void HandleTags(List<string> currentTags)
    {
        foreach (string tag in currentTags)
        {
            //parse the tag
            string[] splitTag = tag.Split(':');
            if (splitTag.Length != 2)
            {
                Debug.Log("Tag could not be appropriately parsed:" + tag);
            }
            string tagKey = splitTag[0].Trim();
            string tagValue = splitTag[1].Trim();

            //handle the tag
            switch(tagKey)
            {
                case SPEAKER_TAG:
                    displayNameText.text = tagValue;
                    break;
                case PORTRAIT_TAG:
                    portraitAnimator.Play(tagValue);
                    break;
                case LAYOUT_TAG:
                    layoutAnimator.Play(tagValue);
                    break;
                default:
                    Debug.LogWarning("Tag came in but is not currently being handled: " + tag);
                    break;
            }
        }
    }


    private void ExitDialogue()
    {
        dialoguePlaying = false;

        // inform other parts of our system that we've finished dialogue
        GameEventsManager.instance.dialogueEvents.DialogueFinished();

        // let player move again
        _playerRef.GetComponent<Player>().PlayerCanMove = true;
        _playerRef.GetComponent<InputBufferCombatSystem>().enabled = true;

        // input event context
        GameEventsManager.instance.inputEvents.ChangeInputEventContext(InputEventContext.DEFAULT);

        // reset story state
        story.ResetState();
    }

    private bool IsLineBlank(string dialogueLine)
    {
        return dialogueLine.Trim().Equals("") || dialogueLine.Trim().Equals("\n");
    }
}