using System;
using System.IO;
using System.Collections.Generic;
using GSGE;
using GSGE.Code;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Graphics;
using System.Windows.Forms;

using GSGE.Code.Things;
using GSGE.Code.Players;
using GSGE.Code.GUI;
using GSGE.Code.Helpers;

using XKeys = Microsoft.Xna.Framework.Input.Keys;
using CColor = System.Drawing.Color;
using XColor = Microsoft.Xna.Framework.Color;

namespace Bastion.TAS
{
    [Flags]
    public enum ETASManagerState
    {
        ETMS_DISABLED = 0,
        ETMS_PAUSED = 1,
        ETMS_ENABLED = 2,
        ETMS_FRAMESTEP = 4,
        ETMS_RECORDING = 8,
        ETMS_PLAYBACK = 16
    }

    [Flags]
    public enum EBreakPointType
    {
        BREAKTYPE_DEFAULT = 0,
        BREAKTYPE_FAST = 1 << 0,
        BREAKTYPE_NORMAL =  1 << 1
    }

    public class BreakState
    {
        public string CurrentFile { get; set; }
        public int lineNo { get; set; }
        public EBreakPointType breakType { get; set; }
    }


    public class Manager
    {

        public bool CheckLoading()
        {
            foreach(GameScreen screen in App.SingletonApp.ScreenManager.screens)
            {
                if (screen.GetType() == typeof(LoadScreen))
                    return true;
            }

            return false;
        }
        public static void Init(GraphicsDevice _gd)
        {
            if (Manager.Instance == null)
            {
                Manager.Instance = new Manager();
                Manager.Instance.bastionGD = _gd;


                Manager.Instance.DoInitialize();
            }
        }

        public void DoInitialize()
        {
            this.UpdateCallRate = 1;
            this.OSDSpriteBatch = new SpriteBatch(this.bastionGD);

        }

        public static void DebugLog(string contents)
        {
            File.AppendAllText("TASDebugLog.txt", contents);
        }

        public void HandleManagerInput()
        {
            if (OnePress(XKeys.F1))
            {
                this.Paused = !this.Paused;
            }

            if(OnePress(XKeys.F4) && !IsKeyDown(XKeys.LeftAlt, this.CurrentState))
            {
                this.InitPlayback();
            }

            if (OnePress(XKeys.Add))
            {
                // ???
                if (this.UpdateCallRate + 1 <= 20)
                    this.UpdateCallRate++;
            }

            if (OnePress(XKeys.Subtract))
            {
                if (this.UpdateCallRate - 1 >= 1)
                    this.UpdateCallRate--;
            }

            if (OnePress(XKeys.Divide))
            {
                this.UpdateCallRate = 1;
            }

            if(OnePress(XKeys.F7))
            {
                this.singleStepMode = !this.singleStepMode;
            }

            if(OnePress(XKeys.C))
            {
                Player playerOne = PlayerManager.getPlayer();

                try
                {
                    if (playerOne != null)
                    {
                        var cursorPoint = playerOne.getInputHandler().m_cursor.getLocation();

                        Clipboard.SetText(string.Format("1,Move,{0},{1}", cursorPoint.X, cursorPoint.Y));
                    }
                    else
                        DebugLog("CopyCursor: PlayerOne is null?\n");
                }
                catch (Exception e)
                {
                    DebugLog("CopyCursor: Caught exception -> " + e.ToString() + "\n");
                }
            }

            if(OnePress(XKeys.F2))
            {
                this.CursorUnlocked = !this.CursorUnlocked;
            }
        }

        public bool OnePress(XKeys key)
        {
            bool ret = IsKeyUp(key, this.CurrentState) && IsKeyDown(key, this.OldState);
            return ret;
        }

        public bool HeldPress(XKeys key)
        {
            bool ret = IsKeyDown(key, this.CurrentState);
            return ret;
        }

        public void UpdateOurInputs()
        {
            this.OldState = this.CurrentState;
            this.CurrentState = Keyboard.GetState();
        }

