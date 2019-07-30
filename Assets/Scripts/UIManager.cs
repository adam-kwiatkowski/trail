using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public SlidingNumber OverallScoreText;
    public SlidingNumber TurnScoreText;
    public TMPro.TextMeshProUGUI MultiplierText;
    public Image StarSprite;
    [Space(10)]
    public FloatVariable OverallScore;
    public FloatVariable TurnScore;
    public FloatVariable Multiplier;

    void Start()
    {
    }

    void Update()
    {
        TurnScoreText.Animate = TurnScore.Value == 0;
        MultiplierText.enabled = Multiplier.Value > 0;
        StarSprite.enabled = Multiplier.Value > 0;

        TurnScoreText.SetValue(TurnScore.Value);
        OverallScoreText.SetValue(OverallScore.Value);
        MultiplierText.text = Multiplier.Value.ToString("0");
    }
}
