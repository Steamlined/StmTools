using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using MenuAPI;
using static CitizenFX.Core.Native.API;

namespace StmTools {
    public class StmTools : BaseScript {
        Menu menu;

        bool doRainbowPri = false;
        bool doRainbowSec = false;
        float rainbowSaturation = 1;
        float rainbowValue = 1;
        float rainbowOffset = 180;
        float rainbowSlowRate = 50;
        public StmTools() {
            Tick += OnTick;
            RegisterCommand("stm", new Action<int, List<object>, string>(/*async*/ (src, args, raw) => {
                menu.OpenMenu();
            }
             ), false);

            //Menu stuff
            MenuController.MenuAlignment = MenuController.MenuAlignmentOption.Right;
            MenuController.MenuToggleKey = (Control)(-1);
            menu = new Menu("Stm Tools", "For all your pointless needs!");
            MenuController.AddMenu(menu);
            MenuCheckboxItem menuCheckboxRainbow = new MenuCheckboxItem("Primary Rainbow Mode", "Enables cycling paint for primary zone on your current vehicle (while you're inside it).") { Enabled = true, LeftIcon = MenuItem.Icon.HEALTH_HEART, Style = MenuCheckboxItem.CheckboxStyle.Tick };
            MenuCheckboxItem menuCheckboxRainbowSec = new MenuCheckboxItem("Secondary Rainbow Mode", "Enables cycling paint for secondary zone on your current vehicle (while you're inside it).") { Enabled = true, LeftIcon = MenuItem.Icon.HEALTH_HEART, Style = MenuCheckboxItem.CheckboxStyle.Tick };
            MenuSliderItem sliderRainbowSaturation = new MenuSliderItem("Saturation", "Base Saturation value for the cycled colour", 0, 20, 20);
            MenuSliderItem sliderRainbowValue = new MenuSliderItem("Value", "Base Value... Value for the cycled colour. (Not lightness!)", 0, 20, 20);
            MenuSliderItem sliderRainbowRate = new MenuSliderItem("Speed", "Speed at which the colour cycles.", 1, 21, 11);
            MenuSliderItem sliderRainbowOffset = new MenuSliderItem("Secondary Offset", "How far off the hue for the secondary colour should be, relative to the primary.", 0, 36, 18);
            menu.AddMenuItem(menuCheckboxRainbow);
            menu.AddMenuItem(menuCheckboxRainbowSec);
            menu.AddMenuItem(sliderRainbowSaturation);
            menu.AddMenuItem(sliderRainbowValue);
            menu.AddMenuItem(sliderRainbowRate);
            menu.AddMenuItem(sliderRainbowOffset);

            menu.OnSliderPositionChange += Menu_OnSliderPositionChange;
            menu.OnCheckboxChange += Menu_OnCheckboxChange;

            void Menu_OnCheckboxChange(Menu menu, MenuCheckboxItem menuItem, int itemIndex, bool newCheckedState) {
                Debug.WriteLine($"Checkbox {menuItem.Label} set to {newCheckedState}");
                if (menuItem == menuCheckboxRainbow) {
                    doRainbowPri = newCheckedState;

                }
                if (menuItem == menuCheckboxRainbowSec) {
                    doRainbowSec = newCheckedState;
                }
            }

            void Menu_OnSliderPositionChange(Menu menu, MenuSliderItem sliderItem, int oldPosition, int newPosition, int itemIndex) {
                Debug.WriteLine($"Slider {sliderItem.Label} moved to value {newPosition}");
                if (sliderItem == sliderRainbowRate) rainbowSlowRate = (-(float)newPosition + 21) * 10;
                if (sliderItem == sliderRainbowSaturation) rainbowSaturation = (float)newPosition / (float)sliderItem.Max;
                if (sliderItem == sliderRainbowValue) rainbowValue = (float)newPosition / (float)sliderItem.Max;
                if (sliderItem == sliderRainbowOffset) rainbowOffset = newPosition * 10;
            }
        }



        private async Task OnTick() {
            if (doRainbowPri || doRainbowSec) {
                var vehicle = Game.PlayerPed.CurrentVehicle;
                if (vehicle == null) { doRainbowPri = doRainbowSec = false; return; }
                if (doRainbowPri) vehicle.Mods.CustomPrimaryColor = HsvToRgb(Game.GameTime / rainbowSlowRate % 360f, rainbowSaturation, rainbowValue);
                if (doRainbowSec) vehicle.Mods.CustomSecondaryColor = HsvToRgb((Game.GameTime / rainbowSlowRate % 360f) + rainbowOffset, rainbowSaturation, rainbowValue);
                await Delay(100);
            }
        }

        public static void SendChatMessage(string message, string title = "StmTools", int r = 0, int g = 128, int b = 255) {
            var msg = new Dictionary<string, object> {
                ["color"] = new[] { r, g, b },
                ["args"] = new[] { title, message }
            };
            TriggerEvent("chat:addMessage", msg);
        }

        Color HsvToRgb(float H, float S, float V) {
            int r, g, b;
            while (H < 0) { H += 360; };
            while (H >= 360) { H -= 360; };
            float R, G, B;
            if (V <= 0) { R = G = B = 0; }
            else if (S <= 0) {
                R = G = B = V;
            }
            else {
                float hf = H / 60;
                int i = (int)Math.Floor(hf);
                float f = hf - i;
                float pv = V * (1 - S);
                float qv = V * (1 - S * f);
                float tv = V * (1 - S * (1 - f));
                switch (i) {

                    // Red is the dominant color

                    case 0:
                        R = V;
                        G = tv;
                        B = pv;
                        break;

                    // Green is the dominant color

                    case 1:
                        R = qv;
                        G = V;
                        B = pv;
                        break;
                    case 2:
                        R = pv;
                        G = V;
                        B = tv;
                        break;

                    // Blue is the dominant color

                    case 3:
                        R = pv;
                        G = qv;
                        B = V;
                        break;
                    case 4:
                        R = tv;
                        G = pv;
                        B = V;
                        break;

                    // Red is the dominant color

                    case 5:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // Just in case we overshoot on our math by a little, we put these here. Since its a switch it won't slow us down at all to put these here.

                    case 6:
                        R = V;
                        G = tv;
                        B = pv;
                        break;
                    case -1:
                        R = V;
                        G = pv;
                        B = qv;
                        break;

                    // The color is not defined, we should throw an error.

                    default:
                        //LFATAL("i Value error in Pixel conversion, Value is %d", i);
                        R = G = B = V; // Just pretend its black/white
                        break;
                }
            }
            r = Clamp((int)(R * 255.0));
            g = Clamp((int)(G * 255.0));
            b = Clamp((int)(B * 255.0));
            return Color.FromArgb(r, g, b);
        }

        /// <summary>
        /// Clamp a value to 0-255
        /// </summary>
        int Clamp(int i) {
            if (i < 0) return 0;
            if (i > 255) return 255;
            return i;
        }
    }
}
