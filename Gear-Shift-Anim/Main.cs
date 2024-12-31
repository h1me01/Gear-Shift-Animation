using GTA;
using GTA.Native;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Gear_Shifting_Anim
{
    public class ShiftAnim : Script
    {

        private ScriptSettings config;

        private Audio audio = new Audio();

        private float textPosX = 0.925f, textPosY = 0.85f;
        private int prevGear = -1;

        private bool printGearText = true;
        private bool isLeftHandDrive = true; // default is left hand drive

        //Sounds
        private float volume = 0;

        // MT
        private bool useMT = false;
        private bool printMTInfo = false;

        private FnGetBool isMTActive;
        private FnGetIntPtr MTVersion;
        private FnSetVoid ToggleMTSteeringAnimation;
        private FnGetBool MTNeutralGear;

        public ShiftAnim()
        {
            IntPtr mtLib = Dll.GetModuleHandle(@"Gears.asi");
            if (mtLib == IntPtr.Zero)
                Utility.Log("Couldn't get module handle.");
            else
                Utility.Log("Load Gears.asi success!");

            // init stuff
            try
            {
                config = ScriptSettings.Load(@"scripts\GShift\GearShiftingAnimation.ini");

                // mt integration
                useMT = config.GetValue<bool>("Options", "MTMode", true);
                printMTInfo = config.GetValue<bool>("Options", "MTInformationText", true);
                isMTActive = CheckAddr<FnGetBool>(mtLib, "MT_IsActive");
                MTVersion = CheckAddr<FnGetIntPtr>(mtLib, "MT_GetVersion");
                ToggleMTSteeringAnimation = CheckAddr<FnSetVoid>(mtLib, "MT_ToggleSteeringAnimation");
                MTNeutralGear = CheckAddr<FnGetBool>(mtLib, "MT_NeutralGear");

                if (isMTActive == null || MTVersion == null || ToggleMTSteeringAnimation == null || MTNeutralGear == null)
                {
                    // disable any MT integration
                    useMT = false;

                    if (useMT)
                        Utility.Log("Error initializing MT integration. Make sure you have everything properly installed.");
                    else
                        Utility.Log("Disabling MT integration");
                }

                if (useMT && printMTInfo)
                {
                    var strResult = Marshal.PtrToStringAnsi(MTVersion());
                    Utility.Log("MT Ver: " + strResult);
                    Utility.Log("MT Act: " + (isMTActive() ? "Yes" : "No"));
                }

                // ini options
                printGearText = config.GetValue<bool>("Options", "GearsText", true);

                // set to right hand drive if set to true
                if (config.GetValue<bool>("Options", "RightHandDrive", false))
                    isLeftHandDrive = false;

                volume = config.GetValue<float>("Sound", "Volume", 50f) / 100f;
            }
            catch (Exception ex)
            {
                Utility.Log("Error initializing: " + ex.Message);
            }

            Tick += Loop;
        }

        private void Loop(object source, EventArgs e)
        {
            Ped ped = Game.Player?.Character;
            if (ped == null || !ped.IsInVehicle())
                return;

            int currGear = ped.CurrentVehicle.CurrentGear;

            // print gear
            if (printGearText)
            {
                string gearText = "Gear: ";
                if (!useMT)
                    gearText += currGear;
                else if (MTNeutralGear()) // in neutral gear?
                    gearText += "Neutral";
                else
                    gearText += currGear;

                ShowText(textPosX, textPosY, gearText);
            }

            // no need to do anything if gear hasn't changed
            if (currGear == prevGear)
                return;

            // choose animation
            bool isCar = ped.CurrentVehicle.Model.IsCar;
            string animToPlay = "veh@driveby@first_person@";

            if (isCar)
                animToPlay += isLeftHandDrive ? "passenger_rear_right_handed@smg" : "passenger_rear_left_handed@smg";
            else
                animToPlay += "bike@driver@1h";

            Function.Call(Hash.REQUEST_ANIM_DICT, animToPlay, true);
            while (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, animToPlay, true))
                Wait(1);  // wait till animation is loaded into memory

            // play animation
            Function.Call(Hash.TASK_PLAY_ANIM, ped, animToPlay, "outro_0", 8.0, 8.0, 800, 50, 0, false, false, false);
            while (Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, ped, animToPlay, "outro_0", 3))
                Wait(1); // wait till animation ends

            if (useMT)
                ToggleMTSteeringAnimation(true);

            // update prevGear
            prevGear = currGear;

            // play sound
            string soundFile = isCar ? "gear" + currGear : "motorcycle";
            audio.init(soundFile);
            audio.play(volume);
        }

        // private helper functions

        private T CheckAddr<T>(IntPtr lib, string funcName) where T : class
        {
            IntPtr mtFunc = Dll.GetProcAddress(lib, funcName);
            if (mtFunc == IntPtr.Zero)
                return null;

            return Marshal.GetDelegateForFunctionPointer(mtFunc, typeof(T)) as T;
        }

        private void ShowText(float x, float y, string text, float size = 0.5f)
        {
            Function.Call(Hash.SET_TEXT_FONT, 0);
            Function.Call(Hash.SET_TEXT_SCALE, size, size);
            Function.Call(Hash.SET_TEXT_COLOUR, 255, 255, 255, 255);
            Function.Call(Hash.SET_TEXT_WRAP, 0.0, 1.0);
            Function.Call(Hash.SET_TEXT_CENTRE, 0);
            Function.Call(Hash.SET_TEXT_OUTLINE, true);
            Function.Call(Hash.BEGIN_TEXT_COMMAND_DISPLAY_TEXT, "STRING");
            Function.Call(Hash.ADD_TEXT_COMPONENT_SUBSTRING_PLAYER_NAME, text);
            Function.Call(Hash.END_TEXT_COMMAND_DISPLAY_TEXT, x, y);
        }

    }
}