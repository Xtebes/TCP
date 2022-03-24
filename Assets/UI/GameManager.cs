using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using FMODUnity;
public class GameManager : MonoBehaviour
{
    [SerializeField]
    Image canvasImage;
    Tween currentTransition;
    [SerializeField]
    GameObject endGameCanvas;
    [SerializeField]
    TextMeshProUGUI endGameCanvasTitle, endGameCanvasReason;
    [SerializeField]
    GameObject playerObject;
    PlayerStats playerStats;
    Player player;
    Input playerInput;
    [SerializeField]
    CountdownTimer timer;
    [SerializeField]
    Volume volume;
    [SerializeField]
    EventSystem eventSystem;
    Color transparent;
    [SerializeField]
    Color[] winColors, lossColors;
    [SerializeField]
    string ranOutOfTimeReason, playerKilledReason, coreCooledReason;
    [SerializeField]
    ChunkManager chunkManager;
    [SerializeField]
    EnemyManager enemyManager;
    [SerializeField]
    CanvasGroup canvasGroup;
    [SerializeField]
    StatisticsPanel statsPanel;
    [SerializeField]
    HeightDisplay heightDisplay;
    Exposure exposure;
    public void ToggleHUD()
    {
        canvasGroup.DOFade(((int)canvasGroup.alpha + 1) % 2, 0.5f).onComplete += ()=> playerInput.toggleHUD.Enable();
        playerInput.toggleHUD.Disable();
    }
    public void Retry()
    {
        SceneManager.LoadScene("Game", LoadSceneMode.Single);
    }
    public void Quit()
    {
        Application.Quit();
    }
    public void EnableLostGameCanvas()
    {
        endGameCanvas.SetActive(true);
        endGameCanvasTitle.text = "Mission Failed";
        endGameCanvasTitle.color = lossColors[0];
        endGameCanvasReason.color = lossColors[1];
    }
    public void EnableWonGameCanvas() 
    { 
        endGameCanvas.SetActive(true);
        endGameCanvasTitle.text = "Mission Cleared";
        endGameCanvasTitle.color = winColors[0];
        endGameCanvasReason.color = winColors[1];
    }
    Tween ExplosionEffect()
    {
        Tween tween = DOTween.To(() => exposure.fixedExposure.value, x => exposure.fixedExposure.value = x, 6f, 2f);
        return tween;
    }
    Tween TransitionToEndGameCanvas(string reasonForEndGame, Color startTransitionColor)
    {
        canvasImage.enabled = true;
        playerInput.inputActionAsset.Disable();
        playerStats.enabled = false;
        endGameCanvasReason.text = reasonForEndGame;
        Tween tween = CanvasColorTransition(startTransitionColor, Color.black, 1);
        tween.onComplete += () =>
        {
            playerInput.inputActionAsset.FindActionMap("UI").Enable();
            playerInput.toggleUpgraders.Disable();
            eventSystem.SetSelectedGameObject(endGameCanvas.transform.GetChild(0).gameObject);
        };
        return tween;
    }
    void RemoveEndGameEvents()
    {
        playerStats.onPlayerKilled = null;
        timer.onRunOutOfTime = null;
        CoreTemperature.onCoreCooled = null;
        RuntimeManager.MuteAllEvents(true);
    }
    Tween CanvasColorTransition(Color startColor, Color endColor, float duration)
    {
        canvasImage.enabled = true;
        canvasImage.color = startColor;
        if (currentTransition != null) currentTransition.Kill();
        return currentTransition = canvasImage.DOColor(endColor, duration);
    }
    void Start()
    {
        RuntimeManager.MuteAllEvents(false);
        volume.sharedProfile.TryGet(out exposure);
        playerInput = playerObject.GetComponent<Input>();
        playerStats = playerObject.GetComponent<PlayerStats>();
        player = playerObject.GetComponent<Player>();
        playerInput.loadedInput = () =>
        {
            playerInput.toggleHUD.performed += ctx => ToggleHUD();
            player.LoadInput(playerInput);
            player.GetComponentInChildren<EnergyWeapon>().LoadInput(playerInput);
            statsPanel.LoadPlayerInput(playerInput);
            heightDisplay.StartCoroutine(heightDisplay.UpdateHeight(playerObject.transform));
        };
        canvasImage.enabled = true;
        transparent = new Color(0, 0, 0, 0);
        CanvasColorTransition(Color.black, transparent, 2);
        CoreTemperature.onCoreCooled = () =>
        {
            RemoveEndGameEvents();
            TransitionToEndGameCanvas(coreCooledReason, canvasImage.color).onComplete += () =>
                EnableWonGameCanvas();
        };
        Kamikaze.onExplode = (position, strength, radius) => FindObjectOfType<ChunkManager>().SphereDeform(position, radius, strength, Color.black);
        //ChunkManager.OnDestroyCrystal = ctx => chunkManager.DestroyCrystal(ctx);
        EnemyManager.onSpawnEnemy = position => enemyManager.TrySpawnEnemy(Random.Range(0, enemyManager.enemyPrefabs.Length), position);
        enemyManager.onEnemyInstantiated += enemy => enemy.target = player;
        playerStats.onPlayerKilled = () =>
        {
            RemoveEndGameEvents();
            TransitionToEndGameCanvas(playerKilledReason, canvasImage.color).onComplete += () =>
                EnableLostGameCanvas();
        };
        timer.onRunOutOfTime = () =>
        {
            RemoveEndGameEvents();
            ExplosionEffect().onComplete += () =>
                CanvasColorTransition(transparent, Color.white, 0.2f).onComplete += () =>
                {
                    exposure.fixedExposure.value = 13;
                    TransitionToEndGameCanvas(ranOutOfTimeReason, Color.white).onComplete += () =>
                        EnableLostGameCanvas();
                };
        };
    }
}