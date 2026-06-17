using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    public float speed = 2f;    
    public Transform[] points;
    private int i;
    
    void Start()
    {
        if (points.Length > 0)
        {
            transform.position = points[0].position;
        }
        Debug.Log(gameObject.name + " started with " + points.Length + " points");
    }

    void Update()
    {
        if (points.Length == 0) return;
        
        // Move towards current target point
        transform.position = Vector2.MoveTowards(transform.position, points[i].position, speed * Time.deltaTime);
        
        // Check if reached the current point
        if (Vector2.Distance(transform.position, points[i].position) < 0.01f)
        {
            // Move to next point
            i = (i + 1) % points.Length;
            Debug.Log(gameObject.name + " moving to point " + i);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(transform);
            Debug.Log(gameObject.name + " picked up player");
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(null);
            Debug.Log(gameObject.name + " released player");
        }
    }
}