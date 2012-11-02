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
        SpriteBatch spriteBatch;
        Z80 z80;
        int[] Memory;

        Texture2D pixel;
        Color[] colors;
        int border;
        bool earActive, micActive;
        long framesRendered = 0;
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
            pixel = new Texture2D(game.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            pixel.SetData<Color>(new[] { Color.White });

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

            //  The ULA renders a single line of pixels every 224 T-states
            //  64 lines of which at least 48 are top border
            //  192 screen + left/right border lines
            //  56 bottom border lines
            //  (64+192+56) * 224 = 69888 T-states per frame
            //  3.5MHz (Z80A) / 69888 = 50.08Hz interrupt
            //  Update and Draw at 50 fps (every 20 ms) - best equivalent to the Spectrum ULA updating a TV display 
            Game.IsFixedTimeStep = true;
            Game.TargetElapsedTime = new TimeSpan(0, 0, 0, 0, 20);

            //  16K ROM, 48K RAM
            Memory = new int[65536];

            //  The Z80
            z80 = new Z80(3.5f, Memory);

            //  ULA handles all I/O
            //  The ULA is generally addressed at port 0xFE
            //  But responds to all even-numbered ports
            //  The high byte of the address is also used to select keyboard half-rows
            z80.AddDevice(this);
           
            LoadROM();
        }

        /// <summary>
        /// Loads a rom into low memory.
        /// </summary>
        private void LoadROM()
        {
            using (FileStream fs = File.Open(Game.Content.RootDirectory + "\\48", FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader br = new BinaryReader(fs))
                {
                    byte[] rom = br.ReadBytes((int)fs.Length);
                    Array.Copy(rom, Memory, rom.Length);
                }
            }
            //  ZEXALL

            //using (FileStream fs = File.Open(Game.Content.RootDirectory + "\\zexall", FileMode.Open, FileAccess.Read))
            //{
            //    using (BinaryReader br = new BinaryReader(fs))
            //    {
            //        byte[] rom = br.ReadBytes((int)fs.Length);
            //        Array.Copy(rom, 0, Memory, 0x8000, rom.Length);
            //    }
            //}
        }

        /// <summary>
        /// Allows the game component to update itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        public override void Update(GameTime gameTime)
        {
            //  Generate the 50Hz clock interrupt
            z80.Interrupt();
            //  Run the cpu for a frame's worth of T-states
            z80.Run(false, 69888);
            base.Update(gameTime);
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
            int borderHeight = 56;
            int borderWidth = 48;
            int screenHeight = 192;
            int screenWidth = 256;
            //  Top border
            for (int y = 0; y < borderHeight; y++)
            {
                for (int x = 0; x < (borderWidth * 2) + screenWidth; x ++)
                {
                    spriteBatch.Draw(pixel, new Vector2(x, y), colors[border]);
                }
            }
            //  Bottom border
            for (int y = borderHeight + screenHeight; y < (borderHeight * 2) + screenHeight; y++)
            {
                for (int x = 0; x < (borderWidth * 2) + screenWidth; x++)
                {
                    spriteBatch.Draw(pixel, new Vector2(x, y), colors[border]);
                }
            }
            //  Left border
            for (int y = borderHeight; y < borderHeight + screenHeight; y++)
            {
                for (int x = 0; x < borderWidth; x++)
                {
                    spriteBatch.Draw(pixel, new Vector2(x, y), colors[border]);
                }
            }
            //  Right border
            for (int y = borderHeight; y < borderHeight + screenHeight; y++)
            {
                for (int x = borderWidth + screenWidth; x < (borderWidth*2) + screenWidth; x++)
                {
                    spriteBatch.Draw(pixel, new Vector2(x, y), colors[border]);
                }
            }
            //  Main screen display
            for (int line = 0; line < 24; line++)
            {
                for (int cha = 0; cha < 32; cha++)
                {
                    //  Attribute (color) map stored from 22528
                    int attribute = Memory[22528 + 32 * line + cha];
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
                    //  Bit 7 sets flashing
                    if ((attribute & 128) == 128 && framesRendered % 32 <= 15)
                    {
                        ink = ink ^ paper;
                        paper = ink ^ ink;
                        ink = ink ^ paper;
                    }

                    //  Pixel by pixel is very inefficient...
                    for (int row = 0; row < 8; row++)
                    {
                        int pixels = Memory[16384 + 2048 * (line / 8) + 32 * (line - 8 * (line / 8)) + 256 * row + cha];
                        int y = line * 8 + row + borderHeight;
                        int startX = cha * 8 + borderWidth;

                        int x = 0;
                        for (int i = 128; i > 0; i /= 2, x++)
                        {
                            if ((pixels & i) == i)
                            {
                                spriteBatch.Draw(pixel, new Vector2(startX + x, y), colors[ink]);
                            }
                            else
                            {
                                spriteBatch.Draw(pixel, new Vector2(startX + x, y), colors[paper]);
                            }

                        }
                    }
                }
            }
            
            spriteBatch.End();
            framesRendered++;
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
                    if (keys.IsKeyDown(Keys.D5)) keyLine -= 16;
                    finalKeys &= keyLine;
                }
                if ((high & 16) == 0)
                {
                    if (keys.IsKeyDown(Keys.D0)) keyLine -= 1;
                    if (keys.IsKeyDown(Keys.D9)) keyLine -= 2;
                    if (keys.IsKeyDown(Keys.D8)) keyLine -= 4;
                    if (keys.IsKeyDown(Keys.D7)) keyLine -= 8;
                    if (keys.IsKeyDown(Keys.D6)) keyLine -= 16;
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
                finalKeys |= 224;
                return finalKeys;
            }
            else
            {
                //  Default value to return if port not recognised
                return 0;
            }
        }
    }
}
