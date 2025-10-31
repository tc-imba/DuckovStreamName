using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Duckov.UI;
using Duckov.Utilities;
using ItemStatsSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using HarmonyLib;
using System.Reflection;
using System.Threading.Tasks;
using Duckov.Modding;
using Newtonsoft.Json.Linq;
using UnityEngine.Networking;
using SodaCraft.Localizations;

namespace DuckovStreamName
{
    [HarmonyPatch] // no typeof(...) here to avoid hard reference
    static class KillFeedGetCharacterNameOptionalPatch
    {
        // Only patch if KillFeed.ModBehaviour exists
        static bool Prepare()
        {
            bool result = AccessTools.TypeByName("KillFeed.ModBehaviour") != null;
            if (!result)
            {
                Debug.LogWarning(
                    "[DuckovStreamName] Cannot find Mod [KillFeed], put it before this mod if you need it.");
            }
            else
            {
                Debug.Log("[DuckovStreamName] patch KillFeed.ModBehaviour.GetCharacterName");
            }

            return result;
        }


        // Tell Harmony which method to patch when present
        static MethodBase TargetMethod()
        {
            var t = AccessTools.TypeByName("KillFeed.ModBehaviour");
            return AccessTools.Method(t, "GetCharacterName", new[] { typeof(CharacterMainControl) });
        }

        // Regular postfix: return void, edit __result
        static void Postfix(CharacterMainControl character, ref string __result)
        {
            if (!character.IsMainCharacter && character.characterPreset != null)
                __result = ModBehaviour.GetCharacterName(character);
        }
    }
    
    [HarmonyPatch] // no typeof(...) here to avoid hard reference
    static class BattlefieldTypeKillNoticeOnKillOptionalPatch
    {
        private static Type _modType;
        private static FieldInfo _fieldCachedConfig;
        private static MethodInfo _updateKillText;
        
        // Only patch if BattlefieldTypeKillNotice.ModBehaviour exists
        static bool Prepare()
        {
            bool result = true;
            _modType = AccessTools.TypeByName("BattlefieldTypeKillNotice.ModBehaviour");
            if (_modType == null) result = false;
            else
            {
                _fieldCachedConfig = AccessTools.Field(_modType, "_cachedConfig");
                _updateKillText = AccessTools.Method(_modType, "UpdateKillText");
            }
            if (_fieldCachedConfig == null || _updateKillText == null) result = false;
            if (!result)
            {
                Debug.LogWarning(
                    "[DuckovStreamName] Cannot find Mod [BattlefieldTypeKillNotice], put it before this mod if you need it.");
            }
            else
            {
                Debug.Log("[DuckovStreamName] patch BattlefieldTypeKillNotice.ModBehaviour.OnKill");
            }

            return result;
        }


        // Tell Harmony which method to patch when present
        static MethodBase TargetMethod()
        {
            var t = AccessTools.TypeByName("BattlefieldTypeKillNotice.ModBehaviour");
            return AccessTools.Method(t, "OnKill", new[] { typeof(Health), typeof(DamageInfo) });
        }

