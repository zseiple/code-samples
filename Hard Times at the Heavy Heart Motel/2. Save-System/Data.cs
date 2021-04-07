/* Name: Data.cs
 * Primary Author: Zackary Seiple
 * Description: Defines the serializable data structures necessary to record key information about the game to be saved and loaded
 * Last Updated: 5/6/2020 (Zackary Seiple)
 * Changes: Added Header
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public static class Data
{
    //Save Settings, Independent of Game
    [System.Serializable]
    public class SaveData
    {
        public bool existingSave;
        public int mostRecentSaveSlot;

        public SaveData()
        {
            existingSave = SaveSystem.AnySaveExists();
            mostRecentSaveSlot = SaveSystem.currentSaveSlot;
        }
    }

    //Game State Data
    /// <summary>
    /// The Combination of all the different instances of Game State Data
    /// </summary>
    [System.Serializable]
    public class GameData
    {
        public Stats gameStats;
        public PlayerData playerData;
        public MainMenuData mainMenuData;
        public PhotoLibraryData libraryData;
        public TutorialData tutorialData;
        public RatTrapData trapData;
        public DoorData doorData;

        public GameData(Player player, MainMenu menu, FileStream stream)
        {
            gameStats = new Stats();
            playerData = new PlayerData(player);
            mainMenuData = new MainMenuData(menu);
            libraryData = new PhotoLibraryData(stream);
            tutorialData = new TutorialData();
            trapData = new RatTrapData();
            doorData = new DoorData();

        }
    }

    //GAME STATE DATA CLASSES
    [System.Serializable]
    public class Stats
    {
        public float playTime;
        public string date;

        public Stats()
        {
            date = System.DateTime.Now.ToShortDateString();
            playTime = GameController._instance.playTime + Time.time - GameController._instance.lastSaveTime;
            GameController._instance.lastSaveTime = Time.time;
        }
    }

    [System.Serializable]
    public class PlayerData
    {
        //Name of what player is in when saving
        public string playerName;

        public PlayerData(Player player)
        {
            playerName = player.name;
        }
    }

    [System.Serializable]
    public class MainMenuData
    {
        public float[] currentTV_pos;

        public MainMenuData(MainMenu menu)
        {
            //Position of TV
            currentTV_pos = new float[3];
            Television tempTV = menu.TVs[MainMenu.GetLastTVIndex()];
            currentTV_pos[0] = tempTV.transform.position.x;
            currentTV_pos[1] = tempTV.transform.position.y;
            currentTV_pos[2] = tempTV.transform.position.z;
        }
    }

    [System.Serializable]
    public class PhotoLibraryData
    {
        public string[] photoImgPaths;
        public int[][] cluesFeatured;
        public string[] labelTexts;
        public string[] detailTexts;
        public uint photoCount;

        public PhotoLibraryData(FileStream stream)
        {

            List<PhotoLibrary.PhotoInfo> photoInfo = PhotoLibrary.GetPhotoInfo();
            photoImgPaths = PhotoLibrary._instance.photoPaths.ToArray();
            photoCount = PhotoLibrary._instance.photoCount;
            //Get image paths, make sure that photos no longer in player's scrapbook are deleted on save
            if (SaveSystem.SaveExists(SaveSystem.currentSaveSlot) && stream.Length != 0)
            {
                List<string> savedPaths;
                BinaryFormatter formatter = new BinaryFormatter();
                PhotoLibraryData data = (formatter.Deserialize(stream) as GameData).libraryData;
                savedPaths = new List<string>(data.photoImgPaths);

                //For each path in the running game state
                for(int i = 0; i < photoInfo.Count; i++)
                {
                    //If a path in the game is not in the save, new photo has been added (Save path to state save)
                    if (!savedPaths.Contains(photoInfo[i].image_path))
                    {
                        savedPaths.Add(photoInfo[i].image_path);
                    }
                }

                int[] indexesToRemove = new int[savedPaths.Count];
                for(int i = 0; i < savedPaths.Count; i++)
                {
                    //If a path in the save contains a path that is no longer in the game state (Photo Deleted in game, delete at path)
                    bool found = false;
                    for(int j = 0; j < photoInfo.Count; j++)
                    {
                        if(savedPaths[i] == photoInfo[j].image_path)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        File.Delete(savedPaths[i]);
                        //Mark this index to be deleted after for loop ends
                        indexesToRemove[i] = 1;
                    }
                }

                //Remove indexes in save data no longer needed
                for(int i = 0; i < indexesToRemove.Length; i++)
                {
                    if (indexesToRemove[i] == 1)
                        savedPaths.RemoveAt(i);
                }

                photoImgPaths = savedPaths.ToArray();
            }

            

            //Set Clues Featured,Label Texts, and detail texts for photos
            cluesFeatured = new int[photoInfo.Count][];
            for(int i = 0; i < photoInfo.Count; i++)
            {
                cluesFeatured[i] = new int[photoInfo[i].cluesFeatured.Length];

                //Set Clues Featured
                for (int j = 0; j < cluesFeatured[i].Length; j++)
                {
                    cluesFeatured[i][j] = photoInfo[i].cluesFeatured[j];
                }

            }

        }
    }

    [System.Serializable]
    public class TutorialData
    {
        public bool tutorialCompleted;

        public TutorialData()
        {
            Debug.Log("TutorialData: Saving completed as: " + Tutorial.instance.isCompleted + " | To save slot: " + SaveSystem.currentSaveSlot);
            this.tutorialCompleted = Tutorial.instance.isCompleted;
        }
    }

    [System.Serializable]
    public class RatTrapData
    {
        public float[] trapIDs;
        public bool[] active;

        public RatTrapData()
        {
            int numTraps = GameController._instance.ratTraps.Length;
            RatTrap currentTrap;

            trapIDs = new float[numTraps];
            active = new bool[numTraps];

            for(int i = 0; i < numTraps; i++)
            {
                currentTrap = GameController._instance.ratTraps[i];

                trapIDs[i] = currentTrap.GetID();
                active[i] = currentTrap.isActive;
            }
        }
    }

    [System.Serializable]
    public class DoorData
    {
        public float[] doorIDs;
        public bool[] locked;

        public DoorData()
        {
            int numDoors = GameController._instance.doors.Length;
            DoorScript currentDoor;

            doorIDs = new float[numDoors];
            locked = new bool[numDoors];

            for(int i = 0; i < numDoors; i++)
            {
                currentDoor = GameController._instance.doors[i];

                doorIDs[i] = currentDoor.GetID();
                locked[i] = currentDoor.isLocked;
            }


        }
    }
}
