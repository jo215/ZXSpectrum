using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using ZXSpectrum.Z_80;
using System.IO;

namespace ZXSpectrum
{
    /// <summary>
    /// Emulates the ZX Spectrum 48K's ULA chip.
    /// </summary>
    public class ULA : Microsoft.Xna.Framework.DrawableGameComponent, IOController
    {
        GraphicsDevice device;
        SpriteBatch spriteBatch;
        Z80 z80;
        Memory Memory;
        Loudspeaker speaker;

        Texture2D pixel, block;
        Texture2D[] patterns;

        Color[] colors;
        int border;

        const int borderHeight = 56;
        const int borderWidth = 48;
        //int screenHeight = 192;
        //int screenWidth = 256;
        float screenScale = 2.0f;

        bool earActive, micActive;

        List<float> buffer = new List<float>();

        List<List<byte>> tapeBlocks = new List<List<byte>>();
        int nextBlock = 0;

        long framesGenerated = 0;

        bool update = true;
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="game"></param>
        /// <param name="spriteBatch"></param>
        public ULA(Game game, SpriteBatch spriteBatch)
            : base(game)
        {
            //  For drawing the display
            this.spriteBatch = spriteBatch;
            this.device = game.GraphicsDevice;
            
            //  Loudspeaker
            speaker = new Loudspeaker();

            //  1 pixel texture
            pixel = new Texture2D(device, 1, 1, false, SurfaceFormat.Color);
            pixel.SetData<Color>(new[] { Color.White });

            //  8x8 pixel texture
            block = new Texture2D(device, 8, 8, false, SurfaceFormat.Color);
            Color[] temp = new Color[64];
            for (int i = 0; i < 64; i++)
            {
                temp[i] = Color.White;
            }
            block.SetData<Color>(temp);

            //  Cache all possible 8-pixel combinations to cut down on draw calls
            patterns = new Texture2D[256];
            for (int i = 0; i < 256; i++)
            {
                temp = new Color[8];
                for (int v = 128, b = 0; v > 0; v /= 2, b++)
                {
                    if ((i & v) == v)
                    {
                        temp[b] = Color.White;
                    }
                    else
                    {
                        temp[b] = Color.Transparent;
                    }
                }
                patterns[i] = new Texture2D(device, 8, 1, false, SurfaceFormat.Color);
                patterns[i].SetData<Color>(temp);
            }

            //  Spectrum colors
            colors = new Color[16];
            colors[0] = new Color(0, 0, 0);
            colors[1] = new Color(0, 0, 0xCD);
            colors[2] = new Color(0xCD, 0, 0);
            colors[3] = new Color(0xCD, 0, 0xCD);
            colors[4] = new Color(0, 0xCD, 0);
            colors[5] = new Color(0, 0xCD, 0xCD);
            colors[6] = new Color(0xCD, 0xCD, 0);
            colors[7] = new Color(0xCD, 0xCD, 0xCD);
            colors[8] = new Color(0, 0, 0);
            colors[9] = new Color(0, 0, 0xFF);
            colors[10] = new Color(0xFF, 0, 0);
            colors[11] = new Color(0xFF, 0, 0xFF);
            colors[12] = new Color(0, 0xFF, 0);
            colors[13] = new Color(0, 0xFF, 0xFF);
            colors[14] = new Color(0xFF, 0xFF, 0);
            colors[15] = new Color(0xFF, 0xFF, 0xFF);           
           
            //  16K ROM, 48K RAM
            Memory = new Memory48K(Game.Content.RootDirectory + "\\48.rom");

            //  The Z80
            z80 = new Z80(Memory);

            //  ULA handles all I/O
            //  The ULA is generally addressed at port 0xFE
            //  But responds to all even-numbered ports
            //  The high byte of the address is also used to select keyboard half-rows
            z80.AddDevice(this);

        }

        /// <summary>
        /// Loads a .TAP file.
        /// </summary>
        /// <param name="fileName"></param>
        public void LoadTAP(string fileName)
        {
            //  Read in all blocks
            tapeBlocks = new List<List<byte>>();
            nextBlock = 0;
            using (FileStream fs = File.Open(fileName, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs))
                {
                    while (fs.Position < fs.Length)
                    {
                        //  2-byte header gives block data length
                        int low = br.ReadByte();
                        int high = br.ReadByte();
                        int blockLength = low + (high << 8);
                        //  Read block data
                        byte[] block = br.ReadBytes(blockLength);
                        tapeBlocks.Add(block.ToList());
                    }
                }
            }
        }