        public static bool IsKeyDown(XKeys key, KeyboardState ks)
        {
            return ks.IsKeyDown(key);
        }

        public static bool IsKeyUp(XKeys key, KeyboardState ks)
        {
            return ks.IsKeyUp(key);
        }

        public void Game_Draw(GameTime gameTime)
        {
            this.OSDSpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);

            string Extra = string.Empty;

            if (CheckLoading())
                Extra += "\n Loading";



            Extra += string.Format("\n StepMode: {0}", this.singleStepMode ? "Single" : "Held");
            Extra += "\n CallRate " + this.UpdateCallRate.ToString();
            Extra += string.Format("\n Cursor Unlocked: {0}", this.CursorUnlocked ? "true" : "false");

            Player playerOne = PlayerManager.getPlayer();
            if (playerOne != null)
            {
                var unit = playerOne.getUnit();
                if(unit != null)
                {
                    float speed = unit.getSpeed();
                    var vel = unit.getVelocity();
                    Extra += string.Format("\n Velocity: ({0}, {1}) | Speed: {2}", vel.X, vel.Y, unit.getSpeed());
                }
            }

            bool playingBack = this.CurManagerState.HasFlag(ETASManagerState.ETMS_PLAYBACK);

            if (playingBack)
            {
                Extra += "\n Playing back";

                int FramesInCurrentInput = TASCurrentInput.Frames;
                int FramesDone = TASCurrentInput.Done;

                int LineNo = TASCurrentInput.isMultilLevel ? TASCurrentInput.InternalLineNo : TASCurrentInput.Line;

                Extra += string.Format("\n [{0}] - {1} / {2}  ({3}) [{4}]", this.TASCurrentInput.fromFile, FramesDone, FramesInCurrentInput, LineNo, TASCurrentInput.ToString());

                Extra += string.Format("\n {0} / {1}", this.CurrentFrame, this.TotalFrameCountOfInputFile);
            }

            XColor xColor = new XColor(255, 0, 0);
            if(this.ShowOSD)
            {
                DrawManager.StringDrawable stringDrawable = DrawManager.getStringDrawable();
                stringDrawable.Font = AssetManager.getFont(Fonts.MENU);
                stringDrawable.Color = XColor.Red;
                stringDrawable.Outline = false;
                stringDrawable.Location = new Vector2(0, 60f);
                using (PooledStringBuilder psb = PooledStringBuilder.create())
                {
                    psb.Text.Append(Extra);
                    stringDrawable.Text = psb.Text;
                    DrawManager.DrawString(OSDSpriteBatch, stringDrawable);
                }
            }


            this.OSDSpriteBatch.End();
        }

        public ETASManagerState Game_Update(GameTime gameTime)
        {
            try
            {
                this.UpdateOurInputs();

                this.HandleManagerInput();

                if (this.Paused)
                {

                    /*
                    if (this.singleStepMode)
                    {
                        if (OnePress(XKeys.OemCloseBrackets))
                        {
                            return ETASManagerState.ETMS_FRAMESTEP;
                        }
                    }
                    else
                    {
                        if(HeldPress(XKeys.OemCloseBrackets))
                        {
                            return ETASManagerState.ETMS_FRAMESTEP;
                        }
                    }*/

                    if (OnePress(XKeys.OemCloseBrackets))
                    {
                        return ETASManagerState.ETMS_FRAMESTEP;
                    }

                    return ETASManagerState.ETMS_PAUSED;
                }

                return ETASManagerState.ETMS_DISABLED;
            }
            catch (Exception e)
            {
                DebugLog("Manager.Game_Update caught exception: " + e.ToString() + "\n");
            }

            return ETASManagerState.ETMS_DISABLED;
        }

