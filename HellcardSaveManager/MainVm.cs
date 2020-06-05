using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
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
            {0x07, "Defiant Roar"},
            {0x08, "Rampage"},
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
            {0x1C, "Meteor"},
            {0x1D, "Initiative" },
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
        #region const + normal properties
        private const string _saveName = "demons.save";
        public string Title
        {
            get { return $"Hellcard Save Manager {Assembly.GetExecutingAssembly().GetName().Version}"; }
        }
        public SavedGame CurrentSave
        {
            get => _currentSave;
            set => SetProperty(ref _currentSave, ref value);
        }
        private SavedGame _currentSave;

        public DirectoryInfo BackupFolder { get; set; }

        public DirectoryInfo DemoDirInfo { get; set; }
        public Boolean IsWatching
        {
            get => _isWatching;
            set => SetProperty(ref _isWatching, ref value);
        }
        private Boolean _isWatching;
        public Boolean isSendMinidumps { get; set; }
        public string GameDir { get; set; }

        public ObservableCollection<SavedGame> Backups { get; } = new ObservableCollection<SavedGame>();
        #endregion

        public MainVm()
        {
            try
            {
                DemoDirInfo = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HELLCARD_Prealpha_demo"));

                BackupFolder = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HELLCARD_Backups"));

                using (var fs = new FileStream((Directory.GetDirectories(DemoDirInfo.FullName)[0] + @"\betalog.txt"), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs, Encoding.Default))
                {
                    string betalog = sr.ReadToEnd();
                    foreach (var line in betalog.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                        if (line.Contains("dir = "))
                        {
                            var logOutput = line.TrimEnd('.');
                            var gameDir = logOutput.Substring(logOutput.IndexOf('=') + 1).Trim();
                            GameDir = gameDir;
                            break;
                        }
                }


                IsWatching = false;

                var saveFileInfo = DemoDirInfo.EnumerateFiles(_saveName, SearchOption.AllDirectories).FirstOrDefault();

                if (saveFileInfo?.Exists != true)
                {
                    var ccg = DemoDirInfo.EnumerateDirectories("game_bod_ccg", SearchOption.AllDirectories).FirstOrDefault();
                    saveFileInfo = new FileInfo(Path.Combine(ccg.FullName, "slot_0", "demons.save"));
                    saveFileInfo.Create().Dispose();
                }


                CurrentSave = LoadSavedGame(saveFileInfo);

                BackupFolder.Create();

                foreach (var fileInfo in BackupFolder.EnumerateFiles("*.save"))
                {
                    Backups.Add(LoadSavedGame(fileInfo));
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Oh no! Something went very wrong!\n"
                    + "You probably haven't installed and/or started BoD-Hellcard yet. Go play a bit! :-)\n\n"
                    + "Otherwise, seek help in the Hellcard Discord Channel and mention following error:\n\n"
                    + ex.GetType().ToString() + ": " + ex.Message + "\n\n\n"
                    + "(This tool was not created by nor is supported by Thing Trunk, it's a community project.)", "Error in Startup");
                Application.Current.Shutdown();
            }
        }

        #region Initialization + reload infos

        public ICommand ReloadCommand => new DelegateCommand(Reload);
        private void Reload()
        {
            CurrentSave = LoadSavedGame(CurrentSave.Location);
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

            character.MaxHp = reader.ReadInt32();
            character.CurrentHp = reader.ReadInt32();

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

        #endregion


        #region Process find, start and event catch

        public ICommand WatchCommand => new DelegateCommand(WatchHellcard);
        private void WatchHellcard()
        {
            var hellcardProcess = Process.GetProcessesByName("HELLCARD_Demo").FirstOrDefault();

            if (hellcardProcess == null)
            {
                Process.Start(GameDir + "HELLCARD_Demo.exe");
                System.Threading.Thread.Sleep(5000);
                hellcardProcess = Process.GetProcessesByName("HELLCARD_Demo").FirstOrDefault();
            }

            if (hellcardProcess == null)
                return;

            EnableREvents(hellcardProcess);
        }

        private void EnableREvents(Process proc)
        {
            proc.EnableRaisingEvents = true;
            proc.Exited += ProcessEnded;
            IsWatching = true;
        }

        private void ProcessEnded(object sender, EventArgs e)
        {
            var proc = sender as Process;
            if (proc != null)
            {
                var rMessage = proc.ExitCode;
                IsWatching = false;
                if (rMessage != 0)
                {
                    isSendMinidumps = true;
                }
            }
        }

        #endregion


        #region Backup handling

        public ICommand DeleteMainSaveCommand => new DelegateCommand(DeleteMainSave, SaveButtons_CanExecute);
        private void DeleteMainSave()
        {
            if (MessageBox.Show("Are you sure that you want to delete your current savegame?", "Delete Save", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                CurrentSave.Location.Delete();
                CurrentSave.Location.Create().Dispose();
                CurrentSave = LoadSavedGame(CurrentSave.Location);
            }

        }


        public ICommand CreateBackupCommand => new DelegateCommand(CreateBackup, SaveButtons_CanExecute);
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

        private bool SaveButtons_CanExecute()
        {
            CurrentSave.Location.Refresh();
            return CurrentSave.Location.Length > 0;
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

        #endregion


        #region Change character name, open log folder, send email

        private byte[] WriteName(byte[] binary, string newName, Character character)
        {
            using (var reader = new BinaryReader(new MemoryStream(binary)))
            {
                using (MemoryStream writeStream = new MemoryStream())
                {
                    using (var writer = new BinaryWriter(writeStream))
                    {
                        writer.Write(reader.ReadBytes(9));
                        for (var i = character.Position - 1; i > 0; i--)
                        { 
                            WriteCharacter(reader, writer);
                        }
                        WriteCharAndName(reader, writer, newName, character.Name);
                        character.Name = newName;
                        return writeStream.GetBuffer().Take((int)writeStream.Length).ToArray();

                    }
                }
            }
        }

        private void WriteCharacter(BinaryReader reader, BinaryWriter writer)
        {
            writer.Write(reader.ReadChars(3)); //Read Class Name
            var nameLen = reader.ReadInt32();
            writer.Write(nameLen);
            writer.Write(reader.ReadChars(nameLen));
            writer.Write(reader.ReadBytes(32)); //all stuff between name and cardCount
            var cardCount = reader.ReadInt32();
            writer.Write(cardCount);
            writer.Write(reader.ReadBytes(cardCount * 4));
            writer.Write(reader.ReadInt32());
        }

        private void WriteCharAndName(BinaryReader reader, BinaryWriter writer, string newName, string oldName)
        {
            writer.Write(reader.ReadChars(3)); // class name
            writer.Write(newName.Length);
            writer.Write(Encoding.ASCII.GetBytes(newName));
            reader.ReadInt32();
            reader.ReadBytes(oldName.Length);
            while (reader.BaseStream.Position != reader.BaseStream.Length)
            {
                writer.Write(reader.ReadByte());
            }
        }

        public ICommand ChangeNamesCommand => new DelegateCommand(ChangeNames, SaveButtons_CanExecute);
        private void ChangeNames()
        {
            var binary = File.ReadAllBytes(CurrentSave.Location.FullName);
            var nameBox = new ChangeNameBox();
            nameBox.mageBox.Text = CurrentSave.Mage.Name;
            nameBox.warriorBox.Text = CurrentSave.Warrior.Name;
            nameBox.rougeBox.Text = CurrentSave.Rogue.Name;
            if (nameBox.ShowDialog() == true)
            {
                if (nameBox.rougeBox.Text != CurrentSave.Rogue.Name)
                {
                    binary = WriteName(binary, nameBox.rougeBox.Text, CurrentSave.Rogue);
                }

                if (nameBox.mageBox.Text != CurrentSave.Mage.Name)
                {
                    binary = WriteName(binary, nameBox.mageBox.Text, CurrentSave.Mage);
                }

                if (nameBox.warriorBox.Text != CurrentSave.Warrior.Name)
                {
                    binary = WriteName(binary, nameBox.warriorBox.Text, CurrentSave.Warrior);
                }
                File.WriteAllBytes(CurrentSave.Location.FullName, binary);
                CurrentSave = LoadSavedGame(CurrentSave.Location);
            }


        }

        public ICommand OpenLogFolderCommand => new DelegateCommand(OpenLogFolder);
        private void OpenLogFolder()
        {
            if (File.Exists(@BackupFolder.FullName + @"\nomsg.txt") == false)
            {
                var msgBox = new MessageCheckBox();
                msgBox.Owner = Application.Current.MainWindow;
                msgBox.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                if (msgBox.ShowDialog() == true)
                {
                    if (msgBox.dontShow.IsChecked == true)
                    {
                        using (File.Create(BackupFolder.FullName + @"\nomsg.txt")) { }
                    }
                }
            }
            Process.Start(Directory.GetDirectories(DemoDirInfo.FullName)[0]);
        }

        public ICommand SendLogsSmtpCommand => new DelegateCommand(SendLogsSmtp);
        private void SendLogsSmtp()
        {
            var winSendMail = new SendLog(Directory.GetDirectories(DemoDirInfo.FullName)[0], isSendMinidumps, GameDir);
            if (winSendMail.ShowDialog() == true)
            {
                isSendMinidumps = false;
            }
        }

        #endregion



    }
}