        // Regular postfix
        static void Postfix(BattlefieldTypeKillNotice.ModBehaviour __instance, Health health, DamageInfo damageInfo)
        {
            try
            {
                var configInstance = _fieldCachedConfig.GetValue(__instance);
                if (configInstance == null) return;
                var showTextField = AccessTools.Field(configInstance.GetType(), "ShowText");
                if (showTextField == null) return;
                bool showText = (bool)showTextField.GetValue(configInstance);
                if (showText)
                {
                    CharacterMainControl character = health.TryGetCharacter();
                    if (character && !character.IsMainCharacter && character.characterPreset != null)
                    {
                        _updateKillText.Invoke(__instance, new object[] { ModBehaviour.GetCharacterName(character)});
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            
            
           
        }
    }
    
    
    // Patch the death information
    [HarmonyPatch(typeof(DamageInfo))]
    public static class DamageInfoPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("GenerateDescription")]
        public static string GenerateDescriptionPostfix(string __result, DamageInfo __instance)
        {
            string description1 = "";
            string description2 = "";
            string str = "";
            if ((UnityEngine.Object)__instance.fromCharacter != (UnityEngine.Object)null)
            {
                if (__instance.fromCharacter.IsMainCharacter)
                    description1 = "DeathReason_Self".ToPlainText();
                else if ((UnityEngine.Object)__instance.fromCharacter.characterPreset != (UnityEngine.Object)null)
                    description1 = ModBehaviour.GetCharacterName(__instance.fromCharacter);
            }

            ItemMetaData metaData = ItemAssetsCollection.GetMetaData(__instance.fromWeaponItemID);
            if (metaData.id > 0)
                description2 = metaData.DisplayName;
            if (__instance.isExplosion)
                description2 = "DeathReason_Explosion".ToPlainText();
            if (__instance.crit > 0)
                str = "DeathReason_Critical".ToPlainText();
            bool flag1 = string.IsNullOrEmpty(description1);
            bool flag2 = string.IsNullOrEmpty(description2);
            if (flag1 & flag2)
                return "?";
            if (flag1)
                return description2;
            if (flag2)
                return description1;
            return $"{description1} ({description2}) {str}";
        }
    }

    // Patch display name in game
    [HarmonyPatch(typeof(HealthBar))]
    public static class CharacterNameDisplay
    {
        [HarmonyPrefix]
        [HarmonyPatch("RefreshCharacterIcon")]
        public static bool RefreshCharacterIconPrefix(HealthBar __instance)
        {
            try
            {
                if (!(bool)(UnityEngine.Object)__instance.target)
                    return true;
                CharacterMainControl character = __instance.target.TryGetCharacter();
                if (!(bool)(UnityEngine.Object)character)
                    return true;
                CharacterRandomPreset characterPreset = character.characterPreset;
                if (!(bool)(UnityEngine.Object)characterPreset)
                    return true;
                characterPreset.showName = true;
                // Debug.Log((object)$"设置角色名称显示: {characterPreset.showName}, 是否主角色: {character.IsMainCharacter}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError((object)$"RefreshCharacterIconPrefix错误: {ex}");
                return true;
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("RefreshCharacterIcon")]
        public static void RefreshCharacterIconPostfix(HealthBar __instance)
        {
            try
            {
                if (!(bool)(UnityEngine.Object)__instance.target)
                    return;
                CharacterMainControl character = __instance.target.TryGetCharacter();
                if (!(bool)(UnityEngine.Object)character)
                    return;
                CharacterRandomPreset characterPreset = character.characterPreset;
                if (!(bool)(UnityEngine.Object)characterPreset)
                    return;
                characterPreset.showName = true;

                var type = __instance.GetType();
                FieldInfo field = type.GetField("nameText", BindingFlags.NonPublic | BindingFlags.Instance);
                TextMeshProUGUI nameText = (TextMeshProUGUI)field.GetValue(__instance);
                nameText.text = ModBehaviour.GetCharacterName(character);
            }
            catch (Exception ex)
            {
                Debug.LogError((object)$"RefreshCharacterIconPostfix错误: {ex}");
            }
        }
    }
    
    // Patch OnDestroy to prevent memory leak
    [HarmonyPatch(typeof(CharacterMainControl))]
    public static class CharacterMainControlPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch("OnDestroy")]
        public static void OnDestroyPrefix(CharacterMainControl __instance)
        {
            Debug.Log($"[DuckovStreamName] {__instance.GetInstanceID()} was destroyed (patched)!");
            ModBehaviour.UnregisterCharacter(__instance);
        }
    }

    public class WeightedTailQueue<T>
    {
        private readonly List<T> _q = new List<T>();

        public int Count => _q.Count;

