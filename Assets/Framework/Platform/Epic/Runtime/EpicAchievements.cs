using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AF.Core;
using Cysharp.Threading.Tasks;
using Epic.OnlineServices;
using Epic.OnlineServices.Achievements;
using PlayEveryWare.EpicOnlineServices;
using UnityEngine;

namespace AF.Platform.Epic
{
    /// <summary>
    /// Epic's achievements. Every unlock goes into <see cref="EpicUnlockQueue"/> first and is sent while the launcher's account
    /// is logged in: right away, and again after the next login. A result that retrying cannot fix drops the unlock; a lost
    /// connection or login stops sending until the next trigger; anything else skips that unlock for the session and keeps it.
    /// <see cref="IsUnlocked"/> reads what Epic reported after login and what was confirmed since.
    /// </summary>
    internal sealed class EpicAchievements : IStoreAchievements
    {
        // Outside saves/: the queue must not be cloud-synced (see EpicUnlockQueue).
        private const string QUEUE_FOLDER = "epic-achievements";

        private readonly HashSet<string> _unlocked = new HashSet<string>(StringComparer.Ordinal);
        private readonly HashSet<string> _skipped = new HashSet<string>(StringComparer.Ordinal);
        private EpicUnlockQueue _queue;
        private ProductUserId _user;
        private bool _sending;

        internal static string QueueDirectory => Path.Combine(Application.persistentDataPath, QUEUE_FOLDER);

        /// <summary>Called by the session with the launcher's deployment and account; tests pass their own queue.</summary>
        internal void Start(EpicUnlockQueue queue)
        {
            _queue = queue;
            if (!queue.IsActive)
                Log.Warning("Epic: the launcher passed no deployment or account id; achievements are not unlocked in this session.");
        }

        public void Unlock(string id)
        {
            if (_unlocked.Contains(id))
                return;
            if (_queue == null || !_queue.IsActive)
            {
                Log.Warning($"Epic: no Epic account in this session; {id} was not unlocked. The game unlocks it again after its next load.");
                return;
            }
            _queue.Add(id);
            Send().Forget();
        }

        public bool IsUnlocked(string id) => _unlocked.Contains(id);

        /// <summary>Called by the session after the Connect login.</summary>
        internal async UniTask OnLoggedIn(ProductUserId user, EpicAccountId account)
        {
            if (_queue == null || !_queue.IsActive || account == null ||
                !string.Equals(account.ToString(), _queue.AccountId, StringComparison.OrdinalIgnoreCase))
            {
                // The queue holds another account's unlocks; they are not sent to this one.
                Log.Error("Epic: the logged-in account is not the launcher's; achievements are not sent in this session.");
                return;
            }
            _user = user;
            await ReadUnlocked();
            await Send();
        }

        private async UniTask ReadUnlocked()
        {
            try
            {
                var achievements = EOSManager.Instance.GetEOSAchievementInterface();
                if (achievements == null)
                    return;
                var completion = new UniTaskCompletionSource<Result>();
                var query = new QueryPlayerAchievementsOptions { LocalUserId = _user, TargetUserId = _user };
                achievements.QueryPlayerAchievements(ref query, null, (ref OnQueryPlayerAchievementsCompleteCallbackInfo data) =>
                {
                    if (data.ResultCode.IsOperationComplete())
                        completion.TrySetResult(data.ResultCode);
                });
                var result = await completion.Task;
                if (result != Result.Success)
                {
                    Log.Error($"Epic: the player's achievements could not be read ({result}).");
                    return;
                }
                var countOptions = new GetPlayerAchievementCountOptions { UserId = _user };
                var count = achievements.GetPlayerAchievementCount(ref countOptions);
                for (uint i = 0; i < count; i++)
                {
                    var copy = new CopyPlayerAchievementByIndexOptions { LocalUserId = _user, TargetUserId = _user, AchievementIndex = i };
                    if (achievements.CopyPlayerAchievementByIndex(ref copy, out var achievement) == Result.Success &&
                        achievement?.AchievementId != null && achievement.Value.UnlockTime != null)
                        _unlocked.Add(achievement.Value.AchievementId.ToString());
                }
            }
            catch (Exception exception)
            {
                Log.Error($"Epic: the player's achievements could not be read ({exception.Message}).");
            }
        }

        /// <summary>
        /// Sends the queue one unlock at a time (one bad id in a batch would fail the others too), including unlocks that
        /// arrive while it sends.
        /// </summary>
        private async UniTask Send()
        {
            if (_sending || _user == null || _queue == null)
                return;
            _sending = true;
            try
            {
                string id;
                while ((id = _queue.Pending.FirstOrDefault(pending => !_skipped.Contains(pending))) != null)
                {
                    if (_unlocked.Contains(id))
                    {
                        _queue.Remove(id);
                        continue;
                    }
                    var result = await SendOne(id);
                    if (result == Result.Success)
                    {
                        _unlocked.Add(id);
                        _queue.Remove(id);
                        Log.Info($"Epic: unlocked {id}.");
                    }
                    else if (IsPermanent(result))
                    {
                        Log.Error($"Epic: {id} cannot be unlocked ({result}); dropped. Check the id in the Epic Developer Portal.");
                        _queue.Remove(id);
                    }
                    else if (IsSessionWide(result))
                    {
                        Log.Warning($"Epic: sending stopped ({result}); the queue is sent again with the next unlock or login.");
                        return;
                    }
                    else
                    {
                        Log.Error($"Epic: {id} could not be unlocked ({result}); kept for the next session.");
                        _skipped.Add(id);
                    }
                }
            }
            finally
            {
                _sending = false;
            }
        }

        private async UniTask<Result> SendOne(string id)
        {
            try
            {
                var achievements = EOSManager.Instance.GetEOSAchievementInterface();
                if (achievements == null)
                    return Result.NoConnection;
                var completion = new UniTaskCompletionSource<Result>();
                var options = new UnlockAchievementsOptions { UserId = _user, AchievementIds = new Utf8String[] { id } };
                achievements.UnlockAchievements(ref options, null, (ref OnUnlockAchievementsCompleteCallbackInfo data) =>
                {
                    // An intermediate result comes first when the SDK retries by itself; only the final one counts.
                    if (data.ResultCode.IsOperationComplete())
                        completion.TrySetResult(data.ResultCode);
                });
                return await completion.Task;
            }
            catch (Exception exception)
            {
                Log.Error($"Epic: unlocking {id} threw ({exception.Message}).");
                return Result.UnexpectedError;
            }
        }

        // Retrying does not fix these: the unlock is dropped.
        private static bool IsPermanent(Result result) => result is Result.InvalidParameters or Result.InvalidRequest or Result.NotFound or
            Result.MissingPermissions or Result.AccessDenied or Result.NotConfigured or Result.Disabled or Result.NotImplemented or
            Result.InvalidProductUserID;

        // Every unlock would get the same answer (no network, throttled, login expiring): stop and send later.
        private static bool IsSessionWide(Result result) => result is Result.NoConnection or Result.NetworkDisconnected or Result.TimedOut or
            Result.TooManyRequests or Result.ServiceFailure or Result.InvalidAuth or Result.InvalidUser or Result.AuthExpired or
            Result.Canceled;
    }
}
