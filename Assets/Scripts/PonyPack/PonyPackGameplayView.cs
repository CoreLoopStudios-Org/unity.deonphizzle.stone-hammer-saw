using System.Collections;
using Game3;
using UnityEngine;

namespace PonyPack
{
    /// <summary>
    /// Gameplay visuals for the Pony Pack round (placeholder shapes until the 3D models are ready) (capsules and cubes, no art needed).
    /// Everything is built at runtime, so the only setup is adding this component next to PonyPackRound.
    ///
    /// You (blue) and a bot opponent (orange) stand on a track. When GO lights up, the player's real
    /// phone pickup (or a mouse click in the Editor) makes the capsule dash. The timing evaluator then
    /// decides who is SAFE; unsafe capsules fall over, the best safe one runs to the finish line.
    /// </summary>
    public class PonyPackGameplayView : MonoBehaviour
    {
        [Header("References (auto-found if empty)")]
        [SerializeField] PonyPackRound round;
        [SerializeField] GyroDetector gyro;

        [Header("Gameplay options")]
        [SerializeField] bool controlCamera = true;
        [Tooltip("Turns off drawing of the scene's 2D panels so only the 3D track is visible (for recording the 3D part).")]
        [SerializeField] bool hideSceneCanvas = false;
        [Tooltip("Editor only: start the first round automatically and skip the stillness wait.")]
        [SerializeField] bool autoRunInEditor = false;
        [Tooltip("Editor only: left mouse click = pick up the phone.")]
        [SerializeField] bool clickToAct = true;
        [SerializeField] bool autoNextRound = true;
        [SerializeField] float nextRoundDelay = 3f;

        [Header("Bot opponent (reaction time after GO, seconds)")]
        [SerializeField] float botMinReaction = 0.3f;
        [SerializeField] float botMaxReaction = 1.8f;

        const float LaneX = 1.3f;
        const float FinishZ = 12f;
        const float BaseY = 1f;   // capsule center height (capsule is 2 units tall)

        static readonly Color PlayerColor = new Color(0.25f, 0.55f, 1f);
        static readonly Color BotColor = new Color(1f, 0.6f, 0.15f);
        static readonly Color FallenColor = new Color(0.85f, 0.15f, 0.15f);

        Transform _player, _bot;
        Renderer _playerR, _botR, _signalR;
        PonyPackTimingEvaluator _evaluator;

        float _botActionTime;
        bool _botActed;
        float _countdownLeft;
        bool _resolving;

        string _banner = "";
        string _detail = "";
        int _playerWins, _botWins;

        // ------------------------------------------------------------ setup

        void Awake()
        {
            if (round == null) round = FindFirstObjectByType<PonyPackRound>();
            if (gyro == null) gyro = FindFirstObjectByType<GyroDetector>();
            _evaluator = new PonyPackTimingEvaluator(round != null ? round.HalfWindowSeconds : 0.5f);
            BuildWorld();
            ResetVisuals();
        }

        void OnEnable()
        {
            if (round != null) round.StateChanged += OnRoundState;
            if (gyro != null) gyro.onCountdownTick.AddListener(OnCountdownTick);
        }

        void OnDisable()
        {
            if (round != null) round.StateChanged -= OnRoundState;
            if (gyro != null) gyro.onCountdownTick.RemoveListener(OnCountdownTick);
        }

        void Start()
        {
            if (hideSceneCanvas)
            {
                var canvasGo = GameObject.Find("Canvas");
                if (canvasGo != null && canvasGo.TryGetComponent<Canvas>(out var c)) c.enabled = false;
            }

            if (controlCamera && Camera.main != null)
            {
                var t = Camera.main.transform;
                t.position = new Vector3(0f, 6.5f, -7f);
                t.LookAt(new Vector3(0f, 0.5f, 6f));
            }

            if (autoRunInEditor && Application.isEditor && round != null && round.State == RoundState.Idle)
                StartCoroutine(DelayedStart());
        }

