using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GSGE;
using GSGE.Code;
using GSGE.Code.Things;
using GSGE.Code.Players;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace Bastion.TAS
{
    [Flags]
    public enum InputState
    { 
        Default=0,
        Move = 1 << 0,
        Stop = 1 << 1,
        Evade = 1 << 2,
        Attack1 = 1 << 3,
        Attack2 = 1 << 4,
        SecretSkill = 1 << 5,
        Defend = 1 << 6,
        Heal = 1 << 7,
        UseInteract = 1 << 8,
        Reload = 1 << 9,
        OpenPack = 1 << 10,
        NextTarget = 1 << 11,
        PrevTarget = 1 << 12,
        Menu       = 1 << 13,
        Click      = 1 << 14,      // Left mouse button
        ProceedCancel = 1 << 15,    // Escape+Enter alias
        Proceed    = 1 << 16
     
    }

    public class InputRecord
    {
        public InputRecord() { }

        public KeyboardState GetKeyboardRecordState()
        {
            List<Keys> keyList = new List<Keys>();

            if (IsEvade())
                keyList.Add(Keys.Space);

            if (IsStop())
                keyList.Add(Keys.S);

            if (IsHeal())
                keyList.Add(Keys.F);

            if (IsUseInteract())
                keyList.Add(Keys.E);

            if (IsReload())
                keyList.Add(Keys.R);

            if (IsOpenPack())
                keyList.Add(Keys.Tab);

            if (IsAttack2())
                keyList.Add(Keys.W);

            if (IsMenu())
                keyList.Add(Keys.Escape);

            if (IsProceed())
                keyList.Add(Keys.Enter);

            if (IsProceedCancel())
            {
                keyList.Add(Keys.Escape);
                keyList.Add(Keys.Enter);
            }

            Keys[] _out = keyList.ToArray();
            return new KeyboardState(_out);
        }

        public MouseState GetMouseRecordState()
        {
           
            ButtonState left, right, middle, xb1, xb2;  
            left = (this.IsClick()) ? ButtonState.Pressed : ButtonState.Released;
            right = (this.IsAttack1()) ? ButtonState.Pressed : ButtonState.Released;
            middle = ButtonState.Released;
            xb1 = this.IsSecretSkill() ? ButtonState.Pressed : ButtonState.Released;
            xb2 = this.IsDefend() ? ButtonState.Pressed : ButtonState.Released;


            int _X = 0, _Y = 0;
            if (this.X != float.PositiveInfinity)
                _X = (int)this.X;

            if (this.Y != float.PositiveInfinity)
                _Y = (int)this.Y;

            // Just remember the last one.
            if (!this.IsMove())
            {
                _X = (int)Manager.Instance.LastInputCursorPos.X;
                _Y = (int)Manager.Instance.LastInputCursorPos.Y;
            }
  

            Player playerOne  = PlayerManager.getPlayer();
            Vector2 worldLocation = new Vector2(0f, 0f);
            if (playerOne != null)
            {
                var unit = playerOne.m_unit;

                if(unit != null)
                    worldLocation = unit.m_location;
            }


           // Vector2 kidToScreen = Functions.convertWorldToScreenLocation(ref worldLocation);

            return new MouseState((int)_X, (int)_Y, 0, left, middle, right, xb1, xb2);

        }

        public InputRecord(string line, int ln2, int ln3, string fromfile="Bastion.rec")
        {
            try
            {
                this.fromFile = fromfile;
                this.isMultilLevel = false;

                if(this.fromFile != "Bastion.rec")
                {
                    this.InternalLineNo = 0;
                    this.isMultilLevel = true;
                }

                this.X = float.PositiveInfinity;
                this.Y = float.PositiveInfinity;
                this.SeedValue = -1;
                this.HasSeed = false;

                string[] parms = line.Split(',');
                int index = 0;

                int tempInt = 0;
                if (!int.TryParse(parms[0], out tempInt))
                {
                    this.Frames = 0;
                    return;
                }


                this.Frames = tempInt;

                this.Line = ln2;
                this.InternalLineNo = ln3;

                float tempf;
                int tempi;

                for(int i = 1; i < parms.Length; i++)
                {
                    switch (parms[i].ToUpper().Trim())
                    {
                        case "MOVE":
                            {
                                this._State |= InputState.Move;
                                if (float.TryParse(parms[i + 1], out tempf))
                                    this.X = tempf;

                                if (float.TryParse(parms[i + 2], out tempf))
                                    this.Y = tempf;

                                i += 2;
                            }
                            break;
                        case "ATTACK1":
                            this._State |= InputState.Attack1;
                            break;
                        case "ATTACK2":
                            this._State |= InputState.Attack2;
                            break;
                        case "STOP":
                            this._State |= InputState.Stop;
                            break;
                        case "EVADE":
                            this._State |= InputState.Evade;
                            break;
                        case "SECRETSKILL":
                            this._State |= InputState.SecretSkill;
                            break;
                        case "HEAL":
                            this._State |= InputState.Heal;
                            break;
                        case "USE":
                            this._State |= InputState.UseInteract;
                            break;
                        case "DEFEND":
                            this._State |= InputState.Defend;
                            break;
                        case "PACK":
                            this._State |= InputState.OpenPack;
                            break;
                        case "RELOAD":
                            this._State |= InputState.Reload;
                            break;
                        case "MENU":
                            this._State |= InputState.Menu;
                            break;
                        case "NEXTTARGET":
                            this._State |= InputState.NextTarget;
                            break;
                        case "PREVTARGET":
                            this._State |= InputState.PrevTarget;
                            break;
                        case "CLICK":
                            this._State |= InputState.Click;
                            break;
                        case "PCANCEL":
                            this._State |= InputState.ProceedCancel;
                            break;
                        case "PROCEED":
                            this._State |= InputState.Proceed;
                            break;
                        case "RNG":
                        case "RANDOM":
                        case "SEED":
                            if (int.TryParse(parms[i + 1], out tempi))
                            {
                                this.HasSeed = true;
                                this.SeedValue = tempi;
                            }
                            break;

                    }

                }
            }
            catch(Exception e)
            {
                Manager.DebugLog("InputRecord Constructor caught exception: " + e.ToString() + "\n");
            }
        }

        public bool IsProceed()
        {
            return this._State.HasFlag(InputState.Proceed);
        }

        public bool IsProceedCancel()
        {
            return this._State.HasFlag(InputState.ProceedCancel);
        }

        public bool IsMove()
        {
            return this._State.HasFlag(InputState.Move);
        }

        public bool IsStop()
        {
            return this._State.HasFlag(InputState.Stop);
        }

        public bool IsEvade()
        {
            return this._State.HasFlag(InputState.Evade);
        }

        public bool IsAttack1()
        {
            return this._State.HasFlag(InputState.Attack1);
        }

        public bool IsAttack2()
        {
            return this._State.HasFlag(InputState.Attack2);
        }

        public bool IsSecretSkill()
        {
            return this._State.HasFlag(InputState.SecretSkill);
        }

        public bool IsDefend()
        {
            return this._State.HasFlag(InputState.Defend);
        }

        public bool IsHeal()
        {
            return this._State.HasFlag(InputState.Heal);
        }

        public bool IsUseInteract()
        {
            return this._State.HasFlag(InputState.UseInteract);
        }

        public bool IsReload()
        {
            return this._State.HasFlag(InputState.Reload);
        }

        public bool IsOpenPack()
        {
            return this._State.HasFlag(InputState.OpenPack);
        }

        public bool IsNextTarget()
        {
            return this._State.HasFlag(InputState.NextTarget);
        }

        public bool IsPrevTarget()
        {
            return this._State.HasFlag(InputState.PrevTarget);
        }

        public bool IsMenu()
        {
            return this._State.HasFlag(InputState.Menu);
        }

        public bool IsClick()
        {
            return this._State.HasFlag(InputState.Click);
        }

        public string StateToString()
        {
            StringBuilder sb = new StringBuilder();


            if (IsMove())
            {
                sb.Append(",Move,");
                sb.Append(this.X.ToString() + "," + this.Y.ToString());
            }


            if (IsProceedCancel())
                sb.Append(",PCancel");

            if (IsEvade())
                sb.Append(",Evade");

            if (IsHeal())
                sb.Append(",Heal");

            if (IsNextTarget())
                sb.Append(",NTarget");

            if (IsPrevTarget())
                sb.Append(",PTarget");

            if (IsDefend())
                sb.Append(",Defend");

            if (IsMenu())
                sb.Append(",Menu");

            if (IsOpenPack())
                sb.Append(",OpenPack");

            if (IsReload())
                sb.Append(",Reload");

            if (IsUseInteract())
                sb.Append(",Use");

            if (IsAttack1())
                sb.Append(",Attack1");

            if (IsAttack2())
                sb.Append(",Attack2");

            if (IsSecretSkill())
                sb.Append(",SecretSkill");

            if(IsStop())
                sb.Append(",Stop");

            return sb.ToString();
        }

        public override string ToString()
        {
            return this.Frames == 0 ? string.Empty : Frames.ToString().PadLeft(4, ' ') + "," + this.StateToString();
        }

        public string ToOutputNoFrames()
        {
            return this.Frames == 0 ? string.Empty : " " + this.StateToString();
        }

        public int Line { get; set; }
        public InputState _State { get; set; }

        // For mouse.
        public float X { get; set; }
        public float Y { get; set; }

        public int Frames { get; set; }
        public int Done { get; set; }


        // RNG
        public int SeedValue { get; set; }
        public bool HasSeed { get; set; }

        // Multi-level IF
        public int InternalLineNo { get; set; }
        public bool isMultilLevel { get; set; }
        public string fromFile { get; set; }

        public static bool HasFlag(InputState state, InputState which)
        {
            return (state & which) == which;
        }

        public override bool Equals(object obj)
        {
            return obj is InputRecord && ((InputRecord)obj) == this;
        }

        public override int GetHashCode()
        {
            return (int)_State ^ Frames;
        }

        public static bool operator ==(InputRecord lhs, InputRecord rhs)
        {
            bool lhsNull = (object)lhs is null;
            bool rhsNull = (object)rhs is null;

            if (lhsNull != rhsNull)
            {
                return false;
            }
            else if (lhsNull && rhsNull)
            {
                return true;
            }

            return lhs._State == rhs._State;
        }

        public static bool operator !=(InputRecord lhs, InputRecord rhs)
        {
            bool lhsNull = (object)lhs is null;
            bool rhsNull = (object)rhs is null;

            if (lhsNull != rhsNull)
            {
                return true;
            }
            else if (lhsNull && rhsNull)
            {
                return false;
            }

            return lhs._State != rhs._State;
        }
    }
}
