/* Name: Dialogue.cs
 * Primary Author: Zackary Seiple
 * Description: Handles the adding and execution of dialogue, along with managing the dialogue UI
 * Last Updated: 5/6/2020 (Zackary Seiple)
 * Changes: Added Header
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Dialogue : MonoBehaviour
{
    //Singleton
    public static Dialogue instance;

    //The max number of characters to be displayed in dialogue window
    const int MAX_CHARACTERS = 87;

    //Whether the dialogue is waiting for an action to be performed in game
    public static bool holding = false;

    //UI Elements
    public GameObject panel;
    private GameObject playerPortrait;
    private Image speakerImage;
    private TMP_Text speakerName;
    private TMP_Text dialogueText;
    private GameObject continuePrompt;

    //The lines of dialogue currently queued up
    public Queue<DialogueLine> dialogueQueue;
    /// <summary>
    /// Data needed to deliver a line of dialogue (message, speaker name, speaker picture, and whether the dialogue should hold or not
    /// </summary>
    public class DialogueLine
    {
        public string speakerName;
        public string message;
        public Sprite speakerPicture;
        public bool holdLine = false;

        public DialogueLine(Character character, string message)
        {
            speakerName = instance.characters[(int) character].name;
            speakerPicture = instance.characters[(int) character].picture;
            this.message = message;
        }

        public void TriggerHold(bool hold)
        {
            holdLine = hold;
        }
    }

    public enum Character { Pete, Photographer };
    public CharacterDetails[] characters;
    /// <summary>
    /// The Details of a particular character (name and picture)
    /// </summary>
    [System.Serializable]
    public struct CharacterDetails
    {
        public string name;
        public Sprite picture;
    }

    /// <summary>
    /// Add a line of dialogue to the queue
    /// </summary>
    /// <param name="character">Character object that is delivering the line</param>
    /// <param name="hold">Whether the line should wait for an action after delivery</param>
    /// <param name="message">The dialogue to be delivered</param>
    public static void AddLine(Character character, bool hold, params string[] message)
    {

        foreach (string line in message)
        {
            DialogueLine lineToAdd = new DialogueLine(character, line);
            lineToAdd.TriggerHold(hold);
            instance.dialogueQueue.Enqueue(lineToAdd);
        }
    }

    /// <summary>
    /// Add a line of dialogue to the queue (doesn't hold by default)
    /// </summary>
    /// <param name="character">Character object that is delivering the line</param>
    /// <param name="message">Any number of lines to be delivered by this particular character</param>
    public static void AddLine(Character character, params string[] message)
    {
        AddLine(character, false, message);
    }

    /// <summary>
    /// Force the dialogue to continue
    /// </summary>
    public static void ContinueDialogue()
    {
        if(instance.dialogueQueue.Count > 0)
        instance.dialogueQueue.Peek().TriggerHold(false);
    }
    
    /// <summary>
    /// Force the dialogue to stop
    /// </summary>
    public static void ForceStop()
    {
        if (instance.dialogueQueue != null)
        {
            instance.dialogueQueue.Clear();
            instance.panel.SetActive(false);
            instance.StopAllCoroutines();
        }
    }

    bool dialogueRunning = false;
    public bool textPrinting = false;
    /// <summary>
    /// Coroutine that starts running any dialogue in the queue until empty
    /// </summary>
    /// <returns></returns>
    private IEnumerator RunDialogue()
    {
        dialogueRunning = true;
        char[] currentMessage;

        //Initialize for transition
        dialogueText.text = "";
        this.speakerImage.sprite = dialogueQueue.Peek().speakerPicture;
        this.speakerName.text = dialogueQueue.Peek().speakerName;
        StartCoroutine(SetDialogueWindowActive(true));
        //panel.SetActive(true);

        while (transitionInProgress)
            yield return null;

        //While there are still dialogue lines, continue to run
        while (dialogueQueue.Count > 0)
        {
            currentMessage = dialogueQueue.Peek().message.ToCharArray();
            dialogueText.text = "";
            this.speakerImage.sprite = dialogueQueue.Peek().speakerPicture;
            this.speakerName.text = dialogueQueue.Peek().speakerName;

            //leftClickPriority = true;
            textPrinting = true;
            //Print out the text one character at a time until skip key is pressed
            for (int i = 0; i < currentMessage.Length && !Input.GetKeyDown(KeyCode.Space); i++)
            {
                //If dialogue panel is hidden because player is in main menu, wait for it to become unhidden to continue printing
                while (hidden)
                    yield return null;

                dialogueText.text += currentMessage[i];
                if (i % 2 == 0)
                    yield return null;
            }
            //In case skip key was pressed, print out the rest of the message instantly
            dialogueText.text = dialogueQueue.Peek().message;
            //Clear Mouse Button Down Buffer
            if (Input.GetKeyDown(KeyCode.Space))
                yield return null;
            textPrinting = false;

            //Press Button To continue
            int frameCount = 0, triggerFrame = 30;
            while (!Input.GetKeyDown(KeyCode.Space))
            {
                if (frameCount % triggerFrame == 0 && !continuePrompt.activeSelf)
                    continuePrompt.SetActive(true);
                else if (frameCount % triggerFrame == triggerFrame / 2 && continuePrompt.activeSelf)
                    continuePrompt.SetActive(false);
                else if (frameCount == triggerFrame)
                    frameCount = 0;

                frameCount++;
                yield return null;
            }
            continuePrompt.SetActive(false);


            //Wait if line is designated to hold until condition is met
            //Fade away panel after text is read
            if (dialogueQueue.Peek().holdLine == true)
            {
                StartCoroutine(SetDialogueWindowActive(false));
                while (transitionInProgress)
                    yield return null;
                Tutorial.UpdateObjective();
                Tutorial.ShowObjective(true);
            }

            //Wait for hold to be taken off
            while (dialogueQueue.Peek().holdLine == true)
            {
                holding = true;
                yield return null;
            }
            holding = false;

            Tutorial.ShowObjective(false);
            //Fade Panel back in
            if (panel.activeSelf == false)
            {
                dialogueText.text = "";
                Tutorial.ShowObjective(false);
                StartCoroutine(SetDialogueWindowActive(true));
                while (transitionInProgress)
                    yield return null;
            }
            
            dialogueQueue.Dequeue();
            yield return null;
        }


        StartCoroutine(SetDialogueWindowActive(false));


        while (transitionInProgress)
            yield return null;

        dialogueRunning = false;
    }

    bool transitionInProgress = false;
    /// <summary>
    /// Coroutine that fades the dialogue window UI in and out
    /// </summary>
    /// <param name="active">If True: Dialogue window will appear, If False: Dialogue window will disappear</param>
    /// <returns></returns>
    private IEnumerator SetDialogueWindowActive(bool active)
    {
        transitionInProgress = true;
        float endAlpha;
        //If set Dialogue window to active
        if(active)
        {
            panel.GetComponent<Image>().CrossFadeAlpha(0, 0f, true);
            dialogueText.CrossFadeAlpha(0, 0f, true);
            foreach (Image x in panel.GetComponentsInChildren<Image>())
            {
                x.CrossFadeAlpha(0, 0f, true);
            }


            panel.SetActive(true);
            endAlpha = 1;
        }
        //If set Dialogue window to inactive
        else
        {
            endAlpha = 0;
        }

        panel.GetComponent<Image>().CrossFadeAlpha(endAlpha, 0.5f, true);
        foreach (Image x in panel.GetComponentsInChildren<Image>())
        {
            x.CrossFadeAlpha(endAlpha, 0.5f, true);
        }
        foreach (TMP_Text x in panel.GetComponentsInChildren<TMP_Text>())
        {
            x.CrossFadeAlpha(endAlpha, 0.5f, true);
        }
        foreach (Text x in panel.GetComponentsInChildren<Text>())
        {
            x.CrossFadeAlpha(endAlpha, 0.5f, true);
        }
        yield return new WaitForSecondsRealtime(0.5f);


        if (!active)
            panel.SetActive(false);

        transitionInProgress = false;
    }


    bool hidden = false;
    /// <summary>
    /// Specifically for hiding dialogue when MainMenu is opened
    /// </summary>
    /// <param name="shouldHide">If true: dialogue will be hidden, if false: dialogue window will be unhidden</param>
    private void HideDialogue(bool shouldHide)
    {
        hidden = shouldHide;
        if(shouldHide && dialogueRunning)
        {
            panel.SetActive(false);
            playerPortrait.SetActive(false);
        }
        else if(dialogueRunning && dialogueQueue.Count > 0)
        {
            panel.SetActive(true);
            playerPortrait.SetActive(true);

        }
    }

    void Start()
    {
        if (SaveSystem.SaveExists(SaveSystem.currentSaveSlot))
            TriggerLoad();
    }

    private void OnEnable()
    {
        MainMenu.OnMainMenuTriggered += HideDialogue;
    }

    private void Awake()
    {
        instance = this;

        panel = GameObject.Find("HUD").transform.Find("Dialogue").gameObject;
        playerPortrait = GameObject.Find("CharacterPortrait");
        speakerImage = panel.transform.Find("PhotoSlot").Find("Image").GetComponent<Image>();
        speakerName = panel.transform.Find("PhotoSlot").Find("NameLabel").GetComponent<TMP_Text>();
        dialogueText = panel.transform.Find("Text").GetComponent<TMP_Text>();
        continuePrompt = panel.transform.Find("Prompt").gameObject;

        continuePrompt.SetActive(false);
        panel.SetActive(false);
        dialogueQueue = new Queue<DialogueLine>();

    }

    // Update is called once per frame
    void Update()
    {
        if (dialogueQueue != null && dialogueQueue.Count > 0 && !dialogueRunning)
        {
            StartCoroutine(RunDialogue());
        }
    }

    public static void TriggerLoad()
    {
        instance.StartCoroutine(instance.Load());
    }

    private IEnumerator Load()
    {
        while (instance == null)
            yield return null;

        ForceStop();
    }

    private void OnDisable()
    {
        MainMenu.OnMainMenuTriggered -= HideDialogue;
    }
}
