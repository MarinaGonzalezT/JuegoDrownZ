/// <summary>
/// This script belongs to cowsins™ as a part of the cowsins´ FPS Engine. All rights reserved. 
/// </summary>
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using cowsins;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor.Presets;
#endif

#region others
[System.Serializable]
public class Events
{
    public UnityEvent OnShoot, OnReload, OnFinishReload, OnAim, OnAiming, OnStopAim, OnHit, OnInventorySlotChanged, OnEquipWeapon;
}
[System.Serializable]
public class Effects
{
    public GameObject grassImpact, metalImpact, mudImpact, woodImpact, enemyImpact;
}
[System.Serializable]
public class CustomShotMethods
{
    public Weapon_SO weapon;
    public UnityEvent OnShoot;
}
#endregion
namespace cowsins
{
    public class WeaponController : MonoBehaviour
    {
        //References
        [Tooltip("An array that includes all your initial weapons.")] public Weapon_SO[] initialWeapons;

        public WeaponIdentification[] inventory;

        public UISlot[] slots;

        public Weapon_SO weapon;

        [Tooltip("Attach your main camera")] public Camera mainCamera;

        [Tooltip("Attach your camera pivot object")] public Transform cameraPivot;

        private Transform[] firePoint;

        [Tooltip("Attach your weapon holder")] public Transform weaponHolder;

        //Variables

        [Tooltip("max amount of weapons you can have")] public int inventorySize;

        [SerializeField, HideInInspector]
        public bool isAiming;

        private Vector3 aimRot;

        private bool reloading;
        public bool Reloading { get { return reloading; } set { reloading = value; } }

        [Tooltip("If true you won´t have to press the reload button when you run out of bullets")] public bool autoReload;

        private float reloadTime;

        private float coolSpeed;

        [Tooltip("If false, hold to aim, and release to stop aiming.")] public bool alternateAiming;

        [Tooltip("What objects should be hit")] public LayerMask hitLayer;

        [Tooltip("Do you want to resize your crosshair on shooting ? "), SerializeField] private bool resizeCrosshair;

        [Tooltip("Do not draw the crosshair when aiming a weapon")] public bool removeCrosshairOnAiming;

        public bool canMelee;

        public bool CanMelee;

        [SerializeField] private GameObject meleeObject;

        [SerializeField] private Animator holsterMotionObject;

        public Animator HolsterMotionObject
        {
            get { return holsterMotionObject; }
        }

        public float meleeDuration, meleeAttackDamage, meleeRange, meleeCamShakeAmount, meleeDelay, reEnableMeleeAfterAction;

        public bool shooting { get; private set; } = false;

        private float spread;

        private float aimingCamShakeMultiplier, crouchingCamShakeMultiplier = 1;

        private float damagePerBullet;

        private float penetrationAmount;

        private float camShakeAmount;

        // Effects
        public Effects effects;

        public Events events;

        [Tooltip("Used for weapons with custom shot method. Here, " +
            "you can attach your scriptable objects and assign the method you want to call on shoot. " +
            "Please only assign those scriptable objects that use custom shot methods, Otherwise it won´t work or you will run into issues.")]
        public CustomShotMethods[] customShot;

        public UnityEvent customMethod;

        // Internal Use
        private int bulletsPerFire;

        public bool canShoot;

        RaycastHit hit;

        public int currentWeapon;

        private AudioClips audioSFX;

        public WeaponIdentification id;

        private PlayerStats stats;

        private WeaponAnimator weaponAnimator; 

        private GameObject muzzleVFX;

        private float fireRate;

        public bool holding;

        public delegate void PerformShootStyle();

        public PerformShootStyle performShootStyle;

        private delegate IEnumerator Reload();

        private Reload reload;

        private delegate void ReduceAmmo();

        private ReduceAmmo reduceAmmo;

        private AudioClip fireSFX;

        private void OnEnable()
        {
            // Subscribe to the method.
            // Each time we click on the attachment UI, we should perform the assignment.
            UIEvents.onAttachmentUIElementClickedNewAttachment += AssignNewAttachment;
        }

        private void OnDisable()
        {
            // Unsubscribe from the method to avoid issues
            UIEvents.onAttachmentUIElementClickedNewAttachment = null;
        }
        private void Start()
        {
            InitialSettings();
            CreateInventoryUI();
            GetInitialWeapons();
        }

        private void Update()
        {
            HandleUI();
            HandleAimingMotion();
            ManageWeaponMethodsInputs();
            HandleRecoil();
            HandleHeatRatio();
        }

        private float aimingSpeed;
        public void Aim()
        {
            if (!isAiming) events.OnAim.Invoke(); // Invoke your custom method on stop aiming

            isAiming = true;

            if (weapon.applyBulletSpread) spread = weapon.aimSpreadAmount;
            aimingCamShakeMultiplier = weapon.camShakeAimMultiplier;

            events.OnAiming.Invoke();

            float cameraDistance = mainCamera.nearClipPlane + weapon.aimDistance;
            Vector3 cameraCenter = mainCamera.ScreenToWorldPoint(new Vector3(Screen.width / 2f, Screen.height / 2f, cameraDistance));
            Vector3 scopeOffset = id.scope ? id.scope.GetComponent<Scope>().aimingPosition : Vector3.zero;
            id.aimPoint.position = Vector3.Lerp(id.aimPoint.position, cameraCenter + scopeOffset, aimingSpeed * Time.deltaTime);
            id.aimPoint.localRotation = Quaternion.Lerp(id.aimPoint.localRotation, Quaternion.Euler(aimRot), aimingSpeed * Time.deltaTime);

        }

        public void StopAim()
        {
            if (weapon != null && weapon.applyBulletSpread) spread = weapon.spreadAmount;

            if (isAiming) events.OnStopAim.Invoke(); // Invoke your custom method on stop aiming

            isAiming = false;
            aimingCamShakeMultiplier = 1;

            if (id == null) return;
            // Change the position and FOV
            id.aimPoint.localPosition = Vector3.Lerp(id.aimPoint.localPosition, id.originalAimPointPos, aimingSpeed * Time.deltaTime);
            id.aimPoint.localRotation = Quaternion.Lerp(id.aimPoint.localRotation, Quaternion.Euler(id.originalAimPointRot), aimingOutSpeed * Time.deltaTime);

            weaponHolder.localRotation = Quaternion.Lerp(weaponHolder.transform.localRotation, Quaternion.Euler(Vector3.zero), aimingOutSpeed * Time.deltaTime);
        }

