// Handles the server-side game state and works with the PlayerNetwork script to manage player readiness.
// Currently also manages UI stuff... Should probably be put into a separate class... Oh well...

using PurrNet;
using PurrNet.Transports;
using UnityEngine;

public class GameManager : NetworkBehaviour
{

    public GameObject ExampleObject;
    public GameObject ReadyToPlayObject;

    [SerializeField] private GameObject ReadyArea;
    [SerializeField] private GameObject ReadyText;
    [SerializeField] private GameObject TitleText;
    [SerializeField] private GameObject BottomText; // xD
    [SerializeField] private GameObject AnswerMinText;
    [SerializeField] private GameObject AnswerMaxText;

    public int PlayerReadyCount;
    public int PlayerCount;

    public GameObject Ruler;
    public GameObject BackgroundImage;
    
    public int RoundNumber;
    public int TurnNumber;

    private int[] _PlayerBallPosition = new int[0];

    // The countdown is triggered when all players are ready (in the "Ready" area).
    private const float _COUNTDOWN_TIME = 3f;
    private SyncVar<float> _countdownTimer = new SyncVar<float>(_COUNTDOWN_TIME);

    // The countdown is for each player during their turn. The turn will change when the timer has elapsed.
    private const float _QUESTIONCOUNTDOWN_TIME = 10f;
    private SyncVar<float> _questionCountdownTimer = new SyncVar<float>(_QUESTIONCOUNTDOWN_TIME);

    // Questions are stored in a ScriptableObject array, which is loaded from the Resources folder. The questions are picked randomly and sent to all clients.
    private QuestionScriptableObject[] _allQuestions = new QuestionScriptableObject[0];
    private SyncVar<int> _currentQuestionIndex = new SyncVar<int>(0);
    private SyncVar<string> _currentQuestionText = new SyncVar<string>("");
    private SyncVar<int> _currentQuestionAnswer = new SyncVar<int>(0);
    private SyncVar<Vector2> _currentQuestionAnswerRange = new SyncVar<Vector2>(new Vector2(0, 0));
    //private SyncTextureAsset _currentQuestionImage = new SyncTextureAsset(true);

    private bool _isQuestionPicked = false;

    protected override void OnSpawned()
    {
        base.OnSpawned();
        _LoadQuestions();

        // This is a lambda expression.
        // Whenever the _currentQuestionIndex SyncVar changes, the UpdateQuestionValues method is called to update the question values on all clients.
        // The _ means that we don't want to pass any parameters to the UpdateQuestionValues method. We just want to call it whenever the _currentQuestionIndex changes.
        // I had to do this because there was an issue with the SyncVar and UpdateQuestionValues() method being called in the wrong order (network race hazard).
        // This way ensures that the question values are updated only after the _currentQuestionIndex changes, and not before.
        _currentQuestionIndex.onChanged += _ => UpdateQuestionValues();
    }

    // Loads all questions from the Resources folder into the _allQuestions array. This is called on the server when the GameManager is spawned.
    [ServerRpc(Channel.Unreliable)]
    private void _LoadQuestions()
    {
        _allQuestions = Resources.LoadAll<QuestionScriptableObject>("Questions");
    }

    // Picks a random question from the _allQuestions array and sets the current question values.
    [ServerRpc(Channel.Unreliable)]
    private void _PickRandomQuestion()
    {
        int randomIndex = Random.Range(0, _allQuestions.Length);
        _currentQuestionIndex.value = randomIndex;
    }

    // Gets the current question values from the _allQuestions array and sets the current question values. This is called on the server when a question is picked.
    [ServerRpc(Channel.Unreliable)]
    public void GetQuestionValues()
    {
        _currentQuestionText.value = _allQuestions[_currentQuestionIndex.value].questionText;
        _currentQuestionAnswer.value = _allQuestions[_currentQuestionIndex.value].answer;
        _currentQuestionAnswerRange.value = _allQuestions[_currentQuestionIndex.value].answerRange;
        //_currentQuestionImage = _allQuestions[_currentQuestionIndex.value].image;
    }

    // This updates the question values on all clients. It is called whenever the _currentQuestionIndex SyncVar changes, via a lambda expression subscription.
    [ObserversRpc]
    public void UpdateQuestionValues()
    {
        BottomText.GetComponent<TMPro.TMP_Text>().text = _currentQuestionText.value;
        AnswerMinText.GetComponent<TMPro.TMP_Text>().text = _currentQuestionAnswerRange.value.x.ToString();
        AnswerMaxText.GetComponent<TMPro.TMP_Text>().text = _currentQuestionAnswerRange.value.y.ToString();
        AnswerMinText.SetActive(true);
        AnswerMaxText.SetActive(true);
    }

