using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Editor 工具：一键从硬编码数据生成 19 个 CardConfigSO + 1 个 DeckConfigSO。
/// 菜单路径：Tools → DarkForest → 生成所有卡牌配置
/// </summary>
public static class CardConfigGenerator
{
    private const string ASSET_PATH = "Assets/Game/Data/Cards";
    private const string DECK_PATH  = "Assets/Game/Data";

    [MenuItem("Tools/DarkForest/生成所有卡牌配置")]
    public static void GenerateAll()
    {
        // 确保目录存在
        EnsureDirectory(ASSET_PATH);
        EnsureDirectory(DECK_PATH);

        var allConfigs = new List<CardConfigSO>();

        // ============================================================
        //  1. 宇宙广播 (伪装) ×4
        // ============================================================
        allConfigs.Add(CreateCardConfig(
            "broadcast_yuzhou_fake", "宇宙广播", "伪装",
            CardType.Broadcast, 1, 4,
            broadcastChoice: BroadcastChoice.Fake,
            broadcastDistance: 2
        ));

        // ============================================================
        //  2. 热核打击 ×4
        // ============================================================
        allConfigs.Add(CreateCardConfig(
            "strike_rehe", "热核打击", "打击无特殊效果",
            CardType.Strike, 4, 4,
            damage: 1,
            strikeEffect: StrikeEffect.None
        ));

        // ============================================================
        //  3. 光粒打击 ×4
        // ============================================================
        allConfigs.Add(CreateCardConfig(
            "strike_guangli", "光粒打击", "无论是否被防御均毁灭目标恒星",
            CardType.Strike, 6, 4,
            damage: 2,
            strikeEffect: StrikeEffect.DestroySun
        ));

        // ============================================================
        //  4. 聚变反应堆 ×4
        // ============================================================
        allConfigs.Add(CreateCardConfig(
            "build_jubian", "聚变反应堆", "每回合开始时,能量+1，不依赖恒星",
            CardType.Build, 3, 4,
            defense: 0, produce: 1, needSun: false,
            buildEffect: BuildEffect.None
        ));

        // ============================================================
        //  5. 湮灭打击 ×3
        // ============================================================
        allConfigs.Add(CreateCardConfig(
            "strike_yanmie", "湮灭打击", "无论是否被防御均毁灭目标恒星及所有建造牌",
            CardType.Strike, 8, 3,
            damage: 3,
            strikeEffect: StrikeEffect.DestroySunAndBuild
        ));

        // ============================================================
        //  6. 降维打击 ×3
        // ============================================================
        allConfigs.Add(CreateCardConfig(
            "strike_jiangwei", "降维打击", "彻底清除目标星系",
            CardType.Strike, 10, 3,
            damage: 10,
            strikeEffect: StrikeEffect.DestroyAll
        ));

        // ============================================================
        //  7. 量子幽灵 ×3
        // ============================================================
        allConfigs.Add(CreateCardConfig(
            "build_liangzi", "量子幽灵", "可在等级3及以下的打击中幸存",
            CardType.Build, 8, 3,
            defense: 3, produce: 0, needSun: false,
            buildEffect: BuildEffect.None
        ));

        // ============================================================
        //  8. 反物质引擎 ×3
        // ============================================================
        allConfigs.Add(CreateCardConfig(
            "build_fanwuzhi", "反物质引擎", "每回合开始时,能量+2，不依赖恒星",
            CardType.Build, 6, 3,
            defense: 0, produce: 2, needSun: false,
            buildEffect: BuildEffect.None
        ));

        // ============================================================
        //  9. 戴森球 ×3
        // ============================================================
        allConfigs.Add(CreateCardConfig(
            "build_daisenqiu", "戴森球", "每回合开始时,能量+3，依赖恒星,每个星系只能建造1个",
            CardType.Build, 6, 3,
            defense: 0, produce: 3, needSun: true,
            buildEffect: BuildEffect.None
        ));

        // ============================================================
        // 10. 科技锁死 ×3
        // ============================================================
        allConfigs.Add(CreateCardConfig(
            "strike_keji", "科技锁死", "打击生效时,目标星系玩家需弃掉手中所有建造牌,不影响其生存",
            CardType.Strike, 4, 3,
            damage: 0,
            strikeEffect: StrikeEffect.DestroyHand
        ));

        // ============================================================
        // 11. 恒星广播 (合作) ×9
        // ============================================================
        allConfigs.Add(CreateCardConfig(
            "broadcast_hengxing_coop", "恒星广播", "合作",
            CardType.Broadcast, 0, 9,
            broadcastChoice: BroadcastChoice.Cooperate,
            broadcastDistance: 1
        ));

        // ============================================================
        // 12. 恒星广播 (伪装) ×5
        // ============================================================
        allConfigs.Add(CreateCardConfig(
            "broadcast_hengxing_fake", "恒星广播", "伪装",
            CardType.Broadcast, 0, 5,
            broadcastChoice: BroadcastChoice.Fake,
            broadcastDistance: 1
        ));

        // ============================================================
        // 13. 掩体星环 ×5
        // ============================================================
        allConfigs.Add(CreateCardConfig(
            "build_yanti", "掩体星环", "可在等级2及以下的打击中幸存",
            CardType.Build, 6, 5,
            defense: 2, produce: 0, needSun: false,
            buildEffect: BuildEffect.None
        ));

        // ============================================================
        // 14. 太阳能阵列 ×5
        // ============================================================
        allConfigs.Add(CreateCardConfig(
            "build_taiyangneng", "太阳能阵列", "每回合开始时,能量+1，依赖恒星",
            CardType.Build, 2, 5,
            defense: 0, produce: 1, needSun: true,
            buildEffect: BuildEffect.None
        ));

        // ============================================================
        // 15. 宇宙广播 (合作) ×6
        // ============================================================
        allConfigs.Add(CreateCardConfig(
            "broadcast_yuzhou_coop", "宇宙广播", "合作",
            CardType.Broadcast, 1, 6,
            broadcastChoice: BroadcastChoice.Cooperate,
            broadcastDistance: 2
        ));

        // ============================================================
        // 16. 超距广播 (合作) ×2
        // ============================================================
        allConfigs.Add(CreateCardConfig(
            "broadcast_chaoju_coop", "超距广播", "合作",
            CardType.Broadcast, 2, 2,
            broadcastChoice: BroadcastChoice.Cooperate,
            broadcastDistance: 10
        ));

        // ============================================================
        // 17. 超距广播 (伪装) ×2
        // ============================================================
        allConfigs.Add(CreateCardConfig(
            "broadcast_chaoju_fake", "超距广播", "伪装",
            CardType.Broadcast, 2, 2,
            broadcastChoice: BroadcastChoice.Fake,
            broadcastDistance: 10
        ));

        // ============================================================
        // 18. 光速飞船 ×2
        // ============================================================
        allConfigs.Add(CreateCardConfig(
            "build_guangsu", "光速飞船", "可跃迁至随机其他星系,不能携带能量和建造牌,且只能使用1次",
            CardType.Build, 10, 2,
            defense: 0, produce: 0, needSun: false,
            buildEffect: BuildEffect.Fly
        ));

        // ============================================================
        // 19. 监听基地 ×2
        // ============================================================
        allConfigs.Add(CreateCardConfig(
            "build_jianting", "监听基地", "所在星系接收广播后可不做回应",
            CardType.Build, 2, 2,
            defense: 0, produce: 0, needSun: false,
            buildEffect: BuildEffect.NoReply
        ));

        // ============================================================
        // 保存所有 CardConfigSO 并标记为 dirty
        // ============================================================
        foreach (var config in allConfigs)
        {
            EditorUtility.SetDirty(config);
        }
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[CardConfigGenerator] {allConfigs.Count} 个 CardConfigSO 已生成到 {ASSET_PATH}");

        // ============================================================
        // 创建 DeckConfigSO
        // ============================================================
        var deckConfig = ScriptableObject.CreateInstance<DeckConfigSO>();
        deckConfig.cards = allConfigs;

        string deckAssetPath = Path.Combine(DECK_PATH, "Deck_Standard.asset");
        AssetDatabase.CreateAsset(deckConfig, deckAssetPath);
        EditorUtility.SetDirty(deckConfig);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[CardConfigGenerator] DeckConfigSO 已生成到 {deckAssetPath}");

        int total = 0;
        foreach (var c in allConfigs) total += c.countInDeck;
        Debug.Log($"[CardConfigGenerator] 完成！{allConfigs.Count} 种卡牌，共 {total} 张。");
    }