        /// <summary>
        /// Forces the Weapon to go back to its initial position 
        /// </summary>
        public void ForceAimReset()
        {
            isAiming = false;
            if (id?.aimPoint)
            {
                id.aimPoint.localPosition = id.originalAimPointPos;
                id.aimPoint.localRotation = Quaternion.Euler(id.originalAimPointRot);
            }
            weaponHolder.localRotation = Quaternion.Euler(Vector3.zero);
        }

        public void SetCrouchCamShakeMultiplier()
        {
            if (weapon)
                crouchingCamShakeMultiplier = weapon.camShakeCrouchMultiplier;
        }

        public void ResetCrouchCamShakeMultiplier()
        {
            crouchingCamShakeMultiplier = 1;
        }

        private float aimingOutSpeed;

        private void HandleAimingMotion()
        {
            aimingOutSpeed = (weapon != null) ? aimingSpeed : 2;
            if (isAiming && weapon != null) mainCamera.fieldOfView = Mathf.Lerp(mainCamera.fieldOfView, weapon.aimingFOV, aimingSpeed * Time.deltaTime);
        }

        public void HandleHitscanProjectileShot()
        {
            foreach (var p in firePoint)
            {
                canShoot = false; // since you have already shot, you will have to wait in order to being able to shoot again
                bulletsPerFire = weapon.bulletsPerFire;
                StartCoroutine(HandleShooting());

                // Adding a layer of realism, bullet shells get instantiated and interact with the world
                // We should  first check if we really wanna do this
                if (weapon.showBulletShells && (int)weapon.shootStyle != 2)
                {
                    var bulletShell = Instantiate(weapon.bulletGraphics, p.position, mainCamera.transform.rotation);
                    Rigidbody shellRigidbody = bulletShell.GetComponent<Rigidbody>();
                    float torque = Random.Range(-15f, 15f);
                    Vector3 shellForce = mainCamera.transform.right * 5 + mainCamera.transform.up * 5;
                    shellRigidbody.AddTorque(mainCamera.transform.right * torque, ForceMode.Impulse);
                    shellRigidbody.AddForce(shellForce, ForceMode.Impulse);
                }
            }
            if (weapon.timeBetweenShots == 0) SoundManager.Instance.PlaySound(fireSFX, 0, weapon.pitchVariationFiringSFX, true, 0);
            Invoke(nameof(CanShoot), fireRate);
        }

        public void HandleMeleeShot() => StartCoroutine(HandleMeleeShotCoroutine());
        private IEnumerator HandleMeleeShotCoroutine()
        {
            canShoot = false;

            // Determine if we want to add an effect for FOV
            if (weapon.applyFOVEffectOnShooting) mainCamera.fieldOfView = mainCamera.fieldOfView - weapon.FOVValueToSubtract;

            int randomIndex = Random.Range(1, weapon.amountOfShootAnimations + 1);

            // Generate the animation name based on the random index
            string randomAnimation = "shooting" + (randomIndex == 1 ? "" : randomIndex.ToString());

            // Get the Animator component from the current weapon
            Animator animator = inventory[currentWeapon].GetComponentInChildren<Animator>();

            // Play the selected random animation
            CowsinsUtilities.ForcePlayAnim(randomAnimation, animator);

            SoundManager.Instance.PlaySound(fireSFX, 0, weapon.pitchVariationFiringSFX, true, 0);

            if (weapon == null) yield break;

            yield return new WaitForSeconds(weapon.hitDelay);

            MeleeAttack(weapon.attackRange, weapon.damagePerHit);
            CamShake.instance.ShootShake(weapon.camShakeAmount * aimingCamShakeMultiplier * crouchingCamShakeMultiplier);
            Invoke(nameof(CanShoot), weapon.attackRate);
        }

        public void CustomShot()
        {
            // If we want to use fire Rate
            if (!weapon.continuousFire)
            {
                canShoot = false;
                Invoke(nameof(CanShoot), fireRate);
            }

            // Continuous fire
            customMethod?.Invoke();
        }
        private void SelectCustomShotMethod()
        {
            // Iterate through each item in the array
            for (int i = 0; i < customShot.Length; i++)
            {
                // Assign the on shoot event to the unity event to call it each time we fire
                if (customShot[i].weapon == weapon)
                {
                    customMethod = customShot[i].OnShoot;
                    return;
                }
            }

            Debug.LogError("Appropriate weapon scriptable object not found in the custom shot array (under the events tab). Please, configure the weapon scriptable object and the suitable method to fix this error");
        }
        private IEnumerator HandleShooting()
        {
            /// Determine wether we are sending a raycast, aka hitscan weapon, we are spawning a projectile or melee attacking
            int style = (int)weapon.shootStyle;

            weaponAnimator.StopWalkAndRunMotion();

            // Rest the bullets that have just been shot
            reduceAmmo?.Invoke();

            //Determine weapon class / style
            int i = 0;
            while (i < bulletsPerFire)
            {
                if (weapon == null) yield break;
                shooting = true;

                CamShake.instance.ShootShake(camShakeAmount * aimingCamShakeMultiplier * crouchingCamShakeMultiplier);
                if (weapon.useProceduralShot) ProceduralShot.Instance.Shoot(weapon.proceduralShotPattern);

                // Determine if we want to add an effect for FOV
                if (weapon.applyFOVEffectOnShooting)
                {
                    float fovAdjustment = isAiming ? weapon.AimingFOVValueToSubtract : weapon.FOVValueToSubtract;
                    mainCamera.fieldOfView -= fovAdjustment;
                }
                foreach (var p in firePoint)
                {
                    if (muzzleVFX != null)
                        Instantiate(muzzleVFX, p.position, mainCamera.transform.rotation, mainCamera.transform); // VFX
                }
                CowsinsUtilities.ForcePlayAnim("shooting", inventory[currentWeapon].GetComponentInChildren<Animator>());
                if (weapon.timeBetweenShots != 0) SoundManager.Instance.PlaySound(fireSFX, 0, weapon.pitchVariationFiringSFX, true, 0);

                ProgressRecoil();

                if (style == 0) HitscanShot();
                else if (style == 1)
                {
                    yield return new WaitForSeconds(weapon.shootDelay);
                    ProjectileShot();
                }

                yield return new WaitForSeconds(weapon.timeBetweenShots);
                i++;
            }
            shooting = false;
            yield break;
        }
        /// <summary>
        /// Hitscan weapons send a raycast that IMMEDIATELY hits the target.
        /// That is why this shooting method is mostly used for pistols, snipers, rifles or SMGs
        /// </summary>
        private void HitscanShot()
        {
            events.OnShoot.Invoke();
            if (resizeCrosshair && UIController.instance.crosshair != null) UIController.instance.crosshair.Resize(weapon.crosshairResize * 100);

            Transform hitObj;

            //This defines the first hit on the object
            Vector3 dir = CowsinsUtilities.GetSpreadDirection(spread, mainCamera);
            Ray ray = new Ray(mainCamera.transform.position, dir);

            if (Physics.Raycast(ray, out hit, weapon.bulletRange, hitLayer))
            {
                float dmg = damagePerBullet * stats.damageMultiplier;
                Hit(hit.collider.gameObject.layer, dmg, hit, true);
                hitObj = hit.collider.transform;

                if (hit.transform.TryGetComponent<Rigidbody>(out Rigidbody rb))
                {
                    rb.AddForceAtPosition(ray.direction * 10, hit.point, ForceMode.Impulse);
                }

                //Handle Penetration
                Ray newRay = new Ray(hit.point, ray.direction);
                RaycastHit newHit;

                if (Physics.Raycast(newRay, out newHit, penetrationAmount, hitLayer))
                {
                    if (hitObj != newHit.collider.transform)
                    {
                        float dmg_ = damagePerBullet * stats.damageMultiplier * weapon.penetrationDamageReduction;
                        Hit(newHit.collider.gameObject.layer, dmg_, newHit, true);
                    }
                }

                // Handle Bullet Trails
                if (weapon.bulletTrail == null) return;

                foreach (var p in firePoint)
                {
                    TrailRenderer trail = Instantiate(weapon.bulletTrail, p.position, Quaternion.identity);

                    StartCoroutine(SpawnTrail(trail, hit));
                }
            }
        }

