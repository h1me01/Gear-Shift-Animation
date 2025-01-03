﻿using GTA;
using GTA.Native;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Gear_Shifting_Anim
{
    public class ShiftAnim : Script
    {
        private ScriptSettings config;

        private Audio audio = new Audio();

        private float textPosX = 0.925f, textPosY = 0.85f;
        private int prevGear = -1;

        // options
        private bool printGearText = true;
        private bool shiftWithLeg = false;
        private bool animMotorcycle = true;
        private bool useSound = true;
        
        private string rightHandAnimDict = "veh@driveby@first_person@passenger_rear_right_handed@smg";
        private string rightHandAnim = "outro_0";
        
        private string leftHandAnimDict = "veh@driveby@first_person@passenger_rear_left_handed@smg";
        private string leftHandAnim = "outro_0";

        private string motorcycleAnimDict = "veh@driveby@first_person@bike@driver@1h";
        private string motorcycleAnim = "outro_0";

        private string motorcycleLegAnimDict = "veh@bike@dirt@front@base";
        private string motorcycleLegAnim = "start_engine";
        
        private float volume = 0;

        // MT
        private bool useMT = false;
        private bool printMTInfo = false;

        private FnGetBool MT_IsActive;
        private FnGetIntPtr MT_GetVersion;
        private FnSetVoid MT_ToggleSteeringAnimation;
        private FnGetBool MT_NeutralGear;
        private FnGetInt MT_GetShiftMode;

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

                // access MT functions
                MT_IsActive = CheckAddr<FnGetBool>(mtLib, "MT_IsActive");
                MT_GetVersion = CheckAddr<FnGetIntPtr>(mtLib, "MT_GetVersion");
                MT_ToggleSteeringAnimation = CheckAddr<FnSetVoid>(mtLib, "MT_ToggleSteeringAnimation");
                MT_NeutralGear = CheckAddr<FnGetBool>(mtLib, "MT_NeutralGear");
                MT_GetShiftMode = CheckAddr<FnGetInt>(mtLib, "MT_GetShiftMode");

                // if one of the functions is null, disable MT integration
                if (MT_IsActive == null || MT_GetVersion == null || MT_ToggleSteeringAnimation == null || MT_NeutralGear == null || MT_GetShiftMode == null)
                {
                    useMT = false;

                    if (useMT)
                        Utility.Log("Error initializing MT integration. Make sure you have everything properly installed.");
                    else
                        Utility.Log("Disabling MT integration");
                }

                if (useMT && printMTInfo)
                {
                    var strResult = Marshal.PtrToStringAnsi(MT_GetVersion());
                    Utility.Log("MT Ver: " + strResult);
                    Utility.Log("MT Act: " + (MT_IsActive() ? "Yes" : "No"));
                }

                // ini options
                printGearText = config.GetValue<bool>("Options", "GearsText", true);
                shiftWithLeg = config.GetValue<bool>("Motorcycle", "ShiftWithLeg", false);
                animMotorcycle = config.GetValue("Motorcycle", "UseMotorcycleShiftingAnim", true);

                // animations
                rightHandAnimDict = config.GetValue("Animations", "RightHandAnimDict", rightHandAnimDict);
                rightHandAnim = config.GetValue("Animations", "RightHandAnim", rightHandAnim);
                
                leftHandAnimDict = config.GetValue("Animations", "LeftHandAnimDict", leftHandAnimDict);
                leftHandAnim = config.GetValue("Animations", "LeftHandAnim", leftHandAnim);

                motorcycleAnimDict = config.GetValue("Animations", "MotorcycleAnimDict", motorcycleAnimDict);
                motorcycleAnim = config.GetValue("Animations", "MotorcycleAnim", motorcycleAnim);

                motorcycleLegAnimDict = config.GetValue("Animations", "MotorcycleLegAnimDict", motorcycleLegAnimDict);
                motorcycleLegAnim = config.GetValue("Animations", "MotorcycleLegAnim", motorcycleLegAnim);

                // sounds
                useSound = config.GetValue("Sounds", "UseSound", true);
                volume = config.GetValue<float>("Sounds", "Volume", 50f) / 100f;
            }
            catch (Exception ex)
            {
                Utility.Log("Error initializing: " + ex.Message);
            }

            Tick += Loop;
        }

        private async void playAnim(Ped ped, int currGear)
        {
            if (useMT)
                MT_ToggleSteeringAnimation(false);

            string animDict, animToPlay;
            string soundFile = "gear" + currGear;
            Vehicle veh = ped.CurrentVehicle;
            bool isCar = veh.Model.IsCar;
            bool isLeftHand = isLeftHandDrive(veh);

            if (isCar)
            {
                animDict = isLeftHand ? leftHandAnimDict : rightHandAnimDict;
                animToPlay = isLeftHand ? leftHandAnim : rightHandAnim;
            }
            else
            {
                // if motorcycle anim isn't enabled, do nothing
                if (!animMotorcycle)
                    return;

                if (shiftWithLeg)
                {
                    animDict = motorcycleLegAnimDict;
                    animToPlay = motorcycleLegAnim;
                    soundFile = "MGearLeg";
                }
                else
                {
                    animDict = motorcycleAnimDict;
                    animToPlay = motorcycleAnim;
                    soundFile = "MGear";
                }
            }

            if (!Function.Call<bool>(Hash.HAS_ANIM_DICT_LOADED, animDict))
                Function.Call(Hash.REQUEST_ANIM_DICT, animDict);

            if (!Function.Call<bool>(Hash.IS_ENTITY_PLAYING_ANIM, ped, animDict, animToPlay, 3))
            {
                Function.Call(Hash.TASK_PLAY_ANIM, ped, animDict, animToPlay, 5f, -3f, -1);

                // play sound
                if (useSound)
                {
                    audio.init(soundFile);
                    audio.play(volume);
                }

                if (useMT)
                    await Task.Delay(1000);
            }
            else if (useMT)
                await Task.Delay(500);

            if (useMT)
                MT_ToggleSteeringAnimation(true);
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
                else if (MT_NeutralGear()) // in neutral gear?
                    gearText += "Neutral";
                else
                    gearText += currGear;

                ShowText(textPosX, textPosY, gearText);
            }

            // if using mt, check if in neutral gear, if so, set gear to -1
            if (useMT)
                if (MT_NeutralGear())
                    currGear = -1;

            // no need to update if gear is the same
            if (currGear == prevGear)
                return;

            playAnim(ped, currGear);

            // update prevGear
            prevGear = currGear;
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

        private bool isLeftHandDrive(Vehicle veh)
        {
            int driverSeatBoneIdx = Function.Call<int>(Hash.GET_ENTITY_BONE_INDEX_BY_NAME, veh, "seat_dside_f");
            GTA.Math.Vector3 driverSeatPos = Function.Call<GTA.Math.Vector3>(Hash.GET_WORLD_POSITION_OF_ENTITY_BONE, veh, driverSeatBoneIdx);
            GTA.Math.Vector3 driverSeatPosRel = Function.Call<GTA.Math.Vector3>(Hash.GET_OFFSET_FROM_ENTITY_GIVEN_WORLD_COORDS, veh, driverSeatPos.X, driverSeatPos.Y, driverSeatPos.Z);
            return !(driverSeatPosRel.X > 0.01f);
        }
    }
}