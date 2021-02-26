using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Commands;
using System.Reflection;
using System.IO;
using Discord.Audio;
using System.Diagnostics;
using NAudio.Wave;
using NAudio.MediaFoundation;

namespace BigMohammadBot.Modules
{
    public class Record : ModuleBase<SocketCommandContext>
    {
        private SocketVoiceChannel connectedChannel;

        private Dictionary<ulong, Process> ffmpegProcesses = new Dictionary<ulong, Process>();

        private async Task HandleStreamCreated(ulong id, AudioInStream stream)
        {
            Console.WriteLine("Stream was created for " + id);
            //await Task.Delay(1000 * 30);
            //await connectedChannel.DisconnectAsync();

            //using (FileStream outputFileStream = new FileStream("Recordings/" + id + ".txt", FileMode.Create))
            //{
            //    await stream.CopyToAsync(outputFileStream);
            //    outputFileStream.Close();
            //}

            ffmpegProcesses[id] = CreateFfmpegOut("Recordings/" + id + ".wav");
            using (var ffmpegOutStdinStream = ffmpegProcesses[id].StandardInput.BaseStream)
            {
                try
                {
                    var buffer = new byte[3840];
                    while (await stream.ReadAsync(buffer, 0, buffer.Length) > 0)
                    {
                        await ffmpegOutStdinStream.WriteAsync(buffer, 0, buffer.Length);
                        await ffmpegOutStdinStream.FlushAsync();
                        Console.WriteLine(System.Text.Encoding.Default.GetString(buffer));
                    }
                }
                finally
                {
                    await ffmpegOutStdinStream.FlushAsync();
                }
            }
        }

        private async Task HandleStreamDestroyed(ulong id)
        {
            await ffmpegProcesses[id].StandardInput.BaseStream.FlushAsync();
            ffmpegProcesses[id].StandardInput.BaseStream.Close();
            ffmpegProcesses[id].Close();
            Console.WriteLine("Finished writing " + id);
        }

        private Process CreateStream(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                Arguments = $"-loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }
        private Process CreateFfmpegOut(string savePath)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg.exe",
                Arguments = $"-hide_banner -loglevel panic -ac 2 -f s16le -ar 48000 -i pipe:0 -acodec pcm_u8 -ar 22050 \"{savePath}\"",
                // Minimal version for piping etc
                //Arguments = $"-c 2 -f S16_LE -r 44100",
                UseShellExecute = false,
                RedirectStandardInput = true,
            });
        }

        [Command("record", RunMode = RunMode.Async)]
        public async Task Task1(SocketVoiceChannel connectingChannel)
        {
            if (Globals.AdminUserIds.Contains(Context.Message.Author.Id)) // admin command
            {
                IAudioClient audioClient = await connectingChannel.ConnectAsync();
                connectedChannel = connectingChannel;
                audioClient.StreamCreated += HandleStreamCreated;
                audioClient.StreamDestroyed += HandleStreamDestroyed;
                foreach (var pair in audioClient.GetStreams())
                {
                    HandleStreamCreated(id: pair.Key, stream: pair.Value);
                }

                //var format = new WaveFormat(48000, 1);
                //var waveStream = new RawSourceWaveStream("asdasd");
                //var reader = new Mp3FileReader("Data/WalkingMoon.mp3");
                //var naudio = WaveFormatConversionStream.CreatePcmStream(reader);
                //var stream = audioClient.CreatePCMStream(AudioApplication.Music, 64000);
                //await naudio.CopyToAsync(stream);
                //await stream.FlushAsync();

                using (var ffmpeg = CreateStream("Data/RecordingStarted.mp3"))
                using (var output = ffmpeg.StandardOutput.BaseStream)
                using (var stream = audioClient.CreatePCMStream(AudioApplication.Music, 64000))
                {
                    await output.CopyToAsync(stream);
                    await stream.FlushAsync();
                    Console.WriteLine("Spoken!");
                } 
            }
        }
    }
}