        public bool ReadMultiLevelInputFile(string fileName, int otherFileCount, ref int outRecordsRead, ref int outLinesRead)
        {
            try
            {
                int lineCount = otherFileCount;
                int otherLineCount = 0;

                if (this.segmentedFileStream != null)
                {
                    this.segmentedFileStream.Dispose();
                    this.segmentedFileStream = null;
                }

                string pathToCurrentFile = this.currentDirectory;
                pathToCurrentFile += fileName;

                this.segmentedFileStream = new FileStream(pathToCurrentFile, FileMode.Open, FileAccess.Read, FileShare.Read);

                using (StreamReader sr = new StreamReader(this.segmentedFileStream))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();
                        if (line.IndexOf("Runto", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            this.breakState.CurrentFile = fileName;
                            this.breakState.lineNo = otherLineCount;
                            this.breakState.breakType = EBreakPointType.BREAKTYPE_FAST;

                            this.RuntoLineNo = (int)otherLineCount;

                            lineCount++;
                            otherLineCount++;
                            continue;
                        }
                        else if (line.IndexOf("Walkto", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            //this.WalktoLineNo = lines;
                            this.breakState.CurrentFile = fileName;
                            this.breakState.lineNo = otherLineCount;
                            this.breakState.breakType = EBreakPointType.BREAKTYPE_NORMAL;

                            this.WalktoLineNo = (int)otherLineCount;

                            lineCount++;
                            otherLineCount++;
                            continue;
                        }

                        InputRecord r = new InputRecord(line, (int)++lineCount, (int)++otherLineCount, fileName);

                        if (r.Frames != 0)
                        {
                            this.InputRecords.Add(r);
                            this.TotalFrameCountOfInputFile += r.Frames;
                        }
                    }
                }

                this.segmentedFileStream.Dispose();
                this.segmentedFileStream = null;
                return true;
            }
            catch (Exception e)
            {
                DebugLog("Manager.ReadMultiLevelInputFile caught exception: " + e.ToString() + "\n");
                return false;
            }
        }