        public void Push(T item, double weight)
        {
            weight = Math.Max(weight, 0);
            weight = Math.Min(weight, 1);
            int N = _q.Count;
            if (N == 0 || weight == 0.0)
            {
                // Empty queue OR weight == 0 => put at the end.
                _q.Add(item);
                Debug.Log($"push item {item} at index 0, count {_q.Count}");
                return;
            }

            // number of eligible tail positions (at least 1)
            int m = Math.Max(1, (int)Math.Ceiling(N * weight));
            int start = Math.Max(0, N - m); // first eligible index in the tail window

            // choose an insertion index in [start, N] (N means "at the end")
            int idx = UnityEngine.Random.Range(start, N + 1);
            _q.Insert(idx, item);
            Debug.Log($"push item {item} at index {idx}, count {_q.Count}");
        }

        public T Pop()
        {
            if (_q.Count == 0)
                throw new InvalidOperationException("Pop from empty queue.");

            T item = _q[0];
            _q.RemoveAt(0);
            Debug.Log($"pop item {item} at index 0, count {_q.Count}");
            // Push(item, weight);
            return item;
        }

        // Check if an element is already in the queue
        public bool Contains(T item)
        {
            return _q.Contains(item);
        }

        // Always put element at the end
        public void PushBack(T item)
        {
            _q.Add(item);
        }
    }

    [System.Serializable]
    public class DuckovStreamNameConfig
    {
        public string roomId = "";
        public string roomUid = "";
        public int guardBaseScore = 100;
        public float guardExtraMultiplier = 0;
    }
    
    public class ModBehaviour : Duckov.Modding.ModBehaviour
    {
        class User
        {
            public string Uid;
            public int Score = 0;
            public double CalculatedScore = 0;
            public string Name = "";
            public int GuardValue = 0;
            public int Rank = 0;
            public double Weight = 0;


            public void CalculateScore()
            {
                CalculatedScore = Score;
                if (GuardValue > 0)
                {
                    CalculatedScore += Instance.config.guardBaseScore + Instance.config.guardExtraMultiplier * Score;
                }
            }
        }
        
        public static ModBehaviour Instance = null;
        public static string MOD_NAME = "DuckovStreamName";
        public DuckovStreamNameConfig config = new DuckovStreamNameConfig();
        private static string persistentConfigPath => Path.Combine(Application.streamingAssetsPath, "DuckovStreamNameConfig.txt");
        
        
        
        
        private Harmony? _harmony;
        private static WeightedTailQueue<string> _uidQueue = new WeightedTailQueue<string>();
        private static Dictionary<string, User> _uidUserMap = new Dictionary<string, User>();
        private static Dictionary<int, User> _idUserMap = new Dictionary<int, User>();

        private void Awake()
        {
            Instance = this;
        }


        private void OnModActivated(ModInfo info, Duckov.Modding.ModBehaviour behaviour)
        {
            if (info.name == ModConfigAPI.ModConfigName)
            {
                Debug.Log("[DuckovStreamName] ModConfig activated!");
                SetupModConfig();
                LoadConfigFromModConfig();
            }
        }
        
        private void SaveConfig(DuckovStreamNameConfig config)
        {
            try
            {
                string json = JsonUtility.ToJson(config, true);
                File.WriteAllText(persistentConfigPath, json);
                Debug.Log("[DuckovStreamName] Config saved");
            }
            catch (Exception e)
            {
                Debug.LogError($"[DuckovStreamName] Failed to save config: {e}");
            }
        }
        
        private void OnModConfigOptionsChanged(string key)
        {
            if (!key.StartsWith(MOD_NAME + "_"))
                return;

            // 使用新的 LoadConfig 方法读取配置
            LoadConfigFromModConfig();

            // 保存到本地配置文件
            SaveConfig(config);

            // 更新当前显示的文本样式（如果正在显示）
            // UpdateUsers();

            Debug.Log($"[DuckovStreamName] ModConfig updated - {key}");
        }

