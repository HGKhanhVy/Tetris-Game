using System;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BrickStacker
{
    // Trạng thái trận 1v1 đua điểm hiện tại. Gameplay đọc/ghi qua đây,
    // MultiplayerManager cập nhật từ mạng. Active=false nghĩa là đang chơi solo.
    public static class MultiplayerMatch
    {
        public static bool Active;
        public static bool Preview;   // solo xem thử giao diện online (không có mạng/đối thủ)
        public static int Level;
        public static int Seed;

        public static int OpponentScore;
        public static int OpponentLines;
        public static bool OpponentFinished; // đối thủ hoàn thành màn trước → mình thua
        public static bool OpponentLost;     // đối thủ game over → mình thắng
        public static bool OpponentLeft;     // đối thủ thoát trận → mình thắng vắng mặt
        public static string OpponentName;   // tên đối thủ (rỗng = chưa biết)
        public static int PendingGarbage;    // số hàng rác đối thủ gửi, chờ game áp vào bàn

        // === Chế độ năng lượng/kỹ năng (design §6-11) ===
        public static int OpponentHealth = OnlineConfig.MaxHealth; // máu đối thủ (HUD)
        public static int OpponentEnergy;                          // năng lượng đối thủ (HUD)
        public static int PendingIncomingAttacks;                  // đòn tấn công chờ áp vào máu mình
        public static bool AttackWarningActive;                    // đang cảnh báo đòn tới (§7.1)

        // Ảnh chụp bàn xếp gạch của đối thủ (0 = trống, 1..N = loại khối + 1),
        // index = x + y * cols, y = 0 là đáy. Dirty = có bản mới chưa vẽ.
        public static byte[] OpponentBoard;
        public static int OpponentBoardCols;
        public static int OpponentBoardRows;
        public static bool OpponentBoardDirty;

        // Vị trí quân trên bàn cờ chiến thuật của đối thủ (tường giống mình vì cùng level).
        // (-1,-1) = chưa nhận được.
        public static Vector2Int OpponentTacticalPlayer;
        public static Vector2Int OpponentTacticalEnemy;
        public static Vector2Int OpponentTacticalMonster;

        public static void Begin(int level, int seed)
        {
            Active = true;
            Preview = false;
            Level = level;
            Seed = seed;
            OpponentScore = 0;
            OpponentLines = 0;
            OpponentFinished = false;
            OpponentLost = false;
            OpponentLeft = false;
            OpponentBoard = null;
            OpponentBoardCols = 0;
            OpponentBoardRows = 0;
            OpponentBoardDirty = false;
            OpponentTacticalPlayer = new Vector2Int(-1, -1);
            OpponentTacticalEnemy = new Vector2Int(-1, -1);
            OpponentTacticalMonster = new Vector2Int(-1, -1);
            PendingGarbage = 0;
            OpponentHealth = OnlineConfig.MaxHealth;
            OpponentEnergy = 0;
            PendingIncomingAttacks = 0;
            AttackWarningActive = false;
            // OpponentName giữ nguyên — có thể đã nhận từ bắt tay trước khi Begin chạy.
        }

        public static void Reset()
        {
            Active = false;
            Preview = false;
        }
    }

    // Quản lý phòng đấu 1v1 qua Unity Multiplayer Services (Sessions + Relay + NGO).
    // NetworkManager được dựng bằng code, không cần prefab hay scene object.
    // Không spawn NetworkObject nào — toàn bộ trao đổi qua named messages:
    //   READY: client gửi sau khi kết nối + đã đăng ký handler → báo sẵn sàng nhận.
    //   START: host gửi level + seed khi nhận READY → cả hai vào BrickGame.
    //   STATE: điểm/số hàng/cờ kết thúc, gửi mỗi khi thay đổi.
    // Handler phải đăng ký từ OnServerStarted/OnClientStarted (trước khi kết nối xong):
    // NGO vứt bỏ named message không có handler, gửi START sớm hơn là mất trắng.
    public class MultiplayerManager : MonoBehaviour
    {
        const string ReadyMsg = "BLOCKFALL_MP_READY";
        const string StartMsg = "BLOCKFALL_MP_START";
        const string StateMsg = "BLOCKFALL_MP_STATE";
        const string BoardMsg = "BLOCKFALL_MP_BOARD";
        const string GarbageMsg = "BLOCKFALL_MP_GARBAGE";
        const string SkillMsg = "BLOCKFALL_MP_SKILL";

        // Người chơi chủ yếu ở Việt Nam — ghim Relay về Singapore thay vì để QoS
        // tự chọn (QoS hay fail trên WebGL → rơi về region mặc định xa lắc, lag nặng).
        const string RelayRegion = "asia-southeast1";

        public const byte FlagFinished = 1; // người gửi đã hoàn thành màn (thắng)
        public const byte FlagLost = 2;     // người gửi đã thua (game over)

        public static MultiplayerManager Instance { get; private set; }

        // Lobby UI lắng nghe các sự kiện này (luôn gọi trên main thread).
        public event Action OpponentJoined;
        public event Action OpponentLeftLobby;

        // Session property đánh index để query: String1 = chế độ, String2 = mã phòng 4 số.
        const string ModePropertyKey = "mode";
        const string CodePropertyKey = "roomcode";
        const string ModeCode = "code";
        const string ModeQuick = "quick";

        ISession session;
        string roomCode; // mã 4 chữ số dạng chuỗi (giữ số 0 đầu), null với phòng ghép nhanh
        int quickScanGeneration; // tăng mỗi lần rời phòng — vòng quét ghép nhanh cũ tự dừng
        ulong opponentClientId = ulong.MaxValue;
        bool matchStarted;
        bool startAcked;       // host: client đã xác nhận nhận START
        int chosenLevel;       // host: level/seed đã chọn, dùng khi gửi lại START
        int chosenSeed;
        int lastSentScore = -1, lastSentLines = -1;
        byte lastSentFlags;

        public bool InSession => session != null;
        public string RoomCode => roomCode;

        public static MultiplayerManager Ensure()
        {
            if (Instance != null)
                return Instance;

            var go = new GameObject("Multiplayer Manager");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<MultiplayerManager>();

            var networkManager = go.AddComponent<NetworkManager>();
            var transport = go.AddComponent<UnityTransport>();
#if UNITY_WEBGL
            // Nền Web: Relay cấp endpoint wss — transport phải dùng WebSocket,
            // nếu không StartHost fail "Mismatched Relay configuration and network interface".
            transport.UseWebSockets = true;
#endif
            networkManager.NetworkConfig = new NetworkConfig
            {
                NetworkTransport = transport,
                EnableSceneManagement = false, // scene tự load hai bên, không sync qua NGO
                PlayerPrefab = null            // không spawn object — chỉ dùng named messages
            };

            // Đăng ký cố định một lần — handler message phải có mặt TRƯỚC khi kết nối xong.
            networkManager.OnServerStarted += Instance.RegisterHandlers;
            networkManager.OnClientStarted += Instance.RegisterHandlers;
            networkManager.OnClientConnectedCallback += Instance.OnClientConnected;
            networkManager.OnClientDisconnectCallback += Instance.OnClientDisconnected;

            return Instance;
        }

        // Tạo phòng 2 người với mã 4 chữ số tự sinh (chuỗi, giữ số 0 đầu — vd "0042").
        // Session để public cho JoinRoomAsync query được theo mã; "riêng tư" nằm ở chỗ
        // chỉ ai biết mã mới tìm — đủ dùng cho game casual.
        // NGO tắt bất đồng bộ sau LeaveAsync — start host mới khi shutdown chưa xong
        // sẽ làm session mới treo vĩnh viễn. Đợi tối đa ~8 giây (vượt qua cả watchdog
        // 5s trong EnsureNetworkStoppedAfterLeave) cho tắt hẳn.
        static async Task WaitForNetworkIdleAsync()
        {
            var networkManager = NetworkManager.Singleton;
            for (int i = 0; i < 480 && networkManager != null
                 && (networkManager.IsListening || networkManager.ShutdownInProgress); i++)
                await Task.Yield();
        }

        public async Task<string> CreateRoomAsync()
        {
            if (!await ServicesManager.EnsureSignedInAsync())
                throw new InvalidOperationException("Không kết nối được Unity Services");
            await WaitForNetworkIdleAsync();

            roomCode = UnityEngine.Random.Range(0, 10000).ToString("D4");
            session = await CreateRelaySessionAsync(() => new SessionOptions
            {
                Name = "BF_CODE_" + roomCode + "_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                MaxPlayers = 2,
                IsPrivate = false,
                SessionProperties = new System.Collections.Generic.Dictionary<string, SessionProperty>
                {
                    { ModePropertyKey, new SessionProperty(ModeCode, VisibilityPropertyOptions.Public, PropertyIndex.String1) },
                    { CodePropertyKey, new SessionProperty(roomCode, VisibilityPropertyOptions.Public, PropertyIndex.String2) }
                }
            });
            HookSession();
            return roomCode;
        }

        // Tạo session Relay ghim region gần; region lỗi thì rơi về mặc định (QoS).
        static async Task<ISession> CreateRelaySessionAsync(Func<SessionOptions> makeOptions)
        {
            try
            {
                return await MultiplayerService.Instance.CreateSessionAsync(
                    makeOptions().WithRelayNetwork(RelayRegion));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Multiplayer] Region {RelayRegion} lỗi ({e.Message}) — dùng region mặc định.");
                return await MultiplayerService.Instance.CreateSessionAsync(
                    makeOptions().WithRelayNetwork());
            }
        }

        public async Task JoinRoomAsync(string code)
        {
            if (!await ServicesManager.EnsureSignedInAsync())
                throw new InvalidOperationException("Không kết nối được Unity Services");
            await WaitForNetworkIdleAsync();

            code = (code ?? "").Trim();

            var results = await MultiplayerService.Instance.QuerySessionsAsync(new QuerySessionsOptions
            {
                Count = 5,
                FilterOptions = new System.Collections.Generic.List<FilterOption>
                {
                    new FilterOption(FilterField.StringIndex2, code, FilterOperation.Equal),
                    new FilterOption(FilterField.AvailableSlots, "1", FilterOperation.GreaterOrEqual)
                }
            });

            if (results.Sessions.Count == 0)
                throw new InvalidOperationException("Không tìm thấy phòng " + code + ".\nKiểm tra lại mã nhé!");

            session = await MultiplayerService.Instance.JoinSessionByIdAsync(results.Sessions[0].Id);
            roomCode = code;
            HookSession();
        }

        // Query tìm phòng quick chạy sẵn ở nền khi người chơi mở overlay 1 vs 1 —
        // bấm GHÉP NHANH là có kết quả liền, đỡ một vòng round-trip (~0.5-1s).
        static Task<QuerySessionsResults> prewarmedQuickQuery;
        static float prewarmedQuickAt = float.NegativeInfinity;

        public static void PrewarmQuickQuery()
        {
            if (!ServicesManager.IsSignedIn)
                return;
            try
            {
                prewarmedQuickQuery = QueryQuickSessionsAsync();
                prewarmedQuickAt = Time.realtimeSinceStartup;
            }
            catch (Exception)
            {
                prewarmedQuickQuery = null;
            }
        }

        // Ghép nhanh: tìm phòng quick còn chỗ để vào; không có thì tự tạo phòng chờ.
        // Trả về true nếu vào được phòng có sẵn, false nếu đang làm host chờ người lạ.
        public async Task<bool> QuickMatchAsync()
        {
            if (!await ServicesManager.EnsureSignedInAsync())
                throw new InvalidOperationException("Không kết nối được Unity Services");
            await WaitForNetworkIdleAsync();

            QuerySessionsResults results = null;
            var warm = prewarmedQuickQuery;
            prewarmedQuickQuery = null;
            if (warm != null && Time.realtimeSinceStartup - prewarmedQuickAt < 6f)
            {
                try { results = await warm; }
                catch (Exception) { results = null; } // query nền lỗi — query lại bình thường
            }
            if (results == null)
                results = await QueryQuickSessionsAsync();

            foreach (var info in results.Sessions)
            {
                try
                {
                    session = await MultiplayerService.Instance.JoinSessionByIdAsync(info.Id);
                    roomCode = null;
                    HookSession();
                    return true;
                }
                catch (Exception)
                {
                    // Phòng vừa đầy hoặc vừa giải tán — thử phòng kế tiếp.
                }
            }

            session = await CreateRelaySessionAsync(() => new SessionOptions
            {
                Name = "BF_QUICK_" + Guid.NewGuid().ToString("N").Substring(0, 12),
                MaxPlayers = 2,
                IsPrivate = false,
                SessionProperties = new System.Collections.Generic.Dictionary<string, SessionProperty>
                {
                    { ModePropertyKey, new SessionProperty(ModeQuick, VisibilityPropertyOptions.Public, PropertyIndex.String1) }
                }
            });
            roomCode = null;
            HookSession();
            // Hai người bấm ghép nhanh gần cùng lúc sẽ cùng không thấy nhau (query chạy
            // trước khi phòng kia kịp vào index) → cả hai làm host chờ vĩnh viễn.
            // Quét lại định kỳ để hai phòng chờ tìm thấy nhau.
            ScanForQuickOpponentAsync(session, ++quickScanGeneration);
            return false;
        }

        static Task<QuerySessionsResults> QueryQuickSessionsAsync()
        {
            return MultiplayerService.Instance.QuerySessionsAsync(new QuerySessionsOptions
            {
                Count = 5,
                FilterOptions = new System.Collections.Generic.List<FilterOption>
                {
                    new FilterOption(FilterField.StringIndex1, ModeQuick, FilterOperation.Equal),
                    new FilterOption(FilterField.AvailableSlots, "1", FilterOperation.GreaterOrEqual)
                }
            });
        }

        // Đợi bằng Task.Yield thay vì Task.Delay — Task.Delay dựa vào thread pool,
        // không chạy được trên WebGL.
        static async Task WaitRealtimeAsync(float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end)
                await Task.Yield();
        }

        // Host phòng chờ ghép nhanh quét phòng quick khác theo chu kỳ. Thấy phòng thì
        // chỉ bên có session Id LỚN hơn rời phòng mình sang phòng kia — tie-break để
        // hai host không đổi chỗ chéo nhau (cả hai cùng nhảy → hai phòng đều trống).
        async void ScanForQuickOpponentAsync(ISession own, int generation)
        {
            while (true)
            {
                await WaitRealtimeAsync(4f);

                if (generation != quickScanGeneration || session != own || matchStarted)
                    return;
                // Đã có người vào phòng mình (kể cả đang bắt tay chưa READY) — thôi quét.
                var networkManager = NetworkManager.Singleton;
                if (opponentClientId != ulong.MaxValue || (networkManager != null
                    && networkManager.IsHost && networkManager.ConnectedClientsIds.Count > 1))
                    return;

                QuerySessionsResults results;
                try { results = await QueryQuickSessionsAsync(); }
                catch (Exception) { continue; } // mạng chập chờn — thử lại vòng sau

                if (generation != quickScanGeneration || session != own || matchStarted
                    || opponentClientId != ulong.MaxValue)
                    return;

                foreach (var info in results.Sessions)
                {
                    if (info.Id == own.Id || string.CompareOrdinal(own.Id, info.Id) <= 0)
                        continue;

                    await LeaveAsync();
                    int myGeneration = quickScanGeneration; // LeaveAsync vừa tăng
                    await WaitForNetworkIdleAsync();
                    try
                    {
                        var joined = await MultiplayerService.Instance.JoinSessionByIdAsync(info.Id);
                        if (myGeneration != quickScanGeneration)
                        {
                            // Người chơi hủy chờ giữa chừng — rời luôn phòng vừa vào.
                            LeaveSessionInBackground(joined);
                            return;
                        }
                        session = joined;
                        roomCode = null;
                        HookSession();
                    }
                    catch (Exception)
                    {
                        // Phòng kia vừa biến mất — xếp hàng lại từ đầu (trừ khi đã hủy chờ).
                        if (myGeneration == quickScanGeneration)
                        {
                            try { await QuickMatchAsync(); }
                            catch (Exception e) { Debug.LogWarning($"[Multiplayer] Ghép nhanh lại thất bại: {e.Message}"); }
                        }
                    }
                    return;
                }
            }
        }

        // Rời phòng/trận. An toàn gọi nhiều lần. Reset trạng thái và tắt NGO NGAY
        // để có thể tạo/vào phòng mới liền; session.LeaveAsync có thể treo rất lâu
        // (đã gặp khi test) nên cho chạy nền, không await.
        public Task LeaveAsync()
        {
            MultiplayerMatch.Reset();
            MultiplayerMatch.OpponentName = "";
            quickScanGeneration++; // dừng vòng quét ghép nhanh đang chạy (nếu có)
            matchStarted = false;
            startAcked = true; // dừng coroutine gửi lại START nếu còn chạy
            roomCode = null;
            opponentClientId = ulong.MaxValue;
            lastSentScore = lastSentLines = -1;
            lastSentFlags = 0;

            var current = session;
            session = null;

            if (current != null)
            {
                UnhookSession(current);
                // Để SDK tự tắt NGO qua leave (gọi NetworkManager.Shutdown thủ công
                // sẽ phá state machine của SDK — nó cảnh báo rõ trong source).
                LeaveSessionInBackground(current);
                // Nhưng leave đôi khi treo — watchdog ép tắt NGO sau 5s nếu cần.
                StartCoroutine(EnsureNetworkStoppedAfterLeave());
            }

            return Task.CompletedTask;
        }

        static async void LeaveSessionInBackground(ISession current)
        {
            try { await current.LeaveAsync(); }
            catch (Exception e) { Debug.LogWarning($"[Multiplayer] Lỗi khi rời phòng: {e.Message}"); }
        }

        System.Collections.IEnumerator EnsureNetworkStoppedAfterLeave()
        {
            float deadline = Time.realtimeSinceStartup + 5f;
            var networkManager = NetworkManager.Singleton;
            // session != null nghĩa là đã vào phòng mới — NGO đang dùng, không đụng.
            while (Time.realtimeSinceStartup < deadline
                   && session == null && networkManager != null && networkManager.IsListening)
                yield return null;

            if (session == null && networkManager != null
                && networkManager.IsListening && !networkManager.ShutdownInProgress)
            {
                Debug.LogWarning("[Multiplayer] Rời phòng quá chậm — buộc tắt NGO.");
                networkManager.Shutdown();
            }
        }

        void HookSession()
        {
            session.RemovedFromSession += OnRemovedFromSession;
            session.Deleted += OnRemovedFromSession;
            session.PlayerLeaving += OnSessionPlayerLeft;
        }

        void UnhookSession(ISession current)
        {
            current.RemovedFromSession -= OnRemovedFromSession;
            current.Deleted -= OnRemovedFromSession;
            current.PlayerLeaving -= OnSessionPlayerLeft;
        }

        void RegisterHandlers()
        {
            var messaging = NetworkManager.Singleton != null ? NetworkManager.Singleton.CustomMessagingManager : null;
            if (messaging == null)
                return;
            messaging.RegisterNamedMessageHandler(ReadyMsg, OnReadyMessage);
            messaging.RegisterNamedMessageHandler(StartMsg, OnStartMessage);
            messaging.RegisterNamedMessageHandler(StateMsg, OnStateMessage);
            messaging.RegisterNamedMessageHandler(BoardMsg, OnBoardMessage);
            messaging.RegisterNamedMessageHandler(GarbageMsg, OnGarbageMessage);
            messaging.RegisterNamedMessageHandler(SkillMsg, OnSkillMessage);
        }

        // READY có 2 nghĩa theo byte đầu: 1 = client sẵn sàng nhận START,
        // 2 = client xác nhận ĐÃ nhận START (ACK — host ngừng gửi lại).
        const byte ReadyKindHello = 1;
        const byte ReadyKindAck = 2;

        void SendReady(byte kind)
        {
            var networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsListening)
                return;
            string name = ServicesManager.PlayerName ?? "";
            using var writer = new FastBufferWriter(8 + name.Length * 4, Allocator.Temp);
            writer.WriteValueSafe(kind);
            writer.WriteValueSafe(name);
            networkManager.CustomMessagingManager.SendNamedMessage(
                ReadyMsg, NetworkManager.ServerClientId, writer, NetworkDelivery.ReliableSequenced);
        }

        void OnClientConnected(ulong clientId)
        {
            var networkManager = NetworkManager.Singleton;

            // Client vừa kết nối xong (handler đã đăng ký từ OnClientStarted)
            // → báo host mình sẵn sàng nhận START.
            if (!networkManager.IsHost && clientId == networkManager.LocalClientId)
                SendReady(ReadyKindHello);
        }

        // Host nhận READY: đối thủ chắc chắn đã nghe được message → khai trận.
        void OnReadyMessage(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out byte kind);
            if (reader.Length - reader.Position > 0)
            {
                reader.ReadValueSafe(out string senderName);
                if (!string.IsNullOrEmpty(senderName))
                    MultiplayerMatch.OpponentName = senderName;
            }

            var networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsHost)
                return;

            if (kind == ReadyKindAck)
            {
                startAcked = true;
                return;
            }

            if (matchStarted)
            {
                // Client gửi lại READY (vd. chưa nhận được START) → gửi lại ngay.
                SendStart(senderId);
                return;
            }

            opponentClientId = senderId;
            OpponentJoined?.Invoke();

            chosenLevel = UnityEngine.Random.Range(1, LevelProgress.MaxLevels + 1);
            chosenSeed = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            startAcked = false;

            SendStart(senderId);
            // START có thể thất lạc (client đổi scene, mạng chập chờn...) —
            // gửi lại định kỳ tới khi client ACK, trận mới chắc chắn nổ ra hai bên.
            StartCoroutine(ResendStartUntilAcked());

            StartLocalMatch(chosenLevel, chosenSeed);
        }

        void SendStart(ulong target)
        {
            var networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsListening)
                return;
            string name = ServicesManager.PlayerName ?? "";
            using var writer = new FastBufferWriter(sizeof(int) * 2 + 8 + name.Length * 4, Allocator.Temp);
            writer.WriteValueSafe(chosenLevel);
            writer.WriteValueSafe(chosenSeed);
            writer.WriteValueSafe(name);
            networkManager.CustomMessagingManager.SendNamedMessage(
                StartMsg, target, writer, NetworkDelivery.ReliableSequenced);
        }

        System.Collections.IEnumerator ResendStartUntilAcked()
        {
            for (int attempt = 0; attempt < 15 && !startAcked; attempt++)
            {
                yield return new WaitForSecondsRealtime(2f);
                if (startAcked || session == null || opponentClientId == ulong.MaxValue)
                    yield break;
                SendStart(opponentClientId);
            }
        }

        void OnStartMessage(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int level);
            reader.ReadValueSafe(out int seed);
            if (reader.Length - reader.Position > 0)
            {
                reader.ReadValueSafe(out string hostName);
                if (!string.IsNullOrEmpty(hostName))
                    MultiplayerMatch.OpponentName = hostName;
            }
            // ACK mỗi lần nhận (kể cả START lặp lại — ACK trước có thể đã thất lạc).
            SendReady(ReadyKindAck);
            StartLocalMatch(level, seed);
        }

        void StartLocalMatch(int level, int seed)
        {
            if (matchStarted)
                return;
            matchStarted = true;
            lastSentScore = lastSentLines = -1;
            lastSentFlags = 0;

            MultiplayerMatch.Begin(level, seed);
            GameSession.SelectedLevel = level;
            GameSession.JourneyLevel = level;
            SceneManager.LoadScene("BrickGame");
        }

        // Gameplay gọi mỗi khi điểm/hàng/cờ thay đổi; chỉ gửi khi khác lần trước.
        // Kèm máu + năng lượng để HUD đối thủ hiển thị (design §6.1).
        int lastSentHealth = -1, lastSentEnergy = -1;
        public void SendState(int score, int lines, byte flags, int health = -1, int energy = -1)
        {
            if (!matchStarted)
                return;
            // Đã gửi cờ kết thúc thì trận coi như xong — không gửi thêm gì nữa.
            if ((lastSentFlags & (FlagFinished | FlagLost)) != 0)
                return;
            if (score == lastSentScore && lines == lastSentLines && flags == lastSentFlags
                && health == lastSentHealth && energy == lastSentEnergy)
                return;

            var networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsListening)
                return;

            ulong target = networkManager.IsHost ? opponentClientId : NetworkManager.ServerClientId;
            if (networkManager.IsHost && opponentClientId == ulong.MaxValue)
                return;

            lastSentScore = score;
            lastSentLines = lines;
            lastSentFlags = flags;
            lastSentHealth = health;
            lastSentEnergy = energy;

            using var writer = new FastBufferWriter(sizeof(int) * 4 + 1, Allocator.Temp);
            writer.WriteValueSafe(score);
            writer.WriteValueSafe(lines);
            writer.WriteValueSafe(flags);
            writer.WriteValueSafe(health);
            writer.WriteValueSafe(energy);
            networkManager.CustomMessagingManager.SendNamedMessage(
                StateMsg, target, writer, NetworkDelivery.ReliableSequenced);
        }

        // Gửi ảnh chụp bàn xếp gạch + vị trí 3 quân bàn cờ (6 byte, 255 = không có)
        // cho đối thủ vẽ bàn mini. Gameplay tự throttle tần suất.
        public void SendBoard(byte[] cells, byte cols, byte rows, byte[] tactical6)
        {
            if (!matchStarted || cells == null || cells.Length != cols * rows)
                return;
            if (tactical6 == null || tactical6.Length != 6)
                return;

            var networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsListening)
                return;

            ulong target = networkManager.IsHost ? opponentClientId : NetworkManager.ServerClientId;
            if (networkManager.IsHost && opponentClientId == ulong.MaxValue)
                return;

            using var writer = new FastBufferWriter(cells.Length + 8, Allocator.Temp);
            writer.WriteValueSafe(cols);
            writer.WriteValueSafe(rows);
            writer.WriteBytesSafe(cells, cells.Length);
            writer.WriteBytesSafe(tactical6, 6);
            // Bàn mini chỉ là hình minh họa — bản mới thay bản cũ, mất gói không sao.
            networkManager.CustomMessagingManager.SendNamedMessage(
                BoardMsg, target, writer, NetworkDelivery.UnreliableSequenced);
        }

        void OnBoardMessage(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out byte cols);
            reader.ReadValueSafe(out byte rows);
            int count = cols * rows;
            if (count <= 0 || count > 4096 || reader.Length - reader.Position < count)
                return;

            if (MultiplayerMatch.OpponentBoard == null || MultiplayerMatch.OpponentBoard.Length != count)
                MultiplayerMatch.OpponentBoard = new byte[count];
            reader.ReadBytesSafe(ref MultiplayerMatch.OpponentBoard, count);
            MultiplayerMatch.OpponentBoardCols = cols;
            MultiplayerMatch.OpponentBoardRows = rows;

            if (reader.Length - reader.Position >= 6)
            {
                var tactical = new byte[6];
                reader.ReadBytesSafe(ref tactical, 6);
                MultiplayerMatch.OpponentTacticalPlayer = ReadTacticalPos(tactical[0], tactical[1]);
                MultiplayerMatch.OpponentTacticalEnemy = ReadTacticalPos(tactical[2], tactical[3]);
                MultiplayerMatch.OpponentTacticalMonster = ReadTacticalPos(tactical[4], tactical[5]);
            }

            MultiplayerMatch.OpponentBoardDirty = true;
        }

        static Vector2Int ReadTacticalPos(byte x, byte y)
        {
            return x == 255 || y == 255 ? new Vector2Int(-1, -1) : new Vector2Int(x, y);
        }

        // Gửi hàng rác trừng phạt khi clear 3+ hàng cùng lúc (Reliable — không được mất).

        void OnGarbageMessage(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out byte rows);
            MultiplayerMatch.PendingGarbage += Mathf.Clamp(rows, 1, 4);
        }

        // Gửi 1 sự kiện dùng kỹ năng lên đối thủ (design §6.3). Attack/Garbage tác động
        // sang bàn đối thủ; Shield là cục bộ nên KHÔNG gửi. seq để chống double-apply (§15).
        int skillSendSeq;
        public void SendSkill(OnlineSkill skill, byte amount)
        {
            if (!matchStarted)
                return;
            if ((lastSentFlags & (FlagFinished | FlagLost)) != 0)
                return;

            var networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsListening)
                return;

            ulong target = networkManager.IsHost ? opponentClientId : NetworkManager.ServerClientId;
            if (networkManager.IsHost && opponentClientId == ulong.MaxValue)
                return;

            skillSendSeq++;
            using var writer = new FastBufferWriter(sizeof(int) + 2, Allocator.Temp);
            writer.WriteValueSafe(skillSendSeq);
            writer.WriteValueSafe((byte)skill);
            writer.WriteValueSafe(amount);
            networkManager.CustomMessagingManager.SendNamedMessage(
                SkillMsg, target, writer, NetworkDelivery.ReliableSequenced);
        }

        int lastSkillSeqApplied = -1;
        void OnSkillMessage(ulong senderId, FastBufferReader reader)
        {
            reader.ReadValueSafe(out int seq);
            reader.ReadValueSafe(out byte skillId);
            reader.ReadValueSafe(out byte amount);

            // Chống áp trùng khi mạng retry gửi lại (design §15).
            if (seq <= lastSkillSeqApplied)
                return;
            lastSkillSeqApplied = seq;

            switch ((OnlineSkill)skillId)
            {
                case OnlineSkill.Attack:
                    MultiplayerMatch.PendingIncomingAttacks += Mathf.Max(1, amount);
                    MultiplayerMatch.AttackWarningActive = true;
                    break;
                case OnlineSkill.Garbage:
                    MultiplayerMatch.PendingGarbage += Mathf.Clamp(amount, 1, 4);
                    break;
            }
        }

        void OnStateMessage(ulong senderId, FastBufferReader reader)
        {
            startAcked = true; // STATE tới nghĩa là đối thủ chắc chắn đã vào trận
            reader.ReadValueSafe(out int score);
            reader.ReadValueSafe(out int lines);
            reader.ReadValueSafe(out byte flags);
            int health = -1, energy = -1;
            if (reader.Length - reader.Position >= sizeof(int) * 2)
            {
                reader.ReadValueSafe(out health);
                reader.ReadValueSafe(out energy);
            }

            MultiplayerMatch.OpponentScore = score;
            MultiplayerMatch.OpponentLines = lines;
            if (health >= 0) MultiplayerMatch.OpponentHealth = health;
            if (energy >= 0) MultiplayerMatch.OpponentEnergy = energy;
            if ((flags & FlagFinished) != 0) MultiplayerMatch.OpponentFinished = true;
            if ((flags & FlagLost) != 0) MultiplayerMatch.OpponentLost = true;
        }

        void OnClientDisconnected(ulong clientId)
        {
            // Mình đã chủ động rời phòng (session null) thì đây là shutdown của chính mình.
            if (session == null)
                return;

            var networkManager = NetworkManager.Singleton;
            bool opponentGone = networkManager != null && networkManager.IsHost
                ? clientId == opponentClientId
                : true; // là client thì mất kết nối nghĩa là host đã rời

            if (opponentGone)
                HandleOpponentGone();
        }

        void OnSessionPlayerLeft(string playerId) => HandleOpponentGone();
        void OnRemovedFromSession() => HandleOpponentGone();

        void HandleOpponentGone()
        {
            if (MultiplayerMatch.Active)
                MultiplayerMatch.OpponentLeft = true; // gameplay tự xử: thắng vắng mặt
            else
                OpponentLeftLobby?.Invoke();
        }
    }
}
