/* Name: SaveSystem.cs
 * Primary Author: Zackary Seiple
 * Description: Handles the Saving and Loading of game states, as well as functionality for starting new games and deleting saves
 * Last Updated: 5/6/2020 (Zackary Seiple)
 * Changes: Added Header
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine.SceneManagement;

public static class SaveSystem
{
    public const int MAX_SAVE_SLOTS = 3;
    [Range(1,MAX_SAVE_SLOTS)]
    public static int currentSaveSlot = 1;
    public static bool existingSaveData = false;
    public static bool enterTVOnThisLoad = false;
    public static bool loading = false;
    public static Data.GameData gameData;
    public static Data.SaveData saveData;

    public delegate void SaveSlotUpdate(int saveSlot, string date, float playTime);
    public static event SaveSlotUpdate OnUpdatedSaveStats;

    public delegate void SaveSlotDelete(int saveSlot);
    public static event SaveSlotDelete OnDeleteSave;

    private static string saveDataPath = Path.Combine(Application.persistentDataPath, "_GameData");
    private static string fileExtension = ".david";

    private static BinaryFormatter formatter;

    public static string GetSavePath(int saveSlot)
    {
        return Path.Combine(saveDataPath, string.Format("save{0}", saveSlot) + fileExtension);
    }

    public static void Save(int saveSlot)
    {
        loading = true;
        formatter = new BinaryFormatter();
        Data.GameData newGameData;
        //Game Save
        using (var stream = new FileStream(Path.Combine(saveDataPath, string.Format("save{0}", saveSlot) + fileExtension), FileMode.Create))
        {
            Player player = (Player) GameObject.FindObjectOfType(typeof(Player));
            Player[] players = (Player[]) GameObject.FindObjectsOfType(typeof(Player));
            if(players.Length > 1)
            {
                foreach(Player x in players)
                {
                    if (!x.name.Contains("Player"))
                        player = x;
                }
            }
            else
            {
                foreach (Player x in players)
                {
                    if (x.name.Contains("Player"))
                        player = x;
                }

            }


            newGameData = new Data.GameData(player, (MainMenu)GameObject.FindObjectOfType(typeof(MainMenu)), stream);
            formatter.Serialize(stream, newGameData);
        }

        //Save SaveInfo
        using (var stream = new FileStream(Path.Combine(saveDataPath, "saveInfo" + fileExtension), FileMode.Create))
        {
            Data.SaveData saveData = new Data.SaveData();
            formatter.Serialize(stream, saveData);
        }


        //Update Save Slot to reflect save info
        OnUpdatedSaveStats?.Invoke(saveSlot, newGameData.gameStats.date, newGameData.gameStats.playTime);

        loading = false;
        Log.AddEntry("Save Completed");
    }

    /// <summary>
    /// Loads up data from a save slot
    /// </summary>
    /// <param name="saveSlot">The save slot to load from</param>
    /// <param name="enterTVOnLoad">if True, will force the player directly into the tv on method call</param>
    public static void Load(int saveSlot, bool enterTVOnLoad=false)
    {
        enterTVOnThisLoad = enterTVOnLoad;
        loading = true;
        Debug.Log("Starting to load");
        string path;

        //Create Save Directory if non-existant
        if (!Directory.Exists(saveDataPath))
        {
            Directory.CreateDirectory(saveDataPath);
        }

        //Create universal saveInfo file for game-state independent save stuff
        if (File.Exists(path = Path.Combine(saveDataPath, "saveInfo" + fileExtension)))
        {
            Data.SaveData data;
            using (var stream = new FileStream(path, FileMode.Open))
            {
                if (stream.Length != 0)
                {
                    formatter = new BinaryFormatter();
                    data = formatter.Deserialize(stream) as Data.SaveData;
                    //Load General Save information
                    SaveSystem.existingSaveData = data.existingSave;
                    if (GameController.initialLoad)
                        SaveSystem.currentSaveSlot = data.mostRecentSaveSlot;
                }
            }
        }
        else
        {
            File.Create(Path.Combine(saveDataPath, "saveInfo" + fileExtension)).Dispose();
        }

        if (File.Exists(path = Path.Combine(saveDataPath, string.Format("save{0}", saveSlot) + fileExtension)))
        {
            Debug.Log("STARTING LOAD");
            //Decode Game Data
            using (var stream = new FileStream(path, FileMode.Open))
            {
                if (stream.Length != 0)
                {
                    formatter = new BinaryFormatter();
                    gameData = formatter.Deserialize(stream) as Data.GameData;
                }
            }
            
        }
        else
        {
            gameData = null;
            
            Debug.Log("Save Data for Slot " + saveSlot + " Not Found.");

        }

        currentSaveSlot = saveSlot;
        SceneManager.LoadScene(0, LoadSceneMode.Single);

        //Load Game Stats for Save Slots
        for (int i = 1; i <= MAX_SAVE_SLOTS; i++)
        {
            int temp = i;
            if (File.Exists(path = Path.Combine(saveDataPath, string.Format("save{0}", temp) + fileExtension)))
            {
                formatter = new BinaryFormatter();
                Data.GameData data;
                using (var stream = new FileStream(path, FileMode.Open))
                {
                    if (stream.Length != 0)
                    {
                        data = formatter.Deserialize(stream) as Data.GameData;
                        OnUpdatedSaveStats?.Invoke(temp, data.gameStats.date, data.gameStats.playTime);
                        //GameController.UpdateSaveSlotInfo(temp, data.gameStats.date, data.gameStats.playTime);
                    }
                }

            }
        }

        loading = false;
    }

    /// <summary>
    /// Deletes save data in a given save slot
    /// </summary>
    /// <param name="saveSlot">The save slot to delete</param>
    public static void DeleteSave(int saveSlot)
    {
        string path;
        if (File.Exists(path = Path.Combine(saveDataPath, string.Format("save{0}", saveSlot) + fileExtension)))
        {
            formatter = new BinaryFormatter();
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite))
            {
                Data.PhotoLibraryData data = (formatter.Deserialize(stream) as Data.GameData).libraryData;
                foreach(string imgPath in data.photoImgPaths)
                {
                    File.Delete(imgPath);
                }
            }

            File.Delete(path);
            OnDeleteSave?.Invoke(saveSlot);
        }
        else
        {
            Debug.Log("Can't Delete Save Slot, No Save Data for Slot " + saveSlot + " Found.");
        }
    }

    public static void NewGame(int saveSlot)
    {
        currentSaveSlot = saveSlot;
        if(SaveExists(saveSlot))
            DeleteSave(currentSaveSlot);
        Load(saveSlot);
    }

    /// <summary>
    /// Tests a save slot to see if it has data in it
    /// </summary>
    /// <param name="saveSlot">The saveslot to test</param>
    /// <returns>True if exists, false if not</returns>
    public static bool SaveExists(int saveSlot)
    {
        return File.Exists(Path.Combine(saveDataPath, string.Format("save{0}", saveSlot) + fileExtension));
    }

    /// <summary>
    /// Sees if there's any existing save data
    /// </summary>
    /// <returns>True if save data exists, false if not</returns>
    public static bool AnySaveExists()
    {
        for(int i = 1; i <= MAX_SAVE_SLOTS; i++)
        {
            int temp = i;
            if(SaveExists(temp))
            {
                return true;
            }
        }

        return false;
    }
}