        private void SetupModConfig()
        {
            if (!ModConfigAPI.IsAvailable())
            {
                Debug.LogWarning("[DuckovStreamName] ModConfig not available");
                return;
            }
            Debug.Log("准备添加ModConfig配置项");
            // 添加配置变更监听
            ModConfigAPI.SafeAddOnOptionsChangedDelegate(OnModConfigOptionsChanged);

            // 根据当前语言设置描述文字
            SystemLanguage[] chineseLanguages = {
                SystemLanguage.Chinese,
                SystemLanguage.ChineseSimplified,
                SystemLanguage.ChineseTraditional
            };
            
            bool isChinese = chineseLanguages.Contains(LocalizationManager.CurrentLanguage);
            
            // 添加配置项
            ModConfigAPI.SafeAddInputWithSlider(
                MOD_NAME,
                "roomId",
                isChinese ? "直播间id" : "room id",
                typeof(string),
                config.roomId
            );
            
            ModConfigAPI.SafeAddInputWithSlider(
                MOD_NAME,
                "roomUid",
                isChinese ? "主播uid" : "room uid",
                typeof(string),
                config.roomUid
            );
            
            ModConfigAPI.SafeAddInputWithSlider(
                MOD_NAME,
                "guardBaseScore",
                isChinese ? "舰长基础贡献值" : "Guard Base Score",
                typeof(int),
                config.guardBaseScore,
                new Vector2(0, 2000)
            );
            
            ModConfigAPI.SafeAddInputWithSlider(
                MOD_NAME,
                "guardExtraMultiplier",
                isChinese ? "舰长贡献值额外倍率" : "Guard Extra Multiplier",
                typeof(float),
                config.guardExtraMultiplier,
                new Vector2(0f, 10f)
            );
            
            Debug.Log("[DuckovStreamName] ModConfig setup completed");
        }
        
        private void LoadConfigFromModConfig()
        {
            config.roomId = ModConfigAPI.SafeLoad<string>(MOD_NAME, "roomId", config.roomId);
            config.roomUid = ModConfigAPI.SafeLoad<string>(MOD_NAME, "roomUid", config.roomUid);
            config.guardBaseScore = ModConfigAPI.SafeLoad<int>(MOD_NAME, "guardBaseScore", config.guardBaseScore);
            config.guardExtraMultiplier = ModConfigAPI.SafeLoad<float>(MOD_NAME, "guardExtraMultiplier", config.guardExtraMultiplier);
        }


        protected override void OnAfterSetup()
        {
            base.OnAfterSetup();
            Debug.Log("[DuckovStreamName] Enabled.");
            _harmony = new Harmony("com.tcimba.duckov.streamname");
            _harmony.PatchAll();
            Debug.Log("[DuckovStreamName] Harmony patches applied.");
            ModManager.OnModActivated += OnModActivated;
            SceneLoader.onAfterSceneInitialize += this.OnLevelLoaded;
            
            if (ModConfigAPI.IsAvailable())
            {
                Debug.Log("[DuckovStreamName] ModConfig already available!");
                SetupModConfig();
                LoadConfigFromModConfig();
            }
        }

        protected override void OnBeforeDeactivate()
        {
            base.OnBeforeDeactivate();
            Debug.Log("[DuckovStreamName] Disabled.");
            _harmony?.UnpatchAll(_harmony.Id);
            _harmony = null;
            Debug.Log("[DuckovStreamName] Harmony patches removed.");
            ModManager.OnModActivated -= OnModActivated;
            SceneLoader.onAfterSceneInitialize -= this.OnLevelLoaded;
        }

