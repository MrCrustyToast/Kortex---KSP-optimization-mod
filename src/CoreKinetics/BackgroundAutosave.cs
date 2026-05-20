using UnityEngine;
using HarmonyLib;
using System;
using System.Threading.Tasks;

namespace CoreKinetics
{
    [HarmonyPatch(typeof(GamePersistence), "SaveGame", new Type[] { typeof(string), typeof(string), typeof(string), typeof(SaveMode) })]
    public static class BackgroundAutosavePatch
    {
        private static bool isSavingActive = false;

        public static bool Prefix(string saveName, string saveDir, string screenshotDir, SaveMode saveMode)
        {
            if (string.IsNullOrEmpty(saveName) || string.IsNullOrEmpty(saveDir)) return true;
            
            //anti-spam
            if (isSavingActive)
            {
                ScreenMessages.PostScreenMessage("Kortex : Une sauvegarde est déjà en cours...", 2f, ScreenMessageStyle.UPPER_CENTER);
                return false; 
            }

            try
            {
                isSavingActive = true;

                Game snapshot = HighLogic.CurrentGame;
                GamePersistence.SaveGame(HighLogic.CurrentGame, "persistent", HighLogic.SaveFolder, SaveMode.OVERWRITE);
                if (snapshot == null)
                {
                    isSavingActive = false;
                    return true;
                }

                ConfigNode rootNode = new ConfigNode();
                snapshot.Save(rootNode);

                if (GameEvents.onGameStateSave != null) 
                {
                    GameEvents.onGameStateSave.Fire(rootNode);
                }

                string path = string.Format("{0}saves/{1}/{2}.sfs", KSPUtil.ApplicationRootPath, saveDir, saveName);

                ScreenMessages.PostScreenMessage("Kortex : Sauvegarde asynchrone...", 2f, ScreenMessageStyle.UPPER_CENTER);

                Task.Run(() =>
                {
                    try 
                    { 
                        rootNode.Save(path); 
                        //maint thread flag and notif
                        MainThreadDispatcher.RunOnMainThread(() => {
                            isSavingActive = false;
                        });
                    }
                    catch (Exception e)
                    {
                        //IF fails log without crashing KSP
                        MainThreadDispatcher.RunOnMainThread(() => {
                            Debug.LogError($"[Kortex] Échec de l'écriture asynchrone : {e.Message}");
                            ScreenMessages.PostScreenMessage("Erreur : Sauvegarde Kortex échouée.", 4f, ScreenMessageStyle.UPPER_CENTER);
                            isSavingActive = false;
                        });
                    }
                });

                return false; //Cut the native saves from KSP
            }
            catch 
            { 
                isSavingActive = false;
                return true; //If bugs too much let KSP manage it
            }
        }
    }
}
