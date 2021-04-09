﻿using GoogleCast.Models.Media;
using Homehook.Models.Jellyfin;
using Homehook.Services;
using HomehookCommon.Models;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Homehook.Hubs
{
    public class ReceiverHub : Hub
    {
        private readonly CastService _castService;
        private readonly LanguageService _languageService;
        private readonly JellyfinService _jellyfinService;
        private readonly LoggingService<ReceiverHub> _loggingService;

        public ReceiverHub(CastService castService, LanguageService languageService, JellyfinService jellyfinService, LoggingService<ReceiverHub> loggingService)
        {
            _castService = castService;
            _languageService = languageService;
            _jellyfinService = jellyfinService;
            _loggingService = loggingService;
        }

        public Task<IEnumerable<string>> GetReceivers() =>
            Task.FromResult(_castService.ReceiverServices.Select(receiverService => receiverService.Receiver.FriendlyName));

        public async Task<ReceiverStatus> GetStatus(string receiverName) =>
            await (await _castService.GetReceiverService(receiverName)).GetReceiverStatus();

        public async Task Play(string receiverName) =>
            await (await _castService.GetReceiverService(receiverName)).PlayAsync();

        public async Task Stop(string receiverName) =>
            await (await _castService.GetReceiverService(receiverName)).StopAsync();

        public async Task Pause(string receiverName) =>
            await (await _castService.GetReceiverService(receiverName)).PauseAsync();

        public async Task Next(string receiverName) =>
            await (await _castService.GetReceiverService(receiverName)).NextAsync();

        public async Task Previous(string receiverName) =>
            await (await _castService.GetReceiverService(receiverName)).PreviousAsync();

        public async Task Seek(string receiverName, double timeToSeek) =>
            await (await _castService.GetReceiverService(receiverName)).SeekAsync(timeToSeek);

        public async Task ChangeCurrentMedia(string receiverName, int mediaId) =>
            await (await _castService.GetReceiverService(receiverName)).ChangeCurrentMediaAsync(mediaId);

        public async Task ChangeRepeatMode(string receiverName, RepeatMode repeatMode) =>
            await (await _castService.GetReceiverService(receiverName)).ChangeRepeatModeAsync(repeatMode);

        public async Task SetPlaybackRate(string receiverName, double playbackRate) =>
            await (await _castService.GetReceiverService(receiverName)).SetPlaybackRateAsync(playbackRate);

        public async Task InsertQueue(string receiverName, string searchTerm, int? insertBefore)
        {
            Phrase phrase = await _languageService.ParseJellyfinSimplePhrase(searchTerm);
            await _loggingService.LogDebug("ReceiverHub - parsed phrase.", $"Succesfully parsed the following phrase from the search term: {searchTerm}", phrase);

            phrase.UserId = await _jellyfinService.GetUserId(phrase.User);
            if (string.IsNullOrWhiteSpace(phrase.UserId))
            {
                await _loggingService.LogWarning($"ReceiverHub - no user found", $"{phrase.SearchTerm}, or the default user, returned no available user IDs.", phrase);
                return;
            }

            IEnumerable<QueueItem> items = await _jellyfinService.GetItems(phrase);
            await _loggingService.LogDebug($"ReceiverHub - items found.", $"Found {items.Count()} item(s) with the search term {phrase.SearchTerm}.");
            await _loggingService.LogInformation($"ReceiverHub - items found.", "Found the following items:", items);
            if (!items.Any())
                await _loggingService.LogWarning($"ReceiverHub - no results", $"{phrase.SearchTerm} returned no search results.", phrase);

            await (await _castService.GetReceiverService(receiverName)).InsertQueueAsync(items, insertBefore);
        }

        public async Task RemoveQueue(string receiverName, IEnumerable<int> itemIds) =>
            await (await _castService.GetReceiverService(receiverName)).RemoveQueueAsync(itemIds);

        public async Task UpQueue(string receiverName, IEnumerable<int> itemIds) =>
            await (await _castService.GetReceiverService(receiverName)).UpQueueAsync(itemIds);

        public async Task DownQueue(string receiverName, IEnumerable<int> itemIds) =>
            await (await _castService.GetReceiverService(receiverName)).DownQueueAsync(itemIds);

        public async Task ShuffleQueue(string receiverName) =>
            await (await _castService.GetReceiverService(receiverName)).ShuffleQueueAsync();

        public async Task SetVolume(string receiverName, float volume) =>
            await (await _castService.GetReceiverService(receiverName)).SetVolumeAsync(volume);

        public async Task ToggleMute(string receiverName) =>
            await (await _castService.GetReceiverService(receiverName)).ToggleMutedAsync();
    }
}