        IEnumerator DelayedStart()
        {
            yield return new WaitForSeconds(0.5f);
            if (round.State == RoundState.Idle) round.StartRound();
        }

        void BuildWorld()
        {
            var root = new GameObject("PonyPackWorld").transform;
            root.SetParent(transform, false);

            // ground
            MakeBox(root, "Ground", new Vector3(0f, -0.1f, 6f), new Vector3(7f, 0.2f, 16f), new Color(0.18f, 0.2f, 0.25f));
            // lane stripes
            MakeBox(root, "LaneBlue", new Vector3(-LaneX, 0.005f, 6f), new Vector3(1.8f, 0.02f, 15f), new Color(0.2f, 0.3f, 0.5f));
            MakeBox(root, "LaneOrange", new Vector3(LaneX, 0.005f, 6f), new Vector3(1.8f, 0.02f, 15f), new Color(0.5f, 0.35f, 0.15f));
            // start and finish lines
            MakeBox(root, "StartLine", new Vector3(0f, 0.02f, -1.2f), new Vector3(6f, 0.03f, 0.25f), Color.white);
            MakeBox(root, "FinishLine", new Vector3(0f, 0.02f, FinishZ + 1f), new Vector3(6f, 0.03f, 0.5f), new Color(1f, 0.9f, 0.2f));

            // signal light: red while waiting, green on GO
            var signal = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            signal.name = "SignalLight";
            Destroy(signal.GetComponent<Collider>());
            signal.transform.SetParent(root, false);
            signal.transform.position = new Vector3(0f, 3.2f, 1f);
            signal.transform.localScale = Vector3.one * 1.1f;
            _signalR = signal.GetComponent<Renderer>();

            _player = MakeCapsule(root, "Player", PlayerColor, out _playerR);
            _bot = MakeCapsule(root, "Bot", BotColor, out _botR);
        }

        static Transform MakeBox(Transform parent, string name, Vector3 pos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().material.color = color;
            return go.transform;
        }

        static Transform MakeCapsule(Transform parent, string name, Color color, out Renderer r)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = name;
            Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            r = go.GetComponent<Renderer>();
            r.material.color = color;
            return go.transform;
        }

        void ResetVisuals()
        {
            StopAllCoroutines();
            _resolving = false;
            _botActed = false;
            _banner = "";
            _detail = "";
            Place(_player, _playerR, -LaneX, PlayerColor);
            Place(_bot, _botR, LaneX, BotColor);
            SetSignal(new Color(0.9f, 0.15f, 0.15f));
        }

        static void Place(Transform t, Renderer r, float x, Color color)
        {
            t.position = new Vector3(x, BaseY, 0f);
            t.rotation = Quaternion.identity;
            t.localScale = Vector3.one;
            r.material.color = color;
        }

        void SetSignal(Color c) { if (_signalR != null) _signalR.material.color = c; }

        // ------------------------------------------------------------ round events

        void OnCountdownTick(float remaining) => _countdownLeft = remaining;

        void OnRoundState(RoundState s)
        {
            switch (s)
            {
                case RoundState.Preparation:
                    ResetVisuals();
                    if (autoRunInEditor && Application.isEditor && gyro != null && gyro.editorTestMode)
                        StartCoroutine(AutoCountdown());
                    break;

                case RoundState.Countdown:
                    SetSignal(new Color(1f, 0.7f, 0.1f));
                    break;

                case RoundState.Trigger:
                    SetSignal(new Color(0.15f, 0.9f, 0.25f));
                    _botActionTime = round.GoTime + Random.Range(botMinReaction, botMaxReaction);
                    _botActed = false;
                    break;

                case RoundState.Result:
                    if (!_resolving) StartCoroutine(ResolveRoutine());
                    break;
            }
        }

        IEnumerator AutoCountdown()
        {
            yield return new WaitForSeconds(1.5f);
            if (round.State == RoundState.Preparation) gyro.ForceCountdown();
        }

