namespace BrickStacker
{
    // Thao tác mà một bước hướng dẫn đang chờ người chơi thực hiện. Trong lúc chờ, MỌI thao tác
    // khác bị khoá để người chơi chỉ có đúng một việc phải làm.
    public enum TutorialAction
    {
        None,            // bước tự chạy, không chờ thao tác
        Continue,        // chỉ cần bấm nút TIẾP trên thẻ hướng dẫn
        RotateButton,    // bấm nút XOAY
        TapRotate,       // chạm lên bàn xếp gạch để xoay
        SwipeDrop,       // vuốt xuống để thả nhanh
        MoveSideways,    // kéo ngang để đưa khối sang trái/phải
        TacticalMove,    // đi một ô trên bàn cờ
        FormCluster      // ghép được một cụm tài nguyên (bước này KHÔNG khoá gameplay)
    }
}