        /// <summary>
        /// Reads the next unprocessed block from a pre-loaded .TAP file.
        /// </summary>
        public void LoadBlock()
        {
            List<byte> currentBlock = tapeBlocks[nextBlock];
            nextBlock = (nextBlock + 1) % tapeBlocks.Count;

            if ((z80.GetShadowFlagsAsByte() & 0x01) == 0x01)
            {
                //  Load command
                if (z80.A2 == currentBlock[0])
                {
                    int blockLength = z80.E + (z80.D << 8);
                    int address = z80.IXL + (z80.IXH << 8);
                    for (int i = 0; i < Math.Min(blockLength, currentBlock.Count - 1); i++)
                    {
                        if (address >= 0x4000)
                        {
                            Memory[address] = currentBlock[i + 1];
                        }
                        address = (address + 1) & 0xffff;
                    }

                    if (blockLength > currentBlock.Count - 1)
                    {
                        //  R: tape Load error
                        z80.Reset(Flag.Carry);
                    }
                    else
                    {
                        //  Success!
                        z80.Set(Flag.Carry);
                    }
                }
                else
                {
                    //  R: tape Load error
                    z80.Reset(Flag.Carry);
                }
            }
            else
            {
                //  Verify command
                z80.Set(Flag.Carry);
            }
            z80.PC = 0x05e2;
        }

