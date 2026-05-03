using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Standard for text

public class MainMenu : MonoBehaviour
{
    [Header("Transition Screen References")]
    public CanvasGroup transitionScreenGroup; // The panel containing your background and text
    public float fadeDuration = 2.0f;
    public float displayDuration = 4.0f; // How long the text stays visible before loading

    public void Play()
    {
        // Instead of loading instantly, we start the transition sequence
        StartCoroutine(IntroSequence());
    }

    private IEnumerator IntroSequence()
    {
        // 1. Fade the screen in
        float elapsed = 0;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            if (transitionScreenGroup != null)
                transitionScreenGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        // 2. Wait for the player to read the text
        yield return new WaitForSeconds(displayDuration);

        // 3. Load the next scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    public void Quit()
    {
        Application.Quit();
        Debug.Log("Player has quit the game.");
    }
}
