using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace RazBot
{
    static class Music
    {
        public static Dictionary<string, Playlist> Playlists { get; set; }
        public static string LoadedPlaylist { get; set; }
        public static SongData QueuedSong { get; set; }
        public static SongData DownloadedSong { get; set; }
        public static SongData LoadedSong { get; set; }

        static Music()
        {
            Playlists = LoadPlaylists();
            LoadedPlaylist = BotTools.Settings["loaded_playlist"];
            foreach (var playlist in Playlists)
            {
                Directory.CreateDirectory($"{BotTools.BasePath}\\playlists\\{playlist.Key}");
            }
        }

        private static Dictionary<string, Playlist> LoadPlaylists()
        {
            string filePath = $"{BotTools.BasePath}\\resources\\playlists.json";
            if (File.Exists(filePath))
            {
                var pl = BotTools.ReadFromJson<Dictionary<string, Playlist>>("resources\\playlists");
                return pl;
            }
            else
            {
                var newPL = new Dictionary<string, Playlist>()
                {
                    ["request"] = new Playlist(),
                    ["default"] = new Playlist()
                };
                BotTools.WriteToJson(newPL, "resources\\playlists");
                return newPL;
            }
        }

        public static void QueueNextSong()
        {
            if (BotTools.Settings["random_play"] == true)
            {
                Random rand = new Random();
                var playlistCandidates = Playlists.Where(p => p.Value.Songs.Count > 0);
                Music.LoadedPlaylist = playlistCandidates.ElementAt(rand.Next(playlistCandidates.Count())).Key;
                BotTools.Settings["loaded_playlist"] = Music.LoadedPlaylist;
                BotTools.SaveSettings();
            }

            DownloadedSong = QueuedSong;
            SongData nextSong = GetPlaylist("request").Songs.Count > 0 ?
                                GetPlaylist("request").GetNext() :
                                GetPlaylist(LoadedPlaylist).GetNext();

            if (nextSong != null)
            {
                QueuedSong = YTE.GetSongData(nextSong.URL, requestor: nextSong.Requestor);
            }
        }

        public static void SavePlaylists()
        {
            BotTools.WriteToJson(Playlists, "resources\\playlists");
        }

        public static Playlist GetPlaylist(string playlistName)
        {
            if (Playlists.ContainsKey(playlistName))
            {
                return Playlists[playlistName];
            }
            else
            {
                Playlists[playlistName] = new Playlist(new List<SongData>());
                return Playlists[playlistName];
            }
        }

        public static void AddPlaylist(string playlistName, Playlist playlist)
        {
            if (!Playlists.ContainsKey(playlistName))
            {
                Playlists[playlistName] = playlist;
                SavePlaylists();
            }
        }

        public static void ClearRequests()
        {
            DirectoryInfo dir = new DirectoryInfo($"{BotTools.BasePath}\\playlists\\request");
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                try
                {
                    file.Attributes = FileAttributes.Normal;
                    File.Delete(file.FullName);
                }
                catch { }
            }
            Music.Playlists["request"].Songs.Clear();
            if (DownloadedSong.IsRequest) DownloadedSong = null;
            Music.SavePlaylists();
        }

        public static string FillSongRequest(string requestBody, string songRequestor = "RazBot", bool bypassVet = false)
        {
            SongData songData = YTE.GetSongData(requestBody, requestor: songRequestor);

            if (songData.Duration <= TimeSpan.FromMinutes(10))
            {
                if (BotTools.Settings["vet_requests"] && !bypassVet)
                {
                    DiscordBot.PostVetRequestToDJChannel(songData);
                    return $"@{songData.Requestor}, Your request has been submitted for review! razBot";
                }
                else
                {
                    try
                    {
                        Music.GetPlaylist("request").AddSong(songData);
                        var placement = Music.GetPlaylist("request").Songs.Count;
                        if (DownloadedSong != null && placement == 1)
                        {
                            if (DownloadedSong.IsRequest)
                            {
                                placement++;
                            }
                            else
                            {
                                DownloadedSong = null;
                                GetPlaylist(LoadedPlaylist).CycleBack();
                            }
                        }
                        var placementWord = BotTools.GetPlacementWord(placement);
						
                        Music.SavePlaylists();
                        return $"@{songData.Requestor}, Your request is {placementWord} in the queue! razCool";
                    }
                    catch (ArgumentException)
                    {
                        BotTools.LogLine($"{songRequestor}'s request rejected: Already in queue");
                        return $"Sorry @{songRequestor}, that request is already queued!";
                    }
                    catch (FormatException e)
                    {
                        BotTools.LogToSessionLogFile(e.ToString());
                        BotTools.LogLine($"{songRequestor}'s request rejected: Bad url");
                        return $"Sorry @{songRequestor}, I can't access a YouTube video from that link! It may not be available in my region, or the url may not be properly formatted.";
                    }
                }
            }
            else
            {
                return $"{songData.Requestor}'s request was rejected: The audio was over 10 minutes long! razS";
            }
        }
    }


    public class Playlist
    {
        public List<SongData> Songs { get; set; }
        public bool Persistent { get; set; }
        public string Name { get; set; }

        public Playlist()
        {
            Songs = new List<SongData>();
            Persistent = true;
            Name = "playlist";
        }

        public Playlist(List<SongData> songs, bool persistent=true, string name="unnamed")
        {
            Songs = songs;
            Persistent = persistent;
            Name = name;
        }

        public SongData GetNext()
        {
            if (Songs.Count > 0)
            {
                var song = Songs.First();
                if (Persistent)
                {
                    Songs.Add(song);
                }
                Songs.RemoveAt(0);
                Music.SavePlaylists();
                return song;

            }
            else
            {
                return null;
            }
        }

        public SongData GetSong(string title)
        {
            return Songs.FirstOrDefault(s => s.Title == title);
        }

        public void CycleBack()
        {
            if (Songs.Count > 1)
            {
                Songs.Insert(0, Songs.Last());
                Songs.RemoveAt(Songs.Count - 1);
                Music.SavePlaylists();
            }
        }

        public bool ContainsSongWithID(string id)
        {
            return Songs.Any(s => s.ID == id);
        }

        public bool ContainsSong(SongData songData)
        {
            return Songs.Any(s => s.ID == songData.ID);
        }

        public void AddSong(SongData songData)
        {
            if (!songData.IsEmpty)
            {
                if (!ContainsSongWithID(songData.ID))
                {
                    Songs.Add(songData);
                    Music.SavePlaylists();
                    return;
                }
                else
                {
                    throw new ArgumentException();
                }
            }
            throw new FormatException();
        }

        public void RemoveSong(int index)
        {
            if (index >= 0)
            {
                Songs.RemoveAt(Math.Min(index, Songs.Count - 1));
            }
            else
            {
                Songs.RemoveAt(Math.Max(0, Songs.Count + index));
            }
            Music.SavePlaylists();
        }

        public void RemoveSong(SongData songData)
        {
            var idx = Songs.FindIndex(s => s.ID == songData.ID);
            if (idx >= 0)
            {
                BotTools.LogLine($"Removing \"{songData.Title}\" from \"{Name}\"");
                RemoveSong(idx);
            }
            else
            {
                BotTools.LogLine($"\"{songData.Title}\" doesn't exist in \"{Name}\"");
            }
        }

        public void Shuffle()
        {
            Songs.Shuffle();
        }
    }
}