        private IEnumerator SpawnTrail(TrailRenderer trail, RaycastHit hit)
        {
            float time = 0;
            Vector3 startPos = trail.transform.position;

            while (time < 1)
            {
                trail.transform.position = Vector3.Lerp(startPos, hit.point, time);
                time += Time.deltaTime / trail.time;

                yield return null;
            }

            trail.transform.position = hit.point;

            Destroy(trail.gameObject, trail.time);
        }
        /// <summary>
        /// projectile shooting spawns a projectile
        /// Add a rigidbody to your bullet gameObject to make a curved trajectory
        /// This method is pretty much always used for grenades, rocket lFaunchers and grenade launchers.
        /// </summary>
        private void ProjectileShot()
        {
            events.OnShoot.Invoke();
            if (resizeCrosshair && UIController.instance.crosshair != null) UIController.instance.crosshair.Resize(weapon.crosshairResize * 100);

            Ray ray = mainCamera.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
            Vector3 destination = (Physics.Raycast(ray, out hit) && !hit.transform.CompareTag("Player")) ? destination = hit.point + CowsinsUtilities.GetSpreadDirection(weapon.spreadAmount, mainCamera) : destination = ray.GetPoint(50f) + CowsinsUtilities.GetSpreadDirection(weapon.spreadAmount, mainCamera);

            foreach (var p in firePoint)
            {
                Bullet bullet = Instantiate(weapon.projectile, p.position, p.transform.rotation) as Bullet;

                if (weapon.explosionOnHit) bullet.explosionVFX = weapon.explosionVFX;

                bullet.hurtsPlayer = weapon.hurtsPlayer;
                bullet.explosionOnHit = weapon.explosionOnHit;
                bullet.explosionRadius = weapon.explosionRadius;
                bullet.explosionForce = weapon.explosionForce;

                bullet.criticalMultiplier = weapon.criticalDamageMultiplier;
                bullet.destination = destination;
                bullet.player = this.transform;
                bullet.speed = weapon.speed;
                bullet.GetComponent<Rigidbody>().isKinematic = (!weapon.projectileUsesGravity) ? true : false;
                bullet.damage = damagePerBullet * stats.damageMultiplier;
                bullet.duration = weapon.bulletDuration;
            }
        }
        /// <summary>
        /// Moreover, cowsins´ FPS ENGINE also supports melee attacking
        /// Use this for Swords, knives etc
        /// </summary>
        public void MeleeAttack(float attackRange, float damage)
        {
            events.OnShoot.Invoke();

            Vector3 basePosition = id != null ? id.transform.position : transform.position;
           Collider[] col = Physics.OverlapSphere(basePosition + mainCamera.transform.parent.forward * attackRange / 2, attackRange, hitLayer);

            float dmg = damage * stats.damageMultiplier;

            foreach (var c in col)
            {
                if (c.CompareTag("Critical") || c.CompareTag("BodyShot"))
                {
                    CowsinsUtilities.GatherDamageableParent(c.transform).Damage(dmg, false);
                    break;
                }
                else if (c.GetComponent<IDamageable>() != null)
                {
                    c.GetComponent<IDamageable>().Damage(dmg, false);
                    break;
                }

            }

            //VISUALS
            Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
            if (Physics.Raycast(ray, out hit, attackRange, hitLayer))
            {
                Hit(hit.collider.gameObject.layer, 0f, hit, false);
            }
        }
        public void SecondaryMeleeAttack()
        {
            CanMelee = false;
            CowsinsUtilities.PlayAnim("hit", holsterMotionObject);
            meleeObject.SetActive(true);
            Invoke("Melee", meleeDelay);
        }

        private void Melee()
        {
            MeleeAttack(meleeRange, meleeAttackDamage);
            CamShake.instance.ShootShake(meleeCamShakeAmount * aimingCamShakeMultiplier * crouchingCamShakeMultiplier);
        }

        public void FinishMelee()
        {
            CowsinsUtilities.PlayAnim("finished", holsterMotionObject);
            meleeObject.SetActive(false);
            Invoke("ReEnableMelee", reEnableMeleeAfterAction);
        }