        // ------------------------------------------------------------ per frame

        void Update()
        {
            if (round == null) return;

            // Editor: click = pick up the phone
            if (clickToAct && Application.isEditor && gyro != null && gyro.editorTestMode && Input.GetMouseButtonDown(0))
            {
                if (round.State == RoundState.Trigger) gyro.ForceSuccess();
                else if (round.State == RoundState.Preparation || round.State == RoundState.Countdown) gyro.ForceEarlyPickup();
            }

            // idle bob and crouch while waiting
            float bob = Mathf.Sin(Time.time * 3f) * 0.05f;
            if (round.State == RoundState.Preparation)
            {
                _player.position = new Vector3(-LaneX, BaseY + bob, _player.position.z);
                _bot.position = new Vector3(LaneX, BaseY - bob, _bot.position.z);
            }
            else if (round.State == RoundState.Countdown)
            {
                var crouch = new Vector3(1.1f, 0.8f, 1.1f);
                _player.localScale = crouch;
                _bot.localScale = crouch;
                _player.position = new Vector3(-LaneX, BaseY * 0.8f, 0f);
                _bot.position = new Vector3(LaneX, BaseY * 0.8f, 0f);
            }
            else if (round.State == RoundState.Trigger)
            {
                if (_player.localScale != Vector3.one && !_resolving) { _player.localScale = Vector3.one; _bot.localScale = Vector3.one; }

                if (!_botActed && Time.time >= _botActionTime)
                {
                    _botActed = true;
                    StartCoroutine(Dash(_bot, 3f));
                }
            }
        }

        // ------------------------------------------------------------ result

        IEnumerator ResolveRoutine()
        {
            _resolving = true;
            _player.localScale = Vector3.one;
            _bot.localScale = Vector3.one;

            var pj = round.LastJudgement;

            if (pj.Outcome == TimingOutcome.TooEarly)
                StartCoroutine(Fall(_player, _playerR));      // false start: down immediately
            else if (pj.Outcome != TimingOutcome.NoAction)
                StartCoroutine(Dash(_player, 3f));

            // let the bot make its move (or the window run out) before judging
            float guard = Time.time + 3f;
            while (!_botActed && Time.time < guard)
            {
                if (Time.time >= _botActionTime) { _botActed = true; StartCoroutine(Dash(_bot, 3f)); }
                yield return null;
            }
            yield return new WaitForSeconds(0.6f);

            var bj = _evaluator.Evaluate(round.WindowCenterTime, _botActionTime);
            var best = PonyPackTimingEvaluator.FindBest(new[] { pj, bj });
            bool playerBest = best.Contains(0);
            bool botBest = best.Contains(1);

            if (!pj.IsSafe && pj.Outcome != TimingOutcome.TooEarly) StartCoroutine(Fall(_player, _playerR));
            if (!bj.IsSafe) StartCoroutine(Fall(_bot, _botR));
            if (playerBest) StartCoroutine(RunToFinish(_player));
            if (botBest) StartCoroutine(RunToFinish(_bot));

            if (playerBest && !botBest) { _banner = "YOU WIN!"; _playerWins++; }
            else if (botBest && !playerBest) { _banner = pj.IsSafe ? "SAFE - BOT WAS CLOSER" : "YOU ARE ELIMINATED"; _botWins++; }
            else if (playerBest && botBest) { _banner = "TIE!"; }
            else { _banner = "NOBODY IS SAFE"; }

            _detail = "You: " + Describe(pj) + "\nBot: " + Describe(bj);

            if (autoNextRound)
            {
                yield return new WaitForSeconds(nextRoundDelay);
                _resolving = false;
                round.StartRound();
            }
        }

