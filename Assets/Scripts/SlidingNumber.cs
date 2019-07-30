using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class SlidingNumber : MonoBehaviour
{
    public bool Animate = true;
    public float AnimationTime = 1.0f;

    private TMPro.TextMeshProUGUI text;
    private float initialValue;
    private float currentValue;
    private float targetValue;

    public void AddValue(float value)
    {
        initialValue = currentValue;
        targetValue += value;
    }

    public void SetValue(float value)
    {
        initialValue = currentValue;
        targetValue = value;
    }

    void Start()
    {
        text = GetComponent<TMPro.TextMeshProUGUI>();
    }
    // Update is called once per frame
    void Update()
    {
        if (currentValue != targetValue)
        {
            if (Animate)
            {
                if (initialValue < targetValue)
                {
                    currentValue += (Time.deltaTime * AnimationTime) * (targetValue - initialValue);
                    if (currentValue >= targetValue)
                        currentValue = targetValue;
                }
                else
                {
                    currentValue -= (Time.deltaTime * AnimationTime) * (initialValue - targetValue);
                    if (currentValue <= targetValue)
                        currentValue = targetValue;
                }
            }
            else
            {
                currentValue = targetValue;
            }
            text.text = currentValue.ToString("0");
        }
    }
}