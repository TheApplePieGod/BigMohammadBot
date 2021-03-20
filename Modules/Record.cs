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

namespace BigMohammadBot.Modules
{
    public class Record : ModuleBase<SocketCommandContext>
    {
        private static SocketVoiceChannel connectedChannel;

        private static Dictionary<ulong, Process> ffmpegProcesses = new Dictionary<ulong, Process>();
        private static long currentId = 0;

        private async Task HandleStreamCreated(ulong id, AudioInStream stream)
        {
            var User = await Context.Client.Rest.GetGuildUserAsync(Globals.MohammadServerId, id);
            if (User != null && User.IsBot)
                return;

            Console.WriteLine("Stream was created for " + id);

            //using (FileStream outputFileStream = new FileStream("Recordings/" + id + ".txt", FileMode.Create))
            //{
            //    await stream.CopyToAsync(outputFileStream);
            //    outputFileStream.Close();
            //}

            ffmpegProcesses[id] = CreateFfmpegOut("Recordings/" + id + " - " + currentId + ".wav");
            using (var ffmpegStream = ffmpegProcesses[id].StandardInput.BaseStream)
            using (var ffmpegMainStream = ffmpegProcesses[0].StandardInput.BaseStream)
            {
                var buffer = new byte[3840];
                while (await stream.ReadAsync(buffer, 0, buffer.Length) > 0)
                {
                    await ffmpegStream.WriteAsync(buffer, 0, buffer.Length);
                    await ffmpegStream.FlushAsync();

                    await ffmpegMainStream.WriteAsync(buffer, 0, buffer.Length);
                    await ffmpegMainStream.FlushAsync();

                    //Console.WriteLine(System.Text.Encoding.Default.GetString(buffer));
                }
            }
        }

        private async Task HandleStreamDestroyed(ulong id)
        {
            if (id != 0)
            {
                var User = await Context.Client.Rest.GetGuildUserAsync(Globals.MohammadServerId, id);
                if (User != null && User.IsBot)
                    return;
            }

            await ffmpegProcesses[id].StandardInput.BaseStream.FlushAsync();
            ffmpegProcesses[id].StandardInput.BaseStream.Close();
            ffmpegProcesses[id].Close();
            Console.WriteLine("Finished writing " + id);
        }

        private async Task Disconnected(Exception e)
        {
            await HandleStreamDestroyed(0);
        }

        private Process CreateStream(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }

        private Process CreateFfmpegOut(string savePath)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
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
            if (Globals.AdminUserIds.Contains(Context.Message.Author.Id))
            {
                IAudioClient audioClient = await connectingChannel.ConnectAsync();
                connectedChannel = connectingChannel;
                currentId = DateTimeOffset.Now.ToUnixTimeSeconds();
                ffmpegProcesses[0] = CreateFfmpegOut("Recordings/" + "0 - " + currentId + ".wav");
                audioClient.StreamCreated += HandleStreamCreated;
                audioClient.StreamDestroyed += HandleStreamDestroyed;
                audioClient.Disconnected += Disconnected;
                foreach (var pair in audioClient.GetStreams())
                {
                    HandleStreamCreated(id: pair.Key, stream: pair.Value);
                }


                using (var ffmpeg = CreateStream("Data/WalkingMoon.mp3"))
                using (var output = ffmpeg.StandardOutput.BaseStream)
                using (var stream = audioClient.CreatePCMStream(AudioApplication.Music, 64000))
                {
                    await output.CopyToAsync(stream);
                    await stream.FlushAsync();
                    Console.WriteLine("Spoken!");
                }
            }
        }

        [Command("stop")]
        public async Task Task2()
        {
            if (Globals.AdminUserIds.Contains(Context.Message.Author.Id))
            {
                await connectedChannel.DisconnectAsync();
                foreach (var pair in ffmpegProcesses)
                {
                    HandleStreamDestroyed(pair.Key);
                }
            }
        }

        //[Command("download")]
        //public async Task Task3(int recordingId)
        //{
        //    var GuildUser = Context.Guild.GetUser(Context.User.Id);
        //    if (!GuildUser.GuildPermissions.Administrator && !Globals.AdminUserIds.Contains(Context.Message.Author.Id))
        //        throw new Exception("You do not have permission to run that command");
        //    else
        //    {
                
        //    }
        //}
    }
}
