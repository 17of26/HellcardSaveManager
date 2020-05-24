using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;

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
        public List<int> Cards { get; set; }

        public string CardString
        {
            get
            {
                var cardSort = new Dictionary<int, int>(); // CardID, Count

                var cardList = "";

                foreach (var card in Cards)
                {
                    if (cardSort.ContainsKey(card))
                    {
                        cardSort[card]++;
                    }
                    else
                    {
                        cardSort[card] = 1;
                    }
                }

                foreach (var pair in cardSort)
                {
                    cardList += $"{pair.Value} {_cardMapping[pair.Key]}, ";
                }

                return cardList.Trim().Trim(',');
            }
        }

        public override string ToString()
        {
            return $"{Name}:  Floor {Floor}, {CurrentHp}/{MaxHp}";
        }

        private readonly Dictionary<int, string> _cardMapping = new Dictionary<int, string>
        {
            {0x00, "Block"},
            {0x01, "Strike"},
            {0x02, "Mighty Blow"},
            {0x03, "Caltrops"},
            {0x04, "Cluster"},
            {0x05, "Tactics"},
            {0x06, "Whirlwind"},
            {0x07, "Sinister Gaze"},
            {0x08, "Wild Strike "},
            {0x09, "Barricade"},
            {0x0A, "Sacrifice"},
            {0x0B, "Arrow"},
            {0x0C, "Quiver"},
            {0x0D, "Finesse"},
            {0x0E, "Arrow Rain"},
            {0x0F, "Mastery"},
            {0x10, "Fortify"},
            {0x11, "Cover"},
            {0x12, "Knockback"},
            {0x13, "Luck"},
            {0x14, "Missile"},
            {0x15, "Lightning"},
            {0x16, "Meditation"},
            {0x17, "Armageddon"},
            {0x18, "Dark Pact"},
            {0x19, "Teleport"},
            {0x1A, "Healing Aura"},
            {0x1B, "Link"},
            {0x1C, "Meteor"}
        };

        public int Position { get; set; }

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
            DemoDirInfo = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HELLCARD_Prealpha_demo"));

            BackupFolder = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HELLCARD_Backups"));

            var saveFileInfo = DemoDirInfo.EnumerateFiles(_saveName, SearchOption.AllDirectories).FirstOrDefault();

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

                    ReadCharacter(reader, savedGame, 1);
                    ReadCharacter(reader, savedGame, 2);
                    ReadCharacter(reader, savedGame, 3);
                }
            }
            catch (Exception)
            {
                // Can't really do much if it fails
            }

            return savedGame;
        }

        private void ReadCharacter(BinaryReader reader, SavedGame savedGame, int position)
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

            character.Cards = new List<int>();

            for (var i = 0; i < character.CardCount; ++i)
                character.Cards.Add(reader.ReadInt32());

            reader.ReadInt32(); // unknown

            switch (character.Class.ToLower())
            {
                case "mag":
                    savedGame.Mage = character;
                    savedGame.Mage.Position = position;
                    break;
                case "war":
                    savedGame.Warrior = character;
                    savedGame.Warrior.Position = position;
                    break;
                case "rog":
                    savedGame.Rogue = character;
                    savedGame.Rogue.Position = position;
                    break;
            }
        }

        public ICommand ChangeNamesCommand => new DelegateCommand(ChangeNames);
        public void ChangeNames()
        {
            var binary = File.ReadAllBytes(CurrentSave.Location.FullName);
            Trace.WriteLine(binary[0x0C]);


        }

        public ICommand SendLogsCommand => new DelegateCommand(SendLogs);

        private void SendLogs()
        {
            if (File.Exists(@BackupFolder.FullName + @"\nomsg.txt") == false) {
                var msgBox = new MessageCheckBox();
                msgBox.Owner = Application.Current.MainWindow;
                msgBox.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                if (msgBox.ShowDialog() == true)
                {
                    if (msgBox.dontShow.IsChecked == true)
                    {
                        using (File.Create(@BackupFolder.FullName + @"\nomsg.txt")) { }
                    }
                }
            }
            Process.Start(@Directory.GetDirectories(DemoDirInfo.FullName)[0]);

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

        public DirectoryInfo DemoDirInfo { get; set; }


        public ObservableCollection<SavedGame> Backups { get; } = new ObservableCollection<SavedGame>();
    }
}

