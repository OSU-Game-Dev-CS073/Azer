using UnityEngine;

public class LoopingParallaxLayer : MonoBehaviour
{
    [Header("Camera")]
    public Transform cameraTransform;

    [Header("Background Pieces")]
    public Transform[] pieces;

    [Header("Settings")]
    public float parallaxFactor = 0.5f;
    public float pieceWidth = 30f;

    private Vector3 lastCameraPosition;

    void Start()
    {
        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        lastCameraPosition = cameraTransform.position;
    }

    void LateUpdate()
    {
        if (cameraTransform == null || pieces == null || pieces.Length == 0)
            return;

        Vector3 cameraDelta = cameraTransform.position - lastCameraPosition;

        // 让这一整层根据摄像头移动产生视差
        transform.position += new Vector3(cameraDelta.x * parallaxFactor, 0f, 0f);

        lastCameraPosition = cameraTransform.position;

        // 无限循环：超出一张图宽度后，移动到另一边
        foreach (Transform piece in pieces)
        {
            float distance = cameraTransform.position.x - piece.position.x;

            if (distance > pieceWidth)
            {
                piece.position += Vector3.right * pieceWidth * pieces.Length;
            }
            else if (distance < -pieceWidth)
            {
                piece.position += Vector3.left * pieceWidth * pieces.Length;
            }
        }
    }
}
