/* Name: Possessable.cs
 * Author: Zackary Seiple
 * Description: This abstract script allows any GameObject its descendent's are placed on to be possessed by the main character (ghost). This script also
 *              handles the hightlighting possessable items when the player is looking at them. Also controls the vignette appearing
 *              after the player possesses them
 * Last Updated: 2/18/2020 (Zackary Seiple)
 * Changes: Added header
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Possessable : MonoBehaviour
{
    protected bool canMove;
    private bool isHighlighted = false;
    private static Possessable highlightedObject;
    protected Vector3 camOffset = Vector3.zero;
    public float verticalClamp = 60;

    [SerializeField]
    protected static GameObject possessionVignette;
    private static bool hudActive;
    public static bool HudActive
    {
        get
        {
            return hudActive;
        }
        set
        {
            if (value)
                possessionVignette.SetActive(true);
            else
                possessionVignette.SetActive(false);
            hudActive = value;
        }

    }

    protected delegate void PossessionEvent(bool active);
    protected event PossessionEvent OnPossession;

    public Vector3 GetCameraOffset()
    {
        return camOffset;
    }

    /// <summary>
    /// This is meant to be overrided by descendents of this class, these are the abilities whose input will be checked for
    /// every frame in Update()
    /// </summary>
    public abstract void Ability();

    public void TriggerOnPossession(bool possessionActive)
    {
        OnPossession?.Invoke(possessionActive);
    }

    /// <summary>
    /// Toggles the highlight on the GameObject that this is on, if something else is highlighted this will also unhighlight
    /// that object
    /// </summary>
    public void TriggerHighlight()
    {
        if (!isHighlighted && this != highlightedObject)
        {
            if(highlightedObject != null)
            highlightedObject.TriggerHighlight();

            highlightedObject = this;
            StartCoroutine(Highlight());
        }
        else if(this == highlightedObject)
        {
            highlightedObject = null;
        }
    }


    protected virtual void Start()
    {
        if (possessionVignette == null)
            possessionVignette = GameObject.Find("HUD").transform.Find("PossessionVignette").gameObject;

        HudActive = false;

        OnPossession += ToggleVignette;
        OnPossession += ToggleMeshRenderer;
    }

    /// <summary>
    /// Toggles the Vignette HUD in the player's view
    /// </summary>
    /// <param name="possessionActive"></param>
    public void ToggleVignette(bool possessionActive)
    {
        HudActive = possessionActive;
    }

    public void ToggleMeshRenderer(bool possessionActive)
    {
        if(GetComponent<MeshRenderer>())
        {
            GetComponent<MeshRenderer>().enabled = !possessionActive;
        }
    }

    /// <summary>
    /// Getter for the current highlighted GameObject
    /// </summary>
    /// <returns>The currently highlighted GameObject</returns>
    public static Possessable GetHighlightedObject()
    {
        return highlightedObject;
    }

    /// <summary>
    /// Getter for this object's isHighlighted bool
    /// </summary>
    /// <returns>true if this GameObject is highlighted, false if it is not</returns>
    public bool IsHighlighted()
    {
        return isHighlighted;
    }

    /// <summary>
    /// The Coroutine that handles the actual highlighting of the object (The fading in an out of a color)
    /// </summary>
    /// <returns></returns>
    public IEnumerator Highlight()
    {
        isHighlighted = true;

        //HIGHLIGHT VARIABLE
        float transitionTime = 0.75f;
        float currentTime = 0;

        Material mat = GetComponent<MeshRenderer>().material;
        mat.EnableKeyword("_EMISSION");
        Color baseColor = Color.black;
        Color highlightColor = new Color(0 / 255f, 0 / 255f, 140 / 255f);

        mat.SetColor("_EmissionColor", baseColor);
        while (this == highlightedObject)
        {
            //Shift to highlighted color
            while(mat.GetColor("_EmissionColor") != highlightColor && this == highlightedObject)
            {
                mat.SetColor("_EmissionColor", Color.Lerp(baseColor, highlightColor, Mathf.Clamp01(currentTime / transitionTime) ));
                currentTime += Time.deltaTime;
                yield return null;
            }

            currentTime = 0;

            while(mat.GetColor("_EmissionColor") != baseColor && this == highlightedObject)
            {
                mat.SetColor("_EmissionColor", Color.Lerp(highlightColor, baseColor, Mathf.Clamp01(currentTime / transitionTime) ));
                currentTime += Time.deltaTime;
                yield return null;
            }

            currentTime = 0;
            yield return null;
        }
        mat.SetColor("_EmissionColor", baseColor);
        mat.DisableKeyword("_EMISSION");

        isHighlighted = false;
    }

    protected virtual void Update()
    {
        if (Input.GetMouseButtonDown(0) && GetComponent<Player>() != null && !GameController.menuActive)
            Ability();
    }

    private void OnDisable()
    {
        OnPossession -= ToggleVignette;
        OnPossession -= ToggleMeshRenderer;


    }
}
