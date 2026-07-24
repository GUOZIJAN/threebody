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
    public List<Transform> emptyHandPoints;
    public GameObject cardPrefab;
    public GameObject buildPrefab;
    public GameObject HandCardPanel;

    private Sprite cardBackSprite;
    private Dictionary<string,string> cardDesc;

    private GameManager _game;
    private PlayerManager _players;

    private void Awake()
    {
        Instance = this;
        Services.Register(this);
        emptyHandPoints = new List<Transform>(handPoints);
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

    private void OnPlayCardHandler(int playerId, Card card) => RemoveCardFromHand(_game.currentCard);

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
        //有空位并且实际手牌小于显示手牌
        if (emptyHandPoints.Count > 0 && _players.GetPlayer(0).handCards.Count > handCards.Count)
        {
            Transform handPoint = emptyHandPoints[0];
            emptyHandPoints.RemoveAt(0);
            GameObject newCard = Instantiate(cardPrefab, deckPos.position, deckPos.rotation);

            newCard.AddComponent<CardView>();

            newCard.transform.Find("CostText").GetComponent<TextMeshProUGUI>().text = card.cost.ToString();
            newCard.transform.Find("NameText").GetComponent<TextMeshProUGUI>().text = card.cardname;
            
            //如果是广播卡，索引需加上BroadcastChoice类型
            string key = card.cardname;
            if(card is BroadcastCard bKey)
            {
                key += bKey.choice.ToString();
            }

            //从Resources/Card.json加载的卡牌描述文本
            newCard.transform.Find("DescText").GetComponent<TextMeshProUGUI>().text = cardDesc[key];
            newCard.transform.Find("TypeText").GetComponent<TextMeshProUGUI>().text = card.type.ToString();
            TextMeshProUGUI t =  newCard.transform.Find("PowerText").GetComponent<TextMeshProUGUI>();
            //不同种类 power文本的含义不同
            switch(card.type)
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

            //通过卡牌名称 找到背景资源
            newCard.transform.SetParent(handPoint, false);
            cardBackSprite = Resources.Load<Sprite>("pic/" + card.cardname);
            newCard.transform.Find("Background").GetComponent<UnityEngine.UI.Image>().sprite = cardBackSprite;
            CardView cardView = newCard.GetComponent<CardView>();
            cardView.card = card;
            handCards.Add(cardView);
            cardView.FlyToHand(deckPos.position,handPoint.position);

            emptyHandPoints.Remove(handPoint);
        }
    }

    public void RemoveCardFromHand(GameObject card)
    {
        if (card == null) return;
        CardView cardView = card.GetComponent<CardView>();
        if (handCards.Contains(cardView))
        {
            handCards.Remove(cardView);
            emptyHandPoints.Add(card.transform.parent);
            Destroy(card);
        }
    }
}