        static string Describe(TimingJudgement j)
        {
            switch (j.Outcome)
            {
                case TimingOutcome.Safe: return "SAFE (" + j.Score.ToString("0.00") + " s from center)";
                case TimingOutcome.TooEarly: return "TOO EARLY";
                case TimingOutcome.TooLate: return "TOO LATE (" + j.Score.ToString("0.00") + " s from center)";
                default: return "DID NOT MOVE";
            }
        }

        // ------------------------------------------------------------ small animations

        static IEnumerator MoveZ(Transform t, float toZ, float seconds, float hop = 0f)
        {
            float fromZ = t.position.z;
            for (float e = 0f; e < seconds; e += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, e / seconds);
                var p = t.position;
                p.z = Mathf.Lerp(fromZ, toZ, k);
                p.y = BaseY + Mathf.Sin(k * Mathf.PI) * hop;
                t.position = p;
                yield return null;
            }
            var end = t.position;
            end.z = toZ; end.y = BaseY;
            t.position = end;
        }

        static IEnumerator Dash(Transform t, float distance)
        {
            yield return MoveZ(t, t.position.z + distance, 0.35f, 0.25f);
        }

        static IEnumerator RunToFinish(Transform t)
        {
            yield return MoveZ(t, FinishZ, 1.1f, 0.4f);
            // victory hops
            for (int i = 0; i < 2; i++)
                for (float e = 0f; e < 0.35f; e += Time.deltaTime)
                {
                    var p = t.position;
                    p.y = BaseY + Mathf.Sin(e / 0.35f * Mathf.PI) * 0.7f;
                    t.position = p;
                    yield return null;
                }
        }

        static IEnumerator Fall(Transform t, Renderer r)
        {
            r.material.color = FallenColor;
            Quaternion from = t.rotation;
            Quaternion to = Quaternion.Euler(90f, 0f, 0f);
            Vector3 p0 = t.position;
            for (float e = 0f; e < 0.4f; e += Time.deltaTime)
            {
                float k = e / 0.4f;
                t.rotation = Quaternion.Slerp(from, to, k);
                t.position = new Vector3(p0.x, Mathf.Lerp(p0.y, 0.5f, k), p0.z + k * 0.6f);
                yield return null;
            }
            t.rotation = to;
        }

        // ------------------------------------------------------------ HUD (IMGUI, no setup needed)

        void OnGUI()
        {
            if (round == null) return;

            int h = Screen.height;
            var big = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = h / 8, fontStyle = FontStyle.Bold };
            var mid = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontSize = h / 28 };
            var small = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.UpperLeft, fontSize = h / 36 };

            string centerText = "";
            Color color = Color.white;
            switch (round.State)
            {
                case RoundState.Preparation: centerText = "Hold still..."; break;
                case RoundState.Countdown: centerText = Mathf.CeilToInt(Mathf.Max(_countdownLeft, 0.01f)).ToString(); color = new Color(1f, 0.8f, 0.2f); break;
                case RoundState.Trigger: centerText = "GO!"; color = new Color(0.3f, 1f, 0.4f); break;
                case RoundState.Result: centerText = _banner; color = _banner.StartsWith("YOU WIN") ? new Color(0.3f, 1f, 0.4f) : new Color(1f, 0.4f, 0.4f); break;
            }

            big.normal.textColor = color;
            GUI.Label(new Rect(0, h * 0.08f, Screen.width, h * 0.2f), centerText, big);

            if (round.State == RoundState.Result)
            {
                mid.normal.textColor = Color.white;
                GUI.Label(new Rect(0, h * 0.28f, Screen.width, h * 0.15f), _detail, mid);
            }

            small.normal.textColor = Color.white;
            string hint = Application.isEditor ? "\nEditor: click = pick up phone" : "\nPick up your phone on GO";
            GUI.Label(new Rect(16, 12, Screen.width * 0.6f, h * 0.2f),
                "Pony Pack  |  You " + _playerWins + " - " + _botWins + " Bot\nState: " + round.State + hint, small);
        }
    }
}