        private void ReEnableMelee() => CanMelee = true;

        /// <summary>
        /// If you landed a shot onto an enemy, a hit will occur
        /// This is where that is being handled
        /// </summary>
        private void Hit(LayerMask layer, float damage, RaycastHit h, bool damageTarget)
        {
            events.OnHit.Invoke();
            GameObject impact = null, impactBullet = null;

            // Check the passed layer
            // If it matches any of the provided layers by FPS Engine, then:
            // Instantiate according effect and rotate it accordingly to the surface.
            // Instantiate bullet holes as well.
            switch (layer)
            {
                case int l when l == LayerMask.NameToLayer("Grass"):
                    impact = Instantiate(effects.grassImpact, h.point, Quaternion.identity); // Grass
                    impact.transform.rotation = Quaternion.LookRotation(h.normal);
                    if (weapon != null)
                        impactBullet = Instantiate(weapon.bulletHoleImpact.grassImpact, h.point, Quaternion.identity);
                    break;
                case int l when l == LayerMask.NameToLayer("Metal"):
                    impact = Instantiate(effects.metalImpact, h.point, Quaternion.identity); // Metal
                    impact.transform.rotation = Quaternion.LookRotation(h.normal);
                    if (weapon != null) impactBullet = Instantiate(weapon.bulletHoleImpact.metalImpact, h.point, Quaternion.identity);
                    break;
                case int l when l == LayerMask.NameToLayer("Mud"):
                    impact = Instantiate(effects.mudImpact, h.point, Quaternion.identity); // Mud
                    impact.transform.rotation = Quaternion.LookRotation(h.normal);
                    if (weapon != null) impactBullet = Instantiate(weapon.bulletHoleImpact.mudImpact, h.point, Quaternion.identity);
                    break;
                case int l when l == LayerMask.NameToLayer("Wood"):
                    impact = Instantiate(effects.woodImpact, h.point, Quaternion.identity); // Wood
                    impact.transform.rotation = Quaternion.LookRotation(h.normal);
                    if (weapon != null) impactBullet = Instantiate(weapon.bulletHoleImpact.woodImpact, h.point, Quaternion.identity);
                    break;
                case int l when l == LayerMask.NameToLayer("Enemy"):
                    impact = Instantiate(effects.enemyImpact, h.point, Quaternion.identity); // Enemy
                    impact.transform.rotation = Quaternion.LookRotation(h.normal);
                    if (weapon != null) impactBullet = Instantiate(weapon.bulletHoleImpact.enemyImpact, h.point, Quaternion.identity);
                    break;
                default:
                    impact = Instantiate(effects.metalImpact, h.point, Quaternion.identity);
                    impact.transform.rotation = Quaternion.LookRotation(h.normal);
                    if (weapon != null) impactBullet = Instantiate(weapon.bulletHoleImpact.groundIMpact, h.point, Quaternion.identity);
                    break;
            }

            if (h.collider != null && impactBullet != null)
            {
                impactBullet.transform.rotation = Quaternion.LookRotation(h.normal);
                impactBullet.transform.SetParent(h.collider.transform);
            }

            // Apply damage
            if (!damageTarget)
            {
                return;
            }
            float finalDamage = damage * GetDistanceDamageReduction(h.collider.transform);

            // Check if a head shot was landed
            if (h.collider.gameObject.CompareTag("Critical"))
            {
                CowsinsUtilities.GatherDamageableParent(h.collider.transform).Damage(finalDamage * weapon.criticalDamageMultiplier, true);
            }
            // Check if a body shot was landed ( for children colliders )
            else if (h.collider.gameObject.CompareTag("BodyShot"))
            {
                CowsinsUtilities.GatherDamageableParent(h.collider.transform).Damage(finalDamage, false);
            }
            // Check if the collision just comes from the parent
            else if (h.collider.GetComponent<IDamageable>() != null)
            {
                h.collider.GetComponent<IDamageable>().Damage(finalDamage, false);
            }
        }


        private void CanShoot() => canShoot = true;

        private void FinishedSelection() => selectingWeapon = false;

        public void StartReload() => StartCoroutine(reload());

        /// <summary>
        /// Handle Reloading
        /// </summary>
        private IEnumerator DefaultReload()
        {

            // Play reload sound
            SoundManager.Instance.PlaySound(id.bulletsLeftInMagazine == 0 ? weapon.audioSFX.emptyMagReload : weapon.audioSFX.reload, .1f, 0, true, 0);
            reloading = true;
            yield return new WaitForSeconds(.001f);

            // Play animation
            CowsinsUtilities.PlayAnim("reloading", inventory[currentWeapon].GetComponentInChildren<Animator>());

            // Wait reloadTime seconds, assigned in the weapon scriptable object.
            yield return new WaitForSeconds(reloadTime);

            // Reload has finished
            events.OnFinishReload.Invoke();

            canShoot = true;
            if (!reloading) yield break;

            // Run custom event
            events.OnReload.Invoke();

            reloading = false;

            // Set the proper amount of bullets, depending on magazine type.
            if (!weapon.limitedMagazines) id.bulletsLeftInMagazine = id.magazineSize;
            else
            {
                if (id.totalBullets > id.magazineSize) // You can still reload a full magazine
                {
                    id.totalBullets = id.totalBullets - (id.magazineSize - id.bulletsLeftInMagazine);
                    id.bulletsLeftInMagazine = id.magazineSize;
                }
                else if (id.totalBullets == id.magazineSize) // You can only reload a single full magazine more
                {
                    id.totalBullets = id.totalBullets - (id.magazineSize - id.bulletsLeftInMagazine);
                    id.bulletsLeftInMagazine = id.magazineSize;
                }
                else if (id.totalBullets < id.magazineSize) // You cant reload a whole magazine
                {
                    int bulletsLeft = id.bulletsLeftInMagazine;
                    if (id.bulletsLeftInMagazine + id.totalBullets <= id.magazineSize)
                    {
                        id.bulletsLeftInMagazine = id.bulletsLeftInMagazine + id.totalBullets;
                        if (id.totalBullets - (id.magazineSize - bulletsLeft) >= 0) id.totalBullets = id.totalBullets - (id.magazineSize - bulletsLeft);
                        else id.totalBullets = 0;
                    }
                    else
                    {
                        int ToAdd = id.magazineSize - id.bulletsLeftInMagazine;
                        id.bulletsLeftInMagazine = id.bulletsLeftInMagazine + ToAdd;
                        if (id.totalBullets - ToAdd >= 0) id.totalBullets = id.totalBullets - ToAdd;
                        else id.totalBullets = 0;
                    }
                }
            }
        }

