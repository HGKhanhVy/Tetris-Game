namespace BrickStacker
{
    // GD v3 §15: ba Skill tiêu Energy. Id dùng luôn cho gói mạng.
    public enum OnlineSkill : byte
    {
        GarbageDrop = 1,   // §15.1 cost 4, +1 hàng rác
        LifeDrain = 2,     // §15.2 cost 4, hút 16 máu
        OverloadBlast = 3, // §15.3 cost 7, 25 dmg / phá khiên +8
        Attack = 4,        // §12 đòn Kiếm tự động (id mạng — không phải skill người chơi bấm)
        DrainHeal = 5      // §15.2 gói mạng: bên bị Hút máu báo lượng máu thực mất để bên gây hồi đúng
    }
}
