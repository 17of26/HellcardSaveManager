using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
        public int MaxHp { get; set; }
        public int CurrentHp { get; set; }
        public int Gold { get; set; }
        public int Slots { get; set; }
        public int Level { get; set; }
        public int CardCount { get; set; }
        public List<int> Cards { get; set; } = new List<int>();

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

    internal class Companion : Character
    {
        public int Id { get; set; }
        public int Type { get; set; }
        public int MaxMana { get; set; }
        public int Mana { get; set; }
        public List<int> FutureCards { get; set; } = new List<int>();
    }

    internal class SPCharacter : Character
    {
        public int CompanionCount { get; set; }
        public List<Companion> Companions { get; set; } = new List<Companion>();
    }

    internal class MPCharacter : Character
    {

    }

    internal class SPSavedGame
    {
        public FileInfo Location { get; set; }
        public SPCharacter Mage { get; set; }
        public SPCharacter Warrior { get; set; }
        public SPCharacter Rogue { get; set; }

    }

    internal class MPSavedGame
    {
        public FileInfo Location { get; set; }
        public MPCharacter Mage { get; set; }
        public MPCharacter Warrior { get; set; }
        public MPCharacter Rogue { get; set; }

    }

    internal class MainVm : ObservableObject
    {
        #region const + normal properties
        private const string _saveName = "demons.save";
        public string Title
        {
            get { return $"Hellcard Save Manager {Assembly.GetExecutingAssembly().GetName().Version}"; }
        }
        public SPSavedGame CurrentSPSave
        {
            get => _currentSPSave;
            set => SetProperty(ref _currentSPSave, ref value);
        }
        private SPSavedGame _currentSPSave;
        public MPSavedGame CurrentMPSave
        {
            get => _currentMPSave;
            set => SetProperty(ref _currentMPSave, ref value);
        }
        private MPSavedGame _currentMPSave;

        public DirectoryInfo BackupFolder { get; set; }

        public DirectoryInfo MPDirInfo { get; set; }
        public DirectoryInfo SPDirInfo { get; set; }
        public int SelectedInxedSPMP { get; set; }
        public Boolean IsWatching
        {
            get => _isWatching;
            set => SetProperty(ref _isWatching, ref value);
        }
        private Boolean _isWatching;
        public Boolean IsSendMinidumps { get; set; }
        public string GameDir { get; set; }
        public int ExitCode
        {
            get =>_exitCode;
            set => SetProperty(ref _exitCode, ref value);
        }
        private int _exitCode;

        public ObservableCollection<SPSavedGame> BackupsSP { get; } = new ObservableCollection<SPSavedGame>();
        public ObservableCollection<MPSavedGame> BackupsMP { get; } = new ObservableCollection<MPSavedGame>();
        #endregion

        public MainVm()
        {
            try
            {
                MPDirInfo = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HELLCARD_Prealpha_demo"));
                SPDirInfo = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HELLCARD_Prealpha_demo_single"));

                BackupFolder = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HELLCARD_Backups"));


                SPDirInfo.Create();
                SPDirInfo.Create();


                var mpBetaLog = SPDirInfo.EnumerateFiles("betalog.txt", SearchOption.AllDirectories).FirstOrDefault();
                var spBetaLog = SPDirInfo.EnumerateFiles("betalog.txt", SearchOption.AllDirectories).FirstOrDefault();

                if (mpBetaLog.Exists)
                {
                    GameDir = GetGameDir(mpBetaLog.FullName);
                } else if (spBetaLog.Exists)
                {
                    GameDir = GetGameDir(spBetaLog.FullName);
                }

                
                IsWatching = false;
                ExitCode = int.MaxValue;

                var saveFileInfoMP = MPDirInfo.EnumerateFiles(_saveName, SearchOption.AllDirectories).FirstOrDefault();
                var saveFileInfoSP = SPDirInfo.EnumerateFiles(_saveName, SearchOption.AllDirectories).FirstOrDefault();

                saveFileInfoMP = checkSaveFileInfo(saveFileInfoMP, MPDirInfo);
                saveFileInfoSP = checkSaveFileInfo(saveFileInfoSP, SPDirInfo);

                CurrentMPSave = LoadMPSavedGame(saveFileInfoMP);
                CurrentSPSave = LoadSPSavedGame(saveFileInfoSP);

                BackupFolder.Create();

                foreach (var fileInfo in BackupFolder.EnumerateFiles("*.mp.save"))
                {
                    BackupsMP.Add(LoadMPSavedGame(fileInfo));
                }

                foreach (var fileInfo in BackupFolder.EnumerateFiles("*.sp.save"))
                {
                    BackupsSP.Add(LoadSPSavedGame(fileInfo));
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

        private string GetGameDir(string betaLogDir)
        {
            using (var fs = new FileStream((betaLogDir), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var sr = new StreamReader(fs, Encoding.Default))
            {
                string betalog = sr.ReadToEnd();
                foreach (var line in betalog.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
                    if (line.Contains("dir = "))
                    {
                        var logOutput = line.TrimEnd('.');
                        var gameDir = logOutput.Substring(logOutput.IndexOf('=') + 1).Trim();
                        return gameDir;
                    }
            }
            return null; //if this happpens . . .
        }

        private FileInfo checkSaveFileInfo(FileInfo saveFileInfo, DirectoryInfo dirInfo)
        {
            if (saveFileInfo?.Exists != true)
            {
                var ccg = dirInfo.EnumerateDirectories("game_bod_ccg", SearchOption.AllDirectories).FirstOrDefault();
                saveFileInfo = new FileInfo(Path.Combine(ccg.FullName, "slot_0", "demons.save"));
                saveFileInfo.Create().Dispose();
            }
            return saveFileInfo;
        }

        #region Initialization + reload infos

        public ICommand ReloadCommand => new DelegateCommand(Reload);
        private void Reload()
        {
            CurrentMPSave = LoadMPSavedGame(CurrentMPSave.Location);
            CurrentSPSave = LoadSPSavedGame(CurrentSPSave.Location);
        }

        private MPSavedGame LoadMPSavedGame(FileInfo fileInfo)
        {
            var savedGame = new MPSavedGame { Location = fileInfo };

            try
            {
                using (var reader = new BinaryReader(File.Open(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    reader.ReadBytes(9);

                    ReadMPCharacter(reader, savedGame, 1);
                    ReadMPCharacter(reader, savedGame, 2);
                    ReadMPCharacter(reader, savedGame, 3);
                }
            }
            catch (Exception)
            {
                // Can't really do much if it fails
            }

            return savedGame;
        }

        private SPSavedGame LoadSPSavedGame(FileInfo fileInfo)
        {
            var savedGame = new SPSavedGame { Location = fileInfo };

            try
            {
                using (var reader = new BinaryReader(File.Open(fileInfo.FullName, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    reader.ReadBytes(9);
                    
                    ReadSPCharacter(reader, savedGame, 1);
                    ReadSPCharacter(reader, savedGame, 2);
                    ReadSPCharacter(reader, savedGame, 3);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Ex: " + ex);
                // Can't really do much if it fails
            }

            return savedGame;
        }


        private void ReadMPCharacter(BinaryReader reader, MPSavedGame savedGame, int position)
        {
            var character = new MPCharacter();

            for (var i = 0; i < 3; ++i)
                character.Class += reader.ReadChar();

            var nameLen = reader.ReadInt32();

            for (var i = 0; i < nameLen; ++i)
                character.Name += reader.ReadChar();

            character.Floor = reader.ReadInt32();

            character.MaxHp = reader.ReadInt32();
            character.CurrentHp = reader.ReadInt32();

            var gold = reader.ReadInt32();

            reader.ReadInt32(); // unlocked slots
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

        private void ReadSPCharacter(BinaryReader reader, SPSavedGame savedGame, int position)
        {
            var character = new SPCharacter();

            for (var i = 0; i < 3; ++i)
                character.Class += reader.ReadChar();

            var nameLen = reader.ReadInt32();

            Trace.WriteLine(nameLen);

            for (var i = 0; i < nameLen; ++i)
                character.Name += reader.ReadChar();

            character.Floor = reader.ReadInt32();

            character.MaxHp = reader.ReadInt32();
            character.CurrentHp = reader.ReadInt32();

            character.Gold = reader.ReadInt32();

            character.Slots = reader.ReadInt32();
            character.Level = reader.ReadInt32(); 
            reader.ReadInt32(); // placeholder
            reader.ReadInt32(); // placeholder

            character.CardCount = reader.ReadInt32();

            character.Cards = new List<int>();

            for (var i = 0; i < character.CardCount; ++i)
                character.Cards.Add(reader.ReadInt32());

            character.CompanionCount = reader.ReadInt32();

            Trace.WriteLine("Count: " + character.CompanionCount + "Class: " + character.Class);


            reader.ReadInt32(); //unknown

            for (var i = 0; i < character.CompanionCount; i ++)
            {
                character.Companions.Add(new Companion());

                var companionNameLen = reader.ReadInt32();

                Trace.WriteLine(nameLen);

                for (var j = 0; j < companionNameLen; j++)
                    character.Companions[i].Name += reader.ReadChar();

                Trace.WriteLine(character.Companions[i].Name);

                character.Companions[i].Floor = reader.ReadInt32();

                reader.ReadInt32(); //type (class I guess)

                character.Companions[i].Level = reader.ReadInt32();
                character.Companions[i].MaxHp = reader.ReadInt32();
                character.Companions[i].CurrentHp = reader.ReadInt32();
                character.Companions[i].MaxMana = reader.ReadInt32();
                character.Companions[i].Mana = reader.ReadInt32();

                Trace.WriteLine("Floor: " + character.Companions[i].Floor);
                Trace.WriteLine("MaxHP: " + character.Companions[i].MaxHp);

                Trace.WriteLine("CurHP: " + character.Companions[i].CurrentHp);

                Trace.WriteLine("MaxMana: " + character.Companions[i].MaxMana);

                Trace.WriteLine("Mana: " + character.Companions[i].Mana);




                reader.ReadInt32(); //unknown
                reader.ReadInt32(); //unknown
                reader.ReadInt32(); //unknown
                reader.ReadInt32(); //unknown
                reader.ReadInt32(); //unknown
                reader.ReadInt32(); //unknown

                character.Companions[i].CardCount = reader.ReadInt32();

                Trace.WriteLine("lencards:" + character.Companions[i].CardCount);

                for (var j = 0; j < character.Companions[i].CardCount; j++)
                {
                    character.Companions[i].Cards.Add(reader.ReadInt32());
                }

                var futureCardsLen = reader.ReadInt32();

                for (var j = 0; j < futureCardsLen; j++)
                {
                    character.Companions[i].FutureCards.Add(reader.ReadInt32());
                }


                //if (character.CompanionCount == (i+1)) { reader.ReadInt32(); }  //unknown
                reader.ReadInt32();
                Trace.WriteLine("testc:" + (character.CompanionCount == (i + 1)));


            }


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
                hellcardProcess = Process.GetProcessesByName("HELLCARD_Demo_single").FirstOrDefault();

            }

            if (hellcardProcess == null)
            {
                MessageBox.Show("It seems like Hellcard is curently not running.\nPlease start the game and try again!", "Watch Hellcard", MessageBoxButton.OK);
                return;
            }

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
                ExitCode = proc.ExitCode; 
                IsWatching = false;
                if (ExitCode != 0)
                {
                    IsSendMinidumps = true;
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
                if (SelectedInxedSPMP == 0)
                {
                    CurrentSPSave.Location.Delete();
                    CurrentSPSave.Location.Create().Dispose();
                    CurrentSPSave = LoadSPSavedGame(CurrentSPSave.Location);
                }
                else
                {
                    CurrentMPSave.Location.Delete();
                    CurrentMPSave.Location.Create().Dispose();
                    CurrentMPSave = LoadMPSavedGame(CurrentMPSave.Location);
                }
            }

        }

        public ICommand CreateBackupCommand => new DelegateCommand(CreateBackup, SaveButtons_CanExecute);
        private void CreateBackup()
        {
            BackupFolder.Create();

            var i = 1;
            string newFile;

            var saveType = "mp";
            if (SelectedInxedSPMP == 0) { saveType = "sp"; };

            do
            {
                newFile = Path.Combine(BackupFolder.FullName, $"{i++}_{_saveName.Split('.')[0]}.{saveType}.{_saveName.Split('.')[1]}");
            } while (File.Exists(newFile));

            if (SelectedInxedSPMP == 0) { 
                CurrentSPSave.Location.CopyTo(newFile);
                BackupsSP.Insert(0, LoadSPSavedGame(new FileInfo(newFile)));
            }
            else {
                CurrentMPSave.Location.CopyTo(newFile);
                BackupsMP.Insert(0, LoadMPSavedGame(new FileInfo(newFile)));
            };
        }


        private Boolean SaveButtons_CanExecute()
        {
            if (SelectedInxedSPMP == 0)
            {
                CurrentSPSave.Location.Refresh();
                return CurrentSPSave.Location.Length > 0;

            }
            else
            {
                CurrentMPSave.Location.Refresh();
                return CurrentMPSave.Location.Length > 0;
            }
        }

        public ICommand RestoreSPCommand => new DelegateCommand<SPSavedGame>(RestoreSP);
        private void RestoreSP(SPSavedGame game)
        {
            game.Location.CopyTo(CurrentSPSave.Location.FullName, true);

            CurrentSPSave = LoadSPSavedGame(CurrentSPSave.Location);
        }

        public ICommand RestoreMPCommand => new DelegateCommand<MPSavedGame>(RestoreMP);
        private void RestoreMP(MPSavedGame game)
        {
            game.Location.CopyTo(CurrentMPSave.Location.FullName, true);

            CurrentMPSave = LoadMPSavedGame(CurrentMPSave.Location);
        }

        public ICommand DeleteSPCommand => new DelegateCommand<SPSavedGame>(DeleteSP);
        private void DeleteSP(SPSavedGame game)
        {
            BackupsSP.Remove(game);

            game.Location.Delete();
        }
        public ICommand DeleteMPCommand => new DelegateCommand<MPSavedGame>(DeleteMP);
        private void DeleteMP(MPSavedGame game)
        {
            BackupsMP.Remove(game);

            game.Location.Delete();
        }

        #endregion


        #region Change character name, open log folder, send email

        private byte[] WriteMPName(byte[] binary, string newName, MPCharacter character)
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
                            WriteCharacterMP(reader, writer);
                        }
                        WriteCharAndName(reader, writer, newName, character.Name);
                        character.Name = newName;
                        return writeStream.GetBuffer().Take((int)writeStream.Length).ToArray();

                    }
                }
            }
        }

        private byte[] WriteSPName(byte[] binary, string newName, SPCharacter character)
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
                            WriteCharacterSP(reader, writer);
                        }
                        WriteCharAndName(reader, writer, newName, character.Name);
                        character.Name = newName;
                        return writeStream.GetBuffer().Take((int)writeStream.Length).ToArray();

                    }
                }
            }
        }

        private void WriteCharacterMP(BinaryReader reader, BinaryWriter writer)
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

        private void WriteCharacterSP(BinaryReader reader, BinaryWriter writer)
        {
            writer.Write(reader.ReadChars(3)); //Read Class Name
            var nameLen = reader.ReadInt32();
            writer.Write(nameLen);
            writer.Write(reader.ReadChars(nameLen));
            writer.Write(reader.ReadBytes(32)); //all stuff between name and cardCount
            var cardCount = reader.ReadInt32();
            writer.Write(cardCount);
            writer.Write(reader.ReadBytes(cardCount * 4));

            var companionCount = reader.ReadInt32();
            writer.Write(companionCount);
            writer.Write(reader.ReadInt32());

            for (var i = 0; i < companionCount; i++)
            {
                writer.Write(reader.ReadInt32());
                writer.Write(reader.ReadInt32());

                var companionNameLen = reader.ReadInt32();

                writer.Write(companionNameLen);
                writer.Write(reader.ReadChars(companionNameLen));

                writer.Write(reader.ReadInt32());
                writer.Write(reader.ReadInt32());
                writer.Write(reader.ReadInt32());
                writer.Write(reader.ReadInt32());
                writer.Write(reader.ReadInt32());
                writer.Write(reader.ReadInt32());
                writer.Write(reader.ReadInt32());

                writer.Write(reader.ReadInt32());
                writer.Write(reader.ReadInt32());
                writer.Write(reader.ReadInt32());
                writer.Write(reader.ReadInt32());
                writer.Write(reader.ReadInt32());
                writer.Write(reader.ReadInt32());

                var companionCardCount = reader.ReadInt32();

                writer.Write(companionCardCount);

                writer.Write(reader.ReadBytes(companionCardCount * 4));

                writer.Write(reader.ReadBytes(11 * 4)); //future cards to come

                writer.Write(reader.ReadInt32());

            }

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

        public ICommand ChangeMPNamesCommand => new DelegateCommand(ChangeMPNames, SaveButtons_CanExecute);
        private void ChangeMPNames()
        {

            var binary = File.ReadAllBytes(CurrentMPSave.Location.FullName);
            var nameBox = new ChangeNameBox();

            nameBox.Owner = Application.Current.MainWindow;
            nameBox.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            nameBox.mageBox.Text = CurrentMPSave.Mage.Name;
            nameBox.warriorBox.Text = CurrentMPSave.Warrior.Name;
            nameBox.rougeBox.Text = CurrentMPSave.Rogue.Name;
            if (nameBox.ShowDialog() == true)
            {
                if (nameBox.rougeBox.Text != CurrentMPSave.Rogue.Name)
                {
                    binary = WriteMPName(binary, nameBox.rougeBox.Text, CurrentMPSave.Rogue);
                }

                if (nameBox.mageBox.Text != CurrentMPSave.Mage.Name)
                {
                    binary = WriteMPName(binary, nameBox.mageBox.Text, CurrentMPSave.Mage);
                }

                if (nameBox.warriorBox.Text != CurrentMPSave.Warrior.Name)
                {
                    binary = WriteMPName(binary, nameBox.warriorBox.Text, CurrentMPSave.Warrior);
                }
                File.WriteAllBytes(CurrentMPSave.Location.FullName, binary);
                CurrentMPSave = LoadMPSavedGame(CurrentMPSave.Location);
            }
        }

        public ICommand ChangeSPNamesCommand => new DelegateCommand(ChangeSPNames, SaveButtons_CanExecute);
        private void ChangeSPNames()
        {

            var binary = File.ReadAllBytes(CurrentSPSave.Location.FullName);
            var nameBox = new ChangeNameBox();

            nameBox.Owner = Application.Current.MainWindow;
            nameBox.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            nameBox.mageBox.Text = CurrentSPSave.Mage.Name;
            nameBox.warriorBox.Text = CurrentSPSave.Warrior.Name;
            nameBox.rougeBox.Text = CurrentSPSave.Rogue.Name;
            if (nameBox.ShowDialog() == true)
            {
                if (nameBox.rougeBox.Text != CurrentSPSave.Rogue.Name)
                {
                    binary = WriteSPName(binary, nameBox.rougeBox.Text, CurrentSPSave.Rogue);
                }

                if (nameBox.mageBox.Text != CurrentSPSave.Mage.Name)
                {
                    binary = WriteSPName(binary, nameBox.mageBox.Text, CurrentSPSave.Mage);
                }

                if (nameBox.warriorBox.Text != CurrentSPSave.Warrior.Name)
                {
                    binary = WriteSPName(binary, nameBox.warriorBox.Text, CurrentSPSave.Warrior);
                }
                File.WriteAllBytes(CurrentMPSave.Location.FullName, binary);
                CurrentMPSave = LoadMPSavedGame(CurrentMPSave.Location);
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
            
            var spBox = new SPorMP();

            spBox.Owner = Application.Current.MainWindow;
            spBox.WindowStartupLocation = WindowStartupLocation.CenterOwner;


            if (spBox.ShowDialog() == true)
            {
                Process.Start(Directory.GetDirectories(SPDirInfo.FullName)[0]);
            } else
            {
                Process.Start(Directory.GetDirectories(MPDirInfo.FullName)[0]);
            }

        }

        public ICommand SendLogsSmtpCommand => new DelegateCommand(SendLogsSmtp);
        private void SendLogsSmtp()
        {
            var dir = Directory.GetDirectories(MPDirInfo.FullName)[0];

            var spBox = new SPorMP();

            spBox.Owner = Application.Current.MainWindow;
            spBox.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (spBox.ShowDialog() == true)
            {
                dir = Directory.GetDirectories(SPDirInfo.FullName)[0];
            }


            var winSendMail = new SendLog(dir, IsSendMinidumps, GameDir);
            if (winSendMail.ShowDialog() == true)
            {
                IsSendMinidumps = false;
            }
        }

        #endregion

        #region Help Menu Section

        public ICommand ViewHelpCommand => new DelegateCommand(HelpWindow);

        private void HelpWindow()
        {
            MessageBox.Show("If you want to send a log automatically you can do that with the \"Send Logs automatically\" Button (You can also send them manually to support@thingtrunk.com).\n\nIf you choose the \"Watch Hellcard\" option it will listen for Hellcard crashes and attaches a crashdump if you send a mail automatically.\n\n If you'd like to change the charakter names or backup your save because you would like to play more than one charakter of each class, you can do that in the \"Manage saves\" tab of the tool.\n\nIf you need any further help just ask on the BoD discord.", "Help", MessageBoxButton.OK);
        }

        public ICommand AboutCommand => new DelegateCommand(AboutWindow);

        private void AboutWindow()
        {
            MessageBox.Show("This is a report tool and save manager written by _Q_, Flecki and Essarielle.\nYou can reach them on the BoD discord if you find any bugs or need help.", "About", MessageBoxButton.OK);
        }

        public ICommand ShowWebsiteCommand => new DelegateCommand<string>(ShowWebsite);

        private void ShowWebsite(string url)
        {
            Process.Start(new ProcessStartInfo(url));
        }


        #endregion


    }
}

