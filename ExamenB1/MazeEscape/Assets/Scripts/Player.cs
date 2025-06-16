using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    [Header("🏃 Configuración de Movimiento")]
    [SerializeField] private float baseSpeed = 5.0f;
    [SerializeField] private float boostSpeed = 9.0f;
    [SerializeField] private float boostDuration = 2.0f;
    [SerializeField] private float boostCooldown = 5.0f;
    [SerializeField] private float accelerationTime = 0.1f;
    
    [Header("🎮 Configuración del Juego")]
    [SerializeField] private int keysNeededToWin = 4;
    [SerializeField] private float comboResetTime = 3.0f;
    
    [Header("🎨 Efectos Visuales")]
    [SerializeField] private ParticleSystem walkParticles;
    [SerializeField] private ParticleSystem sprintParticles;
    [SerializeField] private ParticleSystem keyCollectParticles;
    [SerializeField] private ParticleSystem wallHitParticles;
    [SerializeField] private TrailRenderer playerTrail;
    [SerializeField] private Animator playerAnimator;
    
    [Header("🔊 Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip walkSound;
    [SerializeField] private AudioClip sprintSound;
    [SerializeField] private AudioClip keyCollectSound;
    [SerializeField] private AudioClip wallHitSound;
    [SerializeField] private AudioClip winSound;
    [SerializeField] private AudioClip deathSound;
    
    [Header("📱 UI Referencias")]
    [SerializeField] private Text keyAmountText;  // Cambiar a Text normal
    [SerializeField] private Text winText;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text comboText;
    [SerializeField] private Image sprintBar;
    [SerializeField] private Image healthBar;
    [SerializeField] private GameObject sprintIndicator;
    [SerializeField] private Canvas gameUI;
    
    [Header("🏰 Referencias de Objetos")]
    [SerializeField] private GameObject door;
    [SerializeField] private Camera mainCamera;
    
    [Header("⚡ Power-ups")]
    [SerializeField] private float baseInvulnerabilityTime = 0.1f;
    
    // Variables de estado
    private float currentSpeed;
    private float targetSpeed;
    private bool canBoost = true;
    private bool isSprintActive = false;
    private bool isInvulnerable = false;
    private int collectedKeys = 0;
    private int currentScore = 0;
    private int currentCombo = 0;
    private float comboTimer = 0f;
    private Vector2 lastMovementDirection;
    private Vector2 currentVelocity;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    
    // Animación y feedback
    private Vector3 originalScale;
    private bool isWalking = false;
    private float walkSoundTimer = 0f;
    private const float WALK_SOUND_INTERVAL = 0.3f;
    
    // Eventos
    public static event Action<int> OnKeyCollected;
    public static event Action<int> OnScoreChanged;
    public static event Action<int> OnComboChanged;
    public static event Action OnPlayerWin;
    public static event Action OnPlayerDeath;

    #region Unity Lifecycle
    
    private void Awake()
    {
        InitializeComponents();
    }
    
    private void Start()
    {
        InitializePlayer();
    }
    
    private void Update()
    {
        HandleInput();
        UpdateMovement();
        UpdateAnimations();
        UpdateUI();
        UpdateCombo();
        UpdateAudio();
    }
    
    private void FixedUpdate()
    {
        ApplyMovement();
    }
    
    #endregion
    
    #region Initialization
    
    private void InitializeComponents()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
            
        if (mainCamera == null)
            mainCamera = Camera.main;
            
        ValidateComponents();
    }
    
    private void InitializePlayer()
    {
        currentSpeed = baseSpeed;
        targetSpeed = baseSpeed;
        originalScale = transform.localScale;
        
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
            
        UpdateAllUI();
        SetupTrail();
        
        // Efecto de spawn
        StartCoroutine(SpawnEffect());
    }
    
    private void ValidateComponents()
    {
        if (rb == null) Debug.LogError("Player necesita un Rigidbody2D!");
        if (spriteRenderer == null) Debug.LogWarning("No se encontró SpriteRenderer");
        if (mainCamera == null) Debug.LogWarning("No se encontró cámara principal");
    }
    
    #endregion
    
    #region Input and Movement
    
    private void HandleInput()
    {
        Vector2 input = GetMovementInput();
        
        if (input != Vector2.zero)
        {
            lastMovementDirection = input;
            targetSpeed = isSprintActive ? boostSpeed : baseSpeed;
            isWalking = true;
        }
        else
        {
            targetSpeed = 0f;
            isWalking = false;
        }
        
        HandleSprintInput();
    }
    
    private Vector2 GetMovementInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        
        // También mantener soporte para flechas
        if (Input.GetKey(KeyCode.LeftArrow)) horizontal = -1f;
        else if (Input.GetKey(KeyCode.RightArrow)) horizontal = 1f;
        
        if (Input.GetKey(KeyCode.UpArrow)) vertical = 1f;
        else if (Input.GetKey(KeyCode.DownArrow)) vertical = -1f;
        
        return new Vector2(horizontal, vertical).normalized;
    }
    
    private void UpdateMovement()
    {
        // Suavizar la velocidad
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime / accelerationTime);
        currentVelocity = lastMovementDirection * currentSpeed;
    }
    
    private void ApplyMovement()
    {
        if (rb != null)
        {
            rb.velocity = currentVelocity;
        }
        else
        {
            transform.Translate(currentVelocity * Time.deltaTime);
        }
    }
    
    #endregion
    
    #region Sprint System
    
    private void HandleSprintInput()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift) && canBoost && !isSprintActive)
        {
            StartCoroutine(SprintCoroutine());
        }
    }
    
    private IEnumerator SprintCoroutine()
    {
        // Efectos de inicio de sprint
        StartSprintEffects();
        
        canBoost = false;
        isSprintActive = true;
        
        // Duración del sprint con barra visual
        yield return StartCoroutine(SprintDurationCoroutine());
        
        // Finalizar sprint
        EndSprintEffects();
        isSprintActive = false;
        
        // Cooldown con barra visual
        yield return StartCoroutine(SprintCooldownCoroutine());
        
        canBoost = true;
        ResetSprintIndicator();
    }
    
    private void StartSprintEffects()
    {
        // Partículas
        if (sprintParticles != null)
            sprintParticles.Play();
            
        // Sonido
        PlaySound(sprintSound);
        
        // Efecto visual del jugador
        StartCoroutine(SprintColorEffect());
        
        // Trail más intenso
        if (playerTrail != null)
        {
            playerTrail.time = 1.0f;
            playerTrail.startWidth = 0.3f;
        }
        
        // Animación de escala
        StartCoroutine(ScaleEffect(1.2f, 0.1f));
    }
    
    private void EndSprintEffects()
    {
        if (sprintParticles != null)
            sprintParticles.Stop();
            
        if (playerTrail != null)
        {
            playerTrail.time = 0.5f;
            playerTrail.startWidth = 0.1f;
        }
    }
    
    private IEnumerator SprintDurationCoroutine()
    {
        float timer = 0f;
        while (timer < boostDuration)
        {
            timer += Time.deltaTime;
            if (sprintBar != null)
                sprintBar.fillAmount = 1f - (timer / boostDuration);
            yield return null;
        }
    }
    
    private IEnumerator SprintCooldownCoroutine()
    {
        float timer = 0f;
        while (timer < boostCooldown)
        {
            timer += Time.deltaTime;
            if (sprintBar != null)
                sprintBar.fillAmount = timer / boostCooldown;
            yield return null;
        }
    }
    
    #endregion
    
    #region Visual Effects
    
    private IEnumerator SpawnEffect()
    {
        transform.localScale = Vector3.zero;
        
        float timer = 0f;
        float duration = 0.5f;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float scale = Mathf.Lerp(0f, 1f, timer / duration);
            transform.localScale = originalScale * scale;
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
    
    private IEnumerator ScaleEffect(float targetScale, float duration)
    {
        Vector3 startScale = transform.localScale;
        Vector3 endScale = originalScale * targetScale;
        
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, endScale, timer / duration);
            yield return null;
        }
        
        // Volver al tamaño original
        timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            transform.localScale = Vector3.Lerp(endScale, originalScale, timer / duration);
            yield return null;
        }
        
        transform.localScale = originalScale;
    }
    
    private IEnumerator SprintColorEffect()
    {
        if (spriteRenderer == null) yield break;
        
        Color sprintColor = Color.yellow;
        float timer = 0f;
        
        while (isSprintActive)
        {
            timer += Time.deltaTime * 10f;
            spriteRenderer.color = Color.Lerp(originalColor, sprintColor, Mathf.Sin(timer) * 0.5f + 0.5f);
            yield return null;
        }
        
        spriteRenderer.color = originalColor;
    }
    
    private void SetupTrail()
    {
        if (playerTrail != null)
        {
            playerTrail.time = 0.5f;
            playerTrail.startWidth = 0.1f;
            playerTrail.endWidth = 0.01f;
        }
    }
    
    private void ResetSprintIndicator()
    {
        if (sprintIndicator != null)
        {
            sprintIndicator.SetActive(true);
            StartCoroutine(BlinkEffect(sprintIndicator, Color.green));
        }
    }
    
    private IEnumerator BlinkEffect(GameObject obj, Color color)
    {
        Image img = obj.GetComponent<Image>();
        if (img == null) yield break;
        
        for (int i = 0; i < 3; i++)
        {
            img.color = color;
            yield return new WaitForSeconds(0.1f);
            img.color = Color.white;
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    #endregion
    
    #region Camera Effects
    
    private void ShakeCamera(float intensity = 0.1f, float duration = 0.1f)
    {
        if (mainCamera != null)
            StartCoroutine(CameraShakeCoroutine(intensity, duration));
    }
    
    private IEnumerator CameraShakeCoroutine(float intensity, float duration)
    {
        Vector3 originalPos = mainCamera.transform.position;
        float timer = 0f;
        
        while (timer < duration)
        {
            float x = UnityEngine.Random.Range(-intensity, intensity);
            float y = UnityEngine.Random.Range(-intensity, intensity);
            
            mainCamera.transform.position = originalPos + new Vector3(x, y, 0);
            
            timer += Time.deltaTime;
            yield return null;
        }
        
        mainCamera.transform.position = originalPos;
    }
    
    #endregion
    
    #region Audio System
    
    private void UpdateAudio()
    {
        if (isWalking && currentSpeed > 0.1f)
        {
            walkSoundTimer += Time.deltaTime;
            if (walkSoundTimer >= WALK_SOUND_INTERVAL)
            {
                PlaySound(walkSound, 0.3f);
                walkSoundTimer = 0f;
            }
        }
        else
        {
            walkSoundTimer = 0f;
        }
    }
    
    private void PlaySound(AudioClip clip, float volume = 1.0f)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip, volume);
        }
    }
    
    #endregion
    
    #region Animation System
    
    private void UpdateAnimations()
    {
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsWalking", isWalking);
            playerAnimator.SetBool("IsSprinting", isSprintActive);
            playerAnimator.SetFloat("Speed", currentSpeed / baseSpeed);
        }
        
        // Rotación basada en dirección
        if (lastMovementDirection != Vector2.zero)
        {
            float angle = Mathf.Atan2(lastMovementDirection.y, lastMovementDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.AngleAxis(angle - 90f, Vector3.forward), Time.deltaTime * 10f);
        }
        
        // Partículas de caminar
        UpdateWalkParticles();
    }
    
    private void UpdateWalkParticles()
    {
        if (walkParticles != null)
        {
            if (isWalking && currentSpeed > 0.1f)
            {
                if (!walkParticles.isPlaying)
                    walkParticles.Play();
            }
            else
            {
                if (walkParticles.isPlaying)
                    walkParticles.Stop();
            }
        }
    }
    
    #endregion
    
    #region Collision System
    
    // REEMPLAZA TU OnCollisionEnter2D CON ESTA VERSIÓN SIMPLE:

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Keys")
        {
            // Efecto visual INMEDIATO al recoger llave
            StartCoroutine(KeyCollectEffectNew());
        
            Destroy(collision.gameObject);
            collectedKeys++;
            UpdateKeyCounterNow();
        
            // Verificar puerta
            CheckDoorUnlock();
        }        

        if (collision.gameObject.tag == "Princess")
        {
            // Efecto de VICTORIA ÉPICA
            StartCoroutine(VictoryEffectNew());
        }

        if (collision.gameObject.tag == "Enemies")
        {
            // Efecto de muerte dramático
            StartCoroutine(DeathEffectNew());
        }

        if (collision.gameObject.tag == "Walls")
        {
            // Efecto de rebote en pared
            StartCoroutine(WallHitEffectNew());
        }
    }
    
    // Método alternativo por si OnCollisionEnter2D no funciona
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"Trigger detectado con: {other.gameObject.name}, Tag: {other.gameObject.tag}");
        
        if (other.CompareTag("Keys"))
        {
            Debug.Log("¡LLAVE DETECTADA POR TRIGGER!");
            HandleKeyCollection(other.gameObject);
        }
        else if (other.CompareTag("Princess"))
        {
            HandleWin();
        }
        else if (other.CompareTag("Enemies") && !isInvulnerable)
        {
            HandlePlayerDeath();
        }
        else if (other.CompareTag("PowerUp"))
        {
            HandlePowerUpCollection(other.gameObject);
        }
    }
    
    private void HandleKeyCollection(GameObject key)
    {
        Debug.Log($"=== RECOGIENDO LLAVE ===");
        Debug.Log($"Llaves ANTES: {collectedKeys}");
        
        // Destruir llave primero
        Destroy(key);
        
        // Incrementar contador
        collectedKeys++;
        Debug.Log($"Llaves DESPUÉS: {collectedKeys}");
        
        // Actualizar UI INMEDIATAMENTE
        UpdateKeyCounterNow();
        
        // Efectos (opcionales, no críticos)
        try 
        {
            PlaySound(keyCollectSound);
            ShakeCamera(0.05f, 0.1f);
            StartCoroutine(ScaleEffect(1.3f, 0.1f));
        }
        catch (System.Exception e)
        {
            Debug.Log($"Error en efectos: {e.Message}");
        }
        
        // Lógica adicional
        currentCombo++;
        comboTimer = comboResetTime;
        
        int points = 100 * (currentCombo > 0 ? currentCombo : 1);
        AddScore(points);
        
        // Verificar puerta
        CheckDoorUnlock();
        
        Debug.Log($"=== FIN RECOGER LLAVE ===");
    }
    
    // Método simple y directo para actualizar contador
    private void UpdateKeyCounterNow()
    {
        Debug.Log("Actualizando contador ahora...");
        
        // Intentar todas las formas posibles
        string newText = $"Keys: {collectedKeys}/{keysNeededToWin}";
        
        // Método 1: Referencia directa
        if (keyAmountText != null)
        {
            keyAmountText.text = newText;
            Debug.Log($"Método 1 exitoso: {newText}");
            return;
        }
        
        // Método 2: Buscar por nombre
        GameObject textObj = GameObject.Find("KeyText");
        if (textObj == null) textObj = GameObject.Find("keyText");
        if (textObj == null) textObj = GameObject.Find("Keys");
        if (textObj == null) textObj = GameObject.Find("KeyAmount");
        
        if (textObj != null)
        {
            Text textComponent = textObj.GetComponent<Text>();
            if (textComponent != null)
            {
                textComponent.text = newText;
                Debug.Log($"Método 2 exitoso: {newText}");
                return;
            }
        }
        
        // Método 3: Buscar en la escena
        Text[] allTexts = FindObjectsOfType<Text>();
        Debug.Log($"Encontrados {allTexts.Length} componentes Text en la escena");
        
        foreach (Text txt in allTexts)
        {
            if (txt.text.Contains("Keys") || txt.text.Contains("keys") || txt.text.Contains("Key"))
            {
                txt.text = newText;
                Debug.Log($"Método 3 exitoso en {txt.gameObject.name}: {newText}");
                return;
            }
        }
        
        Debug.LogError("NO SE PUDO ACTUALIZAR EL CONTADOR! Verifica que:");
        Debug.LogError("1. Tienes un componente Text en la escena");
        Debug.LogError("2. El texto contiene la palabra 'Keys'");
        Debug.LogError("3. O asigna la referencia keyAmountText en el Inspector");
    }
    
    private void HandleWin()
    {
        // Efectos de victoria
        PlaySound(winSound);
        ShakeCamera(0.2f, 0.5f);
        
        if (winText != null)
        {
            winText.text = "¡VICTORIA!";
            winText.color = new Color(1f, 0.84f, 0f); // Color dorado
            StartCoroutine(WinTextAnimation());
        }
        
        // Puntos bonus
        AddScore(1000);
        
        OnPlayerWin?.Invoke();
        enabled = false;
    }
    
    private void HandlePlayerDeath()
    {
        // Efectos de muerte
        PlaySound(deathSound);
        ShakeCamera(0.3f, 0.3f);
        
        // Efecto visual de muerte
        StartCoroutine(DeathEffect());
        
        OnPlayerDeath?.Invoke();
    }
    
    private void HandleWallCollision(Vector2 hitPoint)
    {
        // Efectos de colisión
        PlaySound(wallHitSound, 0.5f);
        ShakeCamera(0.03f, 0.05f);
        
        // Partículas en el punto de impacto
        if (wallHitParticles != null)
        {
            GameObject particles = Instantiate(wallHitParticles.gameObject, hitPoint, Quaternion.identity);
            Destroy(particles, 2f);
        }
        
        // Empuje hacia atrás
        if (lastMovementDirection != Vector2.zero)
        {
            Vector2 pushback = -lastMovementDirection * currentSpeed * Time.deltaTime * 2f;
            transform.Translate(pushback);
        }
        
        // Breve invulnerabilidad para evitar spam
        if (!isInvulnerable)
            StartCoroutine(InvulnerabilityCoroutine(baseInvulnerabilityTime));
    }
    
    private void HandlePowerUpCollection(GameObject powerUp)
    {
        // Aquí puedes agregar diferentes tipos de power-ups
        PlaySound(keyCollectSound, 1.2f); // Sonido más agudo
        AddScore(250);
        Destroy(powerUp);
        
        // Efecto visual
        StartCoroutine(ScaleEffect(1.5f, 0.2f));
        ShowFloatingText("POWER UP!", powerUp.transform.position, Color.cyan);
    }
    
    #endregion
    
    #region Score and Combo System
    
    private void AddScore(int points)
    {
        currentScore += points;
        OnScoreChanged?.Invoke(currentScore);
    }
    
    private void UpdateCombo()
    {
        if (currentCombo > 0)
        {
            comboTimer -= Time.deltaTime;
            if (comboTimer <= 0f)
            {
                currentCombo = 0;
                OnComboChanged?.Invoke(currentCombo);
            }
        }
    }
    
    #endregion
    
    #region UI and Feedback
    
    private void UpdateAllUI()
    {
        UpdateKeyUI();
        UpdateScoreUI();
        UpdateComboUI();
    }
    
    private void UpdateUI()
    {
        // Actualización continua de barras, etc.
        if (sprintBar != null && !isSprintActive && canBoost)
        {
            sprintBar.fillAmount = 1f;
        }
    }
    
    private void UpdateKeyUI()
    {
        string newText = $"Keys: {collectedKeys}/{keysNeededToWin}";
        
        if (keyAmountText != null)
        {
            keyAmountText.text = newText;
        }
    }
    
    private void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Puntos: {currentScore:N0}";
        }
    }
    
    private void UpdateComboUI()
    {
        if (comboText != null)
        {
            if (currentCombo > 1)
            {
                comboText.text = $"COMBO x{currentCombo}!";
                comboText.gameObject.SetActive(true);
                
                // Color basado en combo
                if (currentCombo >= 5)
                    comboText.color = Color.red;
                else if (currentCombo >= 3)
                    comboText.color = new Color(1f, 0.5f, 0f); // Color naranja
                else
                    comboText.color = Color.yellow;
            }
            else
            {
                comboText.gameObject.SetActive(false);
            }
        }
    }
    
    private void ShowFloatingText(string text, Vector3 worldPosition, Color color)
    {
        // Aquí crearías un texto flotante que sube y desaparece
        // Por simplicidad, solo lo loggeo, pero podrías crear un prefab de UI flotante
        Debug.Log($"Floating Text: {text} at {worldPosition}");
    }
    
    private IEnumerator UIBounceEffect(Transform uiElement)
    {
        Vector3 originalScale = uiElement.localScale;
        Vector3 targetScale = originalScale * 1.2f;
        
        float timer = 0f;
        float duration = 0.1f;
        
        // Escalar hacia arriba
        while (timer < duration)
        {
            timer += Time.deltaTime;
            uiElement.localScale = Vector3.Lerp(originalScale, targetScale, timer / duration);
            yield return null;
        }
        
        // Escalar hacia abajo
        timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            uiElement.localScale = Vector3.Lerp(targetScale, originalScale, timer / duration);
            yield return null;
        }
        
        uiElement.localScale = originalScale;
    }
    
    private IEnumerator WinTextAnimation()
    {
        if (winText == null) yield break;
        
        Transform textTransform = winText.transform;
        Vector3 originalScale = textTransform.localScale;
        
        // Animación de victoria épica
        for (int i = 0; i < 3; i++)
        {
            yield return StartCoroutine(ScaleUIElement(textTransform, originalScale * 1.5f, 0.2f));
            yield return new WaitForSeconds(0.1f);
        }
    }
    
    private IEnumerator ScaleUIElement(Transform element, Vector3 targetScale, float duration)
    {
        Vector3 startScale = element.localScale;
        float timer = 0f;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            element.localScale = Vector3.Lerp(startScale, targetScale, timer / duration);
            yield return null;
        }
        
        element.localScale = targetScale;
    }
    
    #endregion
    
    #region Special Effects
    
    private IEnumerator InvulnerabilityCoroutine(float duration)
    {
        isInvulnerable = true;
        
        // Efecto de parpadeo
        if (spriteRenderer != null)
        {
            float timer = 0f;
            while (timer < duration)
            {
                spriteRenderer.color = Color.red;
                yield return new WaitForSeconds(0.05f);
                spriteRenderer.color = originalColor;
                yield return new WaitForSeconds(0.05f);
                timer += 0.1f;
            }
        }
        
        isInvulnerable = false;
    }
    
    private IEnumerator DeathEffect()
    {
        // Efecto de muerte dramático
        if (spriteRenderer != null)
        {
            float timer = 0f;
            while (timer < 1f)
            {
                timer += Time.deltaTime;
                Color color = spriteRenderer.color;
                color.a = Mathf.Lerp(1f, 0f, timer);
                spriteRenderer.color = color;
                
                // Rotación de muerte
                transform.Rotate(0, 0, 720 * Time.deltaTime);
                
                yield return null;
            }
        }
        
        yield return new WaitForSeconds(1f);
        RestartLevel();
    }
    
    #endregion
    
    #region Game Logic
    
    private void CheckDoorUnlock()
    {
        if (collectedKeys >= keysNeededToWin && door != null)
        {
            StartCoroutine(EpicDoorUnlockEffectNew());
        }
    }
    
    private IEnumerator DoorUnlockEffect()
    {
        // Efecto de puerta abriéndose
        ShakeCamera(0.1f, 0.2f);
        PlaySound(keyCollectSound, 1.5f);
        
        if (door != null)
        {
            // Animación de desaparición de puerta
            float timer = 0f;
            Vector3 originalScale = door.transform.localScale;
            
            while (timer < 0.5f)
            {
                timer += Time.deltaTime;
                door.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, timer / 0.5f);
                door.transform.Rotate(0, 0, 360 * Time.deltaTime);
                yield return null;
            }
            
            Destroy(door);
        }
        
        ShowFloatingText("¡PUERTA ABIERTA!", transform.position, Color.green);
    }
    
    private void RestartLevel()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }
    
    // ¡AGREGAR ESTOS MÉTODOS CON NOMBRES ÚNICOS!
    
    private IEnumerator KeyCollectEffectNew()
    {
        // Efecto visual del player
        Vector3 originalScale = transform.localScale;
        
        // Agrandar player brevemente
        transform.localScale = originalScale * 1.3f;
        
        // Cambiar color brevemente
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.yellow;
            
            yield return new WaitForSeconds(0.1f);
            
            spriteRenderer.color = originalColor;
        }
        
        // Volver al tamaño normal
        transform.localScale = originalScale;
        
        // Efecto de cámara
        if (mainCamera != null)
        {
            StartCoroutine(CameraShakeCoroutine(0.1f, 0.2f));
        }
    }

    private IEnumerator VictoryEffectNew()
    {
        // Texto de victoria con efecto
        if (winText != null)
        {
            winText.text = "🎉 YOU WIN! 🎉";
            winText.color = Color.yellow;
            
            // Hacer el texto grande y con animación
            Transform textTransform = winText.transform;
            Vector3 originalScale = textTransform.localScale;
            
            // Animación de crecimiento del texto
            for (int i = 0; i < 3; i++)
            {
                textTransform.localScale = originalScale * 1.5f;
                yield return new WaitForSeconds(0.1f);
                textTransform.localScale = originalScale;
                yield return new WaitForSeconds(0.1f);
            }
            
            // Dejar el texto grande al final
            textTransform.localScale = originalScale * 1.2f;
        }
        
        // Efectos en el player
        if (spriteRenderer != null)
        {
            // Cambiar color a dorado
            spriteRenderer.color = Color.yellow;
        }
        
        // Efecto de cámara épico
        StartCoroutine(CameraShakeCoroutine(0.3f, 1.0f));
        
        // Hacer que el player rebote de felicidad
        for (int i = 0; i < 5; i++)
        {
            transform.localScale = Vector3.one * 1.2f;
            yield return new WaitForSeconds(0.1f);
            transform.localScale = Vector3.one;
            yield return new WaitForSeconds(0.1f);
        }
        
        enabled = false; // Desactivar el player
    }

    private IEnumerator DeathEffectNew()
    {
        // Efecto visual de muerte
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red;
        }
        
        // Shake de cámara
        StartCoroutine(CameraShakeCoroutine(0.2f, 0.5f));
        
        // Rotar el player como si muriera
        float timer = 0f;
        while (timer < 1f)
        {
            timer += Time.deltaTime;
            transform.Rotate(0, 0, 720 * Time.deltaTime);
            yield return null;
        }
        
        yield return new WaitForSeconds(0.5f);
        
        // Reiniciar nivel
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private IEnumerator WallHitEffectNew()
    {
        // Efecto de rebote
        if (spriteRenderer != null)
        {
            Color originalColor = spriteRenderer.color;
            spriteRenderer.color = Color.red;
            
            yield return new WaitForSeconds(0.1f);
            
            spriteRenderer.color = originalColor;
        }
        
        // Pequeño shake
        StartCoroutine(CameraShakeCoroutine(0.05f, 0.1f));
        
        // Empujar al player hacia atrás
        Vector3 pushDirection = -transform.up * 0.5f;
        transform.position += pushDirection;
    }

    private IEnumerator EpicDoorUnlockEffectNew()
    {
        // Efecto de cámara cuando se abre la puerta
        StartCoroutine(CameraShakeCoroutine(0.2f, 0.5f));
        
        if (door != null)
        {
            // Hacer que la puerta parpadee antes de desaparecer
            SpriteRenderer doorSprite = door.GetComponent<SpriteRenderer>();
            if (doorSprite != null)
            {
                Color originalColor = doorSprite.color;
                
                // Parpadeo
                for (int i = 0; i < 6; i++)
                {
                    doorSprite.color = Color.yellow;
                    yield return new WaitForSeconds(0.1f);
                    doorSprite.color = originalColor;
                    yield return new WaitForSeconds(0.1f);
                }
            }
            
            // Animación de desaparición
            float timer = 0f;
            Vector3 originalScale = door.transform.localScale;
            
            while (timer < 0.5f)
            {
                timer += Time.deltaTime;
                door.transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, timer / 0.5f);
                door.transform.Rotate(0, 0, 360 * Time.deltaTime);
                yield return null;
            }
            
            Destroy(door);
        }
        
        // Mostrar mensaje de puerta abierta
        Debug.Log("🚪 ¡PUERTA ABIERTA! ¡Ve por la princesa! 👑");
    }
    
    #endregion
    
    #region Public Methods
    
    public int GetCollectedKeys() => collectedKeys;
    public int GetCurrentScore() => currentScore;
    public int GetCurrentCombo() => currentCombo;
    public bool CanSprint() => canBoost && !isSprintActive;
    public bool IsSprintActive() => isSprintActive;
    public bool IsInvulnerable() => isInvulnerable;
    
    #endregion
    
    #region Cleanup
    
    private void OnDestroy()
    {
        // Limpiar eventos
        OnKeyCollected = null;
        OnScoreChanged = null;
        OnComboChanged = null;
        OnPlayerWin = null;
        OnPlayerDeath = null;
    }
    
    #endregion
}