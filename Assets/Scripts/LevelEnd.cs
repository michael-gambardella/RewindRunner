using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider2D))]
public class LevelEnd : MonoBehaviour
{
    [SerializeField] private bool loadNextScene = true;

    private void Reset()
    {
        var c = GetComponent<BoxCollider2D>();
        c.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (loadNextScene)
        {
            int i = SceneManager.GetActiveScene().buildIndex;
            SceneManager.LoadScene(i + 1);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