        public static async Task<string> GetJsonAsync(string url)
        {
            using (var req = UnityWebRequest.Get(url))
            {
                var op = req.SendWebRequest();
                while (!op.isDone) await Task.Yield();

#if UNITY_2020_2_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
                if (req.isHttpError || req.isNetworkError)
#endif
                {
                    Debug.LogError(req.error);
                    return null;
                }

                return req.downloadHandler.text;
            }
        }

        private async void OnLevelLoaded(SceneLoadingContext context)
        {
            await UpdateUsers();
        }

        private async Task UpdateUsers()
        {
            string url =
                $"https://api.live.bilibili.com/xlive/general-interface/v1/rank/queryContributionRank?ruid={config.roomUid}&room_id={config.roomId}&page=1&page_size=100&type=online_rank&switch=contribution_rank&platform=web";
            Debug.Log($"[DuckovStreamName] Getting users from {url}");
            string json = await GetJsonAsync(url);
            JObject root = JObject.Parse(json);
            Debug.Log("Code: " + (int)root["code"]);
            Debug.Log("Message: " + (int)root["message"]);
            Debug.Log("Count: " + (int)root["data"]["count"]);

            _uidUserMap.Clear();
            List<User> users = new List<User>();

            foreach (var item in root["data"]["item"])
            {
                User user = new User();
                user.Uid = item["uid"].ToString();
                user.Name = item["name"].ToString();
                user.Score = int.Parse(item["score"].ToString());
                user.GuardValue = int.Parse(item["guard_level"].ToString());
                user.CalculateScore();
                _uidUserMap.Add(user.Uid, user);
                users.Add(user);
            }

            if (users.Count == 0) return;

            // calculate the rank for users
            users.Sort((a, b) => b.CalculatedScore.CompareTo(a.CalculatedScore));
            int currentRank = 0;
            users[0].Rank = currentRank;
            for (int i = 1; i < users.Count; i++)
            {
                if (users[i].CalculatedScore != users[i - 1].CalculatedScore)
                {
                    currentRank = i;
                }

                users[i].Rank = currentRank;
            }

            // user rank to calculate weight and push into queue
            for (int i = 0; i < users.Count; i++)
            {
                users[i].Weight = (double)(users.Count - users[i].Rank) / users.Count;
                if (!_uidQueue.Contains(users[i].Uid))
                {
                    _uidQueue.Push(users[i].Uid, users[i].Weight);
                    Debug.Log(
                        $"[DuckovStreamName] Init user: {users[i].Uid} [{users[i].Name}], weight: {users[i].Weight}, rank: {users[i].Rank}");
                }
            }
        }

        private static User GetRandomUser()
        {
            while (_uidQueue.Count > 0)
            {
                string uid = _uidQueue.Pop();
                if (!_uidUserMap.ContainsKey(uid)) continue;
                User user = _uidUserMap[uid];
                _uidQueue.Push(user.Uid, user.Weight);
                Debug.Log(
                    $"[DuckovStreamName] Use user: {user.Uid} [{user.Name}], weight: {user.Weight}, rank: {user.Rank}");
                return user;
            }

            return null;
        }

        public static void UnregisterCharacter(CharacterMainControl character)
        {
            int id = character.GetInstanceID();
            _idUserMap.Remove(id);
        }

        public static string GetCharacterName(CharacterMainControl character)
        {
            try
            {
                int id = character.GetInstanceID();
                User user = null;
                if (_idUserMap.ContainsKey(id))
                {
                    Debug.Log($"[DuckovStreamName] character id {id} found in idUserMap");
                    user = _idUserMap[id];
                }
                else
                {
                    Debug.Log($"[DuckovStreamName] character id {id} not found in idUserMap, get a new user");
                    user = GetRandomUser();
                    _idUserMap[id] = user;
                }

                if (user == null)
                {
                    return $"({character.characterPreset.DisplayName}) 当前无观众";
                }

                return $"({character.characterPreset.DisplayName}) {user.Name}";
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return $"({character.characterPreset.DisplayName}) 生成失败";
            }
        }
    }
}