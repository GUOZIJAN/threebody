using UnityEngine;
using System.Collections.Generic;
using TMPro;
using DG.Tweening;
using Unity.VisualScripting;

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

    private void Awake()
    {
        Instance = this;
        emptyHandPoints = new List<Transform>(handPoints);
        handCards = new List<CardView>();
    }

    private void OnEnable()
    {
        EventManager.OnDrawCard += SpawnCard;
    }

    private void OnDisable()
    {
        EventManager.OnDrawCard -= SpawnCard;
    }

    public void SpawnCard(Card card)
    {
        if (emptyHandPoints.Count > 0)
        {
            Transform handPoint = emptyHandPoints[0];
            emptyHandPoints.RemoveAt(0);
            GameObject newCard = Instantiate(cardPrefab, deckPos.position, deckPos.rotation);

            newCard.AddComponent<CardView>();

            newCard.transform.Find("CostText").GetComponent<TextMeshProUGUI>().text = card.cost.ToString();
            newCard.transform.Find("NameText").GetComponent<TextMeshProUGUI>().text = card.cardname;
            //后续补充简介
            newCard.transform.Find("DescText").GetComponent<TextMeshProUGUI>().text = card.cardname;
            newCard.transform.Find("TypeText").GetComponent<TextMeshProUGUI>().text = card.type.ToString();
            TextMeshProUGUI t =  newCard.transform.Find("PowerText").GetComponent<TextMeshProUGUI>();
            //不同种类 power文本的含义不同
            switch(card.type)
            {
                case CardType.Broadcast:
                    t.text = ((BroadcastCard)card).distance.ToString();
                    break;
                case CardType.Strike:
                    t.text = ((StrikeCard)card).damage.ToString();
                    break;
                case CardType.Build:
                    t.text = "";
                    break;
            }

            //通过卡牌名称 找到背景资源
            newCard.transform.SetParent(HandCardPanel.transform, false);
            cardBackSprite = Resources.Load<Sprite>("pic/" + card.cardname);
            newCard.transform.Find("Background").GetComponent<UnityEngine.UI.Image>().sprite = cardBackSprite;
            CardView cardView = newCard.GetComponent<CardView>();
            handCards.Add(cardView);
            cardView.FlyToHand(deckPos.position,handPoint.position);
        }
    }
}