        /// <summary>
        /// Loads a .SNA snapshot file.
        /// This is the simplest snapshot format, but corrupts 2 bytes below the stack pointer, and requires a RETN instruction
        /// to restart the program - discouraged.
        /// </summary>
        /// <param name="fileName"></param>
        public void LoadSNA(string fileName)
        {
            using (FileStream fs = File.Open(fileName, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs))
                {
                    z80.I = br.ReadByte();
                    z80.L2 = br.ReadByte();
                    z80.H2 = br.ReadByte();
                    z80.E2 = br.ReadByte();
                    z80.D2 = br.ReadByte();
                    z80.C2 = br.ReadByte();
                    z80.B2 = br.ReadByte();
                    z80.SetShadowFlags(br.ReadByte());
                    z80.A2 = br.ReadByte();
                    z80.L = br.ReadByte();
                    z80.H = br.ReadByte();
                    z80.E = br.ReadByte();
                    z80.D = br.ReadByte();
                    z80.C = br.ReadByte();
                    z80.B = br.ReadByte();
                    z80.IYL = br.ReadByte();
                    z80.IYH = br.ReadByte();
                    z80.IXL = br.ReadByte();
                    z80.IXH = br.ReadByte();
                    if ((br.ReadByte() & 4) == 4)
                    {
                        z80.IFF1 = true;
                        z80.IFF2 = true;
                    }
                    else
                    {
                        z80.IFF2 = false;
                        z80.IFF1 = false;
                    }
                    z80.R = br.ReadByte();
                    z80.SetFlags(br.ReadByte());
                    z80.A = br.ReadByte();
                    z80.SP = br.ReadByte();
                    z80.SP += (br.ReadByte() << 8);
                    z80.interruptMode = br.ReadByte();
                    this.border = br.ReadByte();
                    byte[] ram = br.ReadBytes(49152);
                    for (int i = 0; i < ram.Length; i++)
                    {
                        Memory[i + 16384, true] = ram[i];
                    }

                    z80.RETN();
                }
            }
        }

        /// <summary>
        /// Saves the current state of the emulator in .SNA format.
        /// </summary>
        /// <param name="fileName"></param>
        public void SaveSNA(string fileName)
        {
            using (FileStream fs = File.Open(Game.Content.RootDirectory + "\\" + fileName, FileMode.OpenOrCreate, FileAccess.Write))
            {
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    Memory[z80.SP - 1] = z80.PC >> 8;
                    Memory[z80.SP - 2] = z80.PC & 0xff;
                    bw.Write((byte)z80.I);
                    bw.Write((byte)z80.L2);
                    bw.Write((byte)z80.H2);
                    bw.Write((byte)z80.E2);
                    bw.Write((byte)z80.D2);
                    bw.Write((byte)z80.C2);
                    bw.Write((byte)z80.B2);
                    bw.Write((byte)z80.GetShadowFlagsAsByte());
                    bw.Write((byte)z80.A2);
                    bw.Write((byte)z80.L);
                    bw.Write((byte)z80.H);
                    bw.Write((byte)z80.E);
                    bw.Write((byte)z80.D);
                    bw.Write((byte)z80.C);
                    bw.Write((byte)z80.B);
                    bw.Write((byte)z80.IYL);
                    bw.Write((byte)z80.IYH);
                    bw.Write((byte)z80.IXL);
                    bw.Write((byte)z80.IXH);
                    if (z80.IFF2 == true)
                    {
                        bw.Write((byte) 4);
                    }
                    else
                    {
                        bw.Write((byte) 0);
                    }
                    bw.Write((byte)z80.R);
                    bw.Write((byte)z80.GetFlagsAsByte());
                    bw.Write((byte)z80.A);
                    bw.Write((byte)(z80.SP & 0xff));
                    bw.Write((byte)(z80.SP >> 8));
                    bw.Write((byte)z80.interruptMode);
                    bw.Write((byte)this.border);

                    for (int i = 16384; i < Memory.Length; i++)
                    {
                        bw.Write((byte)Memory[i, true]);
                    }
                }
            }
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// 
        //  The ULA renders a single line of pixels to the display every 224 T-states
        //  64 lines including top border
        //  192 screen + left/right border lines
        //  56 bottom border lines
        //  (64+192+56) * 224 = 69888 T-states per frame
        //  3.5MHz (Z80A) / 69888 = 50.08Hz interrupt
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            //  We sync updates with audio, rather than the other way around
            if (speaker.sfx.PendingBufferCount > 1)
                update = false;
            else
                update = true;

            if (update)
            {
                //  Generate the 50Hz clock interrupt
                z80.Interrupt();

                //  Run the cpu for a frame's worth of T-states (69888)

                //  for 44.1KHz audio / 50 frames we require 882 audio samples per frame
                for (int i = 0; i < 882; i++)
                {
                    //  69888 / 882 = 79 TStates per sample
                    z80.Run(79);
                    //  we just take the last value sent to the ear output
                    if (earActive)
                    {
                        buffer.Add(1f);
                    }
                    else
                    {
                        buffer.Add(0f);
                    }
                    //  Check for LOAD
                    if (z80.fastLoad)
                    {
                        LoadBlock();
                        z80.fastLoad = false;
                    }
                }
                speaker.SendBuffer(buffer);
                buffer.Clear();

                //  Check control keys
                GetUserInput();

                framesGenerated++;
                base.Update(gameTime);
            }
        }

        /// <summary>
        /// Gets user input to the emulator (over and above spectrum-routed input)
        /// </summary>
        private void GetUserInput()
        {
            KeyboardState keys = Keyboard.GetState();
            if (keys.IsKeyDown(Keys.F5))
            {
                //  F5 - Save Snapshot
                System.Windows.Forms.SaveFileDialog f = new System.Windows.Forms.SaveFileDialog();
                f.Filter = "SNA files (*.sna)|*.sna";
                f.Title = "Save snapshot";
                f.OverwritePrompt = true;
                System.Windows.Forms.DialogResult r = f.ShowDialog();
                if (r == System.Windows.Forms.DialogResult.OK)
                {
                    SaveSNA(f.FileName);
                }
                
            }
            if (keys.IsKeyDown(Keys.F4))
            {
                //  F4  - Load
                System.Windows.Forms.OpenFileDialog f = new System.Windows.Forms.OpenFileDialog();
                f.Filter = "SNA files (*.sna)|*.sna|TAP files (*.tap)|*.tap";
                f.Title = "Load tape or snapshot";
                System.Windows.Forms.DialogResult r = f.ShowDialog();
                if (r == System.Windows.Forms.DialogResult.OK)
                {
                    if (f.FilterIndex == 1)
                    {
                        LoadSNA(f.FileName);
                    }
                    else
                    {
                        LoadTAP(f.FileName);
                    }
                }

            }
            if (keys.IsKeyDown(Keys.F12))
            {
                //  F12 - Reset
                z80.Memory.ClearRAM();
                z80.Reset();
            }
            if (keys.IsKeyDown(Keys.Escape))
            {
                //  Escape - Quit emulator
                Game.Exit();
            }
        }

        /// <summary>
        /// Draws the display.
        /// </summary>
        /// <param name="gameTime"></param>
        public override void Draw(GameTime gameTime)
        {
            spriteBatch.Begin();
            //  Pixels in RAM 16384-22527
            //  Each 1/3 of screen (64 lines / 8 characters) stored in alternate lines of 256 pixels 
            
            //DrawBorder();
            //  Cheat-draw the border by clearing in the correct color - this wont work for the flashing load border
            device.Clear(colors[border]);

            //  Main screen display
            for (int line = 0; line < 24; line++)
            {
                for (int cha = 0; cha < 32; cha++)
                {
                    //  Attribute (color) map stored from 22528
                    int attribute = Memory[22528 + 32 * line + cha, true];
                    //  Bits 0-2 set ink color
                    int ink = attribute & 7;
                    //  Bits 3-5 set paper (background) color
                    int paper = (attribute >> 3) & 7;
                    if ((attribute & 64) == 64)
                    {
                        //  Bit 6 sets bright variant;
                        ink += 8;
                        paper += 8;
                    }

                    //  Starting x-position for each character
                    float startX = (cha * 8 + borderWidth) * screenScale;

                    //  Draw an 8x8 block in current paper color for each character position
                    spriteBatch.Draw(block, new Vector2(startX, (borderHeight + (line * 8)) * screenScale), null, colors[paper], 0f, Vector2.Zero, screenScale, SpriteEffects.None, 0f);

                    //  Draw each pixel line
                    for (int row = 0; row < 8; row++)
                    {
                        int pixels = Memory[16384 + 2048 * (line / 8) + 32 * (line - 8 * (line / 8)) + 256 * row + cha, true];
                        //  Bit 7 sets flashing - invert pixels
                        if ((attribute & 128) == 128 && framesGenerated % 32 <= 15)
                        {
                            pixels = 255 - pixels;
                        }

                        float y = (line * 8 + row + borderHeight) * screenScale;

                        spriteBatch.Draw(patterns[pixels], new Vector2(startX, y), null, colors[ink], 0f, Vector2.Zero, screenScale, SpriteEffects.None, 0f);                   
                    }
                }
            }
            
            spriteBatch.End();
        }

        /// <summary>
        /// An OUT to a port.
        /// </summary>
        /// <param name="dataByte"></param>
        public void Write(int port, int dataByte)
        {
            //  Port 0xFE should be used to address the ULA, but it actually responds to every even port
            if (port % 2 == 0)
            {
                //  Bits 0 - 2 change the border color
                border = dataByte & 7;
                //  0 in bit 3 activates MIC
                micActive = (dataByte & 8) == 8 ? false : true;
                //  1 in bit 4 activates EAR
                earActive = (dataByte & 16) == 16 ? true : false;
                //  Apparently if one gets activated they both get activated...
                //  need to check this behaviour
            }
        }

        /// <summary>
        /// An IN from a port.
        /// </summary>
        /// <returns></returns>
        public int Read(int port)
        {
            //  Port 0xFE should be used to address the ULA, but it actually responds to every even port
            if (port % 2 == 0)
            {
                KeyboardState keys = Keyboard.GetState();
                //  0 on a high-byte bit selects a half row of keys
                int high = (port >> 8);
                //  0 in 5 lowest bits of result means corresponding key pressed
                int finalKeys = 31;
                //  Result (of 5 lowest bits) is AND of all selected half-rows
                int keyLine = 31;
                if ((high & 1) == 0)
                {
                    if (keys.IsKeyDown(Keys.LeftShift)) keyLine -= 1;
                    if (keys.IsKeyDown(Keys.Z)) keyLine -= 2;
                    if (keys.IsKeyDown(Keys.X)) keyLine -= 4;
                    if (keys.IsKeyDown(Keys.C)) keyLine -= 8;
                    if (keys.IsKeyDown(Keys.V)) keyLine -= 16;
                    finalKeys &= keyLine;
                }
                if ((high & 2) == 0)
                {
                    if (keys.IsKeyDown(Keys.A)) keyLine -= 1;
                    if (keys.IsKeyDown(Keys.S)) keyLine -= 2;
                    if (keys.IsKeyDown(Keys.D)) keyLine -= 4;
                    if (keys.IsKeyDown(Keys.F)) keyLine -= 8;
                    if (keys.IsKeyDown(Keys.G)) keyLine -= 16;
                    finalKeys &= keyLine;
                }
                if ((high & 4) == 0)
                {
                    if (keys.IsKeyDown(Keys.Q)) keyLine -= 1;
                    if (keys.IsKeyDown(Keys.W)) keyLine -= 2;
                    if (keys.IsKeyDown(Keys.E)) keyLine -= 4;
                    if (keys.IsKeyDown(Keys.R)) keyLine -= 8;
                    if (keys.IsKeyDown(Keys.T)) keyLine -= 16;
                    finalKeys &= keyLine;
                }
                if ((high & 8) == 0)
                {
                    if (keys.IsKeyDown(Keys.D1)) keyLine -= 1;
                    if (keys.IsKeyDown(Keys.D2)) keyLine -= 2;
                    if (keys.IsKeyDown(Keys.D3)) keyLine -= 4;
                    if (keys.IsKeyDown(Keys.D4)) keyLine -= 8;
                    if (keys.IsKeyDown(Keys.D5) || keys.IsKeyDown(Keys.Left)) keyLine -= 16;
                    finalKeys &= keyLine;
                }
                if ((high & 16) == 0)
                {
                    if (keys.IsKeyDown(Keys.D0)) keyLine -= 1;
                    if (keys.IsKeyDown(Keys.D9)) keyLine -= 2;
                    if (keys.IsKeyDown(Keys.D8) || keys.IsKeyDown(Keys.Right)) keyLine -= 4;
                    if (keys.IsKeyDown(Keys.D7) || keys.IsKeyDown(Keys.Up)) keyLine -= 8;
                    if (keys.IsKeyDown(Keys.D6) || keys.IsKeyDown(Keys.Down)) keyLine -= 16;
                    finalKeys &= keyLine;
                }
                if ((high & 32) == 0)
                {
                    if (keys.IsKeyDown(Keys.P)) keyLine -= 1;
                    if (keys.IsKeyDown(Keys.O)) keyLine -= 2;
                    if (keys.IsKeyDown(Keys.I)) keyLine -= 4;
                    if (keys.IsKeyDown(Keys.U)) keyLine -= 8;
                    if (keys.IsKeyDown(Keys.Y)) keyLine -= 16;
                    finalKeys &= keyLine;
                }
                if ((high & 64) == 0)
                {
                    if (keys.IsKeyDown(Keys.Enter)) keyLine -= 1;
                    if (keys.IsKeyDown(Keys.L)) keyLine -= 2;
                    if (keys.IsKeyDown(Keys.K)) keyLine -= 4;
                    if (keys.IsKeyDown(Keys.J)) keyLine -= 8;
                    if (keys.IsKeyDown(Keys.H)) keyLine -= 16;
                    finalKeys &= keyLine;
                }
                if ((high & 128) == 0)
                {
                    if (keys.IsKeyDown(Keys.Space)) keyLine -= 1;
                    if (keys.IsKeyDown(Keys.RightShift)) keyLine -= 2;
                    if (keys.IsKeyDown(Keys.M)) keyLine -= 4;
                    if (keys.IsKeyDown(Keys.N)) keyLine -= 8;
                    if (keys.IsKeyDown(Keys.B)) keyLine -= 16;
                    finalKeys &= keyLine;
                }
                //  Bit 6 is the EAR input port
                //  Leave to 1 for now as no tape input
                //  Bits 5 and 7 always 1
                finalKeys |= 160;
                return finalKeys;
            }
            else if ((port & 0xff) == 0x1f)
            {
                //  Kempston Joystick emulation
                //  Allow for cursor keys and gamepad to simulate joystick control
                KeyboardState keys = Keyboard.GetState();
                GamePadState pad =  GamePad.GetState(PlayerIndex.One);
                MouseState mouse = Mouse.GetState();
                int joystickState = 0;
                if (keys.IsKeyDown(Keys.Up) || pad.ThumbSticks.Left.Y > 0.1f)
                {
                    joystickState = joystickState | 8;
                }
                if (keys.IsKeyDown(Keys.Down) || pad.ThumbSticks.Left.Y < -0.1f)
                {
                    joystickState = joystickState | 4;
                }
                if (keys.IsKeyDown(Keys.Left) || pad.ThumbSticks.Left.X < -0.1f)
                {
                    joystickState = joystickState | 2;
                }
                if (keys.IsKeyDown(Keys.Right) || pad.ThumbSticks.Left.X > 0.1f)
                {
                    joystickState = joystickState | 1;
                }
                if (keys.IsKeyDown(Keys.LeftControl) || pad.IsButtonDown(Buttons.A) || mouse.LeftButton == ButtonState.Pressed)
                {
                    joystickState = joystickState | 16;
                }
                return joystickState;
            }
            else
            {
                //  Default value to return if port not recognised
                return 0;
            }
        }
    }
}
