using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace cowsins
{
    public class Letter : MonoBehaviour
    {
        [SerializeField] private CanvasGroup playerUIGroup;
        [SerializeField] private Crosshair crosshair;
        public GameObject letter;
        private PlayerStats playerStats;
        public string creditsSceneName = "EscenaCreditos";
        public static bool isLetterOpen { get; private set; }

        private void Start()
        {
            if (letter != null)
                letter.SetActive(false);

            isLetterOpen = false;
        }

        public void ShowLetter()
        {
            StartCoroutine(ShowLetterRoutine());
        }

        private IEnumerator ShowLetterRoutine()
        {
            isLetterOpen = true;
            letter.SetActive(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            // Buscar PlayerStats
            playerStats = FindFirstObjectByType<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.LoseControl();
            }

            playerUIGroup.alpha = 0;
            playerUIGroup.interactable = false;
            playerUIGroup.blocksRaycasts = false;

            if (crosshair != null)
                crosshair.SetVisibility(false);

            yield return null;
        }

        public void GoToCredits()
        {
            Resume();
            SceneManager.LoadScene(creditsSceneName);
        }

        public void Resume()
        {
            letter.SetActive(false);

            playerUIGroup.alpha = 1;
            playerUIGroup.interactable = true;
            playerUIGroup.blocksRaycasts = true;

            if (crosshair != null)
                crosshair.SetVisibility(true);

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            // Buscar PlayerStats
            playerStats = FindFirstObjectByType<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.CheckIfCanGrantControl();
            }

            isLetterOpen = false;
        }
    }
}