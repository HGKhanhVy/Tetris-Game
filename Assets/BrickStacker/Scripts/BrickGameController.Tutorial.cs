using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BrickStacker
{
    // TUTORIAL TƯƠNG TÁC: người chơi chơi màn thật, nhưng ở mỗi bước game đứng yên, làm tối những
    // chỗ không liên quan, chỉ chừa sáng đúng thứ cần chú ý, cho bàn tay mô phỏng thao tác rồi
    // CHỜ người chơi tự làm đúng mới đi tiếp.
    //
    // Vòng đời một bước:  khoá thao tác thừa -> highlight -> tay mô phỏng -> chờ làm đúng ->
    //                     phản hồi (rung + âm) -> bước sau.
    //
    // Có 2 loại bước:
    //  - Bước trong danh sách (tutorialSteps): chạy tuần tự, mỗi bước có điều kiện Ready riêng.
    //  - Bước xen giữa gameplay (coroutine): khi cụm sắp nổ hoặc vừa nhận tài nguyên — gọi thẳng
    //    từ luồng xử lý cụm để dừng ĐÚNG khoảnh khắc đó.
    public partial class BrickGameController : MonoBehaviour
    {
        // Một bước hướng dẫn. Dùng delegate cho vùng highlight / vị trí bàn tay vì các toạ độ đó
        // chỉ có nghĩa NGAY LÚC bước chạy (khối đang rơi, quân đang đứng đâu).
        class TutorialStep
        {
            public string Title;
            public string Body;
            public TutorialAction Wait = TutorialAction.Continue;
            public Func<bool> Ready;          // null = vào được ngay
            public Func<Rect> Spotlight;      // null = tối cả màn, không chừa ô sáng
            public Func<Vector2> TapHand;     // null = không có tay nhấn
            public Vector2 TapAim;            // Vector2.zero = tay tự chọn hướng theo vị trí chạm
            public Func<Rect> CardAnchor;     // bước không khoanh ô sáng thì thẻ nép vào vùng này
            public bool FreeControl;          // true = không đóng băng game (bước cần khối tiếp tục rơi)
            public Func<Vector2[]> SwipeHand; // null = không có tay vuốt (trả về 2 điểm)
            public TutorialCardPlacement Placement = TutorialCardPlacement.NextToTarget;
        }

        bool tutorialActive;
        bool tutorialHold;                  // true = đóng băng gameplay trong lúc hướng dẫn
        bool tutorialStepShowing;           // đang hiện một bước và chờ người chơi
        bool tutorialInterlude;             // bước xen giữa gameplay (coroutine tự đóng, KHÔNG đụng tutorialStep)
        bool tutorialStepsDone;             // đã hết danh sách bước, chỉ còn các bài học tài nguyên
        bool tutorialFreeControl;           // bước hiện tại cho chơi bình thường, chỉ hiện thẻ nhắc
        bool tutorialCompletionPending;     // còn nợ thẻ "Hoàn thành hướng dẫn" khi thắng
        bool tutorialCompletionShown;
        TutorialAction tutorialWaitFor = TutorialAction.None;
        int tutorialStep;
        bool tutorialContinuePressed;
        bool tutorialAnyClear;              // đã phá được cụm đầu tiên
        bool tutorialPlayerMoved;           // đã đi quân trên bàn cờ lần đầu
        // Loại tài nguyên đang được dạy trong lần nổ này. Chọn ngay lúc cụm còn trên bàn (bước
        // chừa sáng cụm), rồi cả phần giải thích chức năng lẫn phần xem hiệu ứng đều bám theo nó,
        // để một lần nổ chỉ có đúng một mạch bài học.
        Puzzle.ResourceType? tutorialPendingLesson;

        // Tutorial đã tự áp hiệu ứng cho từng nhóm cụm trong lúc giải chuỗi -> luồng chung phải bỏ
        // qua bước cộng dồn cuối chuỗi, nếu không tài nguyên bị cộng hai lần.
        bool tutorialHandledClusterEffects;

        // Cụm của một bậc, tách theo loại tài nguyên để dạy lần lượt.
        readonly List<List<Puzzle.Cluster>> clusterGroups = new List<List<Puzzle.Cluster>>();
        readonly List<Puzzle.ResourceType> clusterGroupOrder = new List<Puzzle.ResourceType>();
        int clusterGroupCount;
        readonly HashSet<Puzzle.ResourceType> tutorialExplainedResources = new HashSet<Puzzle.ResourceType>();
        readonly List<Puzzle.ResourceType> tutorialResourcesToWatch = new List<Puzzle.ResourceType>();
        readonly List<Rect> tutorialCellHighlights = new List<Rect>();

        // Ba loại tài nguyên offline phải dạy đủ trước khi tutorial tự tắt. Người chơi thường ăn
        // cụm giày trước, còn kiếm/khiên có thể mãi sau mới ra -> tutorial phải sống chờ tới lúc đó.
        static readonly Puzzle.ResourceType[] TutorialLessonResources =
        {
            Puzzle.ResourceType.Move,
            Puzzle.ResourceType.Attack,
            Puzzle.ResourceType.Shield
        };

        TutorialOverlayView tutorialView;
        List<TutorialStep> tutorialSteps;

        // === Vòng đời ===

        void TutorialBegin()
        {
            tutorialActive = true;
            tutorialStep = 0;
            tutorialAnyClear = false;
            tutorialPlayerMoved = false;
            tutorialPendingLesson = null;
            tutorialHandledClusterEffects = false;
            tutorialStepsDone = false;
            tutorialInterlude = false;
            tutorialFreeControl = false;
            tutorialCompletionPending = false;
            tutorialCompletionShown = false;
            tutorialExplainedResources.Clear();
            tutorialResourcesToWatch.Clear();

            tutorialView = TutorialOverlayView.Create();
            tutorialView.ContinuePressed += OnTutorialContinue;
            BuildTutorialSteps();
        }

        void OnTutorialContinue()
        {
            RuntimeArt.PlayUiSwitchSound();
            tutorialContinuePressed = true;
        }

        // Gọi mỗi khung hình, TRƯỚC cổng đóng băng.
        void TutorialTick()
        {
            if (!tutorialActive || tutorialSteps == null)
                return;

            if (tutorialStepShowing)
            {
                // Bước xen giữa gameplay do chính coroutine của nó đóng lại — đụng vào đây sẽ
                // nhảy oan một bước trong danh sách.
                if (tutorialInterlude)
                    return;

                // Bước chờ bấm TIẾP: nút do lớp phủ bắt, ở đây chỉ việc đóng bước lại.
                if (tutorialWaitFor == TutorialAction.Continue && tutorialContinuePressed)
                    CompleteTutorialStep();
                return;
            }

            if (tutorialStepsDone)
                return;

            // Đang giải cụm: coroutine giải cụm có thẻ xen giữa của riêng nó, mở thêm bước lúc này
            // sẽ chèn hai thẻ đè lên nhau. Chờ nó xong rồi bước kế mở cũng chưa muộn.
            if (resolving)
                return;

            if (tutorialStep >= tutorialSteps.Count)
            {
                TutorialStepsFinished();
                return;
            }

            var step = tutorialSteps[tutorialStep];
            if (step.Ready != null && !step.Ready())
                return;   // chưa tới lúc: cứ để người chơi chơi bình thường

            EnterTutorialStep(step);
        }

        void EnterTutorialStep(TutorialStep step)
        {
            tutorialStepShowing = true;
            tutorialContinuePressed = false;
            tutorialWaitFor = step.Wait;
            tutorialFreeControl = step.FreeControl;
            // Bước cho chơi tự do thì KHÔNG đóng băng: khối phải tiếp tục rơi thì mới xếp được.
            tutorialHold = !step.FreeControl;

            // Đặt ô sáng TRƯỚC rồi mới hiện thẻ: thẻ cần biết ô sáng nằm đâu để né sang phía kia.
            if (step.Spotlight != null)
            {
                tutorialView.SetSpotlight(step.Spotlight());
            }
            else if (step.FreeControl)
            {
                // Đang chơi thật: không làm tối màn, chỉ để lại thẻ nhắc việc.
                tutorialView.SetDimVisible(false);
                tutorialView.ClearSpotlight();
            }
            else
            {
                tutorialView.SetDimVisible(true);
                tutorialView.ClearSpotlight();
            }

            if (step.CardAnchor != null)
                tutorialView.SetCardAnchor(step.CardAnchor());
            else
                tutorialView.ClearCardAnchor();

            tutorialView.ShowCard(step.Title, step.Body, step.Wait == TutorialAction.Continue, step.Placement);

            if (step.TapHand != null)
            {
                tutorialView.ShowTapHand(step.TapHand(), step.TapAim);
            }
            else if (step.SwipeHand != null)
            {
                var points = step.SwipeHand();
                tutorialView.ShowSwipeHand(points[0], points[1]);
            }
            else
            {
                tutorialView.HideHand();
            }
        }

        void CompleteTutorialStep()
        {
            tutorialStepShowing = false;
            tutorialWaitFor = TutorialAction.None;
            tutorialHold = false;
            tutorialFreeControl = false;
            tutorialStep++;

            tutorialView.HideAll();
            Haptics.Selection();
            feedbacks.Play(GameFeedbackId.TutorialStep);
        }

        // Các bước nói về bàn cờ chỉ vào được khi bàn đã dựng xong và còn đang chạy.
        bool TutorialBoardReady()
        {
            return tacticalBoard != null && tacticalBoard.Status == TacticalBoardStatus.Running;
        }

        // Hết danh sách bước: trả quyền điều khiển cho người chơi, nhưng CHƯA tắt hẳn tutorial nếu
        // còn loại tài nguyên chưa được dạy — mấy bài đó chỉ dạy được khi cụm tương ứng nổ.
        void TutorialStepsFinished()
        {
            tutorialStepsDone = true;
            tutorialCompletionPending = !tutorialCompletionShown;
            tutorialHold = false;
            tutorialWaitFor = TutorialAction.None;
            tutorialView.HideAll();
            TutorialFinishIfDone();
        }

        void TutorialFinishIfDone()
        {
            if (tutorialStepsDone && !tutorialCompletionPending && TutorialAllResourcesExplained())
                TutorialFinish();
        }

        bool TutorialAllResourcesExplained()
        {
            foreach (var resource in TutorialLessonResources)
            {
                if (!tutorialExplainedResources.Contains(resource))
                    return false;
            }
            return true;
        }

        // Thắng bàn cờ sau khi đã học xong: khen một câu rồi mới mở popup thắng.
        bool TutorialWantsCompletionCard()
        {
            return tutorialActive && tutorialCompletionPending && !tutorialCompletionShown;
        }

        IEnumerator TutorialShowCompletion(Action afterwards)
        {
            tutorialCompletionShown = true;
            tutorialCompletionPending = false;

            BeginTutorialInterlude(TacticalBoardScreenRect(), 14f, "Hoàn thành hướng dẫn",
                "Bạn đã nắm được cách chơi. Giờ vào màn chơi thật nhé.", false);
            yield return TutorialWaitUnscaled(2.2f);
            EndTutorialInterlude();

            TutorialFinish();
            afterwards();
        }

        void TutorialFinish()
        {
            tutorialActive = false;
            tutorialCompletionPending = false;
            tutorialFreeControl = false;
            tutorialHold = false;
            tutorialWaitFor = TutorialAction.None;
            if (tutorialView != null)
                tutorialView.HideAll();
        }

        // === Cổng thao tác: gameplay hỏi trước khi cho làm, và báo lại khi đã làm ===

        // true = thao tác này KHÔNG thuộc bước đang hướng dẫn -> bỏ qua.
        bool TutorialBlocks(TutorialAction action)
        {
            if (!tutorialActive || !tutorialStepShowing || tutorialFreeControl)
                return false;
            return tutorialWaitFor != action;
        }

        void TutorialNotify(TutorialAction action)
        {
            if (!tutorialActive || !tutorialStepShowing || tutorialWaitFor != action)
                return;
            CompleteTutorialStep();
        }

        // Bước hướng dẫn CHỈ chờ bấm TIẾP -> khoá hẳn chạm vào vùng chơi cho khỏi bấm lung tung.
        // Ngược lại, bước đang chờ thao tác thật (bấm XOAY, đi quân trên bàn cờ) thì PHẢI để
        // raycast đi qua, nếu không người chơi bấm nút mà không ăn.
        bool TutorialBlocksGameplayRaycasts()
        {
            if (!tutorialStepShowing || tutorialFreeControl)
                return false;
            return tutorialWaitFor == TutorialAction.Continue || tutorialWaitFor == TutorialAction.None;
        }

        // Bàn cờ: chỉ nhận chạm khi bước hiện tại đang CHỜ người chơi đi quân.
        bool TutorialBlocksTactical()
        {
            return tutorialStepShowing && !tutorialFreeControl && tutorialWaitFor != TutorialAction.TacticalMove;
        }

        // Bước đang chờ thao tác trên bàn xếp gạch -> Update vẫn phải đọc input dù đang đóng băng.
        bool TutorialAwaitsPuzzleInput()
        {
            if (!tutorialStepShowing)
                return false;
            return tutorialWaitFor == TutorialAction.MoveSideways
                || tutorialWaitFor == TutorialAction.TapRotate
                || tutorialWaitFor == TutorialAction.SwipeDrop;
        }

        // === Các bước xen giữa gameplay (gọi từ luồng xử lý cụm) ===

        // Dừng NGAY TRƯỚC khi cụm nổ: làm tối chỗ khác, chừa sáng đúng cụm sắp kích hoạt.
        IEnumerator TutorialSpotlightClusters(List<Puzzle.Cluster> clusters)
        {
            if (!tutorialActive || clusters == null || clusters.Count == 0)
                yield break;

            var untaught = FirstUntaughtResource(clusters);
            if (!untaught.HasValue)
                yield break;

            var resource = untaught.Value;
            tutorialPendingLesson = resource;

            // Chỉ chừa sáng ĐÚNG các ô của cụm loại này; mọi thứ khác, kể cả ô loại khác nằm
            // lọt giữa cụm, đều nằm dưới lớp tối.
            CollectClusterCellRects(clusters, resource);
            BeginTutorialInterludeCells(tutorialCellHighlights, TutorialResourceTitle(resource),
                TutorialClusterBodyText(resource), ClusterCellsBounds());

            while (!tutorialContinuePressed)
                yield return null;

            EndTutorialInterlude();
        }

        // Loại đầu tiên trong đợt cụm này mà người chơi chưa được dạy; null = đã dạy hết.
        Puzzle.ResourceType? FirstUntaughtResource(List<Puzzle.Cluster> clusters)
        {
            foreach (var cluster in clusters)
            {
                if (Array.IndexOf(TutorialLessonResources, cluster.Resource) < 0)
                    continue;
                if (tutorialExplainedResources.Contains(cluster.Resource))
                    continue;
                return cluster.Resource;
            }
            return null;
        }

        // Chạy TRƯỚC khi hiệu ứng cụm được áp dụng: giải thích loại vừa được chừa sáng sẽ làm gì,
        // rồi mới thả cho hiệu ứng chạy (TutorialWatchResourceEffects) — nghe xong là thấy ngay.
        IEnumerator TutorialExplainResources()
        {
            tutorialResourcesToWatch.Clear();

            var lesson = tutorialPendingLesson;
            tutorialPendingLesson = null;   // khép lại lần nổ này dù có dạy được hay không

            if (!tutorialActive || !lesson.HasValue || tacticalBoard == null)
                yield break;

            tutorialExplainedResources.Add(lesson.Value);
            tutorialResourcesToWatch.Add(lesson.Value);
            yield return TutorialResourceCard(lesson.Value);
        }

        // Chạy NGAY SAU khi hiệu ứng đã áp dụng: giữ đèn chiếu vào đúng chỗ vừa biến đổi (quái bị
        // đẩy lùi, khiên vừa khoác lên) vài nhịp để người chơi kịp nhìn chức năng chạy thật.
        IEnumerator TutorialWatchResourceEffects()
        {
            if (tutorialActive && tacticalBoard != null)
            {
                foreach (var resource in tutorialResourcesToWatch)
                    yield return TutorialWatchResource(resource);
            }

            tutorialResourcesToWatch.Clear();
            TutorialFinishIfDone();
        }

        IEnumerator TutorialWatchResource(Puzzle.ResourceType resource)
        {
            var target = resource == Puzzle.ResourceType.Attack
                ? tacticalBoard.MonsterPosition
                : tacticalBoard.PlayerPosition;

            BeginTutorialInterlude(TacticalCellScreenRect(target), 18f, "Nhìn nhé",
                TutorialResourceEffectText(resource), false);

            yield return TutorialWaitUnscaled(1.6f);

            EndTutorialInterlude();
        }

        IEnumerator TutorialResourceCard(Puzzle.ResourceType resource)
        {
            Rect spot;

            switch (resource)
            {
                case Puzzle.ResourceType.Move:
                case Puzzle.ResourceType.Shield:
                    spot = TacticalCellScreenRect(tacticalBoard.PlayerPosition);
                    break;
                case Puzzle.ResourceType.Attack:
                    spot = TacticalCellScreenRect(tacticalBoard.MonsterPosition);
                    break;
                default:
                    yield break;
            }

            BeginTutorialInterlude(spot, 18f, TutorialResourceTitle(resource), TutorialResourceDescription(resource));

            while (!tutorialContinuePressed)
                yield return null;

            EndTutorialInterlude();
        }

        // Lời giải thích cụm đang sáng sắp cho gì, viết theo đúng loại tài nguyên của cụm đó.
        static string TutorialClusterBodyText(Puzzle.ResourceType resource)
        {
            switch (resource)
            {
                case Puzzle.ResourceType.Move:
                    return "Khi ghép thành công các khối tài nguyên Giày, người chơi sẽ nhận được 1 lượt di chuyển cho quân xanh.";
                case Puzzle.ResourceType.Attack:
                    return "Khi ghép thành công các khối tài nguyên Kiếm, người chơi sẽ nhận được 1 lượt tấn công vào quái vật tím.";
                case Puzzle.ResourceType.Shield:
                    return "Khi ghép thành công các khối tài nguyên Khiên, người chơi sẽ nhận được 1 lớp lá chắn cho quân xanh.";
                default:
                    return "Ba ô cùng loại dính nhau là một cụm. Cụm sáng sắp biến mất và cho bạn chức năng của nó.";
            }
        }

        // Mô tả chức năng của từng loại cụm. Dùng chung cho bước diễn trong danh sách và cho thẻ
        // bật ra khi người chơi tự ăn được cụm loại đó, để hai chỗ không lệch lời nhau.
        static string TutorialResourceDescription(Puzzle.ResourceType resource)
        {
            switch (resource)
            {
                case Puzzle.ResourceType.Move:
                    return "Cho phép quân xanh di chuyển trên bàn cờ. Combo càng cao, số bước di chuyển nhận được càng nhiều.";
                case Puzzle.ResourceType.Attack:
                    return "Đẩy lùi quái vật tím về phía sau. Combo càng cao, quái vật bị đẩy lùi càng xa.";
                case Puzzle.ResourceType.Shield:
                    return "Tạo lá chắn bảo vệ quân xanh. Combo càng cao, số lớp lá chắn nhận được càng nhiều.";
                default:
                    return "";
            }
        }

        // Tên chức năng của cụm, dùng làm tiêu đề thẻ ngay lúc cụm sắp nổ.
        static string TutorialResourceTitle(Puzzle.ResourceType resource)
        {
            switch (resource)
            {
                case Puzzle.ResourceType.Move: return "Di chuyển";
                case Puzzle.ResourceType.Attack: return "Tấn công";
                case Puzzle.ResourceType.Shield: return "Phòng thủ";
                default: return "Đủ cụm rồi";
            }
        }

        static string TutorialResourceEffectText(Puzzle.ResourceType resource)
        {
            switch (resource)
            {
                case Puzzle.ResourceType.Move: return "Lượt đi vừa tăng lên.";
                case Puzzle.ResourceType.Attack: return "Quái vật tím vừa bị đẩy lùi.";
                case Puzzle.ResourceType.Shield: return "Khiên vừa hiện quanh quân của bạn.";
                default: return "";
            }
        }

        static IEnumerator TutorialWaitUnscaled(float seconds)
        {
            float elapsed = 0f;
            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        void BeginTutorialInterlude(Rect spotlight, float padding, string title, string body,
            bool waitForContinue = true, Rect? cardAnchor = null)
        {
            tutorialView.SetSpotlight(spotlight, padding);
            BeginTutorialInterludeCore(title, body, waitForContinue, cardAnchor);
        }

        // Bản chừa sáng từng ô rời rạc, dùng khi nói về một cụm: khung bao chung sẽ ôm cả những ô
        // loại khác nằm lọt giữa cụm, nhìn như thể chúng cũng được nhắc tới.
        void BeginTutorialInterludeCells(IList<Rect> cells, string title, string body, Rect? cardAnchor)
        {
            tutorialView.SetSpotlightCells(cells);
            BeginTutorialInterludeCore(title, body, true, cardAnchor);
        }

        void BeginTutorialInterludeCore(string title, string body, bool waitForContinue, Rect? cardAnchor)
        {
            tutorialHold = true;
            tutorialStepShowing = true;
            tutorialInterlude = true;
            // Thẻ chỉ để xem (không có nút TIẾP) thì không khoá thao tác dưới danh nghĩa chờ bấm.
            tutorialWaitFor = waitForContinue ? TutorialAction.Continue : TutorialAction.None;
            tutorialContinuePressed = false;

            if (cardAnchor.HasValue)
                tutorialView.SetCardAnchor(cardAnchor.Value);
            else
                tutorialView.ClearCardAnchor();
            tutorialView.ShowCard(title, body, waitForContinue, TutorialCardPlacement.NextToTarget);
            tutorialView.HideHand();
        }

        void EndTutorialInterlude()
        {
            tutorialView.HideAll();
            tutorialStepShowing = false;
            tutorialInterlude = false;
            tutorialWaitFor = TutorialAction.None;
            tutorialHold = false;
            Haptics.Selection();
        }

        // === Danh sách bước ===

        void BuildTutorialSteps()
        {
            tutorialSteps = new List<TutorialStep>
            {
                // --- Mục tiêu tổng quát ---
                new TutorialStep
                {
                    Title = "Chào mừng",
                    Placement = TutorialCardPlacement.Center,
                    Body = "Xếp các khối tài nguyên thành cụm để kích hoạt chức năng. Sử dụng các chức năng nhận được để điều khiển quân trên bàn cờ.",
                    Wait = TutorialAction.Continue
                },

                // --- Ba nhân vật trên bàn cờ: chỉ nhận mặt, cơ chế đi để dành cho bước sau ---
                new TutorialStep
                {
                    Title = "Quân xanh",
                    Body = "Đây là quân cờ của bạn. Hãy di chuyển và tận dụng các chức năng để sống sót trên bàn cờ.",
                    Wait = TutorialAction.Continue,
                    Ready = TutorialBoardReady,
                    Spotlight = TacticalPlayerCellScreenRect
                },
                new TutorialStep
                {
                    Title = "Quân đỏ",
                    Body = "Là quân cờ của kẻ địch. Đây chính là đối thủ bạn cần vượt qua.",
                    Wait = TutorialAction.Continue,
                    Ready = TutorialBoardReady,
                    Spotlight = EnemyCellScreenRect
                },
                new TutorialStep
                {
                    Title = "Quái vật tím - Kẻ truy đuổi",
                    Body = "Quái vật tím truy đuổi cả bạn và quân đỏ.",
                    Wait = TutorialAction.Continue,
                    Ready = TutorialBoardReady,
                    Spotlight = MonsterCellScreenRect
                },

                // --- Mục tiêu trận đấu + điều kiện thắng thua ---
                new TutorialStep
                {
                    Title = "Mục tiêu trận đấu",
                    Body = "Sống sót và khiến quân đỏ bị quái vật hạ trước. Bạn thua nếu bị quái vật bắt khi hết khiên, khi hết giờ, hoặc khi bàn xếp gạch bị đầy.",
                    Wait = TutorialAction.Continue,
                    Ready = TutorialBoardReady,
                    Spotlight = TacticalBoardScreenRect
                },

                // --- Điều khiển khối đang rơi ---
                new TutorialStep
                {
                    Title = "Kéo ngang",
                    Body = "Kéo ngang để đưa khối sang cột khác.",
                    Wait = TutorialAction.MoveSideways,
                    Spotlight = PuzzleBoardScreenRect,
                    SwipeHand = ActivePieceSwipeSideways
                },
                new TutorialStep
                {
                    Title = "Nút xoay",
                    Body = "Nhấn nút xoay để xoay khối.",
                    Wait = TutorialAction.RotateButton,
                    Spotlight = RotateButtonScreenRect,
                    TapHand = RotateButtonScreenCenter
                },
                new TutorialStep
                {
                    Title = "Chạm để xoay",
                    Body = "Chạm vào bất kỳ vị trí nào trên màn hình cũng xoay được khối.",
                    Wait = TutorialAction.TapRotate,
                    TapHand = TapAnywhereScreenPos,
                    TapAim = Vector2.up,
                    CardAnchor = TapAnywhereHandRect
                },
                new TutorialStep
                {
                    Title = "Vuốt xuống",
                    Body = "Vuốt xuống để thả khối rơi ngay.",
                    Wait = TutorialAction.SwipeDrop,
                    Spotlight = PuzzleBoardScreenRect,
                    SwipeHand = ActivePieceSwipeDown
                },

                // --- Ghép cụm: trả quyền điều khiển, chỉ để lại thẻ nhắc việc ---
                new TutorialStep
                {
                    Title = "Ghép tài nguyên",
                    Body = "Xếp 4 ô cùng loại dính liền nhau để tạo thành một cụm.",
                    Wait = TutorialAction.FormCluster,
                    FreeControl = true
                },
                new TutorialStep
                {
                    Title = "Combo",
                    Body = "Ghép càng nhiều ô cùng lúc thì combo càng cao, hiệu quả nhận được càng lớn.",
                    Wait = TutorialAction.Continue,
                    Ready = () => tutorialAnyClear
                },

                // --- Dùng thử từng chức năng, bắt đầu bằng Giày cho dễ hiểu ---
                new TutorialStep
                {
                    Title = "Đi quân trên bàn cờ",
                    Body = "Bạn vừa nhận lượt di chuyển. Chạm quân xanh, rồi chạm ô sáng bên cạnh để đi một ô.",
                    Wait = TutorialAction.TacticalMove,
                    Ready = () => TutorialBoardReady() && tacticalBoard.MoveBank >= 1,
                    Spotlight = TacticalBoardScreenRect,
                    TapHand = TacticalPlayerScreenPos
                },
                new TutorialStep
                {
                    Title = "Cơ chế bàn cờ",
                    Body = "Mỗi khi bạn đi 1 bước, quân đỏ và quái vật tím cũng đi 1 bước. Ngoài ra quái vật còn tự di chuyển sau một khoảng thời gian nhất định.",
                    Wait = TutorialAction.Continue,
                    Ready = () => tutorialPlayerMoved,
                    Spotlight = TacticalBoardScreenRect
                },

                // --- Thả cho tự xử lý; thẻ "Hoàn thành hướng dẫn" bật lên khi thắng ---
                new TutorialStep
                {
                    Title = "Tới lượt bạn",
                    Body = "Hãy sử dụng các tài nguyên để tránh quái vật và đánh bại đối thủ!",
                    Wait = TutorialAction.Continue,
                    Ready = TutorialBoardReady,
                    Spotlight = TacticalBoardScreenRect
                }
            };
        }

        // === Toạ độ màn hình cho highlight / bàn tay ===

        static Rect FullScreenRect()
        {
            return new Rect(Screen.width * 0.25f, Screen.height * 0.35f, Screen.width * 0.5f, Screen.height * 0.3f);
        }

        static Rect ScreenRectOf(RectTransform rect)
        {
            if (rect == null)
                return FullScreenRect();

            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var canvas = rect.GetComponentInParent<Canvas>();
            Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            Vector2 min = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
            Vector2 max = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
            return new Rect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y),
                Mathf.Abs(max.x - min.x), Mathf.Abs(max.y - min.y));
        }

        Rect PuzzleBoardScreenRect()
        {
            return ScreenRectOf(scenePuzzleGridRect);
        }

        Rect TacticalBoardScreenRect()
        {
            return ScreenRectOf(sceneTacticalBoardRect);
        }

        Rect RotateButtonScreenRect()
        {
            return ScreenRectOf(rotateButtonRect);
        }

        Vector2 RotateButtonScreenCenter()
        {
            return RotateButtonScreenRect().center;
        }

        Rect EnemyCellScreenRect()
        {
            return tacticalBoard != null ? TacticalCellScreenRect(tacticalBoard.EnemyPosition) : FullScreenRect();
        }

        Rect TacticalPlayerCellScreenRect()
        {
            return tacticalBoard != null ? TacticalCellScreenRect(tacticalBoard.PlayerPosition) : FullScreenRect();
        }

        Rect MonsterCellScreenRect()
        {
            return tacticalBoard != null ? TacticalCellScreenRect(tacticalBoard.MonsterPosition) : FullScreenRect();
        }

        Vector2 ActivePieceScreenPos()
        {
            // Tâm khối đang rơi; chưa có khối thì lấy giữa bàn.
            int sumX = 0, sumY = 0, count = 0;
            foreach (var cell in Cells(origin, rotation))
            {
                sumX += cell.x;
                sumY += cell.y;
                count++;
            }
            if (count == 0)
                return PuzzleBoardScreenRect().center;
            return PuzzleCellScreenPos(sumX / count, sumY / count);
        }

        // Bước "chạm để xoay": đặt tay ở một chỗ trống bên rìa màn hình, xa hẳn khối đang rơi và
        // xa cả bàn cờ, để người chơi thấy rõ là chạm đâu cũng được chứ không phải chạm trúng khối.
        Vector2 TapAnywhereScreenPos()
        {
            return new Vector2(Screen.width * 0.78f, Screen.height * 0.42f);
        }

        // Vùng bàn tay chiếm chỗ: ngón chỉ LÊN nên đầu ngón ở mép trên, thân tay đổ xuống dưới.
        // Thẻ hướng dẫn nép vào vùng này để chữ nằm ngay cạnh ngón tay đang chỉ.
        Rect TapAnywhereHandRect()
        {
            Vector2 tip = TapAnywhereScreenPos();
            float size = Mathf.Clamp(Screen.height * 0.16f, 90f, 220f);   // khớp TutorialHandCue
            return new Rect(tip.x - size * 0.5f, tip.y - size, size, size);
        }

        Vector2[] ActivePieceSwipeDown()
        {
            Vector2 from = ActivePieceScreenPos();
            Rect board = PuzzleBoardScreenRect();
            float to = Mathf.Max(board.yMin + board.height * 0.12f, from.y - board.height * 0.55f);
            return new[] { from, new Vector2(from.x, to) };
        }

        Vector2[] ActivePieceSwipeSideways()
        {
            Vector2 from = ActivePieceScreenPos();
            Rect board = PuzzleBoardScreenRect();
            float to = Mathf.Min(board.xMax - board.width * 0.1f, from.x + board.width * 0.28f);
            return new[] { from, new Vector2(to, from.y) };
        }

        Vector2 TacticalPlayerScreenPos()
        {
            return tacticalBoard != null
                ? (Vector2)TacticalCellScreenPos(tacticalBoard.PlayerPosition)
                : TacticalBoardScreenRect().center;
        }

        // Vùng một ô bàn cờ trên màn hình (để chừa sáng đúng quân đang nói tới).
        Rect TacticalCellScreenRect(Vector2Int cell)
        {
            if (tacticalBoard == null)
                return FullScreenRect();

            int w = tacticalBoard.Data.BoardWidth;
            int index = cell.y * w + cell.x;
            if (index >= 0 && index < tacticalCellButtons.Count && tacticalCellButtons[index] != null)
                return ScreenRectOf((RectTransform)tacticalCellButtons[index].transform);

            Vector2 center = TacticalCellScreenPos(cell);
            float size = TacticalBoardScreenRect().width / Mathf.Max(1, w);
            return new Rect(center.x - size * 0.5f, center.y - size * 0.5f, size, size);
        }

        // Thu vùng màn hình của từng ô thuộc các cụm ĐÚNG loại đang được giới thiệu.
        void CollectClusterCellRects(List<Puzzle.Cluster> clusters, Puzzle.ResourceType resource)
        {
            tutorialCellHighlights.Clear();
            Rect board = PuzzleBoardScreenRect();
            float cellW = board.width / Mathf.Max(1, Width);
            float cellH = board.height / Mathf.Max(1, Height);

            foreach (var cluster in clusters)
            {
                if (cluster.Resource != resource)
                    continue;
                foreach (var cell in cluster.Cells)
                {
                    Vector3 p = PuzzleCellScreenPos(cell.x, cell.y);
                    tutorialCellHighlights.Add(new Rect(p.x - cellW * 0.5f, p.y - cellH * 0.5f, cellW, cellH));
                }
            }
        }

        // Khung bao quanh các ô vừa thu — chỉ dùng để đặt thẻ chữ cho gần, không dùng để chiếu sáng.
        Rect ClusterCellsBounds()
        {
            if (tutorialCellHighlights.Count == 0)
                return PuzzleBoardScreenRect();

            Rect bounds = tutorialCellHighlights[0];
            for (int i = 1; i < tutorialCellHighlights.Count; i++)
            {
                Rect r = tutorialCellHighlights[i];
                float minX = Mathf.Min(bounds.xMin, r.xMin);
                float minY = Mathf.Min(bounds.yMin, r.yMin);
                float maxX = Mathf.Max(bounds.xMax, r.xMax);
                float maxY = Mathf.Max(bounds.yMax, r.yMax);
                bounds = new Rect(minX, minY, maxX - minX, maxY - minY);
            }
            return bounds;
        }
    }
}
