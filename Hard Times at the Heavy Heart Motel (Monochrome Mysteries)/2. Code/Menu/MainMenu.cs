/* Name: MainMenu.cs
 * Primary Author: Zackary Seiple 
 * Description: Handles the management of every Television (In-world main menu accessible from different poitns) object in the scene, 
 * keeping track of the active TV, management of the initial transition into the Main Menu, and switching Menus via static transition
 * Last Updated: 5/6/2020 (Zackary Seiple)
 * Changes: Added Header
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public static MainMenu _instance;

    //The main camera which will zoom into the TV Menu
    private Camera cam;

    [Header("MainMenu")]
    //The index of the last TV accessed by the player
    private int lastTvIndex = 0;
    //Whether the player is in range of the TV
    private bool playerInTVRange = false;
    //The menu the player is currently on
    private GameObject currentMenu;
    //The Television script of the current TV
    private Television currentTV;

    public delegate void MainMenuEvent(bool isActive);
    public static event MainMenuEvent OnMainMenuTriggered;

    public Television[] TVs;

    public GameObject playerPortrait;


    //Says whether the MAIN MENU ONLY is active
    public static bool active = false;

    private void Start()
    {
        playerInTVRange = true;

        if (SaveSystem.gameData != null)
            _instance.Load(SaveSystem.gameData.mainMenuData);
        else
            loadComplete = true;

    }

    private void Awake()
    {
        _instance = this;

        active = false;
        cam = Camera.main;

        TVs = FindObjectsOfType<Television>();

        playerPortrait = GameObject.Find("CharacterPortrait");

        loadComplete = false;

    }

    bool initialized = false;



    //frameCount: The current frame count
    //triggerFrame: The frame interval to check whether the player is near the TV
    int frameCount = 0, triggerFrame = 20;
    // Update is called once per frame
    void Update()
    {
        //TV UPDATE
        //Check each tv to see if player is in range
        if (frameCount % triggerFrame == 0)
            UpdateTVRanges();
        else if (frameCount % triggerFrame == triggerFrame)
            frameCount = 0;
        frameCount++;

        //Enter/Exit Main Menu
        if (Input.GetKeyDown(KeyCode.F) || (Input.GetKeyDown(KeyCode.Escape) && MainMenu.GetCurrentMenu() == "MainMenu" && GameController.menuActive))
        {
            TriggerMainMenu();
        }
        else if (Input.GetKeyDown(KeyCode.Escape) && GameController.menuActive)
        {
            TriggerSwitchMenu("MainMenu");
        }

    }

    public static int GetLastTVIndex()
    {
        return _instance.lastTvIndex;
    }

    public static string GetCurrentMenu()
    {
        if(_instance.currentMenu)
            return _instance.currentMenu.name;
        return "";
    }

    /// <summary>
    /// Starts the transition in or out of the menu
    /// </summary>
    public static void TriggerMainMenu()
    {
        if (!MainMenu._instance.tvTransitionInProgress && MainMenu.IsInRange() && (!GameController.menuActive || MainMenu.active) && (Player.tv_Visible || (Time.timeSinceLevelLoad < 5 && !MainMenu.active)))
        {
            Debug.Log("Entering TV");
            MainMenu._instance.StartCoroutine(MainMenu._instance.TriggerTV());
        }
        else
        {
            Debug.Log("tvTransitition: " + !MainMenu._instance.tvTransitionInProgress + " | In Range: " + IsInRange() + " | Etc: " + (!GameController.menuActive || MainMenu.active) + " | vis: " + (Player.tv_Visible || Time.timeSinceLevelLoad < 5));

        }
    }

    /// <summary>
    /// Switched the menu screen
    /// </summary>
    /// <param name="menu">The name of the menu to switch to</param>
    public void TriggerSwitchMenu(string menu)
    {
        if (!menuTransitionInProgress)
            StartCoroutine(SwitchMenu(menu));
    }

    public static bool IsInRange()
    {
        return _instance.playerInTVRange;
    }

    /// <summary>
    /// After player starts the game after boot, add resume button
    /// </summary>
    public static void ChangeFromInitialOptions()
    {
        foreach(Television instance in _instance.TVs)
        {
            //Turn new game button off and resume on
            instance.SwapButtons(false, Television.ButtonName.Continue);
            instance.SwapButtons(true, Television.ButtonName.Resume);
        }
    }

    public void SetCurrentTV(Television set)
    {
        currentTV = set;
    }

    public Television GetCurrentTV()
    {
        return currentTV;
    }

    /// <summary>
    /// Check for player in range of all TVs
    /// </summary>
    public static void UpdateTVRanges()
    {
        Transform player = (FindObjectOfType(typeof(Player)) as Player).transform;
        _instance.playerInTVRange = false;
        for (int i = 0; i < _instance.TVs.Length; i++)
        {
            if (_instance.TVs[i].CheckForPlayerInRange(player))
            {
                _instance.playerInTVRange = true;
                _instance.lastTvIndex = i;
                _instance.currentTV = _instance.TVs[i];

            }
        }

        Debug.Log("Failed");
        if (!_instance.playerInTVRange)
            _instance.lastTvIndex = -1;
    }

    //Store the player transform to return after player exits the menu
    Transform tempPlayerStorage = null;
    Quaternion tempRotationStorage = Quaternion.identity;
    public bool tvTransitionInProgress;
    float tvTransitionTime = 0.75f;
    /// <summary>
    /// Handles the flow into the TV main menu;
    /// </summary>
    /// <returns></returns>
    public IEnumerator TriggerTV()
    {

        tvTransitionInProgress = true;
        Player.EnableControls(false);

        while (Player.GetPossessionInProgress())
            yield return null;

        //Changes 'paused' to true if this pauses the game, false if the game was already paused and is now unpausing
        GameController.TogglePause();
        //If true, this function handles the transition INTO the main menu, if false, this function handles the exit
        active = GameController.paused;
        //This variable is static and lets the game know that there is now a menu up (So another menu shouldn't be able to pop up on top of it)
        if(active == true)
        GameController.menuActive = GameController.paused;
        Transform targetTransform;
        Quaternion targetRotation;

        //Take away player controls if moving into main menu
        if (active)
        {
            Player.EnableControls(false);
            OnMainMenuTriggered?.Invoke(true);
        }
        //Exiting menu so return to static
        else
        {
            if (Time.time >= 5 && Dialogue.holding)
            {
                Tutorial.instance.OnFirstTVEnter();
            }
            currentTV.tvStatic.SetActive(true);
            currentTV.mainMenu.SetActive(false);
            currentTV.saveSelect.SetActive(false);
            currentTV.howToPlay.SetActive(false);
            OnMainMenuTriggered?.Invoke(false);
        }


        //Transitioning into TV, store player transform so camera can be returned properly
        if (cam.GetComponentInParent<Player>())
        {
            tempPlayerStorage = cam.transform.parent;
            tempRotationStorage = cam.transform.localRotation;

            targetTransform = currentTV.transform.Find("CamPoint");
            targetRotation = Quaternion.identity;

            Photographer photographerX;
            if((photographerX = cam.GetComponentInParent<Photographer>()) && photographerX.CameraLensActive)
            {
                photographerX.CameraLensActive = false;
            }
        }
        //Transitioning out of TV back into player
        else
        {
            targetTransform = tempPlayerStorage;
            targetRotation = tempRotationStorage;
        }

        var tempRotation = cam.transform.rotation;
        cam.transform.parent = targetTransform;
        cam.transform.rotation = tempRotation;


        //Progress toward target transform's position and rotation
        Vector3 startPos = cam.transform.localPosition;
        Vector3 targetPos = targetTransform.GetComponent<Player>() ? Vector3.zero + targetTransform.GetComponent<Player>().camOffset : Vector3.zero;
        Quaternion startRot = cam.transform.localRotation;

        float currentTime = 0;

        while (currentTime <= tvTransitionTime)
        {
            currentTime += Time.unscaledDeltaTime;
            cam.transform.localPosition = Vector3.Lerp(startPos, targetPos, Mathf.SmoothStep(0f, 1f, currentTime / tvTransitionTime));
            cam.transform.localRotation = Quaternion.Lerp(startRot, targetRotation, Mathf.SmoothStep(0f, 1f, currentTime / tvTransitionTime));
            yield return null;
        }

        //Return player controls if moving out of main menu AFTER TRANSITION
        if (!active)
        {
            Player.EnableControls(true);
            GameController.menuActive = GameController.paused;
        }
        //Entering menu so turn menu on and static off
        else
        {
            currentTV.tvStatic.SetActive(false);
            currentTV.mainMenu.SetActive(true);
            currentMenu = currentTV.mainMenu;

            if (Time.timeSinceLevelLoad > 5)
                SaveSystem.Save(SaveSystem.currentSaveSlot);
        }

        Photographer photographer;
        if ((photographer = cam.GetComponentInParent<Photographer>()) && photographer.CameraLensActive == false)
        {
            photographer.CameraLensActive = true;
        }

        tvTransitionInProgress = false;
    }

    float menuTransitionTime = 0.5f;
    bool menuTransitionInProgress = false;
    /// <summary>
    /// Coroutine that handles the transition to a different screen (TV turning to static and and then showing the menu)
    /// </summary>
    /// <param name="menuNameToSwitchTo">The name of the menu to switch to</param>
    /// <returns></returns>
    private IEnumerator SwitchMenu(string menuNameToSwitchTo)
    {
        menuTransitionInProgress = true;
        if(currentMenu != null)
            currentMenu.SetActive(false);

        while (currentTV == null)
        {
            yield return null;
        }

        currentTV.tvStatic.SetActive(true);

        yield return new WaitForSecondsRealtime(menuTransitionTime);

        currentMenu = currentTV.transform.Find("Screen").Find(menuNameToSwitchTo).gameObject;
        currentMenu.SetActive(true);
        currentTV.tvStatic.SetActive(false);
        menuTransitionInProgress = false;
    }

    public bool loadComplete = false;
    public void Load(Data.MainMenuData mainMenuData)
    {
        var tvPos = new Vector3(mainMenuData.currentTV_pos[0], mainMenuData.currentTV_pos[1], mainMenuData.currentTV_pos[2]);
        Debug.Log("TV POS: " + tvPos);
        Collider[] hit = Physics.OverlapSphere(tvPos, 1f);
        Television result = null;
        foreach (Collider x in hit)
        {
            if (x.GetComponent<Television>())
                result = x.GetComponent<Television>();
        }

        if (result != null)
        {
            Debug.Log("Loaded TV: " + result.transform.parent);
            _instance.SetCurrentTV(result);
            GameController._instance.playerSpawn.transform.position = result.transform.Find("CamPoint").position;
            GameController._instance.playerSpawn.transform.rotation = result.transform.Find("CamPoint").rotation;
        }
        else
        {
            Debug.Log("Failed to load TV");
        }

        loadComplete = true;
        Debug.Log("Main Menu Load Complete");
    }
}