    /// <summary>
    /// 删除所有通过生成器创建的资产（Card + Deck）。
    /// </summary>
    [MenuItem("Tools/DarkForest/删除所有卡牌配置")]
    public static void DeleteAll()
    {
        if (!EditorUtility.DisplayDialog(
            "确认删除",
            $"将删除 {ASSET_PATH} 下的所有资产以及 {DECK_PATH}/Deck_Standard.asset。\n确定要继续吗？",
            "删除", "取消"))
        {
            return;
        }

        // 删除 Card configs
        if (AssetDatabase.IsValidFolder(ASSET_PATH))
        {
            AssetDatabase.DeleteAsset(ASSET_PATH);
        }

        // 删除 Deck config
        string deckAsset = Path.Combine(DECK_PATH, "Deck_Standard.asset");
        if (AssetDatabase.LoadAssetAtPath<DeckConfigSO>(deckAsset) != null)
        {
            AssetDatabase.DeleteAsset(deckAsset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[CardConfigGenerator] 所有卡牌配置资产已删除。");
    }

    // ================================================================
    //  内部工具方法
    // ================================================================

    private static CardConfigSO CreateCardConfig(
        string cardId, string cardName, string description,
        CardType cardType, int cost, int countInDeck,
        BroadcastChoice broadcastChoice = BroadcastChoice.Cooperate,
        int broadcastDistance = 0,
        int damage = 0,
        StrikeEffect strikeEffect = StrikeEffect.None,
        int defense = 0,
        int produce = 0,
        bool needSun = false,
        BuildEffect buildEffect = BuildEffect.None)
    {
        var config = ScriptableObject.CreateInstance<CardConfigSO>();

        config.cardId          = cardId;
        config.cardName        = cardName;
        config.description     = description;
        config.cardType        = cardType;
        config.cost            = cost;
        config.countInDeck     = countInDeck;
        config.broadcastChoice = broadcastChoice;
        config.broadcastDistance = broadcastDistance;
        config.damage          = damage;
        config.strikeEffect    = strikeEffect;
        config.defense         = defense;
        config.produce         = produce;
        config.needSun         = needSun;
        config.buildEffect     = buildEffect;

        string filePath = Path.Combine(ASSET_PATH, $"{cardId}.asset");
        AssetDatabase.CreateAsset(config, filePath);

        return config;
    }

    private static void EnsureDirectory(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            // 递归创建父目录
            string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            string folder = Path.GetFileName(path);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureDirectory(parent);
            }

            AssetDatabase.CreateFolder(parent ?? "Assets", folder);
        }
    }
}
