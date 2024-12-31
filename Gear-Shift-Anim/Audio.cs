using GTA.Native;
using NAudio.Wave;
using System.Diagnostics;
using System;
using System.IO;

namespace Gear_Shifting_Anim
{
    class Audio
    {
        private DirectSoundOut output;
        private WaveFileReader reader;  
        private WaveChannel32 streaming;

        public void init(string soundFile)
        {
            string path = "scripts/GShift/sounds/" + soundFile + ".wav";

            if (!File.Exists(path))
            {
                Utility.Log("Failed to load " + soundFile + ". Make sure you installed all the files needed.");
                return;
            }

            try
            {
                reader = new WaveFileReader(path);
                streaming = new WaveChannel32(reader);
                output = new DirectSoundOut();
                output.Init(streaming);

                //Utility.Log(path);
            }
            catch (Exception ex)
            {
                Utility.Log("Error initializing audio: " + ex.Message);
            }
        }

        public void play(float volume)
        {
            if (output == null || streaming == null)
            {
                Utility.Log("Output or streaming is null. Cannot play sound.");
                return;
            }

            // increase sound if in first person
            if (Function.Call<bool>(Hash.GET_FOLLOW_PED_CAM_VIEW_MODE, 4))
                streaming.Volume = volume + 0.2f;
            else
                streaming.Volume = volume - 0.2f;

            output.Play();
        }
    }
}
