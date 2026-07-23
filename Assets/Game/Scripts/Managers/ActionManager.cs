using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class ActionManager : MonoBehaviour
{
    public static ActionManager Instance;
    public List<StrikeInfo> strikeList = new List<StrikeInfo>();
    public Dictionary<int, BroadcastCard> BroadcastRes = new Dictionary<int, BroadcastCard>();

    // 缓存的依赖
    private ChoiceManager _choice;
    private PlayerManager _players;
    private GalaxyManager _galaxies;
    private GameManager _game;
    private CardManager _cards;
    private UIManager _ui;
    private SpawnManager _spawn;

    private void Awake()
    {
        Instance = this;
        Services.Register(this);
    }

    private void Start()
    {
        _choice  = Services.Get<ChoiceManager>();
        _players = Services.Get<PlayerManager>();
        _galaxies = Services.Get<GalaxyManager>();
        _game    = Services.Get<GameManager>();
        _cards   = Services.Get<CardManager>();
        _ui      = Services.Get<UIManager>();
        _spawn   = Services.Get<SpawnManager>();
    }

    public async Task UseCard(int playerId, Card card)
    {
        PlayerData player = _players.GetPlayer(playerId);

        if (player.energy < card.cost)
        {
            Debug.Log("能量不足，无法使用卡牌！");
            return;
        }
        player.energy -= card.cost;

        switch (card.type)
        {
            case CardType.Broadcast :
                await DoBroadcast(player, (BroadcastCard)card);
                break;

            case CardType.Build :
                await DoBuild(player, (BuildCard)card);
                break;

            case CardType.Strike :
                Galaxy targetGalaxy = await _choice.ChooseGalaxy();
                _choice.ClearGalaxyTcs();
                DoStrike(player, (StrikeCard)card, targetGalaxy);
                break;
        }

        player.handCards.Remove(card);
        if (card.type != CardType.Broadcast)
        {
            _cards.discard.Add(card);
        }
        EventManager.OnPlayCard?.Invoke(playerId, card);
    }

    async public Task DoBroadcast(PlayerData player, BroadcastCard card)
    {
        player.lastBroadcastGalaxy = player.galaxyId;
        BroadcastCard response;
        PlayerData responser;
        Galaxy targetGalaxy;
        BroadcastRes.Clear();

        Debug.Log($"玩家{player.playerId}使用了广播卡{card.cardname}，正在选择广播目标星系...");
        while (true)
        {
            targetGalaxy = await _choice.ChooseGalaxy();
            if (_galaxies.GetDistance(player.galaxyId, targetGalaxy.id) <= card.distance)
            {
                Debug.Log($"玩家{player.playerId}选择了星系{targetGalaxy.id}作为广播目标");
                break;
            }
            else
            {
                Debug.Log("选择的星系超出广播范围，请重新选择！");
            }
        }

        // 处理 AI 广播响应
        foreach (var ai in _game.ais)
        {
            if (ai.data.playerId != player.playerId && ai.data.isAlive)
            {
                response = ai.Respond(player, targetGalaxy, card);
                if (response != null)
                {
                    BroadcastRes[ai.data.playerId] = response;
                }
            }
        }

        // 处理人类玩家广播响应（异步）
        if (player.playerId != 0)
        {
            response = await _game.player.Respond(player, targetGalaxy, card);
            if (response != null)
            {
                BroadcastRes[_game.player.data.playerId] = response;
            }
        }

        Debug.Log($"响应数量：{BroadcastRes.Count}");
        if (BroadcastRes.Count == 0)
        {
            _cards.discard.Add(card);
            player.energy += 1;
            Debug.Log("没有玩家响应广播卡");
            _ui.UpdateBasePanel(player.playerId);
            return;
        }
        else
        {
            _cards.broadcastUsed.Add(card);

            if (_game.currentPlayerId == 0)
            {
                EventManager.OnPlayerChooseBroadcast?.Invoke();
                int key = await _choice.PlayerChoose();
                Debug.Log($"玩家选择了响应{key}");
                EventManager.AfterPlayerChooseBroadcast?.Invoke();
                response = BroadcastRes[key];
                responser = _players.GetPlayer(key);
            }
            else
            {
                response = BroadcastRes.Values.First();
                responser = _players.GetPlayer(BroadcastRes.Keys.First());
            }

            responser.energy -= response.cost;
            responser.handCards.Remove(response);
            if (responser.playerId == 0)
            {
                _spawn.RemoveCardFromHand_Broadcast();
            }
            _game.CompleteBroadcast(card, response, player, responser);

            Card c = _cards.Draw();
            responser.handCards.Add(c);
            EventManager.OnDrawCard?.Invoke(c);

            _ui.UpdateBasePanel(responser.playerId);
            _ui.UpdateBasePanel(player.playerId);
        }
    }

    public async Task DoBuild(PlayerData player, BuildCard card)
    {
        if (card.effect == BuildEffect.Fly)
        {
            FlyTo(player, await _choice.ChooseGalaxy());
        }
        player.buildCards.Add(card);
    }

    public void DoStrike(PlayerData player, StrikeCard card, Galaxy galaxy)
    {
        int distance = _galaxies.GetDistance(player.galaxyId, galaxy.id);
        StrikeInfo newStrike = new StrikeInfo()
        {
            cardName = card.cardname,
            attackerId = player.playerId,
            targetGalaxyId = galaxy.id,
            damage = card.damage,
            effect = card.effect,
            totalDistance = distance,
            remainSteps = distance
        };

        strikeList.Add(newStrike);
    }

    public void FlyTo(PlayerData player, Galaxy galaxy)
    {
        if (!galaxy.isAlive || galaxy.ownerPlayerId != -1)
        {
            Debug.Log("目标星系不可飞行！");
            return;
        }
        player.galaxyId = galaxy.id;
        galaxy.ownerPlayerId = player.playerId;
        player.energy = 0;
        player.handCards.Clear();
        EventManager.OnFly?.Invoke(player, galaxy);
    }

    public void DiscardBuildCard(PlayerData player, BuildCard card)
    {
        player.buildCards.Remove(card);
        _cards.discard.Add(card);
        player.energy += card.cost / 2;
    }
}