using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos.Streams;
using System.IO;
using NAudio.Wave;

namespace RazBot
{
    public static class YTE
    {
        public static SongData songDataCache;
        private static readonly YoutubeClient youtube = new YoutubeClient();

        private async static void GetVideoMetaDataAsync(string url, string requestor)
        {
            if (VideoExists(url))
            {
                for (int attempts = 3; attempts > 0; attempts--)
                {
                    try
                    {
                        var video = await youtube.Videos.GetAsync(url);

                        attempts = 0;
                        songDataCache = new SongData(video, requestor);
                    }
                    catch (YoutubeExplode.Exceptions.VideoUnavailableException e)
                    {
                        if (attempts == 1)
                        {
                            BotTools.LogLine($"Failed to get video data from YouTube");
                            BotTools.LogToSessionLogFile(e.ToString());
                            songDataCache = new SongData();
                            Music.LoadedSong = null;
                        }
                        else
                        {
                            BotTools.LogLine($"Failed to get video data from YouTube. Retrying {attempts-1} more times...");
                            continue;
                        }
                    }
                    catch (Exception e)
                    {
                        BotTools.LogLine($"Failed to get a video from ({url}), make sure the url is properly formatted!");
                        BotTools.LogToSessionLogFile(e.ToString());
                        songDataCache = new SongData();
                        attempts = 0;
                    }
                }
            }
            else
            {
                songDataCache = new SongData();
            }
        }

        public static SongData GetSongData(string url, string requestor = "RazBot")
        {
            songDataCache = null;
            try
            {
                GetVideoMetaDataAsync(url, requestor);
            }
            catch (Exception e)
            {
                BotTools.LogToSessionLogFile($"YTE::GetSongData -> \n{e}");
            }

            while (songDataCache == null) ;
            return songDataCache;
        }

        public async static void FillPlaylistFromYoutube(string url, string playlistName)
        {
            for (int attempts = 2; attempts > 0; attempts--)
            {
                try
                {
                    // Get playlist metadata
                    var playlist = await youtube.Playlists.GetAsync(url);
                    attempts = 0;

                    var title = playlist.Title;
                    var author = playlist.Author;

                    // Enumerate through playlist videos
                    foreach (var video in await youtube.Playlists.GetVideosAsync(playlist.Id))
                    {
                        try
                        {
                            Music.GetPlaylist(playlistName).AddSong(new SongData(video, "RazBot"));
                        }
                        catch (ArgumentException)
                        {
                            continue;
                        }
                    }
                }
                catch (Exception e)
                {
                    if (attempts == 0)
                    {
                        BotTools.LogLine($"Failed to set playlist data");
                        BotTools.LogToSessionLogFile(e.ToString());
                    }
                    else
                    {
                        BotTools.LogLine($"Failed to get playlist data from YouTube. Retrying {attempts} more times...");
                    }
                }
             }
        }

        public async static Task<SongData> DownloadAudio(SongData songData, IProgress<double> myProgress)
        {
            string playlistName = songData.Requestor == "RazBot" ? Music.LoadedPlaylist : "request";
            Directory.CreateDirectory($"{BotTools.BasePath}\\playlists\\{playlistName}");
            string relativePath = $"playlists\\{playlistName}\\{songData.ID}";
            string newPath = $"{BotTools.BasePath}\\{relativePath}.mp4";
            if (!File.Exists(newPath))
            {
                try
                {
                    var streamManifest = await youtube.Videos.Streams.GetManifestAsync(songData.ID);
                    var streamInfo = streamManifest.GetAudioOnly().Where(s => s.Container == YoutubeExplode.Videos.Streams.Container.Mp4).WithHighestBitrate();
                    if (streamInfo != null)
                    {
                        var stream = await Task.Run(() => youtube.Videos.Streams.GetAsync(streamInfo)).ConfigureAwait(false);
                        await Task.Run(async () => {
                            await youtube.Videos.Streams.DownloadAsync(streamInfo, newPath, progress: myProgress);
                        });
                    }
                }
                catch (YoutubeExplode.Exceptions.VideoUnplayableException e)
                {
                    BotTools.LogToSessionLogFile(e.ToString());
                    BotTools.LogLine($"Downloading \"{Music.QueuedSong.Title}\" failed: Video unavailable");
                    if (Music.QueuedSong.IsRequest)
                    {
                        TwitchBot.SendMessage($"Sorry @{Music.QueuedSong.Requestor}, there was a problem retreiving that video!");
                    }
                }
                catch (Exception e)
                {
                    BotTools.LogToSessionLogFile(e.ToString());
                    BotTools.LogLine($"The requested audio couldn't be downloaded from YouTube");
                    Music.QueuedSong = null;
                }
            }
            else
            {
                myProgress.Report(1.0d);
            }
            return Music.QueuedSong;
        }

        private static bool VideoExists(string videoUrl)
        {
            var url = $"http://www.youtube.com/oembed?url={videoUrl}&format=json";
            var json = WebTools.GetJObjectResponse(url);
            return json != null && json.HasValues;
        }
    }

