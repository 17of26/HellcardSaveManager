using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using System.Windows;

namespace HellcardSaveManager
{
    internal class Character
    {
        public string Name { get; set; } = "";
        public string Class { get; set; } = "";
        public int Floor { get; set; }
        public int CurrentHp { get; set; }
        public int MaxHp { get; set; }
        public int CardCount { get; set; }
        public override string ToString()
        {
            return $"{Name}:  Floor {Floor}, {CurrentHp}/{MaxHp}";
        }
    }

    internal class SavedGame
    {
        public FileInfo Location { get; set; }
        public Character Mage { get; set; }
        public Character Warrior { get; set; }
        public Character Rogue { get; set; }
    }

    internal class MainVm : ObservableObject
    {
        private const string _saveName = "demons.save";

        public MainVm()
        {
            demoDirInfo = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HELLCARD_Prealpha_demo"));

            BackupFolder = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HELLCARD_Backups"));

            var saveFileInfo = demoDirInfo.EnumerateFiles(_saveName, SearchOption.AllDirectories).FirstOrDefault();

            Trace.WriteLine(saveFileInfo?.Exists);

            if (saveFileInfo?.Exists != true) 
                return;

            CurrentSave = LoadSavedGame(saveFileInfo);

            System.IO.Directory.CreateDirectory(BackupFolder.FullName);

            foreach (var fileInfo in BackupFolder.EnumerateFiles())
            {
                Backups.Add(LoadSavedGame(fileInfo));
            }

        }

        private SavedGame LoadSavedGame(FileInfo fileInfo)
        {
            var savedGame = new SavedGame { Location = fileInfo };

            try
            {
                using (var reader = new BinaryReader(File.Open(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    reader.ReadBytes(9);

                    ReadCharacter(reader, savedGame);
                    ReadCharacter(reader, savedGame);
                    ReadCharacter(reader, savedGame);
                }
            }
            catch (Exception)
            {
                // Can't really do much if it fails
            }

            return savedGame;
        }

        private void ReadCharacter(BinaryReader reader, SavedGame savedGame)
        {
            var character = new Character();

            for (var i = 0; i < 3; ++i)
                character.Class += reader.ReadChar();

            var nameLen = reader.ReadInt32();

            for (var i = 0; i < nameLen; ++i)
                character.Name += reader.ReadChar();

            character.Floor = reader.ReadInt32();

            character.CurrentHp = reader.ReadInt32();
            character.MaxHp = reader.ReadInt32();

            var gold = reader.ReadInt32();
            
            reader.ReadInt32(); // unknown
            reader.ReadInt32(); // unknown
            reader.ReadInt32(); // unknown
            reader.ReadInt32(); // unknown

            character.CardCount = reader.ReadInt32();

            var cards = new List<int>();

            for (var i = 0; i < character.CardCount; ++i)
                cards.Add(reader.ReadInt32());

            reader.ReadInt32(); // unknown

            switch (character.Class.ToLower())
            {
                case "mag":
                    savedGame.Mage = character;
                    break;
                case "war":
                    savedGame.Warrior = character;
                    break;
                case "rog":
                    savedGame.Rogue = character;
                    break;
            }
        }

        public ICommand SendLogsCommand => new DelegateCommand(SendLogs);

        private void SendLogs()
        {
            MessageBox.Show("Please send the HELLCARD_Demo_lox.txt at support@thingtrunk.com.\nIf you press OK the right folder will open and you just have to copy-paste the file.", "Send Logs");
            Process.Start(@Directory.GetDirectories(demoDirInfo.FullName)[0]);
        }

        public ICommand DeleteMainSaveCommand => new DelegateCommand(DeleteMainSave);

        private void DeleteMainSave()
        {
            if (MessageBox.Show("Are you sure that you want to delete your current savegame?", "Delete Save", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                CurrentSave.Location.Delete();
                MessageBox.Show("Deletion Successfull");
            }

        }

        public ICommand CreateBackupCommand => new DelegateCommand(CreateBackup);

        private void CreateBackup()
        {
            BackupFolder.Create();

            var i = 1;
            string newFile;

            do
            {
                newFile = Path.Combine(BackupFolder.FullName, $"{i++}_{_saveName}");
            } while (File.Exists(newFile));

            CurrentSave.Location.CopyTo(newFile);

            Backups.Insert(0, LoadSavedGame(new FileInfo(newFile)));
        }

        public ICommand RestoreCommand => new DelegateCommand<SavedGame>(Restore);

        private void Restore(SavedGame game)
        {
            game.Location.CopyTo(CurrentSave.Location.FullName, true);

            CurrentSave = LoadSavedGame(CurrentSave.Location);
        }

        public ICommand DeleteCommand => new DelegateCommand<SavedGame>(Delete);

        private void Delete(SavedGame game)
        {
            Backups.Remove(game);

            game.Location.Delete();
        }


        public SavedGame CurrentSave
        {
            get => _currentSave;
            set => SetProperty(ref _currentSave, ref value);
        }
        private SavedGame _currentSave;

        public DirectoryInfo BackupFolder { get; set; }

        public DirectoryInfo demoDirInfo { get; set; }


        public ObservableCollection<SavedGame> Backups { get; } = new ObservableCollection<SavedGame>();
    }
}

