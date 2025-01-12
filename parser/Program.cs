using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace YouTubeChannelParser
{
    class Program
    {
        static async Task Main(string[] args)
        {
            string apiKey = LoadApiKey("appsettings.json");
            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("Пожалуйста, укажите API-ключ в файле appsettings.json.");
                return;
            }

            List<string> channelUsernames = LoadChannels("channels.txt");
            if (channelUsernames.Count == 0)
            {
                Console.WriteLine("Файл channels.txt пуст. Добавьте имена каналов.");
                return;
            }

            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = apiKey,
                ApplicationName = "YouTubeChannelParser"
            });

            foreach (var username in channelUsernames)
            {
                string channelId = await GetChannelIdByUsername(youtubeService, username);
                if (!string.IsNullOrEmpty(channelId))
                {
                    string channelName = await GetChannelNameById(youtubeService, channelId);
                    await FetchRecentVideos(youtubeService, channelId, channelName);
                }
                else
                {
                    Console.WriteLine($"Идентификатор канала не найден для имени пользователя: {username}");
                }
            }

            Console.WriteLine("Нажмите Enter чтобы выйти...");
            Console.ReadLine();
        }

        private static string LoadApiKey(string configFilePath)
        {
            if (!File.Exists(configFilePath)) return null;

            var config = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(configFilePath));
            return config != null && config.ContainsKey("ApiKey") ? config["ApiKey"] : null;
        }

        private static List<string> LoadChannels(string filePath)
        {
            if (!File.Exists(filePath)) return new List<string>();

            var lines = File.ReadAllLines(filePath);
            return lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
        }

        private static async Task<string> GetChannelIdByUsername(YouTubeService youtubeService, string username)
        {
            var searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.Q = username.Replace("@", "");
            searchListRequest.Type = "channel";
            searchListRequest.MaxResults = 1;
            var searchListResponse = await searchListRequest.ExecuteAsync();

            return searchListResponse.Items?.FirstOrDefault()?.Snippet?.ChannelId;
        }

        private static async Task<string> GetChannelNameById(YouTubeService youtubeService, string channelId)
        {
            var channelListRequest = youtubeService.Channels.List("snippet");
            channelListRequest.Id = channelId;
            var channelListResponse = await channelListRequest.ExecuteAsync();

            return channelListResponse.Items?.FirstOrDefault()?.Snippet?.Title;
        }

        private static async Task FetchRecentVideos(YouTubeService youtubeService, string channelId, string channelName)
        {
            var searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.ChannelId = channelId;
            searchListRequest.PublishedAfter = DateTime.UtcNow.AddDays(-2);
            searchListRequest.MaxResults = 50;

            var searchListResponse = await searchListRequest.ExecuteAsync();

            Console.WriteLine($"Видео с канала: {channelName}");

            if (searchListResponse.Items.Count == 0)
            {
                Console.WriteLine("Нет видео за последние 2 дня");
            }
            else
            {
                foreach (var searchResult in searchListResponse.Items)
                {
                    if (searchResult.Id.Kind == "youtube#video")
                    {
                        Console.WriteLine($"https://www.youtube.com/watch?v={searchResult.Id.VideoId}");
                    }
                }
            }

            Console.WriteLine();
        }
    }
}