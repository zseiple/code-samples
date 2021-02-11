/* Name: Photographer.cs
 * Author: Zackary Seiple
 * Description: Contains the behaviour and ability of the Photographer Character. Handles taking pictures and sending them to
 *              the PhotoLibrary to be cropped and stored
 * Last Updated: 5/6/2020 (Zackary Seiple)
 * Changes: Added header
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Photographer : Person
{
    
    //The main camera
    Camera cam;
    //The HUD Canvas Object
    GameObject hud;
    //The rect of the camera (the zone the picture is actually taken in)
    Rect newRect;
    private AudioSource[] audioSources;

    
    private bool cameraLensActive = false;
    public bool CameraLensActive //Controls the camera HUD popping up: Edit this
    {
        get
        {
            return cameraLensActive;
        }
        set
        {
            //Activate Camera HUD Based on this Value
            if(value)
            {
                hud.SetActive(true);
            }
            else
            {
                hud.SetActive(false);
            }
            cameraLensActive = value;
        }
    }

    bool screenshotQueued = false;

    // Start is called before the first frame update
    protected override void Start()
    {
        base.Start();
        cam = Camera.main;
        hud = GameObject.Find("HUD").transform.Find("Camera").gameObject;

        OnPossession += ToggleHUD;

        audioSources = this.GetComponents<AudioSource>();
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
    }

    //Camera
    public bool canTakePhoto = true;
    public override void Ability()
    {
        //if (Dialogue.leftClickPriority == true)
        //    return;

        if (!GameController.paused && canTakePhoto && PhotoLibrary._instance.GetPhotoCount() < PhotoLibrary.MAX_PHOTOS && Time.time > 5)
        {
            Player.EnableControls(false);
            StartCoroutine(CameraFlash());
            audioSources[1].Play();
            TakePhoto(Screen.width, Screen.height);
            Player.EnableControls(true);


        }
        else if (!GameController.paused && canTakePhoto && PhotoLibrary._instance.GetPhotoCount() >= PhotoLibrary.MAX_PHOTOS)
        {
            Log.AddEntry("You have no more room for photos");
        }
    }

    /// <summary>
    /// Pause camera texture for a frame and queue it up to save screen capture
    /// </summary>
    /// <param name="width">The width in pixels of the screen capture</param>
    /// <param name="height">The height in pixels of the screen capture</param>
    public void TakePhoto(int width, int height)
    {
        //Pause the camera texture and process the photo in the next OnRenderObject() call
        cam.targetTexture = RenderTexture.GetTemporary(width, height, 16);
        screenshotQueued = true;
    }

    float flashTime = 1f;
    /// <summary>
    /// Coroutine that makes the screen flash white and fade out when taking a picture
    /// </summary>
    /// <returns></returns>
    private IEnumerator CameraFlash()
    {
        canTakePhoto = false;

        float currentTime = 0;
        Image hudBackground = hud.GetComponent<Image>();
        Color baseColor = hudBackground.color;
        Color flashColor = Color.white;

        hudBackground.color = flashColor;
        yield return new WaitForSeconds(0.5f);

        while(hudBackground.color != baseColor)
        {
            hudBackground.color = Color.Lerp(flashColor, baseColor, currentTime / flashTime);
            currentTime += Time.deltaTime;
            yield return null;
        }

        canTakePhoto = true;
    }

    /// <summary>
    /// Activates and deactivates the Camera Lens HUD
    /// </summary>
    /// <param name="possessionActive">Turns camera lens HUD on if true, off if false</param>
    public void ToggleHUD(bool possessionActive)
    {
        CameraLensActive = possessionActive;
    }


    private void OnRenderObject()
    {
        if (screenshotQueued)
        {
            //Take Picture
            screenshotQueued = false;
            RenderTexture renderTexture = cam.targetTexture;
            Texture2D renderResult = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
            Rect rect = new Rect(0, 0, renderTexture.width, renderTexture.height);
            renderResult.ReadPixels(rect, 0, 0);

            //Release camera to retrun to normal
            RenderTexture.ReleaseTemporary(renderTexture);
            cam.targetTexture = null;


            //Store Picture In Library
            float width = hud.transform.Find("Lens").GetComponent<RectTransform>().sizeDelta.x;
            float height = hud.transform.Find("Lens").GetComponent<RectTransform>().sizeDelta.y;

            PhotoLibrary.StorePhoto(renderResult, 
                                    ClueCatalogue._instance.DetectCluesOnScreen(Screen.width / 2 -  width / 2, 
                                                                                Screen.height / 2 - height / 2,
                                                                                width,
                                                                                height));

        }
    }

    private void OnDisable()
    {
        OnPossession -= ToggleHUD;
    }

}