    [ObserversRpc]
    public void SetQuestionText()
    {
        //BackgroundImage.GetComponent<SpriteRenderer>().sprite = _currentQuestionImage;
        BottomText.GetComponent<TMPro.TMP_Text>().text = _currentQuestionText.value;
        AnswerMinText.GetComponent<TMPro.TMP_Text>().text = _currentQuestionAnswerRange.value.x.ToString();
        AnswerMaxText.GetComponent<TMPro.TMP_Text>().text = _currentQuestionAnswerRange.value.y.ToString();
        _isQuestionPicked = true;

    }

    public void FixedUpdate()
    {
        if (_isQuestionPicked == false)
        {
            UpdateText();
            GetPlayerBallPositions();
            GetProximityToAnswers();
        }
    }

    [ServerRpc(Channel.Unreliable)]
    public void UpdateText()
    {
         // Check if all players are ready. If they are, start the countdown. Otherwise, reset the countdown.
        var playerReadyCount = 0;
        foreach(var player in PlayerNetwork.allPlayers)
        {
            if(player.Value.isReady)
                {
                    playerReadyCount++;
                }

            else
                {
                 PlayerReadyCount = playerReadyCount;
                }
        }
        
        // If all players are ready, start the countdown. Otherwise, reset the countdown.
        if(playerReadyCount == PlayerNetwork.allPlayers.Count)
        {
            SetCountdownText();
            
            UpdateTimer();

            if (_countdownTimer.value <= 0)
            {
                ClearText();
                Ruler.SetActive(true);
                BackgroundImage.SetActive(true);
                _PickRandomQuestion();
                GetQuestionValues();
                if (isServer)
                {
                    SetQuestionText();
                }
            }
        }

        else if(playerReadyCount != PlayerNetwork.allPlayers.Count)
        {
            ResetCountdown();
            ShowText();
        }
    }

    [ServerRpc(Channel.Unreliable)]
    public void UpdateTimer()
    {
        _countdownTimer.value -= Time.deltaTime;
    }

    [ServerRpc(Channel.Unreliable)]
    public void ResetCountdown()
    {
        _countdownTimer.value = _COUNTDOWN_TIME;
    }

    [ServerRpc(Channel.Unreliable)]
    public void UpdateQuestionTimer()
    {
        _questionCountdownTimer.value -= Time.deltaTime;
    }

    [ServerRpc(Channel.Unreliable)]
    public void ResetQuestionCountdown()
    {
        _questionCountdownTimer.value = _QUESTIONCOUNTDOWN_TIME;
    }

    [ObserversRpc]
    public void SetCountdownText()
    {
        ReadyText.SetActive(true);
        TitleText.SetActive(true);
        // Checks if the ReadyText is active, and if it is, sets the text to the countdown timer. If it is not active, it does nothing.
        if (ReadyText.activeSelf)
        {
            ReadyText.GetComponent<TMPro.TMP_Text>().text = $"{Mathf.CeilToInt(_countdownTimer.value)}...";
        }
    }

    [ObserversRpc]
    public void ClearText()
    {
        ReadyArea.SetActive(false);
        ReadyText.SetActive(false);
        TitleText.SetActive(false);
    }
    [ObserversRpc]
    public void ShowText()
    {
        ReadyArea.SetActive(true);
        ReadyText.SetActive(true);
        TitleText.SetActive(true);
        AnswerMinText.SetActive(false);
        AnswerMaxText.SetActive(false);
    }

    // Gets the ball positions of all players and stores them in the _PlayerBallPosition array.
    [ServerRpc(Channel.Unreliable)]
    private void GetPlayerBallPositions()
    {
        var playerIndex = 0;

        _PlayerBallPosition = new int[PlayerNetwork.allPlayers.Count];

        foreach (var player in PlayerNetwork.allPlayers)
            {
                Debug.Log($"Player {player.Value.id} ball position: {player.Value.ballPosition}");
                _PlayerBallPosition[playerIndex] = player.Value.ballPosition;

                playerIndex++;
            }
    }

    // Calculates the proximity of each ball to the min and max answer positions.
    [ServerRpc(Channel.Unreliable)]
    private void GetProximityToAnswers()
    {
        foreach (var player in PlayerNetwork.allPlayers)
        {
            var playerBallPosition = player.Value.ballPosition;

            var distanceToMin = Mathf.Abs(playerBallPosition - AnswerMinText.transform.position.x);
            var distanceToMax = Mathf.Abs(playerBallPosition - AnswerMaxText.transform.position.x);

            Debug.Log($"Player {player.Value.id} ball position: {playerBallPosition}, distance to min answer: {distanceToMin}, distance to max answer: {distanceToMax}");
        }
    }

}