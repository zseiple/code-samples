/* Name: Television.cs
 * Primary Author: Zackary Seiple
 * Description: Handles the functions of each individual TV's menu buttons. Includes Save and Load screens that directly interact with the save systems
 * Last Updated: 5/6/2020 (Zackary Seiple)
 * Changes: Added Header
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class Television : MonoBehaviour
{
    public GameObject screen;
    public GameObject mainMenu, tvStatic, howToPlay, saveSelect, creditsMenu;
    public Animation staticAnim;

    //Main Menu
    public enum ButtonName { NewGame, Continue, Resume, HowToPlay, Quit, LoadGame, Credits };
    private Button[] buttons;

    //Save Select
    private GameObject[] saveSelect_slots;
    private GameObject[] saveSelect_delete;
    private GameObject saveSelect_confirmation;
    private TMP_Text saveSelect_confirmation_message;
    private Button[] saveSelect_confirmation_options;

    //TV
    private MeshRenderer mesh;
    private Vector3 boxCenter;
    private float triggerRadius = 5;

    private void OnEnable()
    {
        SaveSystem.OnUpdatedSaveStats += UpdateSaveSlotInfo;
        SaveSystem.OnDeleteSave += UpdateOptions;
        SaveSystem.OnDeleteSave += DeleteSaveSlotInfo;
    }

    private void OnDisable()
    {
        SaveSystem.OnUpdatedSaveStats -= UpdateSaveSlotInfo;
        SaveSystem.OnDeleteSave -= UpdateOptions;
        SaveSystem.OnDeleteSave -= DeleteSaveSlotInfo;
    }

    // Start is called before the first frame update
    void Awake()
    {
        screen = transform.Find("Screen").gameObject;
        mainMenu = screen.transform.Find("MainMenu").gameObject;
        howToPlay = screen.transform.Find("How To Play").gameObject;
        tvStatic = screen.transform.Find("Static").gameObject;
        saveSelect = screen.transform.Find("SaveSelect").gameObject;
        creditsMenu = screen.transform.Find("Credits").gameObject;

        boxCenter = GetComponent<MeshRenderer>().bounds.center;
        
        Transform menuOptions = transform.Find("Screen").Find("MainMenu").Find("MenuOptions");
        buttons = new Button[] { menuOptions.Find("New Game").GetComponent<Button>(), //0
                                     menuOptions.Find("Continue").GetComponent<Button>(), //1
                                     menuOptions.Find("Resume").GetComponent<Button>(), //2
                                     menuOptions.Find("How To Play").GetComponent<Button>(), //3
                                     howToPlay.transform.Find("Back").GetComponent<Button>(), //4
                                     menuOptions.Find("Quit").GetComponent<Button>(), //5
                                     menuOptions.Find("Load Game").GetComponent<Button>(),//6
                                     menuOptions.Find("Credits").GetComponent<Button>(),//7
                                     creditsMenu.transform.Find("Back").GetComponent<Button>()}; //8

        //Add Listeners
        buttons[0].onClick.AddListener(() => MainMenu._instance.TriggerSwitchMenu("SaveSelect"));
        buttons[0].onClick.AddListener(PrepareForNewGame);
        buttons[1].onClick.AddListener(() => MainMenu.TriggerMainMenu());
        buttons[1].onClick.AddListener(() => MainMenu.ChangeFromInitialOptions());
        buttons[2].onClick.AddListener(() => MainMenu.TriggerMainMenu());
        buttons[3].onClick.AddListener(() => MainMenu._instance.TriggerSwitchMenu("How To Play"));
        buttons[4].onClick.AddListener(() => MainMenu._instance.TriggerSwitchMenu("MainMenu"));
        buttons[5].onClick.AddListener(() => GameController.QuitGame());
        buttons[6].onClick.AddListener(() => MainMenu._instance.TriggerSwitchMenu("SaveSelect"));
        buttons[6].onClick.AddListener(PrepareForLoadGame);
        buttons[7].onClick.AddListener(() => MainMenu._instance.TriggerSwitchMenu("Credits"));
        buttons[8].onClick.AddListener(() => MainMenu._instance.TriggerSwitchMenu("MainMenu"));

        //Save Select Screen
        saveSelect_confirmation = saveSelect.transform.Find("Confirmation").gameObject;
        saveSelect_confirmation_message = saveSelect_confirmation.transform.Find("Message").GetComponent<TMP_Text>();
        saveSelect_confirmation_options = new Button[2];
        saveSelect_confirmation_options[0] = saveSelect_confirmation.transform.Find("Options").Find("Confirm").GetComponent<Button>();
        saveSelect_confirmation_options[1] = saveSelect_confirmation.transform.Find("Options").Find("Cancel").GetComponent<Button>();

        saveSelect.transform.Find("Back").GetComponent<Button>().onClick.AddListener(() => MainMenu._instance.TriggerSwitchMenu("MainMenu"));
        saveSelect.transform.Find("Back").GetComponent<Button>().onClick.AddListener(ResetSaveSelect);

        Transform saveSelectSlots = saveSelect.transform.Find("SaveSlots");
        saveSelect_slots = new GameObject[SaveSystem.MAX_SAVE_SLOTS];
        saveSelect_delete = new GameObject[SaveSystem.MAX_SAVE_SLOTS];

        //For each Save Slot Element on Save Select Screen
        for (int i = 1; i <= SaveSystem.MAX_SAVE_SLOTS; i++)
        {
            int temp = i;
            //Set up Save Slot Objects
            saveSelect_slots[temp - 1] = saveSelectSlots.Find(string.Format("Slot {0}", temp)).gameObject;

            //Set Up Listener for Delete button
            saveSelect_delete[temp - 1] = saveSelectSlots.Find(string.Format("Delete {0}", temp)).gameObject;
            saveSelect_delete[temp - 1].GetComponent<Button>().onClick.AddListener(() => 
            {
                if (!saveSelect_confirmation.activeSelf)
                    StartCoroutine(Confirmation(temp
                                   ,string.Format("Are you sure you want to delete ALL save data in Slot {0}?", temp)
                                   ,() => { SaveSystem.DeleteSave(temp); }));
            });

            if (!SaveSystem.SaveExists(temp))
                saveSelect_delete[temp - 1].SetActive(false);
        }

        //Set the right buttons at start
        SwapButtons(true, ButtonName.NewGame, ButtonName.HowToPlay, ButtonName.Credits, ButtonName.Quit);
        SwapButtons(false, ButtonName.LoadGame, ButtonName.Resume, ButtonName.Continue);
        if (SaveSystem.SaveExists(SaveSystem.currentSaveSlot) && GameController.initialTVTransition == true)
            SwapButtons(true, ButtonName.Continue, ButtonName.LoadGame);
        else if (SaveSystem.SaveExists(SaveSystem.currentSaveSlot) && GameController.initialTVTransition == false)
            SwapButtons(true, ButtonName.Resume);
        

        mainMenu.SetActive(false);
        howToPlay.SetActive(false);
        creditsMenu.SetActive(false);


        mesh = GetComponent<MeshRenderer>();
        boxCenter = transform.forward + mesh.bounds.max;

    }

    public void SwapButtons(bool active, params ButtonName[] indexesToToggle)
    {
        foreach(int x in indexesToToggle)
        {
            buttons[x].gameObject.SetActive(active);
        }
    }

    
    public bool CheckForPlayerInRange(Transform playerTransform)
    {
        Vector3 target = playerTransform.GetComponent<MeshRenderer>().bounds.center;
        //Check area in front of TV for player
        RaycastHit[] hit = Physics.RaycastAll(boxCenter, target - boxCenter, Mathf.Clamp(Vector3.Distance(target, boxCenter),0,triggerRadius));
        bool playerFound = false;
        foreach(RaycastHit x in hit)
        {
            if (x.collider.gameObject.GetComponentInParent<Player>())
                playerFound = true;
        }
        return playerFound;
    }

    private void UpdateSaveSlotInfo(int saveSlot, string newDate, float newPlayTime)
    {
        if (saveSelect_slots == null)
        {
            saveSelect_slots = new GameObject[SaveSystem.MAX_SAVE_SLOTS];
            saveSelect_delete = new GameObject[SaveSystem.MAX_SAVE_SLOTS];
        }

        if (saveSelect_slots[saveSlot - 1] == null)
        {
            saveSelect_slots[saveSlot - 1] = screen.transform.Find("SaveSelect").Find("SaveSlots").Find("Slot " + saveSlot).gameObject;
            saveSelect_delete[saveSlot - 1] = saveSelect_slots[saveSlot - 1].transform.parent.Find("Delete " + saveSlot).gameObject;
        }

        saveSelect_slots[saveSlot - 1].transform.Find("SaveStats").Find("Date").GetComponent<TMP_Text>().text = "Date: " + newDate;

        int hours, minutes, seconds;
        newPlayTime -= (hours = (int)(newPlayTime / 3600)) * 3600;
        newPlayTime -= (minutes = (int)(newPlayTime / 60)) * 60;
        seconds = (int)newPlayTime;

        saveSelect_slots[saveSlot - 1].transform.Find("SaveStats").Find("PlayTime").GetComponent<TMP_Text>().text = string.Format("Playtime: {0:D2}:{1:D2}:{2:D2}", hours, minutes, seconds);
        saveSelect_delete[saveSlot - 1].SetActive(true);
    }

    private void DeleteSaveSlotInfo(int saveSlot)
    {
        saveSelect_slots[saveSlot - 1].transform.Find("SaveStats").Find("Date").GetComponent<TMP_Text>().text = "Date: N/A";
        saveSelect_slots[saveSlot - 1].transform.Find("SaveStats").Find("PlayTime").GetComponent<TMP_Text>().text = "Playtime: --:--:--";
        saveSelect_delete[saveSlot - 1].SetActive(false);
    }

    public IEnumerator Confirmation(int saveSlot, string message, UnityEngine.Events.UnityAction action)
    {
        bool waitForResponse = false, confirmationResponse = false;

        saveSelect_confirmation.SetActive(true);
        saveSelect_confirmation_message.text = message;

        saveSelect_confirmation_options[0].GetComponentInChildren<TMP_Text>().text = "Yes";
        UnityEngine.Events.UnityAction confirm = () => { waitForResponse = false; confirmationResponse = true; };
        saveSelect_confirmation_options[0].onClick.AddListener(confirm);

        saveSelect_confirmation_options[1].GetComponentInChildren<TMP_Text>().text = "No";
        UnityEngine.Events.UnityAction cancel = () => { waitForResponse = false; confirmationResponse = false; };
        saveSelect_confirmation_options[1].onClick.AddListener(cancel);

        waitForResponse = true;
        while (waitForResponse)
            yield return null;

        saveSelect_confirmation.SetActive(false);
        saveSelect_confirmation_options[0].onClick.RemoveListener(confirm);
        saveSelect_confirmation_options[1].onClick.RemoveListener(cancel);

        if(confirmationResponse == true)
        {
            action.Invoke();
        }
            
    }


    //OPTION CONTROLS
    void UpdateOptions(int saveSlot)
    {
        if(!SaveSystem.SaveExists(SaveSystem.currentSaveSlot))
        {
            SwapButtons(false, ButtonName.Resume, ButtonName.Continue);
        }
        else
        {
            SwapButtons(true, ButtonName.Continue);
        }

    }

    private void PrepareForNewGame()
    {
        for (int i = 1; i <= SaveSystem.MAX_SAVE_SLOTS; i++)
        {
            int temp = i;
            saveSelect_slots[temp - 1].GetComponent<Button>().onClick.AddListener(() =>
            {
                string newGame_message = SaveSystem.SaveExists(temp) ?
                    string.Format("Are you sure you want to override existing data in Slot {0}?", temp) : //Confirm overwrite of Save Slot
                    string.Format("Would you like to start a new game in Slot {0}?", temp); //Create new save game

                if (!saveSelect_confirmation.activeSelf)
                    StartCoroutine(Confirmation(temp
                                   , newGame_message
                                   , () => 
                                   { SaveSystem.currentSaveSlot = temp;
                                     SceneManager.LoadScene("Opening Cutscene", LoadSceneMode.Single);
                                     MainMenu.ChangeFromInitialOptions();
                                     ResetSaveSelect();
                                   }));
            });
        }
    }

    private void PrepareForLoadGame()
    {
        for (int i = 1; i <= SaveSystem.MAX_SAVE_SLOTS; i++)
        {
            int temp = i;
            //LoadGame Action
            saveSelect_slots[temp - 1].GetComponent<Button>().onClick.AddListener(() =>
            {
                string message = SaveSystem.SaveExists(temp) ?
                    string.Format("Are you sure you want to load game in Slot {0}?", temp) : //Confirm overwrite of Save Slot
                    null; //Create new save game

                if (!saveSelect_confirmation.activeSelf && message != null)
                    StartCoroutine(Confirmation(temp
                                   , message
                                   , () => { SaveSystem.Load(temp); }));
                else
                    Log.AddEntry(string.Format("No Save Data Found in Slot {0}", temp));
            });
        }
    }

    private void ResetSaveSelect()
    {
        for(int i = 1; i <= SaveSystem.MAX_SAVE_SLOTS; i++)
        {
            int temp = i;
            saveSelect_slots[temp - 1].GetComponent<Button>().onClick.RemoveAllListeners();
        }
    }

}


