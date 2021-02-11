/* Name: PhotoLibrary.cs
 * Primary Author: Zackary Seiple
 * Description: Contains a UI library of all the photos taken by the Photographer. Controls the Cropping, Storing, Deletion, and Updating of the scrapbook with photos
 * Last Updated: 5/6/2020 (Zackary Seiple)
 * Changes: Added Header
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PhotoLibrary : MonoBehaviour
{
    public static PhotoLibrary _instance;
    public const int MAX_PHOTOS = 99;

    [Tooltip("The collection of descriptions and other information for photos")]
    public List<PhotoInfo> photoInfo;
    //The collection of photoslots
    private PhotoSlot[] photoSlots;
    //The number of photos so far
    public uint photoCount = 0;
    //The number of photos to be displayed per page
    private int photosPerPage = 3;
    //The current page number
    private int pageNum = 0;

    [Header("Save System")]
    public List<string> photoPaths;

    private GameObject selectedSlot;
    [HideInInspector]
    public GameObject SelectedSlot
    {
        get { return selectedSlot; }
        set
        {
            //Handle highlights
            if (selectedSlot != null)
                Destroy(selectedSlot.GetComponent<Outline>());
            selectedSlot = value;
            selectedSlot.AddComponent<Outline>().OutlineColor = Color.yellow;
        }
    }

    //UI Elements
    public GameObject photoCollectionMenu;
    public GameObject photoSlotPrefab;
    private GameObject examinePhotoMenu;
    private GameObject photoGrid;
    private GameObject noPhotosText;
    private Button prevPageButton, nextPageButton;
    private int currentPage = 0;

    private delegate void ScrapbookEvent();
    //Called anytime a photo is taken/deleted/or page is changed
    private event ScrapbookEvent OnScrapbookChange;

    /// <summary>
    /// The photo structure that contains the image associated with a photo as well as the clues featured in the photo
    /// </summary>
    public struct PhotoInfo
    {

        [Header("Photo Elements")]
        public Sprite image;
        public string image_path;
        public int[] cluesFeatured;
        public string labelText, detailsText;

        public PhotoInfo(Sprite image, string image_path, params int[] cluesFeatured)
        {
            this.image = image;
            this.image_path = image_path;

            if (cluesFeatured == null)
                cluesFeatured = new int[0];

            this.cluesFeatured = cluesFeatured;

            labelText = detailsText = "";


            foreach(int x in cluesFeatured)
            {

                if (labelText != "")
                    labelText += ", ";

                labelText += ClueCatalogue._instance.clues[x].name;
                detailsText += ClueCatalogue._instance.clues[x].name + ":\n";
                detailsText += ClueCatalogue._instance.clues[x].description + "\n\n";

            }
        }

        public void Deconstruct()
        {
            Destroy(image);
        }

        public void SetClues(params int[] clues)
        {
            cluesFeatured = clues;
        }     

    }
    
    /// <summary>
    /// Triggers the event to update UI on Scrapbook change
    /// </summary>
    public static void TriggerOnScrapbookChange()
    {
        _instance.OnScrapbookChange?.Invoke();
    }

    public static List<PhotoInfo> GetPhotoInfo()
    {
        return _instance.photoInfo;
    }

    /// <summary>
    /// The Photo Slot Prefab and the various parts associated with it
    /// </summary>
    public class PhotoSlot
    {
        public GameObject photoSlot;
        public Image displayImage;
        public Text label;

        [Header("SubMenu")]
        public RectTransform subMenu;
        private float subMenu_TransitionTime = 0.25f;
        public bool subMenu_Active = false;

        public Button examineButton;
        public Button deleteButton;

        public PhotoSlot(GameObject photoSlot)
        {
            this.photoSlot = photoSlot;

            //Assign Submenu
            if (photoSlot.transform.Find("Submenu").GetComponent<RectTransform>())
                this.subMenu = photoSlot.transform.Find("Submenu").GetComponent<RectTransform>();

            //Assign Image
            if (photoSlot.transform.Find("Image").GetComponent<Image>())
                this.displayImage = photoSlot.transform.Find("Image").GetComponent<Image>();

            //Assign Label
            if (photoSlot.transform.Find("Label").GetComponent<Text>())
                this.label = photoSlot.transform.Find("Label").GetComponent<Text>();

            //Assign ExamineButton
            examineButton = subMenu.transform.Find("ExamineButton").GetComponent<Button>();

            //Assign DeleteButton
            deleteButton = subMenu.transform.Find("DeleteButton").GetComponent<Button>();

            //OnPointerEnter - Bring out Submenu
            EventTrigger.Entry onPointerEnterEntry = new EventTrigger.Entry();
            onPointerEnterEntry.eventID = EventTriggerType.PointerEnter;
            onPointerEnterEntry.callback.AddListener((eventData) => { _instance.selectedSlot = eventData.selectedObject; _instance.StartCoroutine(TogglePhotoSlotSubMenu(true)); });
            photoSlot.GetComponent<EventTrigger>().triggers.Add(onPointerEnterEntry);
            //OnPointerExit - Hide Submenu
            EventTrigger.Entry onPointerExitEntry = new EventTrigger.Entry();
            onPointerExitEntry.eventID = EventTriggerType.PointerExit;
            onPointerExitEntry.callback.AddListener((eventData) => { _instance.selectedSlot = null; _instance.StartCoroutine(TogglePhotoSlotSubMenu(false)); });
            photoSlot.GetComponent<EventTrigger>().triggers.Add(onPointerExitEntry);
        }

        /// <summary>
        /// Toggles the submenu with interaction buttons to appear
        /// </summary>
        /// <param name="active">If true: submenu will come into view, if false: submenu will deactivate</param>
        /// <returns></returns>
        public IEnumerator TogglePhotoSlotSubMenu(bool active)
        {
            if (subMenu_Active == active)
                yield break;

            Vector3 startPos = Vector3.zero;
            Vector3 endPos = startPos;
            float shiftValue = subMenu.sizeDelta.x + photoSlot.GetComponentInParent<GridLayoutGroup>().cellSize.x / 2;
            //Debug.Log(subMenu_Active);
            if (!subMenu_Active)
                endPos.x += shiftValue * GameController._instance.mainHUD.GetComponent<CanvasScaler>().scaleFactor;
            else
            {
                startPos.x += shiftValue * GameController._instance.mainHUD.GetComponent<CanvasScaler>().scaleFactor;
            }

            subMenu_Active = !subMenu_Active;
            bool startBool = subMenu_Active;


            float currentTime = 0;
            while (startPos != endPos && subMenu_Active == startBool)
            {
                subMenu.anchoredPosition = Vector3.Lerp(startPos, endPos, currentTime / subMenu_TransitionTime);
                currentTime += Time.unscaledDeltaTime;
                yield return null;
            }
            subMenu.anchoredPosition = endPos;
        }
    }

    private void Awake()
    {
        _instance = this;

        noPhotosText = photoCollectionMenu.transform.Find("NoPhotosText").gameObject;
        examinePhotoMenu = photoCollectionMenu.transform.Find("ExamineMenu").gameObject;
        photoGrid = photoCollectionMenu.transform.Find("Grid").gameObject;
        prevPageButton = photoCollectionMenu.transform.Find("PreviousButton").GetComponent<Button>();
        nextPageButton = photoCollectionMenu.transform.Find("NextButton").GetComponent<Button>();

        prevPageButton.onClick.AddListener(() => { pageNum--; OnScrapbookChange?.Invoke(); });
        nextPageButton.onClick.AddListener(() => { pageNum++; OnScrapbookChange?.Invoke(); });

        prevPageButton.gameObject.SetActive(false);
        nextPageButton.gameObject.SetActive(false);

        //Create PhotoSlots
        photoSlots = new PhotoSlot[photosPerPage];
        for (int i = 0; i < photosPerPage; i++)
        {
            GameObject newSlot = Instantiate<GameObject>(photoSlotPrefab, photoGrid.transform);
            photoSlots[i] = new PhotoSlot(newSlot);
        }

        photoInfo = new List<PhotoInfo>();
        photoPaths = new List<string>();

    }

    // Start is called before the first frame update
    void Start()
    {


        //Save
        if (SaveSystem.gameData != null)
            Load(SaveSystem.gameData.libraryData);

        OnScrapbookChange?.Invoke();

    }

    private void OnEnable()
    {
        OnScrapbookChange += UpdateUI;
    }

    private void OnDisable()
    {
        OnScrapbookChange -= UpdateUI;
    }

    private void Update()
    {
        if (examining && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab)))
            ExaminePhoto(photoBeingExamined);
    }

    bool examining = false;
    PhotoInfo photoBeingExamined;
    /// <summary>
    /// Toggle the examining of a specific photo
    /// </summary>
    /// <param name="photo">The Photo in scrapbook to be examined</param>
    private void ExaminePhoto(PhotoInfo photo)
    {
        photoBeingExamined = photo;
        //If not examining, start examining
        if (examining == false)
        {
            examining = true;

            //Set Description and Label
            Transform examinePhotoSlot = examinePhotoMenu.transform.Find("PhotoSlot");
            examinePhotoSlot.Find("Image").GetComponent<Image>().sprite = photo.image;
            examinePhotoSlot.Find("Label").GetComponent<Text>().text = photo.labelText;
            examinePhotoMenu.transform.Find("Description").GetComponent<Text>().text = photo.detailsText;
            GameController._instance.tabs.SetActive(false);

            //If no details, then there are no clues featured, no need to have description so just center the picture
            if(photo.detailsText == "")
            {
                examinePhotoMenu.transform.Find("Description").GetComponent<LayoutElement>().ignoreLayout = true;
            }

            examinePhotoMenu.SetActive(true);
            photoGrid.SetActive(false);

            //Let the photo be clicked to exit examining mode
            examinePhotoMenu.GetComponent<Button>().onClick.AddListener(() => { ExaminePhoto(photo); });

        }
        //If examining, stop examining
        else if(examining == true)
        {
            examining = false;

            //No need to have the menu be clickable
            examinePhotoMenu.GetComponent<Button>().onClick.RemoveAllListeners();
            GameController._instance.tabs.SetActive(true);

            //if the description was removed, put it back
            if (photo.detailsText == "")
            {
                examinePhotoMenu.transform.Find("Description").GetComponent<LayoutElement>().ignoreLayout = false;
            }
            examinePhotoMenu.SetActive(false);
            photoGrid.SetActive(true);
        }

    }

    /// <summary>
    /// Getter for the number of photos currently possessed by the player
    /// </summary>
    /// <returns>An integer representing the number of photos currently in the scrapbook</returns>
    public int GetPhotoCount()
    {
        return photoInfo.Count;
    }

    /// <summary>
    /// Add details to a photo
    /// </summary>
    /// <param name="photoInfoToAdd">A PhotoInfo Object that contains the details to add</param>
    public static void AddToPhotoInfo(PhotoInfo photoInfoToAdd)
    {
        _instance.photoInfo.Add(photoInfoToAdd);
    }

    /// <summary>
    /// Crops and turns the photo from the Photographer into a sprite to be put into an image and placed in scrapbook
    /// </summary>
    /// <param name="photo">The Texture2D photo taken by the Photographer</param>
    /// <param name="clues">An array of ints representing the indexes of clues in ClueCatalogue</param>
    public static void StorePhoto(Texture2D photo, params int[] clues)
    {
        foreach(int x in clues)
        {
            if(ClueCatalogue._instance.clues[x].name == "Dead Body" && Dialogue.holding)
                //First Photo
                Tutorial.instance.OnFirstPhoto();
        }

        photo = _instance.CropPhoto(photo, 160 * 2, 150 * 2);

        //Create Image
        Sprite newSprite = Sprite.Create(photo, new Rect(0, 0, photo.width, photo.height), Vector2.zero, 100f);
        newSprite.name = string.Format("{1}Photo-{0}", _instance.photoCount, SaveSystem.currentSaveSlot);

        //Save image
        string savePath = Path.Combine(Application.persistentDataPath, "_GameData", newSprite.name + ".png");
        System.IO.File.WriteAllBytes(savePath, photo.EncodeToPNG());
        _instance.photoPaths.Add(savePath);

        newSprite.texture.Apply();

        _instance.photoInfo.Add(new PhotoInfo(newSprite, savePath, clues));        

        _instance.photoCount++;

        _instance.OnScrapbookChange?.Invoke();
    }

    public static void StorePhoto(Texture2D photo)
    {
        StorePhoto(photo, new int[0]);
    }

    /// <summary>
    /// Remove a photo from the scrapbook
    /// </summary>
    /// <param name="photo">Photo struct to be removed</param>
    public void RemovePhoto(PhotoInfo photo)
    {
       int indexToRemove = _instance.photoInfo.FindIndex((PhotoInfo photoI) => { return photoI.image == photo.image && 
                                                                 photoI.labelText == photo.labelText &&
                                                                 photoI.detailsText == photo.detailsText; });
        if (indexToRemove != -1)
        {
            //Destroy Photo Slot GameObject
            _instance.photoInfo[indexToRemove].Deconstruct();
            //Remove from Scrapbook
            _instance.photoInfo.RemoveAt(indexToRemove);
            //Remove photo file
            //System.IO.File.Delete(photoPaths[indexToRemove]);
            photoPaths.RemoveAt(indexToRemove);
        }




        _instance.OnScrapbookChange?.Invoke();
    }

    /// <summary>
    /// Crops the photo from the center while preserving image quality
    /// </summary>
    /// <param name="original">The original photo to be cropped</param>
    /// <param name="targetWidth">The target width in pixels to crop the photo to</param>
    /// <param name="targetHeight">The target height in pixels to crop the photo to</param>
    /// <returns>A Texture2D consisting of the cropped photo</returns>
    private Texture2D CropPhoto(Texture2D original, int targetWidth, int targetHeight)
    {
        int originalWidth = original.width;
        int originalHeight = original.height;
        float originalAspect = (float) originalWidth / originalHeight;
        float targetAspect = (float)targetWidth / targetHeight;
        int xOffset = 0;
        int yOffset = 0;
        float factor = 1;
        if(originalAspect > targetAspect) //Width Is Bigger, so it must be cropped
        {
            factor = (float) targetHeight / originalHeight;
            xOffset = (int)((originalWidth - originalHeight * targetAspect) / 2);
        }
        else //Height is bigger, so it must be croppped
        {
            factor = (float) targetWidth / originalWidth;
            yOffset = (int)((originalHeight - originalWidth * targetAspect) / 2);
        }
        Color32[] data = original.GetPixels32();
        Color32[] dataResult = new Color32[targetWidth * targetHeight];

        for(int y = 0; y < targetHeight; y++)
        {
            for(int x = 0; x < targetWidth; x++)
            {
                var p = new Vector2(Mathf.Clamp(xOffset + x / factor, 0, originalWidth - 1), Mathf.Clamp(yOffset + y / factor, 0, originalHeight - 1));
                var c11 = data[Mathf.FloorToInt(p.x) + originalWidth * (Mathf.FloorToInt(p.y))];
                var c12 = data[Mathf.FloorToInt(p.x) + originalWidth * (Mathf.CeilToInt(p.y))];
                var c21 = data[Mathf.CeilToInt(p.x) + originalWidth * (Mathf.FloorToInt(p.y))];
                var c22 = data[Mathf.CeilToInt(p.x) + originalWidth * (Mathf.CeilToInt(p.y))];
                var f = new Vector2(Mathf.Repeat(p.x, 1f), Mathf.Repeat(p.y, 1f));
                dataResult[x + y * targetWidth] = Color.Lerp(Color.Lerp(c11, c12, p.y), Color.Lerp(c21, c22, p.y), p.x);
            }
        }

        var textureResult = new Texture2D(targetWidth, targetHeight);
        textureResult.SetPixels32(dataResult);
        textureResult.Apply(true);
        return textureResult;
    }

    /// <summary>
    /// Updates the UI Menu to visually reflect the number of photos contained in the scrapbook
    /// </summary>
    private void UpdateUI()
    {
        //Adjust Page num if on Empty Page
        if (pageNum > 0 && photoInfo.Count <= pageNum * photosPerPage)
        {
            pageNum--;
        }

        //Decide Whether to display "No Photos" Text
        if(photoInfo.Count > 0 && noPhotosText.activeSelf == true)
        {
            noPhotosText.SetActive(false);
        }
        else if (photoInfo.Count <= 0 && noPhotosText.activeSelf == false)
        {
            noPhotosText.SetActive(true);
        }

        //Adjust next & previous button visibility
        prevPageButton.gameObject.SetActive(pageNum > 0);
        nextPageButton.gameObject.SetActive(photoInfo.Count > pageNum * photosPerPage + photosPerPage);

        //Update Photos based on Page
        for(int i = 1; i <= photosPerPage; i++)
        {
            int photoSlotIndex = (i - 1);
            int photoInfoIndex = i + (pageNum * photosPerPage) - 1;

            if (photoInfoIndex < photoInfo.Count)
            {
                photoSlots[photoSlotIndex].photoSlot.SetActive(true);
                photoSlots[photoSlotIndex].displayImage.sprite = photoInfo[photoInfoIndex].image;
                photoSlots[photoSlotIndex].label.text = photoInfo[photoInfoIndex].labelText;

                //Fix Listeners
                photoSlots[photoSlotIndex].examineButton.onClick.RemoveAllListeners();
                photoSlots[photoSlotIndex].deleteButton.onClick.RemoveAllListeners();

                //Add correct ones
                photoSlots[photoSlotIndex].examineButton.onClick.AddListener(() => { _instance.ExaminePhoto(photoInfo[photoInfoIndex]); });
                photoSlots[photoSlotIndex].deleteButton.onClick.AddListener(() => { _instance.RemovePhoto(photoInfo[photoInfoIndex]); });
            }
            else
            {
                photoSlots[photoSlotIndex].photoSlot.SetActive(false);
            }
        }
    }

    /// <summary>
    /// Load in the appropriate data for the photo library
    /// </summary>
    /// <param name="libraryData">The library data to load from</param>
    public void Load(Data.PhotoLibraryData libraryData)
    {

        _instance.photoCount = libraryData.photoCount;
        _instance.photoPaths.Clear();
        _instance.photoInfo.Clear();
        string imagePath;
        for (int i = 0; i < libraryData.photoImgPaths.Length; i++)
        {
            imagePath = libraryData.photoImgPaths[i];
            _instance.photoPaths.Add(imagePath);


            Texture2D photo;
            byte[] bytes = System.IO.File.ReadAllBytes(imagePath);
            photo = new Texture2D(160 * 2, 150 * 2);
            photo.LoadImage(bytes);
            Sprite newSprite = Sprite.Create(photo, new Rect(0, 0, photo.width, photo.height), Vector2.zero, 100f);
            newSprite.texture.Apply();
            AddToPhotoInfo(new PhotoInfo(newSprite, imagePath, libraryData.cluesFeatured[i]));
        }

        TriggerOnScrapbookChange();

    }

}
