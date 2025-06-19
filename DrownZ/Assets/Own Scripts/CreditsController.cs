using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.Video;

public class CreditsManager : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject creditsPanel;
    public Animator creditsAnimator;
    public GameObject backMenuButton;
    public Image logo;
    public GameObject finalScreen;

    [Header("Fade")]
    public float fadeDuration = 2f;
    public float waitBeforeLogo = 1f;
    public float waitAfterLogo = 2f;

    private bool logoShown = false;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (creditsAnimator != null)
        {
            creditsAnimator.speed = 0.04f;
            creditsAnimator.Play("Credits");
        }

        finalScreen.SetActive(false);
        logo.gameObject.SetActive(false);

        StartCoroutine(WaitForCreditsToEnd());
    }

    private IEnumerator WaitForCreditsToEnd()
    {
        if (creditsAnimator != null)
        {
            AnimatorStateInfo stateInfo = creditsAnimator.GetCurrentAnimatorStateInfo(0);

            while (stateInfo.normalizedTime < 0.01f)
            {
                stateInfo = creditsAnimator.GetCurrentAnimatorStateInfo(0);
                yield return null;
            }

            while (stateInfo.normalizedTime < 1f)
            {
                stateInfo = creditsAnimator.GetCurrentAnimatorStateInfo(0);
                yield return null;
            }

            OnCreditsFinished();
        }
    }

    public void OnCreditsFinished()
    {
        backMenuButton.SetActive(false);
        StartCoroutine(ShowLogoAndFinalScreen());
    }

    private IEnumerator ShowLogoAndFinalScreen()
    {
        creditsPanel.SetActive(false);
        yield return new WaitForSeconds(waitBeforeLogo);

        logo.gameObject.SetActive(true);
        Color color = logo.color;
        color.a = 0;
        logo.color = color;

        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            color.a = Mathf.Clamp01(elapsed / fadeDuration);
            logo.color = color;
            yield return null;
        }

        yield return new WaitForSeconds(waitAfterLogo);
        logoShown = true;
        logo.gameObject.SetActive(false);
        finalScreen.SetActive(true);
    }

    private void Update()
    {
        if (logoShown && Input.anyKeyDown)
        {
            ReturnToMainMenu();
        }
    }

    public void ReturnToMainMenu()
    {
        PlayerPrefs.DeleteAll();
        SceneManager.LoadScene("MainMenu");
    }
}
