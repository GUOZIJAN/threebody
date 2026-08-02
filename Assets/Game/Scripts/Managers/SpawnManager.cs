using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using DG.Tweening;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance;
    public Transform deckPos;
    public List<CardView> handCards;
    public List<Transform> handPoints;
    public GameObject cardPrefab;
    public GameObject buildPrefab;
    public GameObject HandCardPanel;

    private Dictionary<string,string> cardDesc;

    private GameManager _game;
    private PlayerManager _players;

    private void Awake()
    {
        Instance = this;
        Services.Register(this);
        handCards = new List<CardView>();

        // 从 Resources/Card.json 加载并解析为字典
        cardDesc = LoadCardDescFromResources("Card");
    }

    private void Start()
    {
        _game    = Services.Get<GameManager>();
        _players = Services.Get<PlayerManager>();
    }

    private Dictionary<string, string> LoadCardDescFromResources(string resourceName)
    {
        TextAsset json = Resources.Load<TextAsset>(resourceName);
        if (json == null)
        {
            Debug.LogError($"Resources/{resourceName}.json 加载失败");
            return new Dictionary<string, string>();
        }
        return ParseJsonToDictionary(json.text);
    }

    private Dictionary<string, string> ParseJsonToDictionary(string json)
    {
        var dict = new Dictionary<string, string>();
        if (string.IsNullOrEmpty(json))
            return dict;

        var regex = new Regex("\"([^\"]+)\"\\s*:\\s*\"([^\"]+)\"");
        foreach (Match match in regex.Matches(json))
        {
            dict[match.Groups[1].Value] = match.Groups[2].Value;
        }

        return dict;
    }

    private void OnPlayCardHandler(int playerId, Card card)
    {
        if (playerId == 0)
        {
            // 玩家卡牌：使用动画完成后才重排手牌，避免动画重叠
            if (_game.currentCard != null)
            {
                CardAnimator.Instance.DetachFromHand(_game.currentCard);
                CardAnimator.Instance.AnimatePlayerUse(_game.currentCard, onComplete: () =>
                {
                    RepositionHandCards();
                });
            }
        }
        else
        {
            // AI 卡牌：生成临时卡牌动画
            CardAnimator.Instance.AnimateAIUse(card, playerId);
        }
    }

    private void OnEnable()
    {
        EventManager.OnDrawCard += SpawnCard;
        EventManager.OnPlayCard += OnPlayCardHandler;
    }

    private void OnDisable()
    {
        EventManager.OnDrawCard -= SpawnCard;
        EventManager.OnPlayCard -= OnPlayCardHandler;
    }

    public void SpawnCard(Card card)
    {
        if (handCards.Count >= handPoints.Count) return;

        // 只生成玩家0的手牌——必须检查这张卡是否属于玩家0
        // 旧逻辑用 Count 比较：当 visual > data 时会把 AI 的卡误生成给玩家0
        var p0 = _players.GetPlayer(0);
        if (p0 == null || !p0.handCards.Contains(card)) return;
        if (handCards.Count >= p0.handCards.Count) return;

        Transform handPoint = handPoints[handCards.Count];
        GameObject newCard = Instantiate(cardPrefab, deckPos.position, deckPos.rotation);

        newCard.AddComponent<CardView>();

        newCard.transform.Find("CostText").GetComponent<TextMeshProUGUI>().text = card.cost.ToString();
        newCard.transform.Find("NameText").GetComponent<TextMeshProUGUI>().text = card.cardname;

        string key = card.cardname;
        if (card is BroadcastCard bKey)
            key += bKey.choice.ToString();

        newCard.transform.Find("DescText").GetComponent<TextMeshProUGUI>().text = cardDesc[key];
        newCard.transform.Find("TypeText").GetComponent<TextMeshProUGUI>().text = card.type.ToString();
        TextMeshProUGUI t = newCard.transform.Find("PowerText").GetComponent<TextMeshProUGUI>();
        switch (card.type)
        {
            case CardType.Broadcast:
                t.text = (card is BroadcastCard bc) ? bc.distance.ToString() : "";
                break;
            case CardType.Strike:
                t.text = (card is StrikeCard sc) ? sc.damage.ToString() : "";
                break;
            case CardType.Build:
                t.text = "";
                break;
        }

        Sprite bgSprite = Resources.Load<Sprite>("pic/" + card.cardname);
        newCard.transform.Find("Background").GetComponent<UnityEngine.UI.Image>().sprite = bgSprite;
        newCard.transform.SetParent(handPoint, false);

        CardView cardView = newCard.GetComponent<CardView>();
        cardView.card = card;
        handCards.Add(cardView);
        cardView.FlyToHand(deckPos.position, handPoint.position);
    }

    public void RemoveCardFromHand(GameObject card, bool reposition = true, bool destroy = true)
    {
        if (card == null) return;
        CardView cardView = card.GetComponent<CardView>();
        if (!handCards.Contains(cardView)) return;

        handCards.Remove(cardView);
        if (destroy) Destroy(card);
        if (reposition)
            RepositionHandCards();
    }

    /// <summary>获取卡牌描述文本（供 CardAnimator 等外部使用）</summary>
    public string GetCardDescription(Card card)
    {
        string key = card.cardname;
        if (card is BroadcastCard bc)
            key += bc.choice.ToString();
        cardDesc.TryGetValue(key, out var desc);
        return desc ?? "";
    }

    /// <summary>手牌左对齐：挂到对应 pos 下并滑入归零</summary>
    public void RepositionHandCards()
    {
        for (int i = 0; i < handCards.Count; i++)
        {
            if (handCards[i] == null) continue;
            RectTransform rect = handCards[i].GetComponent<RectTransform>();

            // 如果已在正确位置，确保归零后跳过
            if (rect.parent == handPoints[i])
            {
                rect.anchoredPosition = Vector2.zero;
                continue;
            }

            // 记录旧世界坐标 → 挂到新 pos 归零 → 反算偏移 → 动画滑入
            Vector3 oldWorldPos = rect.position;
            rect.SetParent(handPoints[i], false);
            Vector2 offset = handPoints[i].GetComponent<RectTransform>().InverseTransformPoint(oldWorldPos);
            rect.anchoredPosition = offset;
            rect.DOAnchorPos(Vector2.zero, 0.25f).SetEase(Ease.OutCubic);
        }
    }
}

