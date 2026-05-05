using UnityEngine;
using System.Collections.Generic;

public class SpawnManager : MonoBehaviour
{
    public static SpawnManager Instance;
    public Transform deckPos;
    public List<Transform> handPoints;
    public List<Transform> emptyHandPoints;
    public GameObject cardPrefab;
    public GameObject buildPrefab;

    private void Awake()
    {
        Instance = this;
        emptyHandPoints = new List<Transform>(handPoints);
    }

    public void SpawnCard()
    {
        if (emptyHandPoints.Count > 0)
        {
            Transform handPoint = emptyHandPoints[0];
            emptyHandPoints.RemoveAt(0);
            GameObject newCard = Instantiate(cardPrefab, deckPos.position, deckPos.rotation);
            newCard.GetComponent<CardView>().FlyToHand(deckPos.position,handPoint.position);
        }
    }
}