        private IEnumerator OverheatReload()
        {
            // Currently reloading
            canShoot = false;

            float waitTime = weapon.cooledPercentageAfterOverheat;

            // Stop being able to shoot, prevents from glitches
            CancelInvoke(nameof(CanShoot));

            // Wait until the heat ratio is appropriate to keep shooting
            yield return new WaitUntil(() => id.heatRatio <= waitTime);

            // Reload has finished
            events.OnFinishReload.Invoke();

            reloading = false;
            canShoot = true;
        }

        // On shooting regular reloading weapons, reduce the bullets in the magazine
        private void ReduceDefaultAmmo()
        {
            if (!weapon.infiniteBullets)
            {
                id.bulletsLeftInMagazine -= weapon.ammoCostPerFire;
                if (id.bulletsLeftInMagazine < 0)
                {
                    id.bulletsLeftInMagazine = 0;
                }
            }
        }


        // On shooting overheat reloading weapons, increase the heat ratio.
        private void ReduceOverheatAmmo()
        {
            id.heatRatio += (float)1f / id.magazineSize;
        }

        // Handles overheat weapons reloading.
        private void HandleHeatRatio()
        {
            if (weapon == null || id.magazineSize == 0 || weapon.reloadStyle == ReloadingStyle.defaultReload) return;

            // Handle cooling
            // Dont keep cooling if it is completely cooled
            if (id.heatRatio > 0) id.heatRatio -= Time.deltaTime * coolSpeed;
            if (id.heatRatio > 1) id.heatRatio = 1;
        }

#if UNITY_EDITOR
        public Preset crosshairPreset;
#endif
        /// <summary>
        /// Active your new weapon
        /// </summary>
        public void UnHolster(GameObject weaponObj, bool playAnim)
        {
            canShoot = true;

            weaponObj.SetActive(true);
            id = weaponObj.GetComponent<WeaponIdentification>();

            // Get Shooting Style 
            // We subscribe our performShootStyle method to different functions depending on the shooting style
            switch ((int)weapon.shootStyle)
            {
                case 0: performShootStyle = HandleHitscanProjectileShot; break;
                case 1: performShootStyle = HandleHitscanProjectileShot; break;
                case 2: performShootStyle = HandleMeleeShot; break;
                case 3: performShootStyle = CustomShot; break;
            }

            weaponObj.GetComponentInChildren<Animator>().enabled = true;
            if (playAnim) CowsinsUtilities.PlayAnim("unholster", inventory[currentWeapon].GetComponentInChildren<Animator>());
            SoundManager.Instance.PlaySound(weapon.audioSFX.unholster, .1f, 0, true, 0);
            Invoke("FinishedSelection", .5f);

            if (weapon.shootStyle == ShootStyle.Custom) SelectCustomShotMethod();
            else customMethod = null;

            // Grab the modifiers for the custom set of attachments.
            GetAttachmentsModifiers();

            weaponAnimator.StopWalkAndRunMotion();

            // Define reloading method
            if (weapon.reloadStyle == ReloadingStyle.defaultReload)
            {
                reload = DefaultReload;
                reduceAmmo = ReduceDefaultAmmo;
            }
            else
            {
                reload = OverheatReload;
                reduceAmmo = ReduceOverheatAmmo;
                coolSpeed = weapon.coolSpeed;
            }

            firePoint = inventory[currentWeapon].FirePoint;

            // UI & OTHERS
            if (weapon.infiniteBullets || weapon.reloadStyle == ReloadingStyle.Overheat)
            {
                UIEvents.onDetectReloadMethod?.Invoke(false, !weapon.infiniteBullets);
            }
            else
            {
                UIEvents.onDetectReloadMethod?.Invoke(true, false);
            }

            if ((int)weapon.shootStyle == 2) UIEvents.onDetectReloadMethod?.Invoke(false, false);

            UIEvents.setWeaponDisplay?.Invoke(weapon);
        }

        /// <summary>
        /// Equips the passed attachment.
        /// </summary>
        /// <param name="attachment">Attachment to equip.</param>
        /// <param name="attachmentID">Order ID of the attachment in the WeaponIdentification's compatible attachment array.</param>
        public void AssignNewAttachment(Attachment attachment, int attachmentID)
        {
            AssignAttachmentToWeapon(attachment, attachmentID, currentWeapon);
        }

        /// <summary>
        /// Equips the passed attachment on a specific weapon.
        /// </summary>
        /// <param name="attachment">Attachment to equip.</param>
        /// <param name="attachmentID">Order ID of the attachment in the WeaponIdentification's compatible attachment array.</param>
        /// <param name="index">Inventory index of the weapon to equip the attachment on.</param>
        public void AssignNewAttachmentOnSpecificWeapon(Attachment attachment, int attachmentID, int index)
        {
            AssignAttachmentToWeapon(attachment, attachmentID, index);
        }


