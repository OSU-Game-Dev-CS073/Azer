using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager instance;
    
    [Header("Controls for lerping the Y Damping during player jump/fall")]
    [SerializeField] private float _fallPanAmount = 0.25f;
    [SerializeField] private float _fallYPanTime = 0.35f;
    public float _fallSpeedYDampingChangeThreshold = -15f;
    
    [Header("Normal Y Damping")]
    [SerializeField] private float _normYPanAmount = 0.5f;
    
    public bool IsLerpingYDamping { get; private set; }
    public bool LerpedFromPlayerFalling { get; set; }
    
    private Coroutine _lerpYPanCoroutine;
    private CinemachineVirtualCamera _currentCamera;
    private CinemachineFramingTransposer _framingTransposer;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        
        // Find the camera automatically - no dragging required
        _currentCamera = GetComponent<CinemachineVirtualCamera>();
        if (_currentCamera == null)
            _currentCamera = FindObjectOfType<CinemachineVirtualCamera>();
            
        if (_currentCamera != null)
            _framingTransposer = _currentCamera.GetCinemachineComponent<CinemachineFramingTransposer>();
    }
    
    public void LerpYDamping(bool isPlayerFalling)
    {
        if (_lerpYPanCoroutine != null)
            StopCoroutine(_lerpYPanCoroutine);
        _lerpYPanCoroutine = StartCoroutine(LerpYAction(isPlayerFalling));
    }
    
    private IEnumerator LerpYAction(bool isPlayerFalling)
    {
        if (_framingTransposer == null) yield break;
        
        IsLerpingYDamping = true;
        
        float startDampAmount = _framingTransposer.m_YDamping;
        float endDampAmount = isPlayerFalling ? _fallPanAmount : _normYPanAmount;
        
        if (isPlayerFalling)
            LerpedFromPlayerFalling = true;
        else
            LerpedFromPlayerFalling = false;
        
        float elapsedTime = 0f;
        while (elapsedTime < _fallYPanTime)
        {
            elapsedTime += Time.deltaTime;
            float lerpedPanAmount = Mathf.Lerp(startDampAmount, endDampAmount, (elapsedTime / _fallYPanTime));
            _framingTransposer.m_YDamping = lerpedPanAmount;
            yield return null;
        }
        
        IsLerpingYDamping = false;
    }
}