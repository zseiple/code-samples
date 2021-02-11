/* Name: Tutorial.cs
 * Primary Author: Zackary Seiple - Event System and Objective Framework, events for the tutorial, as well as loading functionality
 * Contributors: Kevon Long - event for entering TV
 *               Matthew Kirchoff - Invisible walls
 * Description: Handles the events and objectives used during the tutorial to guide the player through a set path in learning the game
 * Last Updated: 5/6/2020 (Zackary Seiple)
 * Changes: Added Header
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class Tutorial : MonoBehaviour
{
    public static Tutorial instance;
    public bool isCompleted = false;

    public delegate void TutorialEvent();

    public GameObject[] invisibleWalls;

    private Queue<string> objectives;
    private TMP_Text objectiveText;

    public event TutorialEvent onFirstMovement;
    public event TutorialEvent onFirstRatPossession;
    public event TutorialEvent onFirstTVEnter;
    public event TutorialEvent onPhotographerEnter;
    public event TutorialEvent onFirstPhoto;
    public event TutorialEvent onFirstCloseScrapbook;
    public event TutorialEvent onTutorialEnd;

    bool photographerEntered = false;

    private IEnumerator DialogueScript()
    {

        yield return new WaitForSeconds(5f);
        Dialogue.AddLine(Dialogue.Character.Pete, true, "Hey you, you’re finally awake. It’s about time! I know you’re new to the whole ‘trapped soul’ gig but you’ve been dead for hours. Take a gander around and walk over to your body with the W, A, S, D keys.");
        objectives.Enqueue("Move around (Press W, A, S, or D)");
        Dialogue.AddLine(Dialogue.Character.Pete, "I’m sure you’re itching to get on with your afterlife. But if you don’t want to be stuck in this here motel like good ole Pete, you’ll need to solve your murder.", "You can’t be findin’ peace with unresolved business, now can ya?");
        Dialogue.AddLine(Dialogue.Character.Pete, "You have a couple of perks you can use now. First, you can see the aura of objects around you. These objects can provide important clues about your murder.", "<b>You should take a photo of them at every opportunity</b>. We’ll get back to the picture takin’ later...");
        Dialogue.AddLine(Dialogue.Character.Pete, "Another perk of being newly dead is being able to possess living creatures. You see that rat over there? You can possess him by pressing E and depossess him by pressing Q.");
        Dialogue.AddLine(Dialogue.Character.Pete, "Creatures you possess can also pick up items, like keys or paper balls, with the [F] key. Items with a white aura can be interacted with and will have text saying how to use them right above 'em.");
        Dialogue.AddLine(Dialogue.Character.Pete, true, "Also, items that are picked up by a <b>human</b> you're possessing are brought with you when you decide to possess someone new. How about you try possessing one of these critters, hm.");
        objectives.Enqueue("Possess a rat (Press E to Possess)");
        Dialogue.AddLine(Dialogue.Character.Pete, true, "Now, get out of that rat and head over to a Television set over there. The TV's in each room are also possessable. When you possess the TV, <b>you can access the main menu, save/load, or adjust settings</b>.");
        objectives.Enqueue("Possess a TV by standing in front of it and pressing [F] as a spirit");
        Dialogue.AddLine(Dialogue.Character.Pete, true, "If you’re gonna start solvin’ your murder, you’ll need to have a way to document evidence. See if you can find someone with a camera and bring him back here, would ya? Use what I’ve taught ya so far.");
        OnPhotographerEnter();
        objectives.Enqueue("Find the Photographer and bring him to the crime scene");
        Dialogue.AddLine(Dialogue.Character.Pete, true, "Good job on getting that there photographer in here. Now remember what I was sayin’ about those objects with a white aura? Go and take some photographs of those right quick.");
        objectives.Enqueue("Take a picture your dead body (Left-Click as the Photographer)");
        Dialogue.AddLine(Dialogue.Character.Pete, "Great, you’ve collected some evidence! <b>Now listen, this is important</b>. You'll want to keep the photographer close since you'll find more evidence the more you look around.");
        Dialogue.AddLine(Dialogue.Character.Pete, true, "Take a look at those photographs in your scrapbook by pressing [TAB]. You’ll be able to examine them for more clues there.");
        objectives.Enqueue("Open your scrapbook (Press Tab)");
        Dialogue.AddLine(Dialogue.Character.Pete, "There's also a notepad section of your scrapbook where you can record your thoughts and observations. You’ll definitely want to use that later as well.", "You can also run around to get places faster by holding [LShift] while you're moving. Take a peek at the reference tab in your scrapbook if you're ever lost on how to move about the world.");
        Dialogue.AddLine(Dialogue.Character.Pete, "If you <b>use that brain of yours</b>, imitate all them characters on those detective shows that play on the TV nowadays, and ya keep that camera nearby, you should have this mess figured out in no time.");
        Dialogue.AddLine(Dialogue.Character.Pete, "Well good luck out there youngin! I’ll be rootin’ for ya! You’re on your own for now, but this aint the last you’ll see Ol’ Pete. If you’re clever enough, I’ve got a <b>little gift</b> for ya later. Adios ami-ghost!");
        TriggerTutorialEnd();
    }

    Collider[] hit;
    private IEnumerator WaitForPhotographerEnter()
    {
        while (!photographerEntered && !isCompleted)
        {
            hit = Physics.OverlapBox(new Vector3(77.57f, 13.64f, -72.79f), new Vector3(3.65f / 2, 5.92f / 2, 2.41f / 2), Quaternion.identity);
            foreach (Collider x in hit)
            {
                if (x.GetComponent<Photographer>())
                {
                    photographerEntered = true;
                }
                yield return null;
            }
        }
    }

    private void Awake()
    {
        instance = this;

        isCompleted = false;
        objectives = new Queue<string>();
        objectiveText = GameController._instance.mainHUD.transform.Find("Objective").GetComponent<TMP_Text>();
    }

    private void Start()
    {

        //DontDestroyOnLoad(instance.transform.parent.gameObject);
        //Load
        if (SaveSystem.gameData != null)
            Load(SaveSystem.gameData.tutorialData);

        if (!isCompleted && !tutorialTriggered)
        {
            tutorialTriggered = true;
            StartCoroutine(DialogueScript());
        }
        else
        {
            TriggerTutorialEnd();
        }
        StartCoroutine(WaitForPhotographerEnter());


    }


    private void OnEnable()
    {
        onFirstMovement += TriggerFirstMovement;
        onFirstRatPossession += TriggerFirstRatPossession;
        onFirstTVEnter += TriggerTVEnter;
        onPhotographerEnter += TriggerPhotographerEnter;
        onFirstPhoto += TriggerFirstPhoto;
        onFirstCloseScrapbook += TriggerFirstCloseScrapbook;

    }

    private void OnDisable()
    {
        onFirstMovement -= TriggerFirstMovement;
        onFirstRatPossession -= TriggerFirstRatPossession;
        onFirstTVEnter -= TriggerTVEnter;
        onPhotographerEnter -= TriggerPhotographerEnter;
        onFirstPhoto -= TriggerFirstPhoto;
        onFirstCloseScrapbook -= TriggerFirstCloseScrapbook;

    }


    // Start is called before the first frame update
    bool tutorialTriggered = false;


    public static void UpdateObjective()
    {
        if(instance.objectives.Count > 0)
        {
            instance.objectiveText.text = instance.objectives.Dequeue();
        }
    }

    public static void ShowObjective(bool shouldShowObjective)
    {
        instance.objectiveText.gameObject.SetActive(shouldShowObjective);
    }

    public void OnFirstMovement()
    {
        onFirstMovement?.Invoke();
    }

    public void OnFirstRatPossession()
    {
        onFirstRatPossession?.Invoke();
    }

    public void OnFirstTVEnter()
    {
        onFirstTVEnter?.Invoke();
    }

    public void OnPhotographerEnter()
    {
        onPhotographerEnter?.Invoke();
    }

    public void OnFirstPhoto()
    {
        onFirstPhoto?.Invoke();
    }

    public void OnFirstCloseScrapbook()
    {
        onFirstCloseScrapbook?.Invoke();
    }

    //FIRST MOVEMENT
    private void TriggerFirstMovement()
    {
        StartCoroutine(FirstMovement()); 
    }

    private IEnumerator FirstMovement()
    {
        while(!Dialogue.holding)
        {
            yield return null;
        }

        Dialogue.ContinueDialogue();
        onFirstMovement -= TriggerFirstMovement;

    }

    //RAT POSSESSION
    private void TriggerFirstRatPossession()
    {
        StartCoroutine(FirstRatPossession());
    }

    private IEnumerator FirstRatPossession()
    {
        while (!Dialogue.holding || onFirstMovement != null)
        {
            yield return null;
        }

        Dialogue.ContinueDialogue();
        onFirstRatPossession -= TriggerFirstRatPossession;
    }

    //FIRST TV ENTERED
    private void TriggerTVEnter()
    {
        StartCoroutine(TVEnter());
    }

    private IEnumerator TVEnter()
    {
        while (!Dialogue.holding || onFirstMovement != null || onFirstRatPossession != null)
        {
            yield return null;
        }
        Dialogue.ContinueDialogue();
        onFirstTVEnter -= TriggerTVEnter;
    }

    //PHOTOGRAPHER ENTER
    private void TriggerPhotographerEnter()
    {
        StartCoroutine(PhotographerEnter());
    }

    private IEnumerator PhotographerEnter()
    {
        while (!photographerEntered || !Dialogue.holding || onFirstMovement != null || onFirstRatPossession != null || onFirstTVEnter != null)
        {
            yield return null;
        }

        Dialogue.ContinueDialogue();
        onPhotographerEnter -= TriggerPhotographerEnter;
    }

    //FIRST PHOTO
    private void TriggerFirstPhoto()
    {
        if (onPhotographerEnter == null)
        {
            StartCoroutine(FirstPhoto());
        }
    }

    private IEnumerator FirstPhoto()
    {
        while (!Dialogue.holding || onFirstMovement != null || onFirstRatPossession != null || onFirstTVEnter != null || onPhotographerEnter != null)
        {
            yield return null;
        }
        //Debug.Log("Photo Conditions Met");

        Dialogue.ContinueDialogue();
        onFirstPhoto -= TriggerFirstPhoto;
    }

    //CLOSE SCRAPBOOK
    private void TriggerFirstCloseScrapbook()
    {
        if(onFirstPhoto == null)
            StartCoroutine(FirstCloseScrapbook());
    }

    private IEnumerator FirstCloseScrapbook()
    {
        while (!Dialogue.holding || onFirstMovement != null || onFirstRatPossession != null || onFirstTVEnter != null || onPhotographerEnter != null
                || onFirstPhoto != null)
        {
            yield return null;
        }

        Dialogue.ContinueDialogue();
        onFirstCloseScrapbook -= TriggerFirstCloseScrapbook;
    }

    //ON TUTORIAL END
    private void TriggerTutorialEnd()
    {
        StartCoroutine(TutorialEnd());
    }

    private IEnumerator TutorialEnd()
    {

        //Wait until other parts of the tutorial have been completed
        while(onFirstRatPossession != null || onPhotographerEnter != null
               || onFirstPhoto != null || onFirstCloseScrapbook != null || Dialogue.instance.dialogueQueue.Count > 0)
        {
            if (isCompleted)
                break;
            yield return null;
        }

       

        //INSERT CODE TO OCCUR AFTER TUTORIAL END HERE
        foreach(GameObject wall in invisibleWalls)
        {
            Destroy(wall);
        }

        isCompleted = true;

        if (SaveSystem.gameData.tutorialData.tutorialCompleted == false)
        {
            objectives.Enqueue("Solve your murder");
            UpdateObjective();
            ShowObjective(true);
            yield return new WaitForSeconds(7f);
            ShowObjective(false);
            //Debug.Log("TutorialEnd");
        }

    }

    public static void Load(Data.TutorialData tutorialData)
    {
        Debug.Log("Loading Tutorial isCompleted: " + tutorialData.tutorialCompleted + " | From Slot: " + SaveSystem.currentSaveSlot);
        instance.isCompleted = tutorialData.tutorialCompleted;
    }
}