        /// <summary>
        /// Equips the passed attachment. 
        /// </summary>
        /// <param name="attachment">Attachment to equip</param>
        /// <param name="attachmentID">Order ID of the attachment to equip in the WeaponIdentification compatible attachment array.</param>
        public void AssignAttachmentToWeapon(Attachment attachment, int attachmentID, int index)
        {
            WeaponIdentification curWeapon = inventory[index];

            // Depending on what type of attachment it is:
            // Swap attachment if there is an existing equipped attachment, otherwise just equip it.
            switch (attachment)
            {
                case Barrel barrel:
                    SwapAttachment(curWeapon.barrel, curWeapon.compatibleAttachments.barrels[attachmentID], curWeapon.defaultAttachments.defaultBarrel);
                    curWeapon.barrel = curWeapon.compatibleAttachments.barrels[attachmentID];
                    break;

                case Scope scope:
                    SwapAttachment(curWeapon.scope, curWeapon.compatibleAttachments.scopes[attachmentID], curWeapon.defaultAttachments.defaultScope);
                    curWeapon.scope = curWeapon.compatibleAttachments.scopes[attachmentID];
                    break;

                case Stock stock:
                    SwapAttachment(curWeapon.stock, curWeapon.compatibleAttachments.stocks[attachmentID], curWeapon.defaultAttachments.defaultStock);
                    curWeapon.stock = curWeapon.compatibleAttachments.stocks[attachmentID];
                    break;

                case Grip grip:
                    SwapAttachment(curWeapon.grip, curWeapon.compatibleAttachments.grips[attachmentID], curWeapon.defaultAttachments.defaultGrip);
                    curWeapon.grip = curWeapon.compatibleAttachments.grips[attachmentID];
                    break;

                case Magazine magazine:
                    SwapAttachment(curWeapon.magazine, curWeapon.compatibleAttachments.magazines[attachmentID], curWeapon.defaultAttachments.defaultMagazine);
                    curWeapon.magazine = curWeapon.compatibleAttachments.magazines[attachmentID];
                    curWeapon.GetMagazineSize();
                    break;

                case Flashlight flashlight:
                    SwapAttachment(curWeapon.flashlight, curWeapon.compatibleAttachments.flashlights[attachmentID], curWeapon.defaultAttachments.defaultFlashlight);
                    curWeapon.flashlight = curWeapon.compatibleAttachments.flashlights[attachmentID];
                    break;

                case Laser laser:
                    SwapAttachment(curWeapon.laser, curWeapon.compatibleAttachments.lasers[attachmentID], curWeapon.defaultAttachments.defaultLaser);
                    curWeapon.laser = curWeapon.compatibleAttachments.lasers[attachmentID];
                    break;
            }

            // Finally unholster the weapon but dont play any animation.
            // We require to unholster the weapons for every setting to work properly.
            UnHolster(inventory[currentWeapon].gameObject, false);
        }



        // Assigns a new attachment.
        // if there is an attachment currently, it will be disabled and dropped.
        private void SwapAttachment(Attachment currentAttachment, Attachment newAttachment, Attachment defaultAttachment)
        {
            // Disable current attachment if it exists
            currentAttachment?.gameObject.SetActive(false);
            // Drop it in case it is not a default attachment
            if (currentAttachment != null && currentAttachment != defaultAttachment)
                GetComponent<InteractManager>().DropAttachment(currentAttachment, false);
            // Enable new attachment
            newAttachment.gameObject.SetActive(true);
        }
        private void GetAttachmentsModifiers()
        {
            // Grab references for the variables in case their respective attachments are null or not.

            fireSFX = id.barrel != null ? id.barrel.GetComponent<Barrel>().supressedFireSFX : weapon.audioSFX.shooting[Random.Range(0, weapon.audioSFX.shooting.Length - 1)];

            aimRot = id.scope != null && id.scope != id.defaultAttachments.defaultScope
                ? id.scope.GetComponent<Scope>().aimingRotation
                : weapon.aimingRotation; // Get the weapon aimingRotation
            muzzleVFX = id.barrel != null && id.barrel.GetComponent<Barrel>().uniqueMuzzleFlashVFX != null && weapon.muzzleVFX != null
                ? id.barrel.GetComponent<Barrel>().uniqueMuzzleFlashVFX
                : weapon.muzzleVFX;

            // We assign each value and save them for later
            float baseReloadTime = weapon.reloadTime;
            reloadTime = baseReloadTime;

            float baseAimSpeed = weapon.aimingSpeed;
            aimingSpeed = baseAimSpeed;

            float baseFireRate = weapon.fireRate;
            fireRate = baseFireRate;

            float baseDamagePerBullet = weapon.damagePerBullet;
            damagePerBullet = baseDamagePerBullet;

            float baseCamShakeAmount = weapon.camShakeAmount;
            camShakeAmount = baseCamShakeAmount;

            float basePenetrationAmount = weapon.penetrationAmount;
            penetrationAmount = basePenetrationAmount;

            // Make an array where we will store all the attachment types
            Attachment[] attachments = new Attachment[] {
        inventory[currentWeapon].barrel,
        inventory[currentWeapon].scope,
        inventory[currentWeapon].stock,
        inventory[currentWeapon].grip,
        inventory[currentWeapon].magazine,
        inventory[currentWeapon].flashlight,
        inventory[currentWeapon].laser
    };

            // Iterate through each attachment and update the modifiers
            foreach (Attachment attachment in attachments)
            {
                if (attachment != null)
                {
                    reloadTime += attachment.reloadSpeedIncrease * baseReloadTime;
                    aimingSpeed += attachment.aimSpeedIncrease * baseAimSpeed;
                    fireRate += attachment.fireRateIncrease * baseFireRate;
                    damagePerBullet += attachment.damageIncrease * baseDamagePerBullet;
                    camShakeAmount += attachment.cameraShakeMultiplier * baseCamShakeAmount;
                    penetrationAmount += attachment.penetrationIncrease * basePenetrationAmount;
                }
            }
        }

