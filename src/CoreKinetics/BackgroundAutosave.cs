using System;
using System.IO;
using UnityEngine;

namespace Kortex.CoreKinetics
{

    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class AutosaveWatchdog : MonoBehaviour
    {
        private float _lastBackupTime = 0f;
        private const float BackupInterval = 300f;
        private string _backupFolder;

        private void Awake()
        {
            _backupFolder = Path.Combine(KSPUtil.ApplicationRootPath, "GameData", "Kortex", "Backups");
            if (!Directory.Exists(_backupFolder))
            {
                Directory.CreateDirectory(_backupFolder);
            }
            Debug.Log("[Kortex:CoreKinetics] AutosaveWatchdog initialisé avec succès.");
        }

        private void Update()
        {
            float currentTime = Time.realtimeSinceStartup;

            if (currentTime - _lastBackupTime >= BackupInterval)
            {
                TriggerBackup();
                _lastBackupTime = currentTime;
            }
        }

        private void TriggerBackup()
        {
            try
            {
                string saveFolder = Path.Combine(KSPUtil.ApplicationRootPath, "saves", HighLogic.SaveFolder);
                string persistentSfs = Path.Combine(saveFolder, "persistent.sfs");

                if (File.Exists(persistentSfs))
                {
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    string destFile = Path.Combine(_backupFolder, $"{HighLogic.SaveFolder}_backup_{timestamp}.sfs");
                    
                    
                    System.Threading.Tasks.Task.Run(() =>
                    {
                        Debug.Log("[Kortex:Autosave] Launching asynchronous background save thread...");
                        try
                        {
                            File.Copy(persistentSfs, destFile, true);
                            Debug.Log("[Kortex:Autosave] Background save completed successfully without blocking main thread.");
                            Debug.Log($"[Kortex:CoreKinetics] Historique de sauvegarde créé : {Path.GetFileName(destFile)}");
                            BackupRotation.EnforceRotation();
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[Kortex:Autosave] CRITICAL ERROR: Async file write failed: {ex.Message}");
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Kortex:CoreKinetics] Erreur lors de la création de la sauvegarde historique : {ex.Message}");
            }
        }
    }
}