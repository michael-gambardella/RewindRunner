using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Plays back a list of RecordedFrames so the "past self" appears as a ghost.
/// Attach to the ghost prefab; PlayerController2D will call Play(segment).
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class GhostReplay : MonoBehaviour
{
    [SerializeField] private float replaySpeed = 1f;
    [SerializeField] private float destroyAfterSeconds = 8f;

    private List<RecordedFrame> frames;
    private int index;
    private float timer;

    public void Play(List<RecordedFrame> segment)
    {
        if (segment == null || segment.Count == 0) return;
        frames = new List<RecordedFrame>(segment);
        index = 0;
        timer = 0f;
        transform.position = frames[0].position;
        StartCoroutine(DestroyAfterDelay());
    }

    private void Update()
    {
        if (frames == null || frames.Count == 0) return;

        timer += Time.deltaTime * replaySpeed;
        float frameDuration = 0.02f; // ~50 fps recording
        while (index < frames.Count - 1 && timer >= (index + 1) * frameDuration)
            index++;

        if (index < frames.Count)
        {
            transform.position = frames[index].position;
            // Optional: flip sprite based on velocity for visual clarity
            if (frames[index].velocity.x < -0.1f)
                transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            else if (frames[index].velocity.x > 0.1f)
                transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
        }
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(destroyAfterSeconds);
        Destroy(gameObject);
    }
}
