using UnityEngine;

namespace cowsins
{
    public class ExperienceManager : MonoBehaviour
    {
        // Singleton pattern to ensure that there is only one ExperienceManager instance in the game.
        public static ExperienceManager instance;
        public bool useExperience;
        public int playerLevel;
        public float[] experienceRequirements;

        private float totalExperience;
        private PlayerStats playerStats;
        private float healthIncrease = 5f;

        private void OnEnable()
        {
            // If there is no existing ExperienceManager instance, then set this instance as the singleton.
            if (instance == null) instance = this;
        }

        public void Awake()
        {
            playerLevel = PlayerPrefs.GetInt("PlayerLevel");
            totalExperience = PlayerPrefs.GetFloat("TotalExperience");
        }

        // add experience to the player.
        public void AddExperience(float amount)
        {
            // Increase the player's total experience.
            totalExperience += amount;
            PlayerPrefs.SetFloat("TotalExperience", totalExperience);

            // Check if the player has leveled up.
            CheckForLevelUp();
        }

        // remove experience from the player.
        public void RemoveExperience(float amount)
        {
            // Reduce the player's total experience.
            totalExperience = Mathf.Max(totalExperience - amount, 0);
            PlayerPrefs.SetFloat("TotalExperience", totalExperience);

            // Check if the player has leveled down.
            CheckForLevelDown();
        }

        // check if the player has leveled up.
        private void CheckForLevelUp()
        {
            // While the player's level is less than the maximum level and their total experience is greater than the experience required for the next level, increase the player's level.
            while (playerLevel < experienceRequirements.Length - 1 && totalExperience >= experienceRequirements[playerLevel])
            {
                playerLevel++;
                PlayerPrefs.SetInt("PlayerLevel", playerLevel);
                playerStats = FindFirstObjectByType<PlayerStats>();
                if (playerStats != null)
                {

                    playerStats.IncreaseMaxHealth(healthIncrease);
                    playerStats.events.OnHeal.Invoke();
                }
            }
        }

        // check if the player has leveled down.
        private void CheckForLevelDown()
        {
            // While the player's level is greater than the minimum level and their total experience is less than the experience required for the current level, decrease the player's level.
            while (playerLevel > 0 && totalExperience < experienceRequirements[playerLevel])
            {
                playerLevel--;
                PlayerPrefs.SetInt("PlayerLevel", playerLevel);
            }
        }

        // get the player's level.
        public int GetPlayerLevel()
        {
            // Return the player's level plus one, since the level array starts at zero.
            playerLevel = PlayerPrefs.GetInt("PlayerLevel");
            if (playerLevel < 0 || playerLevel >= experienceRequirements.Length)
            {
                Debug.LogWarning("Player level is out of bounds. Resetting to level 0.");
                playerLevel = 0;
                PlayerPrefs.SetInt("PlayerLevel", playerLevel);
            }
            return playerLevel + 1;
        }

        // get the player's current experience.
        public float GetCurrentExperience()
        {
            playerLevel = PlayerPrefs.GetInt("PlayerLevel");
            // Calculate the player's current experience by subtracting the experience required for the previous level from their total experience.
            float previousLevelExperience = playerLevel > 0 ? experienceRequirements[playerLevel - 1] : 0;
            totalExperience = PlayerPrefs.GetFloat("TotalExperience");
            return totalExperience - previousLevelExperience;
        }

        public float GetTotalExperience()
        {
            totalExperience = PlayerPrefs.GetFloat("TotalExperience");
            return totalExperience;
        }
    }
}