    public static class NAudioTools
    {
        public static void Normalize(string relativeFilePath)
        {
            var inPath = $"{BotTools.BasePath}\\{relativeFilePath}.wav";
            var outPath = $"{BotTools.BasePath}\\{relativeFilePath}_Normalized.wav";
            float max = 0;

            using (var reader = new AudioFileReader(inPath))
            {
                // find the max peak
                float[] buffer = new float[reader.WaveFormat.SampleRate];
                int read;
                do
                {
                    read = reader.Read(buffer, 0, buffer.Length);
                    for (int n = 0; n < read; n++)
                    {
                        var abs = Math.Abs(buffer[n]);
                        if (abs > max) max = abs;
                    }
                } while (read > 0);
                Console.WriteLine($"Max sample value: {max}");

                if (max == 0 || max > 1.0f)
                    throw new InvalidOperationException("File cannot be normalized");

                // rewind and amplify
                reader.Position = 0;
                reader.Volume = 1.0f / max;
                Console.WriteLine($"File volume adjusted to: {reader.Volume}");

                // write out to a new WAV file
                WaveFileWriter.CreateWaveFile16(outPath, reader);
            }
        }

        public static void Mp4ToWav(string relativeFilePath)
        {
            //DeleteAllWavs();
            var inPath = $"{BotTools.BasePath}\\{relativeFilePath}.mp4";
            var outPath = $"{BotTools.BasePath}\\{relativeFilePath}.wav";

            using (MediaFoundationReader reader = new MediaFoundationReader(inPath))
            {
                using (WaveStream pcmStream = WaveFormatConversionStream.CreatePcmStream(reader))
                {
                    WaveFileWriter.CreateWaveFile(outPath, pcmStream);
                }
            }
        }

        public static void WebmToWav(string relativeFilePath)
        {
            //DeleteAllWavs();
            var inPath = $"{BotTools.BasePath}\\{relativeFilePath}.webm";
            var outPath = $"{BotTools.BasePath}\\{relativeFilePath}.wav";

            using (MediaFoundationReader reader = new MediaFoundationReader(inPath))
            {
                using (WaveStream pcmStream = WaveFormatConversionStream.CreatePcmStream(reader))
                {
                    WaveFileWriter.CreateWaveFile(outPath, pcmStream);
                }
            }
        }

        public static void WavToMp4(string relativePath)
        {
            using (var reader = new MediaFoundationReader($"{BotTools.BasePath}\\{relativePath}.wav"))
            {
                MediaFoundationEncoder.EncodeToAac(reader, $"{BotTools.BasePath}\\{relativePath}.mp4");
            }
        }

        public static void DeleteAllWavs()
        {
            DirectoryInfo rootDir = new DirectoryInfo($"{BotTools.BasePath}\\playlists");
            foreach (DirectoryInfo subDir in rootDir.EnumerateDirectories())
            {
                FileInfo[] files = subDir.GetFiles("*.wav")
                                 .Where(p => p.Extension == ".wav").ToArray();
                foreach (FileInfo file in files)
                {
                    try
                    {
                        file.Attributes = FileAttributes.Normal;
                        File.Delete(file.FullName);
                    }
                    catch { }
                }
            }
        }

        public static void DeleteAllMp4s()
        {
            DirectoryInfo rootDir = new DirectoryInfo($"{BotTools.BasePath}\\playlists");
            foreach (DirectoryInfo subDir in rootDir.EnumerateDirectories())
            {
                FileInfo[] files = subDir.GetFiles("*.mp4")
                                 .Where(p => p.Extension == ".mp4").ToArray();
                foreach (FileInfo file in files)
                {
                    try
                    {
                        file.Attributes = FileAttributes.Normal;
                        File.Delete(file.FullName);
                    }
                    catch { }
                }
            }
        }
    }

    public class SongData
    {
        public ThumbnailSet Thumbnails { get; set; }
        public string Description { get; set; }
        public TimeSpan Duration { get; set; }
        public string Requestor { get; set; }
        public string Author { get; set; }
        public string Title { get; set; }
        public string URL { get; set; }
        public string ID { get; set; }
        public bool IsEmpty => Title == null;
        public bool IsRequest => Requestor != null && Requestor != "RazBot";

        public SongData(){ Title = null; }

        public SongData(string url, string requestor)
        {
            SongData songData = YTE.GetSongData(url, requestor: requestor);
            Description = songData.Description;
            Thumbnails = songData.Thumbnails;
            Requestor = songData.Requestor;
            Duration = songData.Duration;
            Author = songData.Author;
            Title = songData.Title;
            URL = songData.URL;
            ID = songData.ID;
        }

        public SongData (YoutubeExplode.Videos.Video video, string requestor)
        {
            Description = video.Description;
            Thumbnails = video.Thumbnails;
            Requestor = requestor;
            Duration = video.Duration;
            URL = (string)video.Url;
            Author = video.Author;
            Title = video.Title;
            ID = video.Id;
        }

        public SongData(Dictionary<string, dynamic> metaData)
        {
            Description = metaData["Description"];
            Thumbnails = metaData["Thumbnails"];
            Requestor = metaData["Requestor"];
            Duration = metaData["Duration"];
            URL = (string)metaData["URL"];
            Author = metaData["Author"];
            Title = metaData["Title"];
            ID = metaData["ID"];
        }

        public SongData(SongData songData)
        {
            Description = songData.Description;
            Thumbnails = songData.Thumbnails;
            Requestor = "RazBot";
            Duration = songData.Duration;
            URL = songData.URL;
            Author = songData.Author;
            Title = songData.Title;
            ID = songData.ID;
        }

        public bool Equals(SongData other)
        {
            if (other != null)
            {
                return this.ID == other.ID;
            }
            else
            {
                return false;
            }
        }
    }
}