        public bool ReadInputFile()
        {
            bool first = true;
            try
            {
                this.InputRecords.Clear();
                FileInfo foo = new FileInfo("Bastion.rec");

                if (!foo.Exists)
                {
                    return false;
                }

                int lines = 0;

                if (this.PlaybackStream == null)
                    this.PlaybackStream = new FileStream("Bastion.rec", FileMode.Open, FileAccess.Read, FileShare.Read);

                using (StreamReader sr = new StreamReader(this.PlaybackStream))
                {
                    while (!sr.EndOfStream)
                    {
                        string line = sr.ReadLine();

                        /*
                        if (line.IndexOf("Runto", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            this.RuntoLineNo = lines;
                            continue;
                        }
                        else if (line.IndexOf("Walkto", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            this.WalktoLineNo = lines;
                            continue;
                        }*/

                        if(line.IndexOf("Runto", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            this.breakState.CurrentFile = Manager.defaultFileName;
                            this.breakState.lineNo = lines;
                            this.breakState.breakType = EBreakPointType.BREAKTYPE_FAST;

                            this.RuntoLineNo = lines;
                            continue;
                        }
                        else if(line.IndexOf("Walkto", StringComparison.OrdinalIgnoreCase) == 0)
                        {
                            this.breakState.CurrentFile = Manager.defaultFileName;
                            this.breakState.lineNo = lines;
                            this.breakState.breakType = EBreakPointType.BREAKTYPE_NORMAL;

                            this.WalktoLineNo = lines;
                            continue;
                        }
                            


                        if (!first)
                        {
                            if (line.IndexOf("Read", StringComparison.OrdinalIgnoreCase) == 0)
                            {
                                string _fileName = line.Substring(line.IndexOf(",") + 1);
                                DebugLog("Manager.ReadInputFile found Read for file: " + _fileName + "\n");
                                lines++;

                                int inRecordsRead = 0, outLinesRead = 0;
                                bool multiResult = ReadMultiLevelInputFile(_fileName, lines, ref inRecordsRead, ref outLinesRead);

                                if(!multiResult)
                                {
                                    DebugLog("Couldn't  read multi level input file: " + _fileName + "\n");
                                }
                                else
                                {
                                    DebugLog("Succesfully read multi level input file: " + _fileName + "\n");
                                }
                                continue;
                            }
                            InputRecord r = new InputRecord(line, ++lines, 0);

                            if(r==null)
                            {
                                DebugLog("Manager.ReadInputFile: r==null?\n");
                                continue;
                            }
                            if (r.Frames != 0)
                            {
                                this.InputRecords.Add(r);
                                this.TotalFrameCountOfInputFile += r.Frames;
                            }

                        }
                        else
                        {
                            lines++;

                            string[] firstLineParams = line.Split(',');
                            //int tempInt = 0;
                            //double tempDouble = 0.0;

                            /*
                            if (!this.PlayedBackAtLeastOnce)
                            {
                                if (int.TryParse(firstLineParams[0], out tempInt))
                                    this.GlobalIntEndRNG = tempInt;

                                if (double.TryParse(firstLineParams[1], out tempDouble))
                                    this.GlobalDoubleRNG = tempDouble;

                                this.PlayedBackAtLeastOnce = true;
                            }*/

                            /* if we do reload it, then reload it. 
                            if (Conf.DoesReloadRNG)
                            {
                                if (int.TryParse(firstLineParams[0], out tempInt))
                                    this.GlobalIntEndRNG = tempInt;

                                if (double.TryParse(firstLineParams[1], out tempDouble))
                                    this.GlobalDoubleRNG = tempDouble;

                            }*/

                            first = false;
                        }
                    }
                }

                if(this.PlaybackStream != null)
                {
                    this.PlaybackStream.Dispose();
                    this.PlaybackStream = null;
                }

                DebugLog("Manager.ReadInputFile iterate count: " + lines + "\n");
                return true;
            }
            catch (Exception e)
            {
                DebugLog("Manager.ReadInputFile caught exception: " + e.ToString() + "\n");
                return false;
            }
        }

        
        public void InitPlayback(bool reset = true, bool fromReload=false)
        {
            this.TotalFrameCountOfInputFile = 0;
            this.RuntoLineNo = -1;
            this.WalktoLineNo = -1;

            // Reset this crap
            this.breakState.lineNo = int.MaxValue;
            this.breakState.breakType = EBreakPointType.BREAKTYPE_DEFAULT;
            this.breakState.CurrentFile = Manager.defaultFileName;

            if (this.CurManagerState.HasFlag(ETASManagerState.ETMS_RECORDING))
                return;


            if (this.CurManagerState.HasFlag(ETASManagerState.ETMS_PLAYBACK) && reset)
            {
                // Disable playback
                this.CurManagerState &= ~ETASManagerState.ETMS_PLAYBACK;
                return;
            }

            this.CurManagerState |= ETASManagerState.ETMS_PLAYBACK;

            bool result = this.ReadInputFile();

            if (!result)
                return;

            this.CurrentFrame = 0;

            this.InputIndex = 0;

            if (this.InputRecords.Count > 0)
            {
                TASCurrentInput = InputRecords[0];
                this.FrameToNext = TASCurrentInput.Frames;
            }
            else
            {
                TASCurrentInput = new InputRecord();
                this.FrameToNext = 1;

                // Disable playback
                //this.CurManagerState &= ~ETASManagerState.ETMS_PLAYBACK;
                return;
            }

            // Hmm, desynch?
            if (fromReload)
                this.managerRand = new Random(1337);


        }

        public int ReloadPlayback()
        {
            int playedBackFrames = this.CurrentFrame;
            InitPlayback(false, true);

            this.CurrentFrame = playedBackFrames;

            while (this.CurrentFrame > this.FrameToNext)
            {
                if (InputIndex + 1 >= this.InputRecords.Count)
                {
                    this.InputIndex++;
                    return this.InputRecords.Count;
                }

                this.TASCurrentInput = this.InputRecords[++this.InputIndex];
                this.FrameToNext += this.TASCurrentInput.Frames;

            }
            return this.InputRecords.Count;
        }


        public /*KeyboardState*/ void DoPlayback(/*KeyboardState[] cur, KeyboardState[] prev*/)
        {
            bool wasFramestepping = OldState.IsKeyDown(XKeys.OemCloseBrackets);
            if (InputIndex < this.InputRecords.Count && !this.CheckLoading())
            {
                if (wasFramestepping)
                {
                    int OldInputDoneCount = TASCurrentInput.Done;
                    int reloadedCount = ReloadPlayback();
                    TASCurrentInput.Done += OldInputDoneCount;
                }

                if (this.CurrentFrame >= this.FrameToNext)
                {
                    if (InputIndex + 1 >= this.InputRecords.Count)
                    {
                        if (wasFramestepping)
                        {
                            int reloadedCount = ReloadPlayback();

                            if (this.InputIndex + 1 >= reloadedCount)
                            {
                                this.InputIndex++;
                                this.CurManagerState &= ~ETASManagerState.ETMS_PLAYBACK;
                                return;
                            }

                        }

                        else
                        {
                            if (this.InputIndex + 1 >= this.InputRecords.Count)
                            {
                                this.InputIndex++;
                                this.CurManagerState &= ~ETASManagerState.ETMS_PLAYBACK;
                                return;
                            }
                        }

                    }

                    TASCurrentInput = this.InputRecords[++this.InputIndex];

                    //DebugLog("Manager.DoPlayback: TASCurrentInput.FromFile = " + this.TASCurrentInput.fromFile + " and breakState.currentFile = " +
                      //  this.breakState.CurrentFile + "\n");

                    if(TASCurrentInput.X != float.PositiveInfinity && TASCurrentInput.Y != float.PositiveInfinity)
                    {
                        this.LastInputCursorPos = new Vector2(TASCurrentInput.X, TASCurrentInput.Y);
                    }

                    if(TASCurrentInput.HasSeed)
                    {
                        this.managerRand = new Random(TASCurrentInput.SeedValue);
                    }

                    if(this.breakState.lineNo != int.MaxValue)
                    {
                        if(this.TASCurrentInput.isMultilLevel && this.TASCurrentInput.fromFile == this.breakState.CurrentFile && this.breakState.breakType ==
                            EBreakPointType.BREAKTYPE_NORMAL)
                        {
                            if(this.TASCurrentInput.InternalLineNo == this.breakState.lineNo)
                            {
                                this.breakState.lineNo = int.MaxValue;
                                this.Paused = true;
                                this.UpdateCallRate = 1;
                            }
                        }
                        else
                        {
                            if(this.TASCurrentInput.fromFile == this.breakState.CurrentFile && !this.TASCurrentInput.isMultilLevel &&
                                this.breakState.breakType == EBreakPointType.BREAKTYPE_NORMAL)
                            {
                                if(this.TASCurrentInput.Line == this.breakState.lineNo)
                                {
                                    this.breakState.lineNo = int.MaxValue;
                                    this.Paused = true;
                                    this.UpdateCallRate = 1;
                                }
                            }
                        }
                       
                        if (this.TASCurrentInput.isMultilLevel && this.breakState.breakType == EBreakPointType.BREAKTYPE_FAST &&
                            this.TASCurrentInput.fromFile == this.breakState.CurrentFile)
                        {
                            // Not there yet.
                            if (this.TASCurrentInput.InternalLineNo < this.breakState.lineNo)
                            {
                                this.UpdateCallRate = 50;
                            }
                            else
                            {
                                this.breakState.lineNo = int.MaxValue;
                                this.Paused = true;
                                this.UpdateCallRate = 1;
                            }
                        }
                        else
                        {
                            if(this.TASCurrentInput.fromFile == this.breakState.CurrentFile && !this.TASCurrentInput.isMultilLevel && this.breakState.breakType ==
                               EBreakPointType.BREAKTYPE_FAST)
                            {
                                // Not there yet.
                                if ((uint)this.TASCurrentInput.Line < this.breakState.lineNo )
                                {
                                    this.UpdateCallRate = 50;
                                }
                                else
                                {
                                    this.breakState.lineNo = int.MaxValue;
                                    this.Paused = true;
                                    this.UpdateCallRate = 1;
                                }

                            }

                        }

                    }
 
                    /*
                    if (RuntoLineNo != -1)
                    {
                        if (TASCurrentInput.Line < RuntoLineNo)
                        {
                            this.UpdateCallRate = 50;
                        }
                        else
                        {

                            RuntoLineNo = -1;
                            this.Paused = true;
                            this.UpdateCallRate = 1;
                        }
                    }
                    else if (WalktoLineNo != -1)
                    {
                        if (TASCurrentInput.Line < WalktoLineNo)
                            this.UpdateCallRate = 1;
                        else
                        {
                            WalktoLineNo = -1;
                            this.Paused = true;
                            this.UpdateCallRate = 1;
                        }

                    }*/
                    this.FrameToNext += TASCurrentInput.Frames;
                }
                else
                {
                    this.TASCurrentInput.Done++;
                }
                this.CurrentFrame++;

                // Here we need to update the game with KeyboardState and MouseState.
                return;//return TASCurrentInput.GetRecordState();
            }

            return;// new KeyboardState();

        }

        public void Manager_handleInput()
        {
            // we need to call DoPlayback in here, it will update
            // TASCurrentInput. We poll in keyboardHandler and mouseHandler.

            if(this.CurManagerState.HasFlag(ETASManagerState.ETMS_PLAYBACK))
                this.DoPlayback();
        }

        public Manager()
        {
            this.breakState = new BreakState()
            {
                lineNo = int.MaxValue, CurrentFile = Manager.defaultFileName, breakType = EBreakPointType.BREAKTYPE_DEFAULT
            };

            this.singleStepMode = true;
            this.LastInputCursorPos = new Vector2(0f, 0f);
            // Default seed
            this.CursorUnlocked = true;
            this.managerRand = new Random(1337);

            this.ShowOSD = true;
            this.UpdateCallRate = 1;
            this.WalktoLineNo = -1;
            this.RuntoLineNo = -1;
            this.CurManagerState = ETASManagerState.ETMS_DISABLED;
            this.PrevManagerState = ETASManagerState.ETMS_DISABLED;
            this.TotalFrameCountOfInputFile = 0;

            this.currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
            this.currentDirectory += "\\Includes\\";

            FileInfo foo = new FileInfo("Bastion.rec");

            if(!foo.Exists)
            {
                FileStream temp = File.Create("Bastion.rec");
                temp.Close();
            }

            this.PlaybackStream = null;
            this.ManagerCurrentInput = new InputRecord();
            this.InputRecords = new List<InputRecord>();
        }
        public static Manager Instance;

        public bool Paused { get; set; }

        public int UpdateCallRate { get; set; }

        public GraphicsDevice bastionGD { get; set; }

        public KeyboardState OldState;
        public KeyboardState CurrentState;

        public int WalktoLineNo { get; set; }
        public int RuntoLineNo { get; set; }

        public ETASManagerState CurManagerState { get; set; }
        public ETASManagerState PrevManagerState { get; set; }

        public int TotalFrameCountOfInputFile { get; set; }

        public InputRecord ManagerCurrentInput { get; set; }
        public List<InputRecord> InputRecords { get; set; }

        public FileStream PlaybackStream { get; set; }
        public int InputIndex { get; set; }
        public int FrameToNext { get; set; }

        public int CurrentFrame { get; set; }

        public InputRecord TASCurrentInput { get; set; }

        public bool ShowOSD { get; set; }

        public SpriteBatch OSDSpriteBatch;

        public bool CursorUnlocked { get; set; }
        public Vector2 LastInputCursorPos { get; set; }
 
        public Random managerRand { get; set; }
        public static Random managerGlobalRand = new Random();

        public bool singleStepMode { get; set; }

        public  const string defaultFileName = "Bastion.rec";
        public BreakState breakState { get; set; }
        public string currentDirectory { get; set; }
        public FileStream segmentedFileStream { get; set; }
    }
}
