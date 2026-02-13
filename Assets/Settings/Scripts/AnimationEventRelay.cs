using UnityEngine;

public class AnimationEventRelay : MonoBehaviour
{
    private Player player;

    private void Awake()
    {
        // Player script lives on the parent
        player = GetComponentInParent<Player>();
        if (player == null)
        {
            Debug.LogError("AnimationEventRelay: Could not find Player in parents.");
        }
    }

    // Animation Events require: public, void, no params
    public void EndAttack()
    {
        Debug.Log("ANIM EVENT: EndAttack fired");
        if (player != null)
            player.EndAttack();
    }

    public void PerformAttackHit()
    {
        Debug.Log("ANIM EVENT: PerformAttackHit fired");
        if (player != null)
            player.PerformAttackHit();
    }
}
