using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class AudioSettingsUI : MonoBehaviour
{
    public Slider masterSlider;
    public AudioMixer mixer;
    public string exposedParamName = "MasterVolume";

    private void Start()
    {
        masterSlider.minValue = 0f;
        masterSlider.maxValue = 1f;
        masterSlider.wholeNumbers = false;

        // 初始化 slider 位置（根据当前 mixer 音量）
        float db;
        if (mixer.GetFloat(exposedParamName, out db))
        {
            float value = Mathf.Pow(10f, db / 20f);
            masterSlider.value = Mathf.Clamp01(value);
        }

        masterSlider.onValueChanged.AddListener(SetMasterVolume);
    }

    public void SetMasterVolume(float value)
    {
        value = Mathf.Clamp(value, 0.0001f, 1f);
        float db = Mathf.Log10(value) * 20f;
        mixer.SetFloat(exposedParamName, db);
    }
}