        private void HandleUI()
        {

            // If we dont own a weapon yet, do not continue
            if (weapon == null)
            {
                UIEvents.disableWeaponUI?.Invoke();
                return;
            }

            UIEvents.enableWeaponDisplay?.Invoke();

            if (weapon.reloadStyle == ReloadingStyle.defaultReload)
            {
                if (!weapon.infiniteBullets)
                {

                    bool activeReloadUI = id.bulletsLeftInMagazine == 0 && !autoReload && !weapon.infiniteBullets;
                    bool activeLowAmmoUI = id.bulletsLeftInMagazine < id.magazineSize / 3.5f && id.bulletsLeftInMagazine > 0;
                    // Set different display settings for each shoot style 
                    if (weapon.limitedMagazines)
                    {
                        UIEvents.onBulletsChanged?.Invoke(id.bulletsLeftInMagazine, id.totalBullets, activeReloadUI, activeLowAmmoUI);
                    }
                    else
                    {
                        UIEvents.onBulletsChanged?.Invoke(id.bulletsLeftInMagazine, id.magazineSize, activeReloadUI, activeLowAmmoUI);
                    }
                }
                else
                {
                    UIEvents.onBulletsChanged?.Invoke(id.bulletsLeftInMagazine, id.totalBullets, false, false);
                }
            }
            else
            {
                UIEvents.onHeatRatioChanged?.Invoke(id.heatRatio);
            }



            //Crosshair Management
            // If we dont use a crosshair stop right here
            if (UIController.instance.crosshair == null)
            {
                return;
            }
            // Detect enemies on aiming
            RaycastHit hit_;
            if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit_, weapon.bulletRange) && hit_.transform.CompareTag("Enemy") || Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out hit_, weapon.bulletRange) && hit_.transform.CompareTag("Critical"))
                UIController.instance.crosshair.SpotEnemy(true);
            else UIController.instance.crosshair.SpotEnemy(false);
        }
        /// <summary>
        /// Procedurally generate the Inventory UI depending on your needs
        /// </summary>
        private void CreateInventoryUI()
        {
            // Adjust the inventory size 
            slots = new UISlot[inventorySize];
            int j = 0; // Control variable
            while (j < inventorySize)
            {
                // Load the slot, instantiate it and set it to the slots array
                var slot = Instantiate(Resources.Load("InventoryUISlot"), Vector3.zero, Quaternion.identity, UIController.instance.inventoryContainer.transform) as GameObject;
                slot.GetComponent<UISlot>().id = j;
                slots[j] = slot.GetComponent<UISlot>();
                j++;
            }
        }

        /// <summary>
        /// Change you current slots, core of the inventory
        /// </summary>
        public void HandleInventory()
        {
            if (InputManager.reloading) return; // Do not change weapons while reloading
                                                // Change slot
            if (InputManager.scrolling > 0 || InputManager.previousweapon)
            {
                ForceAimReset(); // Move Weapon back to the original position
                if (currentWeapon < inventorySize - 1)
                {
                    currentWeapon++;
                    SelectWeapon();
                }
            }
            if (InputManager.scrolling < 0 || InputManager.nextweapon)
            {
                ForceAimReset(); // Move Weapon back to the original position
                if (currentWeapon > 0)
                {
                    currentWeapon--;
                    SelectWeapon();
                }
            }
        }

        [HideInInspector] public bool selectingWeapon;
        public void SelectWeapon()
        {
            canShoot = false;
            selectingWeapon = true;
            UIController.instance.crosshair.SpotEnemy(false);
            events.OnInventorySlotChanged.Invoke(); // Invoke your custom method
            weapon = null;
            // Spawn the appropriate weapon in the inventory

            foreach (WeaponIdentification weapon_ in inventory)
            {
                if (weapon_ != null)
                {
                    weapon_.gameObject.SetActive(false);
                    weapon_.GetComponentInChildren<Animator>().enabled = false;
                    if (weapon_ == inventory[currentWeapon])
                    {
                        weapon = inventory[currentWeapon].weapon;

                        weapon_.GetComponentInChildren<Animator>().enabled = true;
                        UnHolster(weapon_.gameObject, true);

#if UNITY_EDITOR
                        UIController.instance.crosshair.GetComponent<CrosshairShape>().currentPreset = weapon.crosshairPreset;
                        CowsinsUtilities.ApplyPreset(UIController.instance.crosshair.GetComponent<CrosshairShape>().currentPreset, UIController.instance.crosshair.GetComponent<CrosshairShape>());
#endif
                    }
                }
            }

            // Handle the UI Animations
            foreach (UISlot slot in slots)
            {
                slot.transform.localScale = slot.initScale;
                slot.GetComponent<CanvasGroup>().alpha = .2f;
            }
            slots[currentWeapon].transform.localScale = slots[currentWeapon].transform.localScale * 1.2f;
            slots[currentWeapon].GetComponent<CanvasGroup>().alpha = 1;

            CancelInvoke(nameof(CanShoot));

            events.OnEquipWeapon.Invoke(); // Invoke your custom method

        }

        private void GetInitialWeapons()
        {
            if (initialWeapons.Length == 0) return;

            int i = 0;
            while (i < initialWeapons.Length)
            {
                InstantiateWeapon(initialWeapons[i], i, null, null);
                i++;
            }
            weapon = initialWeapons[0];
        }

        public void InstantiateWeapon(Weapon_SO newWeapon, int inventoryIndex, int? _bulletsLeftInMagazine, int? _totalBullets)
        {
            // Instantiate Weapon
            var weaponPicked = Instantiate(newWeapon.weaponObject, weaponHolder);
            weaponPicked.transform.localPosition = newWeapon.weaponObject.transform.localPosition;

            // Destroy the Weapon if it already exists in the same slot
            if (inventory[inventoryIndex] != null) Destroy(inventory[inventoryIndex].gameObject);

            // Set the Weapon
            inventory[inventoryIndex] = weaponPicked;

            // Select weapon if it is the current Weapon
            if (inventoryIndex == currentWeapon)
            {
                weapon = newWeapon;
            }
            else weaponPicked.gameObject.SetActive(false);

            List<AttachmentIdentifier_SO> attachments = null;
            int magCapacityAdded = 0;

            (attachments, magCapacityAdded) = weaponPicked.GetDefaultAttachments();

            foreach (AttachmentIdentifier_SO attachment in attachments)
            {
                (Attachment atc, int id) = GetAttachmentID(attachment, inventory[inventoryIndex]);
                AssignNewAttachment(atc, id);
            }
            // if _bulletsLeftInMagazine is null, calculate magazine size. If not, simply assign _bulletsLeftInMagazine
            inventory[inventoryIndex].bulletsLeftInMagazine = _bulletsLeftInMagazine ?? (newWeapon.magazineSize + magCapacityAdded);
            inventory[inventoryIndex].totalBullets = _totalBullets ??
            (newWeapon.limitedMagazines
                ? newWeapon.magazineSize * newWeapon.totalMagazines
                : newWeapon.magazineSize);

            //UI
            slots[inventoryIndex].weapon = newWeapon;
            slots[inventoryIndex].GetImage();
#if UNITY_EDITOR
            UIController.instance.crosshair.GetComponent<CrosshairShape>().currentPreset = weapon?.crosshairPreset;
            if (UIController.instance.crosshair.GetComponent<CrosshairShape>().currentPreset)
                CowsinsUtilities.ApplyPreset(UIController.instance.crosshair.GetComponent<CrosshairShape>().currentPreset, UIController.instance.crosshair.GetComponent<CrosshairShape>());
#endif

            if (weapon?.shootStyle == ShootStyle.Custom) SelectCustomShotMethod();
            else customMethod = null;

            if (inventoryIndex == currentWeapon) SelectWeapon();

        }

        /// <summary>
        /// Grabs the attachment object and the id given an attachment identifier
        /// </summary>
        /// <param name="atcToCheck">Attachment object to get information about returned.</param>
        /// <param name="wID">Weapon Identification that holds the attachments</param>
        /// <returns></returns>
        private (Attachment, int) GetAttachmentID(AttachmentIdentifier_SO atcToCheck, WeaponIdentification wID)
        {
            // Check for compatibility
            for (int i = 0; i < wID.compatibleAttachments.barrels.Length; i++)
            {
                if (atcToCheck == wID.compatibleAttachments.barrels[i].attachmentIdentifier) return (wID.compatibleAttachments.barrels[i], i);
            }
            for (int i = 0; i < wID.compatibleAttachments.scopes.Length; i++)
            {
                if (atcToCheck == wID.compatibleAttachments.scopes[i].attachmentIdentifier) return (wID.compatibleAttachments.scopes[i], i);
            }
            for (int i = 0; i < wID.compatibleAttachments.stocks.Length; i++)
            {
                if (atcToCheck == wID.compatibleAttachments.stocks[i].attachmentIdentifier) return (wID.compatibleAttachments.stocks[i], i);
            }
            for (int i = 0; i < wID.compatibleAttachments.grips.Length; i++)
            {
                if (atcToCheck == wID.compatibleAttachments.grips[i].attachmentIdentifier) return (wID.compatibleAttachments.grips[i], i);
            }
            for (int i = 0; i < wID.compatibleAttachments.magazines.Length; i++)
            {
                if (atcToCheck == wID.compatibleAttachments.magazines[i].attachmentIdentifier) return (wID.compatibleAttachments.magazines[i], i);
            }
            for (int i = 0; i < wID.compatibleAttachments.flashlights.Length; i++)
            {
                if (atcToCheck == wID.compatibleAttachments.flashlights[i].attachmentIdentifier) return (wID.compatibleAttachments.flashlights[i], i);
            }
            for (int i = 0; i < wID.compatibleAttachments.lasers.Length; i++)
            {
                if (atcToCheck == wID.compatibleAttachments.lasers[i].attachmentIdentifier) return (wID.compatibleAttachments.lasers[i], i);
            }

            // Return an error
            return (null, -1);
        }

        public void ReleaseCurrentWeapon()
        {
            Destroy(inventory[currentWeapon].gameObject);
            weapon = null;
            slots[currentWeapon].weapon = null;
        }
        private float GetDistanceDamageReduction(Transform target)
        {
            if (!weapon.applyDamageReductionBasedOnDistance) return 1;
            if (Vector3.Distance(target.position, transform.position) > weapon.minimumDistanceToApplyDamageReduction)
                return (weapon.minimumDistanceToApplyDamageReduction / Vector3.Distance(target.position, transform.position)) * weapon.damageReductionMultiplier;
            else return 1;
        }

        private void ManageWeaponMethodsInputs()
        {
            if (!InputManager.shooting) holding = false; // Making sure we are not holding}
        }

        private float evaluationProgress, evaluationProgressX;
        private void HandleRecoil()
        {
            if (weapon == null || !weapon.applyRecoil)
            {
                cameraPivot.localRotation = Quaternion.Lerp(cameraPivot.localRotation, Quaternion.Euler(Vector3.zero), 3 * Time.deltaTime);
                return;
            }

            // Going back to normal shooting; 
            float speed = (weapon == null) ? 10 : weapon.recoilRelaxSpeed * 3;
            if (!InputManager.shooting || reloading || !PlayerStats.Controllable)
            {
                cameraPivot.localRotation = Quaternion.Lerp(cameraPivot.localRotation, Quaternion.Euler(Vector3.zero), speed * Time.deltaTime);
                evaluationProgress = 0;
                evaluationProgressX = 0;
            }

            if (weapon == null || reloading || !PlayerStats.Controllable) return;

            if (InputManager.shooting)
            {
                float xamount = (weapon.applyDifferentRecoilOnAiming && isAiming) ? weapon.xRecoilAmountOnAiming : weapon.xRecoilAmount;
                float yamount = (weapon.applyDifferentRecoilOnAiming && isAiming) ? weapon.yRecoilAmountOnAiming : weapon.yRecoilAmount;

                cameraPivot.localRotation = Quaternion.Lerp(cameraPivot.localRotation, Quaternion.Euler(new Vector3(-weapon.recoilY.Evaluate(evaluationProgress) * yamount, -weapon.recoilX.Evaluate(evaluationProgressX) * xamount, 0)), 10 * Time.deltaTime);
            }
        }

        private void ProgressRecoil()
        {
            if (weapon.applyRecoil)
            {
                evaluationProgress += 1f / weapon.magazineSize;
                evaluationProgressX += 1f / weapon.magazineSize;
            }
        }

        public void ToggleFlashLight()
        {
            Flashlight flashlightComponent = inventory[currentWeapon].flashlight.GetComponent<Flashlight>();

            GameObject lightSource = flashlightComponent.lightSource.gameObject;
            bool newLightState = !lightSource.activeSelf;

            flashlightComponent.EnableFlashLight(newLightState);
            flashlightComponent.CheckIfCanTurnOn(newLightState);

            AudioClip soundToPlay = newLightState ? flashlightComponent.turnOnSFX : flashlightComponent.turnOffSFX;
            SoundManager.Instance.PlaySound(soundToPlay, 0, 0, true, 0);
        }

        public void InitializeInspection() => CowsinsUtilities.PlayAnim("inspect", inventory[currentWeapon].GetComponentInChildren<Animator>());

        public void DisableInspection() => CowsinsUtilities.PlayAnim("finishedInspect", inventory[currentWeapon].GetComponentInChildren<Animator>());

        private void InitialSettings()
        {
            stats = GetComponent<PlayerStats>();
            weaponAnimator = GetComponent<WeaponAnimator>();
            inventory = new WeaponIdentification[inventorySize];
            currentWeapon = 0;
            canShoot = true;
            mainCamera.fieldOfView = GetComponent<PlayerMovement>().normalFOV;
            CanMelee = true;
        }
    }
}