using System;
using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class FloatingCombatText : MonoBehaviour
{
    public static FloatingCombatText Instance;
    [SerializeField] private GameObject textPrefab;

    private void Awake()
    {
        Instance = this;
        
    }

    private void Update()
    {
        
    }

    // Pass the already instantiated floating text object
    public void Show(GameObject floatingText)
    {
        if (floatingText == null) return;

        // Animate
        Sequence seq = DOTween.Sequence();

        seq.Append(floatingText.transform.DOMoveY(floatingText.transform.position.y + 2f, 1.5f));

        // Destroy after animation
        seq.OnComplete(() => Destroy(floatingText));
    }
}