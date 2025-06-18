using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace cowsins
{
    public class IntroNarrative : MonoBehaviour
    {
        [Header("Referencias UI")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private CanvasGroup playerUIGroup;
        [SerializeField] private Crosshair crosshair;
        public GameObject skipButton;
        public GameObject[] textos;
        public GameObject[] botones;
        public Image logoImage;

        [Header("Escritura")]
        public float typingSpeed = 1f;
        public float logoFadeDuration = 2f;

        private int currentIndex = 0;
        private TextMeshProUGUI currentText;
        private PlayerStats playerStats;
        private bool finalized = false;

        private static bool hasBeenShown = false;

        public static bool isInit { get; private set; }

        private void Start()
        {
            if (hasBeenShown)
            {
                canvasGroup.gameObject.SetActive(false);
                playerUIGroup.alpha = 1;
                playerUIGroup.interactable = true;
                playerUIGroup.blocksRaycasts = true;

                if (crosshair != null)
                    crosshair.SetVisibility(true);

                isInit = false;
                finalized = true;
            }
            else
            {
                isInit = true;

                playerUIGroup.alpha = 0;
                playerUIGroup.interactable = false;
                playerUIGroup.blocksRaycasts = false;

                if (crosshair != null)
                    crosshair.SetVisibility(false);

                if (skipButton != null)
                    skipButton.SetActive(true);

                foreach (var t in textos) t.SetActive(false);
                foreach (var b in botones) b.SetActive(false);

                StartCoroutine(ShowTextCoroutine());
            }
        }

        private void Update()
        {
            if (isInit) LockCursor();
            else if(!isInit && !finalized) UnlockCursor();
        }

        private void LockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            // Buscar PlayerStats
            playerStats = FindFirstObjectByType<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.LoseControl();
            }
        }

        private void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            // Buscar PlayerStats
            playerStats = FindFirstObjectByType<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.CheckIfCanGrantControl();
            }
            finalized = true;
        }

        IEnumerator ShowTextCoroutine()
        {
            textos[currentIndex].SetActive(true);
            currentText = textos[currentIndex].GetComponentInChildren<TextMeshProUGUI>();
            string fullText = currentText.text;
            currentText.text = "";

            foreach (char c in fullText)
            {
                currentText.text += c;
                yield return new WaitForSeconds(typingSpeed);
            }

            botones[currentIndex].SetActive(true);
        }

        public void Next()
        {
            textos[currentIndex].SetActive(false);
            botones[currentIndex].SetActive(false);
            currentIndex++;

            if (currentIndex < textos.Length)
            {
                StartCoroutine(ShowTextCoroutine());
            }
            else
            {
                StartCoroutine(ShowLogoAndFinish());
            }
        }

        IEnumerator ShowLogoAndFinish()
        {
            skipButton.SetActive(false);
            logoImage.gameObject.SetActive(true);
            Color color = logoImage.color;
            color.a = 0;
            logoImage.color = color;

            float elapsed = 0f;
            while (elapsed < logoFadeDuration)
            {
                elapsed += Time.deltaTime;
                color.a = Mathf.Clamp01(elapsed / logoFadeDuration);
                logoImage.color = color;
                yield return null;
            }

            yield return new WaitForSeconds(3f);

            // Finalizar
            isInit = false;
            hasBeenShown = true;

            canvasGroup.gameObject.SetActive(false);
            playerUIGroup.alpha = 1;
            playerUIGroup.interactable = true;
            playerUIGroup.blocksRaycasts = true;

            if (crosshair != null)
                crosshair.SetVisibility(true);
        }

        public void SkipIntro()
        {
            if (isInit)
            {
                isInit = false;
                hasBeenShown = true;

                canvasGroup.gameObject.SetActive(false);
                playerUIGroup.alpha = 1;
                playerUIGroup.interactable = true;
                playerUIGroup.blocksRaycasts = true;

                if (crosshair != null)
                    crosshair.SetVisibility(true);
            }
        }
    }
}